﻿#nullable enable
using System;
using System.Text;
using AutoPatterns.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Reflection;

namespace AutoPatterns
{
    public abstract class AutoAttributeGenerator : ISourceGenerator
    {
        //TODO add support for text templating engine i.e. Scriban 
        protected const string INDENT_1 = "    ";
        protected const string INDENT_2 = "        ";
        protected const string INDENT_3 = "            ";

        public abstract string AutoPatternName { get; }
        public abstract string AutoAttributeName { get; }

        public abstract void Initialize(GeneratorInitializationContext context);
        public abstract void Execute(GeneratorExecutionContext context);

        internal static readonly DiagnosticDescriptor NoAutoAttributeRule = GetDiagnosticDescriptor(0, "Auto", "Auto attribute '{3}' associated with {2} is not defined");
        internal static readonly DiagnosticDescriptor NoSyntaxReceiver = GetDiagnosticDescriptor(256, "Auto", "Internal error - no appropriate syntax receiver");
        internal static readonly DiagnosticDescriptor CSharpNotSupported = GetDiagnosticDescriptor(257, "Auto", "C# compilation units are NOT supported");
        //internal static readonly DiagnosticDescriptor VisualBasicNotSupported = GetDiagnosticDescriptor(258, "Auto", "VB.NET compilation units are NOT supported");

        protected static DiagnosticDescriptor GetDiagnosticDescriptor(ushort id, string patternName, string message, DiagnosticSeverity diagnosticSeverity = DiagnosticSeverity.Error)
            => new($"Auto{patternName}{id:000}", $"Couldn't generate automatic '{patternName}' pattern", "{0}: " + message, "AutoGenerator", diagnosticSeverity, true);

        protected void ReportDiagnostics(GeneratorExecutionContext context, DiagnosticDescriptor rule, ISymbol? symbol) =>
            context.ReportDiagnostic(Diagnostic.Create(rule, symbol?.Locations[0] ?? Location.None,
                symbol?.Name, symbol?.ContainingNamespace?.ToString(), GetType().FullName, AutoAttributeName
            ));
    }
    
    public abstract class AutoAttributeGenerator<TRenderState, TSettings> : AutoAttributeGenerator
        where TSettings : CommonAutoSettings
    {
        public sealed override string AutoPatternName { get; }
        public sealed override string AutoAttributeName { get; }
        protected string AutoAttributeSource { get; }

        internal readonly DiagnosticDescriptor NonPartialTypeRule;
        internal readonly DiagnosticDescriptor NamespaceAndTypeNamesEqualRule;
        internal readonly DiagnosticDescriptor? InvalidSettingsAttributeRule;

        protected AutoAttributeGenerator(string autoPatternName, string autoAttributeName, string autoAttributeSource, string? invalidSettingsAttributeRuleMessage)
        {
            AutoPatternName = autoPatternName;
            AutoAttributeName = autoAttributeName;
            AutoAttributeSource = autoAttributeSource;

            NonPartialTypeRule = GetDiagnosticDescriptor(1, AutoPatternName, "Type decorated with {3} must be also declared partial");
            NamespaceAndTypeNamesEqualRule = GetDiagnosticDescriptor(2, AutoPatternName, "Type name '{0}' cannot be equal to containing namespace: '{1}'");
            InvalidSettingsAttributeRule = invalidSettingsAttributeRuleMessage == null ? null
                : GetDiagnosticDescriptor(3, AutoPatternName, invalidSettingsAttributeRuleMessage);
        }

        public sealed override void Initialize(GeneratorInitializationContext context) => context.RegisterForSyntaxNotifications(() => new AutoAttributeSyntaxReceiver());

        public sealed override void Execute(GeneratorExecutionContext context)
        {
            DebuggerChecker.CheckDebugger(context, GetType().Name);

            context.AddSource(AutoAttributeName, SourceText.From(AutoAttributeSource, Encoding.UTF8));

            if (context.SyntaxReceiver is not AutoAttributeSyntaxReceiver receiver) { ReportDiagnostics(context, NoSyntaxReceiver, null); return; }

            if (context.Compilation is not CSharpCompilation cSharpCompilation) { ReportDiagnostics(context, CSharpNotSupported, null); return; }


            var options = cSharpCompilation.SyntaxTrees[0].Options as CSharpParseOptions;
            var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(AutoAttributeSource, Encoding.UTF8), options));

            var autoAttributeSymbol = compilation.GetTypeByMetadataName($"Auto.{AutoAttributeName}");
            if (autoAttributeSymbol is null) { ReportDiagnostics(context, NoAutoAttributeRule, null); return; }


            foreach (var type in receiver.CandidateTypes)
            {
                var model = compilation.GetSemanticModel(type.SyntaxTree);

                if (model.GetDeclaredSymbol(type) is { } typeSymbol &&
                    ShouldProcessType(typeSymbol, autoAttributeSymbol, context, out var settings))
                {
                    if (!type.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                    {
                        ReportDiagnostics(context, NonPartialTypeRule, typeSymbol);
                        continue;
                    }

                    if (!typeSymbol.ContainingSymbol.Equals(typeSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
                    {
                        ReportDiagnostics(context, NamespaceAndTypeNamesEqualRule, typeSymbol);
                        continue;
                    }

                    var namespaces = type.SyntaxTree.GetRoot() is CompilationUnitSyntax compilationUnit
                        ? Using.FromCompilationUnit(compilationUnit)
                        : new HashSet<Using>();
                    namespaces.Add("System");


                    if (ShouldRender(typeSymbol, autoAttributeSymbol, context, namespaces, settings, out var state))
                    {
                        var meta = TypeMeta.FromDeclaration(type, typeSymbol);

                        var source = new StringBuilder(512);
                        source.AppendLine(GeneratorUtils.GENERATED_FILE_HEADER);

                        var sortedNamespaces = Using.Sort(namespaces);

                        foreach (var ns in sortedNamespaces)
                            if (!string.Equals(meta.Namespace, ns.NamespaceOrType, StringComparison.Ordinal))
                                source.AppendLine(ns.ToCSharpCode());

                        Render(source, meta, settings, state);
                        context.AddSource($"{typeSymbol.Name}_{AutoPatternName}AutoPattern.cs", SourceText.From(source.ToString(), Encoding.UTF8));
                    }
                }
            }
        }

        protected virtual bool ShouldProcessType(ISymbol typeSymbol, ISymbol autoAttributeSymbol, in GeneratorExecutionContext context,
            [NotNullWhen(true)] out TSettings? settings)
        {
            if (GeneratorUtils.GetAttribute(typeSymbol, autoAttributeSymbol) is { } autoAttributeData)
            {
                if (CommonAutoSettings.TryLoad(autoAttributeData, context, out settings))
                    return true;
                else if (InvalidSettingsAttributeRule is not null)
                    ReportDiagnostics(context, InvalidSettingsAttributeRule, typeSymbol);
            }

            settings = default;
            return false;
        }
        
        protected abstract bool ShouldRender(INamedTypeSymbol typeSymbol, INamedTypeSymbol autoAttributeSymbol,
            in GeneratorExecutionContext context, ISet<Using> namespaces, TSettings settings,
            [NotNullWhen(true)] out TRenderState? state);

        protected abstract void Render(StringBuilder source, TypeMeta typeMeta, TSettings settings, TRenderState state);

        protected void RenderGeneratedAttributes(StringBuilder source) => source.AppendLine($@"
{INDENT_1}[System.CodeDom.Compiler.GeneratedCode(""{GetType().Name}"", ""{Assembly.GetExecutingAssembly().GetName().Version}"")]
{INDENT_1}[System.Runtime.CompilerServices.CompilerGenerated]");

        protected void RenderDebuggerHook(StringBuilder source, CommonAutoSettings settings)
        {
            if (settings.GenerateDebuggerHook)
                source.AppendLine(@$"
#if DEBUG
{INDENT_2}internal void DebuggerHook() {{ System.Diagnostics.Debugger.Launch(); }}
#endif");
        }

        protected sealed class AutoAttributeSyntaxReceiver : ISyntaxReceiver
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly List<TypeDeclarationSyntax> _candidateTypes = new();
            public IEnumerable<TypeDeclarationSyntax> CandidateTypes => _candidateTypes;

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is not TypeDeclarationSyntax tds || tds.AttributeLists.Count == 0) return;

                switch (tds)
                {
                    case StructDeclarationSyntax:
                    case ClassDeclarationSyntax:
                        _candidateTypes.Add(tds);
                        break;
                }
            }
        }
    }
}

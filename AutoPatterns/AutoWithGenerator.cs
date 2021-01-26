using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using AutoPatterns.Utils;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using static AutoPatterns.Utils.GeneratorUtils;

#nullable enable
namespace AutoPatterns
{
    [Generator]
    public sealed partial class AutoWithGenerator : AutoAttributeGenerator<AutoWithSyntaxReceiver>
    {
        private const string PATTERN = "With";
        internal readonly DiagnosticDescriptor NonPartialTypeRule = GetDiagnosticDescriptor(1, PATTERN, "Type decorated with {3} must be also declared partial");
        internal readonly DiagnosticDescriptor NamespaceAndTypeNamesEqualRule = GetDiagnosticDescriptor(2, PATTERN, "Type name '{0}' cannot be equal to containing namespace: '{1}'");
        internal readonly DiagnosticDescriptor InvalidSettingsAttributeRule = GetDiagnosticDescriptor(3, PATTERN, "Attribute {3} must be constructed with 1 boolean value, or with default values");
        internal readonly DiagnosticDescriptor BaseTypeNotDecorated = GetDiagnosticDescriptor(4, PATTERN, "Base '{0}' type must also be decorated with {3} attribute");

        internal readonly DiagnosticDescriptor NoContractMembersRule = GetDiagnosticDescriptor(50, PATTERN, "No properties for With pattern defined at '{0}'", DiagnosticSeverity.Warning);

        public AutoWithGenerator() : base(PATTERN, "AutoWithAttribute", @"using System;
namespace Auto
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    sealed class AutoWithAttribute : Attribute 
    {
        public bool SupportValidation { get; }

        public AutoWithAttribute(bool supportValidation = true) => SupportValidation = supportValidation;
    }
}") { }

        protected override void ProcessNodes(GeneratorExecutionContext context, AutoWithSyntaxReceiver receiver, Compilation compilation, INamedTypeSymbol autoAttributeSymbol)
        {
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


                    if (TryGetProperties(typeSymbol, autoAttributeSymbol, context, namespaces, out var properties))
                    {
                        var meta = TypeMeta.FromDeclaration(type, typeSymbol);

                        string classSource = RenderRecord(meta, properties, namespaces, settings);
                        context.AddSource($"{typeSymbol.Name}_{PATTERN}.cs", SourceText.From(classSource, Encoding.UTF8));
                    }
                }
            }
        }

        private bool ShouldProcessType(ISymbol typeSymbol, ISymbol autoAttributeSymbol, GeneratorExecutionContext context, out AutoWithSettings settings)
        {
            if (GetAttribute(typeSymbol, autoAttributeSymbol) is { } autoAttributeData)
            {
                if (AutoWithSettings.TryFromCurrent(autoAttributeData, context, out settings))
                    return true;
                else
                    ReportDiagnostics(context, InvalidSettingsAttributeRule, typeSymbol);
            }

            settings = default;
            return false;
        }

        private bool TryGetProperties(ITypeSymbol typeSymbol, ISymbol autoAttributeSymbol, in GeneratorExecutionContext context, ICollection<Using> namespaces, out IReadOnlyList<MemberMeta> properties)
        {
            var propertyList = new List<MemberMeta>();

            static string GetTypeMinimalName(ISymbol ts) =>
                ts.ContainingType is { } containingType
                    ? $"{containingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}.{ts.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}"
                    : ts.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            void FetchProperties(INamespaceOrTypeSymbol symbol, bool declaredInBase)
            {
                foreach (var ps in symbol.GetMembers().Where(s => s.Kind == SymbolKind.Property).OfType<IPropertySymbol>())
                {
                    propertyList.Add(new(ps.Name, GetTypeMinimalName(ps.Type), declaredInBase));
                    Using.ExtractNamespaces(ps.Type, namespaces);
                }
            }

            static IEnumerable<INamedTypeSymbol> GetSymbolHierarchy(ITypeSymbol symbol)
            {
                var result = new List<INamedTypeSymbol>();

                while (symbol.BaseType != null)
                {
                    var @base = symbol.BaseType;

                    if (@base.SpecialType == SpecialType.System_Object || @base.SpecialType == SpecialType.System_ValueType)
                        break;

                    result.Insert(0, @base);
                    symbol = @base;
                }

                return result;
            }

            bool basesDecoratedProperly = true;
            foreach (var baseType in GetSymbolHierarchy(typeSymbol))
            {
                if (!ShouldProcessType(baseType, autoAttributeSymbol, context, out _))
                {
                    ReportDiagnostics(context, BaseTypeNotDecorated, baseType);
                    basesDecoratedProperly = false;
                }

                FetchProperties(baseType, true);
            }

            FetchProperties(typeSymbol, false);

            properties = propertyList;

            if (properties.Count == 0)
            {
                ReportDiagnostics(context, NoContractMembersRule, typeSymbol);
                return false;
            }

            return basesDecoratedProperly;
        }
    }

    public sealed class AutoWithSyntaxReceiver : ISyntaxReceiver
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

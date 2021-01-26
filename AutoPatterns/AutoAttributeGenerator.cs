using System.Text;
using AutoPatterns.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace AutoPatterns
{
    public interface IAutoAttributeGenerator : ISourceGenerator
    {
        string AutoPatternName { get; }
        string AutoAttributeName { get; }
    }

    public abstract class AutoAttributeGenerator<TSyntaxReceiver> : IAutoAttributeGenerator
        where TSyntaxReceiver : class, ISyntaxReceiver, new()
    {
        public string AutoPatternName { get; }
        public string AutoAttributeName { get; }
        private readonly string _autoAttributeSource;

        protected AutoAttributeGenerator(string autoPatternName, string autoAttributeName, string autoAttributeSource)
        {
            AutoPatternName = autoPatternName;
            AutoAttributeName = autoAttributeName;
            _autoAttributeSource = autoAttributeSource;
        }

        public void Initialize(GeneratorInitializationContext context) => context.RegisterForSyntaxNotifications(() => new TSyntaxReceiver());

        public void Execute(GeneratorExecutionContext context)
        {
            context.CheckDebugger(GetType().Name);

            context.AddSource(AutoAttributeName, SourceText.From(_autoAttributeSource, Encoding.UTF8));

            if (context.SyntaxReceiver is not TSyntaxReceiver receiver) { ReportDiagnostics(context, AutoDiagnostics.NoSyntaxReceiver, null); return; }

            if (context.Compilation is not CSharpCompilation cSharpCompilation) { ReportDiagnostics(context, AutoDiagnostics.CSharpNotSupported, null); return; }


            var options = cSharpCompilation.SyntaxTrees[0].Options as CSharpParseOptions;
            var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(_autoAttributeSource, Encoding.UTF8), options));

            var autoAttributeSymbol = compilation.GetTypeByMetadataName($"Auto.{AutoAttributeName}");
            if (autoAttributeSymbol is null) { ReportDiagnostics(context, AutoDiagnostics.NoAutoAttributeRule, null); return; }


            ProcessNodes(context, receiver, compilation, autoAttributeSymbol);
        }

        protected abstract void ProcessNodes(GeneratorExecutionContext context, TSyntaxReceiver receiver, Compilation compilation, INamedTypeSymbol autoAttributeSymbol);


        protected static DiagnosticDescriptor GetDiagnosticDescriptor(byte id, string patternName, string message, DiagnosticSeverity diagnosticSeverity = DiagnosticSeverity.Error)
            => AutoDiagnostics.GetDiagnosticDescriptor(id, patternName, message, diagnosticSeverity);

        protected void ReportDiagnostics(GeneratorExecutionContext context, DiagnosticDescriptor rule, ISymbol? symbol) =>
            context.ReportDiagnostic(Diagnostic.Create(rule, symbol?.Locations[0] ?? Location.None,
                symbol?.Name, symbol?.ContainingNamespace?.ToString(), GetType().FullName, AutoAttributeName
                ));
    }

    internal static class AutoDiagnostics
    {
        internal static readonly DiagnosticDescriptor NoAutoAttributeRule = GetDiagnosticDescriptor(0, "Auto", "Auto* attribute '{3}' associated with {2} is not defined");

        internal static readonly DiagnosticDescriptor NoSyntaxReceiver = GetDiagnosticDescriptor(256, "Auto", "Internal error - no appropriate syntax receiver");
        internal static readonly DiagnosticDescriptor CSharpNotSupported = GetDiagnosticDescriptor(257, "Auto", "C# compilation units are NOT supported");
        //internal static readonly DiagnosticDescriptor VisualBasicNotSupported = GetDiagnosticDescriptor(258, "Auto", "VB.NET compilation units are NOT supported");

        internal static DiagnosticDescriptor GetDiagnosticDescriptor(ushort id, string patternName, string message, DiagnosticSeverity diagnosticSeverity = DiagnosticSeverity.Error)
            => new($"Auto{patternName}{id:000}", $"Couldn't generate automatic '{patternName}' pattern", "{0}: " + message, "AutoGenerator", diagnosticSeverity, true);
    }
}

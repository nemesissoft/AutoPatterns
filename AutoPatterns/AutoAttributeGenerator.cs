using System.Text;
using AutoPatterns.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace AutoPatterns
{
    public abstract class AutoAttributeGenerator : ISourceGenerator
    {
        public abstract string AutoPatternName { get; }
        public abstract string AutoAttributeName { get; }

        public abstract void Initialize(GeneratorInitializationContext context);
        public abstract void Execute(GeneratorExecutionContext context);



        internal static readonly DiagnosticDescriptor NoAutoAttributeRule = GetDiagnosticDescriptor(0, "Auto", "Auto* attribute '{3}' associated with {2} is not defined");

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

    public abstract class AutoAttributeGenerator<TSyntaxReceiver> : AutoAttributeGenerator
        where TSyntaxReceiver : class, ISyntaxReceiver, new()
    {
        public sealed override string AutoPatternName { get; }
        public sealed override string AutoAttributeName { get; }
        private readonly string _autoAttributeSource;

        protected AutoAttributeGenerator(string autoPatternName, string autoAttributeName, string autoAttributeSource)
        {
            AutoPatternName = autoPatternName;
            AutoAttributeName = autoAttributeName;
            _autoAttributeSource = autoAttributeSource;
        }

        public sealed override void Initialize(GeneratorInitializationContext context) => context.RegisterForSyntaxNotifications(() => new TSyntaxReceiver());

        public sealed override void Execute(GeneratorExecutionContext context)
        {
            DebuggerChecker.CheckDebugger(context, GetType().Name);

            context.AddSource(AutoAttributeName, SourceText.From(_autoAttributeSource, Encoding.UTF8));

            if (context.SyntaxReceiver is not TSyntaxReceiver receiver) { ReportDiagnostics(context, NoSyntaxReceiver, null); return; }

            if (context.Compilation is not CSharpCompilation cSharpCompilation) { ReportDiagnostics(context, CSharpNotSupported, null); return; }


            var options = cSharpCompilation.SyntaxTrees[0].Options as CSharpParseOptions;
            var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(_autoAttributeSource, Encoding.UTF8), options));

            var autoAttributeSymbol = compilation.GetTypeByMetadataName($"Auto.{AutoAttributeName}");
            if (autoAttributeSymbol is null) { ReportDiagnostics(context, NoAutoAttributeRule, null); return; }


            ProcessNodes(context, receiver, compilation, autoAttributeSymbol);
        }

        protected abstract void ProcessNodes(GeneratorExecutionContext context, TSyntaxReceiver receiver, Compilation compilation, INamedTypeSymbol autoAttributeSymbol);

    }
}

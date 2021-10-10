using System.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoPatterns.Utils
{
    //[Conditional("DEBUG")]
    internal static class DebuggerChecker
    {
        public static void CheckDebugger(GeneratorExecutionContext context, string generatorName)
        {
            if (context.IsOptionEnabled("DebugSourceGenerators") || context.IsOptionEnabled("Debug" + generatorName))
                Debugger.Launch();
        }
    }

    internal static class AnalyzerConfigOptionsExtensions
    {
        /*public static bool IsEnabled(this AnalyzerOptions options, string optionName)
            => IsEnabled(options.AnalyzerConfigOptionsProvider.GlobalOptions, optionName);

        public static bool IsEnabled(this AnalyzerConfigOptionsProvider options, string optionName)
            => IsEnabled(options.GlobalOptions, optionName);*/

        public static bool IsOptionEnabled(this GeneratorExecutionContext context, string optionName)
            => IsOptionEnabled(context.AnalyzerConfigOptions.GlobalOptions, optionName);

        public static bool IsOptionEnabled(this AnalyzerConfigOptions options, string optionName)
            => options.TryGetValue("build_property." + optionName, out var value) && bool.TryParse(value, out var enabled) && enabled;
    }
}

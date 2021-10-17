using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Nemesis.CodeAnalysis;

using NUnit.Framework;

using static AutoPatterns.Tests.Utils;

namespace AutoPatterns.Tests
{
    [TestFixture]
    public partial class AutoWithGeneratorTests
    {
        [TestCase(@"namespace Tests { [Auto.AutoWith] public partial record RecordPoint2d(double X, double Y) { } }")]
        public void DiagnosticsRemoval_LackOfAutoAttribute(string source)
        {
            var compilation = CreateCompilation(source);
            var initialDiagnostics = CompilationUtils.GetCompilationIssues(compilation).ToList();

            Assert.That(initialDiagnostics, Has.All.Contain("The type or namespace name 'Auto"));


            CompilationUtils.RunGenerators(compilation, out var diagnostics, new AutoWithGenerator());
            Assert.That(diagnostics, Is.Empty);
        }

        private static IEnumerable<TestCaseData> EndToEndSources() => EndToEndCases.AutoWithCases()
                .Select((t, i) => new TestCaseData($@"using Auto; using System; using System.Collections.Generic;
namespace AutoPatterns.Tests {{ {t.source} }}")
                    .SetName($"Loa_{i + 1:00}_{t.name}"));

        [TestCaseSource(nameof(EndToEndSources))]
        public void DiagnosticsRemoval_LackOfAutoAttribute_EndToEnd(string source)
            => DiagnosticsRemoval_LackOfAutoAttribute(source);


        private static readonly IEnumerable<TestCaseData> _negativeDiagnostics = new (string source, string rule, string expectedMessagePart)[]
        {
            (@"[AutoWith] class NonPartial { }", nameof(AutoWithGenerator.NonPartialTypeRule), "Type decorated with AutoWithAttribute must be also declared partial"),
            
            (@"partial class Base { } [AutoWith] partial class Derived:Base { public int Number { get; set; } }", nameof(AutoWithGenerator.BaseTypeNotDecorated), "'Base' type must also be decorated with AutoWithAttribute attribute"),

            (@"[AutoWith(1)]              
              partial class NonBool { }", nameof(AutoWithGenerator.InvalidSettingsAttributeRule), "Attribute AutoWithAttribute must be constructed with 1 boolean value, or with default values"),

            (@"[AutoWith] partial class NoProperties { }", nameof(AutoWithGenerator.NoContractMembersRule), "warning AutoWith050: NoProperties: No non-abstract properties for With pattern defined at 'NoProperties'"),
            (@"[AutoWith] abstract partial class Base{} [AutoWith] partial class Der: Base{}", nameof(AutoWithGenerator.NoContractMembersRule),
                "warning AutoWith050: Der: No non-abstract properties for With pattern defined at 'Der'"),

            (@"namespace Test {
    partial class ContainingType { [Auto.AutoWith] partial class Test{} } 
}", nameof(AutoWithGenerator.NamespaceAndTypeNamesEqualRule), "Test: Type name 'Test' cannot be equal to containing namespace: 'AutoPatterns.Tests.Test'"),
        }
            .Select((t, i) => new TestCaseData($@"using Auto; namespace AutoPatterns.Tests {{ {t.source} }}", t.rule, t.expectedMessagePart)
                .SetName($"Negative{i + 1:00}_{t.rule}"));

        [TestCaseSource(nameof(_negativeDiagnostics))]
        public void Diagnostics_CheckNegativeCases(in string source, in string ruleName, in string expectedMessagePart)
        {
            var generator = new AutoWithGenerator();

            var ruleField = typeof(AutoWithGenerator)
                .GetField(ruleName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            var rule = (DiagnosticDescriptor)ruleField?.GetValue(ruleField?.IsStatic == false ? generator : null) ?? throw new NotSupportedException($"Rule '{ruleName}' does not exist");

            var compilation = CreateCompilation(source);

            CompilationUtils.RunGenerators(compilation, out var diagnostics, generator);

            var diagnosticsList = diagnostics.ToList();
            Assert.That(diagnosticsList, Has.Count.EqualTo(1));

            var diagnostic = diagnosticsList.Single();

            Assert.That(diagnostic.Descriptor.Id, Is.EqualTo(rule.Id));
            Assert.That(diagnostic.ToString(), Does.Contain(expectedMessagePart));
        }
    }
}

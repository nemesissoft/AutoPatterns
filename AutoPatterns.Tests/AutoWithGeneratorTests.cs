using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nemesis.CodeAnalysis;

using NUnit.Framework;

using static AutoPatterns.Tests.Utils;

namespace AutoPatterns.Tests
{
    [TestFixture]
    public partial class AutoWithGeneratorTests
    {
        private static readonly IEnumerable<TestCaseData> _endToEndCases = EndToEndCases.AutoWithCases()
            .Select((t, i) => new TestCaseData($@"using Auto;
using System;
using System.Collections.Generic;
namespace AutoPatterns.Tests {{ {t.source} }}", t.expectedCode)
                .SetName($"E2E_{i + 1:00}_{t.name}"));

        [TestCaseSource(nameof(_endToEndCases))]
        public void EndToEndTests(string source, string expectedCode)
        {
            var compilation = CreateCompilation(source);

            var generatedTreesCount = compilation.SyntaxTrees
                .Select(tree => tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().Count()).Sum();

            var initialDiagnostics = CompilationUtils.GetCompilationIssues(compilation);

            Assert.That(initialDiagnostics, Has.All.Contain("The type or namespace name 'Auto"));

            var generatedTrees = GetGeneratedTreesOnly<AutoWithGenerator>(compilation, generatedTreesCount);

            var actual = ScrubGeneratorComments(string.Join(Environment.NewLine, generatedTrees));

            Assert.That(actual, Is.EqualTo(expectedCode).Using(IgnoreNewLinesComparer.EqualityComparer));
        }
    }
}

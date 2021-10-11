using System;
using System.Collections.Generic;
using System.Linq;

using Nemesis.CodeAnalysis;

using NUnit.Framework;

using static AutoPatterns.Tests.Utils;

namespace AutoPatterns.Tests
{
    [TestFixture]
    public class AutoWithGeneratorTests
    {
        private static readonly IEnumerable<TestCaseData> _endToEndCases = EndToEndCases.AutoWithCases()
            .Select((t, i) => new TestCaseData($@"using Auto;
using System;
using System.Collections.Generic;
namespace AutoPatterns.Tests {{ {t.source} }}", t.expectedCode, t.generatedTreesCount)
                .SetName($"E2E_{i + 1:00}_{t.name}"));

        [TestCaseSource(nameof(_endToEndCases))]
        public void EndToEndTests(string source, string expectedCode, int generatedTreesCount)
        {
            var compilation = CreateCompilation(source);

            var issues = CompilationUtils.GetCompilationIssues(compilation);

            var generatedTrees = GetGeneratedTreesOnly<AutoWithGenerator>(compilation, generatedTreesCount);

            var actual = ScrubGeneratorComments(string.Join(Environment.NewLine + Environment.NewLine, generatedTrees));

            Assert.That(actual, Is.EqualTo(expectedCode).Using(IgnoreNewLinesComparer.EqualityComparer));
        }

        //TODO diagnostics tests

        //TODO tests for abstract://do not generate for abstract class + //protected ctor for abstract class


        //TODO tests for lack of validation == lack of generation OnConstructed 
        //TODO check if OnConstructed is called (throw exception ?)

        //TODO test for AutoWithAttribute with 0 and 1 argument 

        //TODO test for Base class with no members but with Derived class with some members 
    }
}

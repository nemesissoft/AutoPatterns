using System;
using System.Collections.Generic;
using System.Linq;

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
namespace AutoPatterns.Tests {{ {t.source} }}", t.expectedCode, t.generatedTreesCount)
                .SetName($"E2E_{i + 1:00}_{t.name}"));

        [TestCaseSource(nameof(_endToEndCases))]
        public void EndToEndTests(string source, string expectedCode, int generatedTreesCount)
        {
            var compilation = CreateCompilation(source);

            var initialDiagnostics = CompilationUtils.GetCompilationIssues(compilation);

            Assert.That(initialDiagnostics, Has.All.Contain("The type or namespace name 'Auto"));

            var generatedTrees = GetGeneratedTreesOnly<AutoWithGenerator>(compilation, generatedTreesCount);

            var actual = ScrubGeneratorComments(string.Join(Environment.NewLine + Environment.NewLine, generatedTrees));

            Assert.That(actual, Is.EqualTo(expectedCode).Using(IgnoreNewLinesComparer.EqualityComparer));
        }



        //TODO tests for abstract://do not generate Withers for abstract class +
        //do not generate withers for abstract properties + do not generate ctor for abstract properties
        //+ protected ctor for abstract class
        /*[Auto.AutoWith(false)] abstract partial class Abstract
            {
                public int NormalNumber { get; }
                public abstract int AbstractNumber { get; }
            }

            [Auto.AutoWith(false)] partial class Der: Abstract
            {
                public override int AbstractNumber { get; }
                public int DerivedNumber { get; }
            }*/


        //TODO tests for lack of validation == lack of generation OnConstructed 


        //TODO test for Base class with no members but with Derived class with some members 
    }
}

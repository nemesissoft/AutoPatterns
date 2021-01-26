using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Nemesis.CodeAnalysis;

using NUnit.Framework;

using static AutoPatterns.Tests.Utils;

namespace AutoPatterns.Tests
{
    [TestFixture]
    public class AutoWithGeneratorTests
    {
        private static IReadOnlyList<(string name, string source, string expectedCode)> AutoWithCases() => new[]
        {
            ("Struct", @"partial struct Main
    {
        public string Text { get; }
        public int Number { get; }
        public DateTime Date { get; }
        public List<DateTime> Dates{ get; private set; }
    }", @"//HEAD
using System;
using System.Collections.Generic;
using Auto;

namespace AutoPatterns.Tests
{
    [System.CodeDom.Compiler.GeneratedCode("""", """")]
    [System.Runtime.CompilerServices.CompilerGenerated]
    partial struct Main 
    {
        public Main(string text, int number, DateTime date, List<DateTime> dates)
        {
            Text = text;
            Number = number;
            Date = date;
            Dates = dates;

            OnConstructed();
        }

        partial void OnConstructed();

        public Main WithText(string value) => new Main(value, Number, Date, Dates);

        public Main WithNumber(int value) => new Main(Text, value, Date, Dates);

        public Main WithDate(DateTime value) => new Main(Text, Number, value, Dates);

        public Main WithDates(List<DateTime> value) => new Main(Text, Number, Date, value);
    }
}")

            //TODO 3 classes in derivation chain
        };

        private static readonly IEnumerable<TestCaseData> _endToEndCases = AutoWithCases()
            .Select((t, i) => new TestCaseData($@"using Auto;
using System;
using System.Collections.Generic;
namespace AutoPatterns.Tests {{[AutoWith] {t.source} }}", t.expectedCode)
                .SetName($"E2E_{i + 1:00}_{t.name}"));

        [TestCaseSource(nameof(_endToEndCases))]
        public void EndToEndTests(string source, string expectedCode)
        {
            var compilation = CreateCompilation(source);

            var issues = CompilationUtils.GetCompilationIssues(compilation);

            var generatedTrees = GetGeneratedTreesOnly<AutoWithGenerator>(compilation);

            var actual = ScrubGeneratorComments(generatedTrees.Single());

            Assert.That(actual, Is.EqualTo(expectedCode).Using(IgnoreNewLinesComparer.EqualityComparer));
        }

        //TODO diagnostics tests
        //TODO tests for lack of validation
        //TODO tests for abstract://do not generate for abstract class + //protected ctor for abstract class
    }
}

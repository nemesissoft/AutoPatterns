using System.Collections.Generic;

namespace AutoPatterns.Tests
{
    static class EndToEndCases
    {
        public static IReadOnlyList<(string name, int generatedTreesCount, string source, string expectedCode)> AutoWithCases() => new[]
      {
            ("Struct", 1, @"[AutoWith] partial struct Main
    {
        public string Text { get; }
        public int Number { get; }
        public DateTime Date { get; }
        public List<DateTime> Dates{ get; private set; }
    }",
                @"using System;
using System.Collections.Generic;
using Auto;

namespace AutoPatterns.Tests
{
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

        [System.Diagnostics.Contracts.Pure]
        public Main WithText(string value) => new Main(value, Number, Date, Dates);

        [System.Diagnostics.Contracts.Pure]
        public Main WithNumber(int value) => new Main(Text, value, Date, Dates);

        [System.Diagnostics.Contracts.Pure]
        public Main WithDate(DateTime value) => new Main(Text, Number, value, Dates);

        [System.Diagnostics.Contracts.Pure]
        public Main WithDates(List<DateTime> value) => new Main(Text, Number, Date, value);
    }
}"),
            ("ThreeClassesInDerivationChain", 3, @"
[Auto.AutoWith] partial class Base { public string BaseText { get; } public int BaseInt { get; } }
[Auto.AutoWith] partial class Derived : Base { public bool DerivedBool { get; } }
[Auto.AutoWith] partial class Derived2 : Derived{}",
                @"using System;
using System.Collections.Generic;
using Auto;

namespace AutoPatterns.Tests
{
    partial class Base 
    {
        public Base(string baseText, int baseInt)
        {
            BaseText = baseText;
            BaseInt = baseInt;

            OnConstructed();
        }

        partial void OnConstructed();

        [System.Diagnostics.Contracts.Pure]
        public Base WithBaseText(string value) => new Base(value, BaseInt);

        [System.Diagnostics.Contracts.Pure]
        public Base WithBaseInt(int value) => new Base(BaseText, value);
    }
}


using System;
using System.Collections.Generic;
using Auto;

namespace AutoPatterns.Tests
{
    partial class Derived 
    {
        public Derived(string baseText, int baseInt, bool derivedBool) : base(baseText, baseInt)
        {
            DerivedBool = derivedBool;

            OnConstructed();
        }

        partial void OnConstructed();

        [System.Diagnostics.Contracts.Pure]
        public new Derived WithBaseText(string value) => new Derived(value, BaseInt, DerivedBool);

        [System.Diagnostics.Contracts.Pure]
        public new Derived WithBaseInt(int value) => new Derived(BaseText, value, DerivedBool);

        [System.Diagnostics.Contracts.Pure]
        public Derived WithDerivedBool(bool value) => new Derived(BaseText, BaseInt, value);
    }
}


using System;
using System.Collections.Generic;
using Auto;

namespace AutoPatterns.Tests
{
    partial class Derived2 
    {
        public Derived2(string baseText, int baseInt, bool derivedBool) : base(baseText, baseInt, derivedBool)
        {

            OnConstructed();
        }

        partial void OnConstructed();

        [System.Diagnostics.Contracts.Pure]
        public new Derived2 WithBaseText(string value) => new Derived2(value, BaseInt, DerivedBool);

        [System.Diagnostics.Contracts.Pure]
        public new Derived2 WithBaseInt(int value) => new Derived2(BaseText, value, DerivedBool);

        [System.Diagnostics.Contracts.Pure]
        public new Derived2 WithDerivedBool(bool value) => new Derived2(BaseText, BaseInt, value);
    }
}")
        };
    }
}

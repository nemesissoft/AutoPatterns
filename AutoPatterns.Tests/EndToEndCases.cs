using System.Collections.Generic;

namespace AutoPatterns.Tests
{
    static class EndToEndCases
    {
        public static IReadOnlyList<(string name, int generatedTreesCount, string source, string expectedCode)> AutoWithCases() => new[]
        {
            ("NoValidation", 1, @"[AutoWith(supportValidation: false)] partial struct Main { public string Text { get; } }",
                @"using System;
using System.Collections.Generic;
using Auto;

namespace AutoPatterns.Tests
{
    partial struct Main 
    {
        public Main(string text)
        {
            Text = text;
        }

        [System.Diagnostics.Contracts.Pure]
        public Main WithText(string value) => new Main(value);
    }
}"),

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
        public Derived(bool derivedBool, string baseText, int baseInt) : base(baseText, baseInt)
        {
            DerivedBool = derivedBool;

            OnConstructed();
        }

        partial void OnConstructed();

        [System.Diagnostics.Contracts.Pure]
        public Derived WithDerivedBool(bool value) => new Derived(value, BaseText, BaseInt);

        [System.Diagnostics.Contracts.Pure]
        public override Derived WithBaseText(string value) => new Derived(DerivedBool, value, BaseInt);

        [System.Diagnostics.Contracts.Pure]
        public override Derived WithBaseInt(int value) => new Derived(DerivedBool, BaseText, value);
    }
}


using System;
using System.Collections.Generic;
using Auto;

namespace AutoPatterns.Tests
{
    partial class Derived2 
    {
        public Derived2(bool derivedBool, string baseText, int baseInt) : base(derivedBool, baseText, baseInt)
        {

            OnConstructed();
        }

        partial void OnConstructed();

        [System.Diagnostics.Contracts.Pure]
        public override Derived2 WithDerivedBool(bool value) => new Derived2(value, BaseText, BaseInt);

        [System.Diagnostics.Contracts.Pure]
        public override Derived2 WithBaseText(string value) => new Derived2(DerivedBool, value, BaseInt);

        [System.Diagnostics.Contracts.Pure]
        public override Derived2 WithBaseInt(int value) => new Derived2(DerivedBool, BaseText, value);
    }
}
"),
          
            ("AbstractProperties", 2, @"[Auto.AutoWith] abstract partial class Abstract
            {
                public int NormalNumber { get; }
                public abstract int AbstractNumber { get; }
            }

            [Auto.AutoWith(false)] partial class Der: Abstract
            {
                public override int AbstractNumber { get; }
                public int DerivedNumber { get; }
            }", 
                @"using System;
using System.Collections.Generic;
using Auto;

namespace AutoPatterns.Tests
{
    abstract partial class Abstract 
    {
        protected Abstract(int normalNumber)
        {
            NormalNumber = normalNumber;

            OnConstructed();
        }

        partial void OnConstructed();
        
        public abstract Abstract WithNormalNumber(int value);
    }
}


using System;
using System.Collections.Generic;
using Auto;

namespace AutoPatterns.Tests
{
    partial class Der 
    {
        public Der(int abstractNumber, int derivedNumber, int normalNumber) : base(normalNumber)
        {
            AbstractNumber = abstractNumber;
            DerivedNumber = derivedNumber;
        }

        [System.Diagnostics.Contracts.Pure]
        public Der WithAbstractNumber(int value) => new Der(value, DerivedNumber, NormalNumber);

        [System.Diagnostics.Contracts.Pure]
        public Der WithDerivedNumber(int value) => new Der(AbstractNumber, value, NormalNumber);

        [System.Diagnostics.Contracts.Pure]
        public override Der WithNormalNumber(int value) => new Der(AbstractNumber, DerivedNumber, value);
    }
}
"),
            ("AdvancedAbstractProperties", 3, @"",
                @"")
            
        };
    }
}

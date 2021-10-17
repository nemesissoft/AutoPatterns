using System.Collections.Generic;

namespace AutoPatterns.Tests
{
    static class EndToEndCases
    {
        public static IReadOnlyList<(string name, string source, string expectedCode)> AutoWithCases() => new[]
        {
            ("NoValidation", @"[AutoWith(supportValidation: false)] partial struct Main { public string Text { get; } }",
                @"using System;
using System.Collections.Generic;
using Auto;

namespace AutoPatterns.Tests
{
    partial struct Main 
    {
        public Main(string text)
        {
            this.Text = text;
        }
    }
}"),

            ("Struct", @"[AutoWith] partial struct Main
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
            this.Text = text;
            this.Number = number;
            this.Date = date;
            this.Dates = dates;

            OnConstructed();
        }

        partial void OnConstructed();
    }
}"),

            ("ThreeClassesInDerivationChain", @"
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
            this.BaseText = baseText;
            this.BaseInt = baseInt;

            OnConstructed();
        }

        partial void OnConstructed();
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
            this.DerivedBool = derivedBool;

            OnConstructed();
        }

        partial void OnConstructed();
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
    }
}"),
          
            ("AbstractProperties", @"[Auto.AutoWith] abstract partial class Abstract
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
            this.NormalNumber = normalNumber;

            OnConstructed();
        }

        partial void OnConstructed();
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
            this.AbstractNumber = abstractNumber;
            this.DerivedNumber = derivedNumber;
        }
    }
}"),
            
            ("AdvancedAbstractProperties", @"
    [Auto.AutoWith(false)] abstract partial class Base1
    {
        public int Normal1 { get; }
        public abstract int Abstract1 { get; }
        public virtual int Virtual1 { get; }
    }

    [Auto.AutoWith(false)] partial class Implementation2 : Base1
    {
        public override int Abstract1 { get; }
        public int Normal2 { get; }
        public virtual int Virtual2 { get; }
    }

    [Auto.AutoWith(false)]
    abstract partial class Base3 : Implementation2
    {
        public override int Abstract1 { get; }
        public abstract int Abstract3 { get; }
        public int Normal3 { get; }
        public override int Virtual1 { get; }
    }
    
    [Auto.AutoWith(false)]
    abstract partial class Implementation4: Base3
    {
        public override int Abstract3 { get; }
        public int Normal4 { get; }
        public override int Abstract1 { get; }
    }
",
                @"using System;
using System.Collections.Generic;
using Auto;

namespace AutoPatterns.Tests
{
    abstract partial class Base1 
    {
        protected Base1(int normal1, int virtual1)
        {
            this.Normal1 = normal1;
            this.Virtual1 = virtual1;
        }
    }
}

using System;
using System.Collections.Generic;
using Auto;

namespace AutoPatterns.Tests
{
    partial class Implementation2 
    {
        public Implementation2(int abstract1, int normal2, int virtual2, int normal1, int virtual1) : base(normal1, virtual1)
        {
            this.Abstract1 = abstract1;
            this.Normal2 = normal2;
            this.Virtual2 = virtual2;
        }
    }
}

using System;
using System.Collections.Generic;
using Auto;

namespace AutoPatterns.Tests
{
    abstract partial class Base3 
    {
        protected Base3(int normal3, int abstract1, int normal2, int virtual2, int normal1, int virtual1) : base(abstract1, normal2, virtual2, normal1, virtual1)
        {
            this.Abstract1 = abstract1;
            this.Normal3 = normal3;
            this.Virtual1 = virtual1;
        }
    }
}

using System;
using System.Collections.Generic;
using Auto;

namespace AutoPatterns.Tests
{
    abstract partial class Implementation4 
    {
        protected Implementation4(int abstract3, int normal4, int normal3, int abstract1, int normal2, int virtual2, int normal1, int virtual1) : base(normal3, abstract1, normal2, virtual2, normal1, virtual1)
        {
            this.Abstract3 = abstract3;
            this.Normal4 = normal4;
            this.Abstract1 = abstract1;
        }
    }
}
"),
            
            ("OnlyAbstract", @"[AutoWith] abstract partial class Abstract1{ } [AutoWith] abstract partial class Abstract2 : Abstract1 { }",
                @"using System;
using System.Collections.Generic;
using Auto;

namespace AutoPatterns.Tests
{
    abstract partial class Abstract1 
    {
        protected Abstract1()
        {

            OnConstructed();
        }

        partial void OnConstructed();
    }
}

using System;
using System.Collections.Generic;
using Auto;

namespace AutoPatterns.Tests
{
    abstract partial class Abstract2 
    {
        protected Abstract2() : base()
        {

            OnConstructed();
        }

        partial void OnConstructed();
    }
}"),

            ("EmptyAbstractAndMembersInDerived", @"[AutoWith] abstract partial class Abstract{ } [AutoWith] partial class Der : Abstract { public int Normal1 { get; } }",
                @"using System;
using System.Collections.Generic;
using Auto;

namespace AutoPatterns.Tests
{
    abstract partial class Abstract 
    {
        protected Abstract()
        {

            OnConstructed();
        }

        partial void OnConstructed();
    }
}

using System;
using System.Collections.Generic;
using Auto;

namespace AutoPatterns.Tests
{
    partial class Der 
    {
        public Der(int normal1) : base()
        {
            this.Normal1 = normal1;

            OnConstructed();
        }

        partial void OnConstructed();
    }
}
"),
        };
    }
}

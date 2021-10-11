using System;
using System.Collections.Generic;
using System.IO;
// ReSharper disable InconsistentNaming

namespace AutoPatterns.Sample
{
    class Program
    {
        static void Main()
        {
            var @base = new Base("", 15, DateTime.MaxValue, null).WithBaseText("AAA");
            Console.WriteLine(@base);

            var der2 = new Derived2("", 15, DateTime.MaxValue, null, false, FileMode.Append, 88).WithBaseText("BBB");
            Console.WriteLine(der2);
        }
    }

    [Auto.AutoWith]
    [Auto.AutoDescribe(true, true)]
    partial class Base
    {
        public string BaseText { get; }
        public int BaseInt { get; }
        public DateTime BaseDate { get; }
        public List<DateTime> BaseDates { get; private set; }
    }

    [Auto.AutoWith(supportValidation: false)]
    [Auto.AutoDescribe(false, true)]
    partial class Derived : Base
    {
        public bool DerivedBool { get; }
        public FileMode DerivedEnum { get; }
        public int DerivedInt { get; }
    }

    [Auto.AutoWith(supportValidation: false)]
    [Auto.AutoDescribe(false, true)]
    partial class Derived2 : Derived
    {
    }


    [Auto.AutoDescribe(true, true)]
    partial class EmptyBase
    {
    }
}


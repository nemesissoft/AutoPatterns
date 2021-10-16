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
            /*var @base = new Base("", 15, DateTime.MaxValue, null).WithBaseText("AAA");
            Console.WriteLine(@base);

            var der2 = new Derived2("", 15, DateTime.MaxValue, null, false, FileMode.Append, 88).WithBaseText("BBB");
            Console.WriteLine(der2);*/
        }
    }

    /*[Auto.AutoWith]
    //[Auto.AutoDescribe(true, true)]
    partial class Base
    {
        public string BaseText { get; }
        public int BaseInt { get; }

        public DateTime BaseDate { get; }
        public List<DateTime> BaseDates { get; private set; }
    }

    [Auto.AutoWith(supportValidation: false)]
    //[Auto.AutoDescribe(false, true)]
    partial class Derived : Base
    {
        public bool DerivedBool { get; }
        public FileMode DerivedEnum { get; }
        public int DerivedInt { get; }
    }

    [Auto.AutoWith(supportValidation: false)]
    //[Auto.AutoDescribe(false, true)]
    partial class Derived2 : Derived
    {
    }*/


    /*[Auto.AutoDescribe(true, true)]
    partial class EmptyBase
    {
    }*/


    [Auto.AutoWith]
    abstract partial class Base1
    {
        public int Normal1 { get; }
        public abstract int Abstract1 { get; }
        public virtual int Virtual1 { get; }
    }

    [Auto.AutoWith(false)]
    partial class Implementation2 : Base1
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

    /*[Auto.AutoWith(false)]
    partial class Implementation4 : Base3
    {

    }*/
}


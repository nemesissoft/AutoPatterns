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
            var b = new Base("", 15, DateTime.MaxValue, null).WithBaseText("123");
            Console.WriteLine(b);
        }
    }

    [Auto.AutoWith]
    [Auto.AutoDescribe(true, true)]
    partial class Base
    {
        public string BaseText { get; }
        public int BaseInt{ get; }
        public DateTime BaseDate{ get; }
        public List<DateTime> BaseDates { get; private set; }
    }

    [Auto.AutoWith(supportValidation:false)]
    [Auto.AutoDescribe(false, true)]
    partial class Derived : Base
    {
        public bool DerivedBool { get; }
        public FileMode DerivedEnum { get; }
    }
}


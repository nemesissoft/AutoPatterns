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
            var b = new Base("", 15, DateTime.MaxValue, null).WithText_1_1("123");
            Console.WriteLine(b.Text_1_1);
        }
    }

    [Auto.AutoWith]
    partial class Base
    {
        public string Text_1_1 { get; }
        public int Int_1_2 { get; }
        public DateTime Date_1_3 { get; }
        public List<DateTime> Dates_1_4 { get; private set; }
    }

    [Auto.AutoWith(supportValidation:false)]
    partial class Derived : Base
    {
        public bool Bool_2_1 { get; }
        public FileMode Enum_2_2 { get; }
    }
}

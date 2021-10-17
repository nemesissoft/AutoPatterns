using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPatterns.Sample2
{
    abstract partial class Base1
    {
        public int Normal1 { get; }
        public abstract int Abstract1 { get; }
        public virtual int Virtual1 { get; }

        protected Base1(int normal1, int virtual1)
        {
            Normal1 = normal1;
            Virtual1 = virtual1;
        }
    }

 
    partial class Implementation2 : Base1
    {
        public override int Abstract1 { get; }
        public int Normal2 { get; }
        public virtual int Virtual2 { get; }

        public Implementation2(int normal1, int virtual1, int abstract1, int normal2, int virtual2) : base(normal1, virtual1)
        {
            Abstract1 = abstract1;
            Normal2 = normal2;
            Virtual2 = virtual2;
        }
    }

 
    abstract partial class Base3 : Implementation2
    {
        public override int Abstract1 { get; }
        public abstract int Abstract3 { get; }
        public int Normal3 { get; }
        public override int Virtual1 { get; }

        protected Base3(int normal1, int virtual1, int abstract1, int normal2, int virtual2, int normal3) : base(normal1, virtual1, abstract1, normal2, virtual2)
        {
            Abstract1 = abstract1;
            Normal3 = normal3;
            Virtual1 = virtual1;
        }
    }

    abstract partial class Implementation4: Base3
    {
        public override int Abstract3 { get; }
        public int Normal4 { get; }
        public override int Abstract1 { get; }

        protected Implementation4(int normal1, int virtual1, int abstract1, int normal2, int virtual2, int normal3, int abstract3, int normal4) 
            : base(normal1, virtual1, abstract1, normal2, virtual2, normal3)
        {
            Abstract3 = abstract3;
            Normal4 = normal4;
            Abstract1 = abstract1;
        }
    }
}

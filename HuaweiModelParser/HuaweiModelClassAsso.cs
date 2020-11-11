using System.Collections.Generic;

namespace HuaweiModelParser
{
    public class HuaweiModelClassAsso
    {
        public string ClassA { get; } // current class
        public string ClassZ { get; } // parent class
        public IReadOnlyList<HuaweiModelClassAssoAttr> AssoAttrs { get; }

        public HuaweiModelClassAsso(in string classA, in string classZ, in IReadOnlyList<HuaweiModelClassAssoAttr> assoAttrs)
        {
            ClassA = classA;
            ClassZ = classZ;
            AssoAttrs = assoAttrs;
        }
    }
}
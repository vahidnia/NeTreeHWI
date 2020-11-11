using System.Collections.Generic;

namespace HuaweiModelParser
{
    public class HuaweiModelClassAggr
    {
        public string ChildClass { get; }
        public string ParentClass { get; }
        public IReadOnlyList<HuaweiModelClassAggrAttr> AggrAttrs { get; }

        public HuaweiModelClassAggr(in string childClass, in string parentClass, in IReadOnlyList<HuaweiModelClassAggrAttr> aggrAttrs)
        {
            ChildClass = childClass;
            ParentClass = parentClass;
            AggrAttrs = aggrAttrs;
        }
    }
}

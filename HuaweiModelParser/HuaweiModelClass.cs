using System.Collections.Generic;
using System.Linq;

namespace HuaweiModelParser
{
    public class HuaweiModelClass
    {
        public string ClassName { get; }
        public string ID { get; } // Value visible in GExport file
        public bool IsAbstract { get; }
        public string Name { get; } // = ID
        public IReadOnlyList<HuaweiModelClassAttr> Attrs { get; }
        public IReadOnlyDictionary<string, HuaweiModelClassAttr> AttrByAttrName { get; }

        public List<HuaweiModelClassAttr> PkAttrs { get; } = new List<HuaweiModelClassAttr>();
        public List<HuaweiModelClassAttr> PkAttrsUsedByChildrenPks { get; } = new List<HuaweiModelClassAttr>();

        public HuaweiModelClass(in string className, in string id, in bool isAbstact, in string name, in IReadOnlyList<HuaweiModelClassAttr> attrs)
        {
            ClassName = className;
            ID = id;
            IsAbstract = isAbstact;
            Name = name;
            Attrs = attrs;
            AttrByAttrName = attrs.ToDictionary(o => o.AttrName);
        }

        public Dictionary<string, Dictionary<string, string>> HuaweiType { get; set; }
    }
}
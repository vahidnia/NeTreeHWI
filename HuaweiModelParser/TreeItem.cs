using System.Collections.Generic;

namespace HuaweiModelParser
{
    public class TreeItem
    {
        public TreeItem Parent { get; set; }
        public List<TreeItem> Children { get; } = new List<TreeItem>();

        public HuaweiModelClassAggr ClassAggr { get; set; }

        public TreeItem(in HuaweiModelClassAggr classAggr)
        {
            ClassAggr = classAggr;
        }
    }
}
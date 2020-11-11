namespace HuaweiModelParser
{
    public class HuaweiModelClassAttr
    {
        public string AttrName { get; }
        public string AttrType { get; }
        public bool IsKey { get; }
        public int? DspOrder { get; }
        public string Name { get; }
        public bool Mandatory { get; }
        public string DefaultValue { get; }
        public bool IsCfgAttr { get; }

        public HuaweiModelClassAttr(in string attrName, in string attrType, in bool isKey, in int? dspOrder, in string name, bool mandatory, string defaultValue, bool isCfgAttr)
        {
            AttrName = attrName;
            AttrType = attrType;
            IsKey = isKey;
            DspOrder = dspOrder;
            Name = name;
            Mandatory = mandatory;
            DefaultValue = defaultValue;
            IsCfgAttr = isCfgAttr;
        }
    }
}
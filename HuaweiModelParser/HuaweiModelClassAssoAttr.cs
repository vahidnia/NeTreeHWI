namespace HuaweiModelParser
{
    public class HuaweiModelClassAssoAttr
    {
        public string AAttr { get; } // current class attribute
        public string ZAttr { get; } // parent class atttribute
        // TODO: RelationRule
        public HuaweiModelClassAssoAttr(in string aAttr, in string zAttr)
        {
            AAttr = aAttr;
            ZAttr = zAttr;
        }
    }
}
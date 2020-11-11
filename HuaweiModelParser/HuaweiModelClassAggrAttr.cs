namespace HuaweiModelParser
{
    public class HuaweiModelClassAggrAttr
    {
        public string CAttr { get; }
        public string PAttr { get; }

        public HuaweiModelClassAggrAttr(in string cAttr, in string pAttr)
        {
            CAttr = cAttr;
            PAttr = pAttr;
        }
    }
}
namespace CnaiToSQLiteKvp
{
    internal class CnaiParameter
    {
        public string MoType { get; }
        public string Name { get; }
        public CnaiParameterValueType ValueType { get; }

        public CnaiParameter(string moType, string name, CnaiParameterValueType valueType)
        {
            MoType = moType;
            Name = name;
            ValueType = valueType;
        }
    }
}
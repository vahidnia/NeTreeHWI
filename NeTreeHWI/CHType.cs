using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GExportToKVP
{
    public static class CHType
    {
        static  CHType()
        {
            CHTypesDic.Add("time", "String");
            CHTypesDic.Add("datetime", "DateTime");
            CHTypesDic.Add("unsignedInt", "UInt16");
            CHTypesDic.Add("string", "String");
            CHTypesDic.Add("long", "Int64");
            CHTypesDic.Add("unsignedLong", "UInt64");
            CHTypesDic.Add("date", "String");
            CHTypesDic.Add("enum(long)", "Int64");
            CHTypesDic.Add("enum(unsignedInt)", "UInt16");
        }
        public static Dictionary<string, string> CHTypesDic = new Dictionary<string, string>();
        
    }
}

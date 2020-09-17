using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Enm3gppToCsvKVPNetCore
{
    class Program
    {
        static void Main(string[] args)
        {

            if (args.Length != 4)
            {
                Console.WriteLine("Usage: enm3gpp2kvp <source.xml> <target.csv>");
                return;
            }
            Stopwatch sw = new Stopwatch();
            sw.Start();


            Enm3GPPToCsvKVPConverter.Convert(args[0], args[1], args[2], ProcessModelTxt(args[3]));
            sw.Stop();
            Console.WriteLine($"Completed in {sw.Elapsed.TotalSeconds} seconds");
        }

        private static Dictionary<string, bool> ProcessModelTxt(string v)
        {
            Dictionary<string, Boolean> attDataType = new Dictionary<string, Boolean>();
            foreach (var item in Directory.GetFiles(v, "*MODEL*.xml"))
                ProcessModel(attDataType, item);

            foreach (var item in Directory.GetFiles(v, "*com*.xml"))
                ProcessModel(attDataType, item);
            return attDataType;
        }

        private static void ProcessModel(Dictionary<string, bool> attDataType, string item)
        {
            Match match = Regex.Match(File.ReadAllText(item), @"<attribute\sname=\""(?<attname>\w+)\"">.+?<dataType>(?<dataType>.+?)</dataType>", RegexOptions.Singleline);
            while (match.Success)
            {
                var isArray = match.Groups["dataType"].Value.Contains("<sequence>");
                if (!attDataType.ContainsKey(match.Groups["attname"].Value))
                    attDataType.Add(match.Groups["attname"].Value, isArray);
                match = match.NextMatch();
            }
        }
    }
}

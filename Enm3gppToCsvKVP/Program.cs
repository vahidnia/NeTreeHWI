using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Enm3gppToCsvKVP
{
    class Program
    {
        private static void Main(string[] args)
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
            {
                Match match = Regex.Match(File.ReadAllText(item), @"<attribute\sname=\""(?<attname>\w+)\"">.+?<dataType>(?<dataType>.+?)</dataType>", RegexOptions.Singleline);
                while (match.Success)
                {
                    var isArray = match.Groups["dataType"].Value.Contains("<sequence>");
                    if (!attDataType.ContainsKey(match.Groups["attname"].Value))
                        attDataType.Add(match.Groups["attname"].Value, isArray);
                    else
                    {
                      
                    }
                    match =  match.NextMatch();

                }
            }

            foreach (var item in Directory.GetFiles(v, "*com*.xml"))
            {
                Match match = Regex.Match(File.ReadAllText(item), @"<attribute\sname=\""(?<attname>\w+)\"">.+?<dataType>(?<dataType>.+?)</dataType>", RegexOptions.Singleline);
                while (match.Success)
                {
                    var isArray = match.Groups["dataType"].Value.Contains("<sequence>");
                    if (!attDataType.ContainsKey(match.Groups["attname"].Value))
                        attDataType.Add(match.Groups["attname"].Value, isArray);
                    else
                    {

                    }
                    match = match.NextMatch();

                }
            }
            return attDataType;
        }

        private static Dictionary<string, Boolean> ProcessModel(string path)
        {
            Dictionary<string, Boolean> attDataType = new Dictionary<string, Boolean>();
            foreach (var item in Directory.GetFiles(path, "*MODEL*.xml"))
            {
                XDocument xmlDoc = XDocument.Load(item);
                var ele = xmlDoc.Descendants("attribute").Select(a => new
                {
                    att = a.Attribute("name").Value,
                    array = a.Elements("dataType").Any(b => b.Elements("sequence").Count() > 0)
                });

                foreach (var xItem in ele)
                {
                    if (!attDataType.ContainsKey(xItem.att))
                        attDataType.Add(xItem.att, xItem.array);
                    else
                    {
                       // if (attDataType.ContainsKey(xItem.att) != xItem.array)
                        //    Console.WriteLine(xItem.att);

                    }

                }
            }
            return attDataType;
        }
    }
}

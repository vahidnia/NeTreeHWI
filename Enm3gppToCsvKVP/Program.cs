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
                Console.WriteLine("Usage: enm3gpp2kvp <source> <target.csv> <ossid> <model>");
                return;
            }
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Enm3GPPToCsvKVPConverter.Convert(args[0], args[1], args[2], args[3]);
            sw.Stop();
            Console.WriteLine($"Completed in {sw.Elapsed.TotalSeconds} seconds");
        }
    }
}

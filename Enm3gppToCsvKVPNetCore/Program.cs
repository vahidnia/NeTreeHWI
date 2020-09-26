using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Enm3gppToCsvKVPNetCore
{

    class Program
    {
        private static void Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string source = "";
            string target = "";
            string model = "";
            if (args.Length == 1)
            {
                source = args[0];
                target = Path.Combine(Path.GetDirectoryName(source), Path.GetFileNameWithoutExtension(source + "Parsed.csv"));
                var CurrentDirectory = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                model = Path.Combine(CurrentDirectory, "model");
            }
            else if (args.Length == 2)
            {
                source = args[0];
                target = args[1];
                var CurrentDirectory = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                model = Path.Combine(CurrentDirectory, "model");
            }
            else if (args.Length == 3)
            {
                source = args[0];
                target = args[1];
                model = args[2];
            }
            else
            {
                Console.WriteLine("Usage: enm3gpp2kvp <source> <target.csv> <model>");
                return;
            }
            Console.WriteLine($"source:{source} \r\ntarget:{target} \r\nmodel:{model}");
            Enm3GPPToCsvKVPConverter.Convert(source, target, model);
            sw.Stop();
            Console.WriteLine($"Completed in {sw.Elapsed.TotalSeconds} seconds");
        }
    }

}

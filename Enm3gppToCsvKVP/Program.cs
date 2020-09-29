using log4net.ElasticSearch.Async;
using Serilog;
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
            var log = new LoggerConfiguration()
                 .WriteTo.File("enm2kvp.log").WriteTo.Console()
                 .CreateLogger();


            Stopwatch sw = new Stopwatch();
            sw.Start();
            string source = "";
            string target = "";
            string model = "";
            if (args.Length == 1)
            {
                source = args[0];
                target = Path.Combine(Directory.GetParent(Path.GetDirectoryName(source)).FullName, "cooked", Path.GetFileNameWithoutExtension(source) + ".csv");
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
                log.Error("Usage: enm3gpp2kvp <source> <target.csv> <model>");
                return;
            }
            log.Information($"source:{source} \r\ntarget:{target} \r\nmodel:{model}");
            Enm3GPPToCsvKVPConverter.Convert(source, target, model);
            sw.Stop();
            log.Information($"Completed in {sw.Elapsed.TotalSeconds} seconds");
        }
    }
}

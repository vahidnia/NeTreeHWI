using Serilog;
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
            var log = new LoggerConfiguration()
                 .WriteTo.File("enm2kvp.log").WriteTo.Console()
                 .CreateLogger();


            Stopwatch sw = new Stopwatch();
            sw.Start();
            string source = "";
            string target = "";
            string model = "";
            Console.WriteLine($"Args count {args.Length}");
            if (args.Length == 1)
            {
                source = args[0];
                Console.WriteLine(Path.GetDirectoryName(source));
                Console.WriteLine(Directory.GetParent(Path.GetDirectoryName(source)).FullName);
                target = Path.Combine(Directory.GetParent(Path.GetDirectoryName(source)).FullName, "cooked", Path.GetFileNameWithoutExtension(source) + ".csv");
                //var CurrentDirectory = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                string CurrentDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                model = Path.Combine(CurrentDirectory, "model");
            }
            else if (args.Length == 2)
            {
                source = args[0];
                target = args[1];
                string CurrentDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
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

using System;
using System.Diagnostics;

namespace Enm3gppToCsvKVPNetCore
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: enm3gpp2kvp <source.xml> <target.csv>");
                return;
            }
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Enm3GPPToCsvKVPConverter.Convert(args[0], args[1], args[2], null);
            sw.Stop();
            Console.WriteLine($"Completed in {sw.Elapsed.TotalSeconds} seconds");
        }
    }
}

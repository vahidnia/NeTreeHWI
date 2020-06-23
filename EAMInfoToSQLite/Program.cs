using System;

namespace EAMInfoToSQLite
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: eaminfo2sqlite <EAMInfo.xml> <target.sqlite>");
                return;
            }

            EAMInfoToDbConverter.Convert(args[0], args[1]);
        }
    }
}
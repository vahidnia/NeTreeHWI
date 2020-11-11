using System;
using System.IO;
using System.Text.RegularExpressions;
using SQLitePCL;

namespace CnaiToSQLiteKvp
{
    class Program
    {
        static void Main(string[] args)
        {
            string target = Path.Combine(Directory.GetParent(Path.GetDirectoryName(args[0])).FullName, "cooked");
            if (args.Length == 2)
            {
                target = args[1];
            }

            raw.SetProvider(new SQLite3Provider_e_sqlite3());

            CnaiExportToKvpDbConverter.Convert(args[0], target);
        }
    }
}

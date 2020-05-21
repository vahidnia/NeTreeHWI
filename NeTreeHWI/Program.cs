using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GExportToKVP
{
    class Program
    {
        static void Main(string[] args)
        {

          

            if (args.Length != 2)
            {
                Console.WriteLine("Usage: gexport2sqlite <source.xml> <target.sqlite>");
                Console.WriteLine("       gexport2sqlite <source.xml.gz> <target.sqlite>");
                Console.WriteLine("       gexport2sqlite *.xml <target.sqlite>");
                return;
            }

            string sourceFileMask = args[0];
            string dbFilePath = args[1];

            var tree = NeTreeConverter.Convert();
            var model = ModelConverter.Convert();

            using (StreamWriter streamWriter = new StreamWriter(dbFilePath))
            {

                Dictionary<string, Dictionary<string, int>> columnIndices = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
                //Dictionary<string, SQLiteCommand> dbInsertCommandCache = new Dictionary<string, SQLiteCommand>(StringComparer.OrdinalIgnoreCase);




                foreach (string filePath in Directory.EnumerateFiles(".", sourceFileMask))
                {
                    string fileName = Path.GetFileName(filePath);
                    string ne = Regex.Match(fileName, @"(?<=^GExport_).+(?=_\d+\.\d+\.\d+\.\d+_)").Value;
                    Console.WriteLine(fileName);
                    if (fileName.EndsWith(".gz"))
                    {
                        using (FileStream compressedStream = File.OpenRead(filePath))
                        {
                            using (Stream stream = new ICSharpCode.SharpZipLib.GZip.GZipInputStream(compressedStream))
                            {
                                GExportToKVPConverter.Convert(stream, dbFilePath, ne, true, streamWriter, columnIndices, model, tree);
                            }
                        }
                    }
                    else
                    {
                        using (FileStream stream = File.OpenRead(filePath))
                        {
                            GExportToKVPConverter.Convert(stream, dbFilePath, ne, true, streamWriter, columnIndices, model, tree);
                        }
                    }
                }
            }
        }
    }
}

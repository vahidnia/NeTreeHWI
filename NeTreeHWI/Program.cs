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




            if (args.Length != 3)
            {
                Console.WriteLine("Usage: gexport2sqlite <source.xml> <target.sqlite>");
                Console.WriteLine("       gexport2sqlite <source.xml.gz> <target.sqlite>");
                Console.WriteLine("       gexport2sqlite *.xml <target.sqlite>");
                return;
            }
            string sourcePath = args[0];
            string sourceFileMask = args[1];
            string dbFilePath = args[2];


            List<StreamWriter> streamWriter = new List<StreamWriter>();

            streamWriter.Add(new StreamWriter(dbFilePath, false));
            streamWriter.Add(new StreamWriter(Path.Combine(Path.GetDirectoryName(dbFilePath), "Tree" + Path.GetFileName(dbFilePath)), false));



            Dictionary<string, Dictionary<string, int>> columnIndices = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
            //Dictionary<string, SQLiteCommand> dbInsertCommandCache = new Dictionary<string, SQLiteCommand>(StringComparer.OrdinalIgnoreCase);
            var model = ModelConverter.Convert();

            foreach (string filePath in Directory.EnumerateFiles(sourcePath, sourceFileMask))
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
                            GExportToKVPConverter.Convert(stream, dbFilePath, ne, true, streamWriter, columnIndices, model);
                        }
                    }
                }
                else
                {
                    using (FileStream stream = File.OpenRead(filePath))
                    {
                        GExportToKVPConverter.Convert(stream, dbFilePath, ne, true, streamWriter, columnIndices, model);
                    }
                }

            }

            streamWriter[0].Flush();
            streamWriter[0].Close();
            streamWriter[1].Flush();
            streamWriter[1].Close();

        }
    }
}

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




            if (args.Length != 5)
            {
                Console.WriteLine("Wrong args!");
                return;
            }
            string sourcePath = args[0];
            string sourceFileMask = args[1];
            string dbFilePath = args[2];
            string modelPath = args[3];
            string ossid = args[4];

            List<StreamWriter> streamWriter = new List<StreamWriter>();

            streamWriter.Add(new StreamWriter(dbFilePath, false));
            streamWriter.Add(new StreamWriter(Path.Combine(Path.GetDirectoryName(dbFilePath), "Tree" + Path.GetFileName(dbFilePath)), false));
            streamWriter.Add(new StreamWriter(Path.Combine(Path.GetDirectoryName(dbFilePath), "Type" + Path.GetFileName(dbFilePath)), false));


            Dictionary<string, Dictionary<string, int>> columnIndices = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
            //Dictionary<string, SQLiteCommand> dbInsertCommandCache = new Dictionary<string, SQLiteCommand>(StringComparer.OrdinalIgnoreCase);


            //EamInfoParser.ExtractNeList()
            var model = ModelConverter.Convert(modelPath);
            //datadatetime,ossid,motype,parametername,type
            //foreach (var item in model.Keys)
            //{
            //    foreach (var itemmoc in model[item].Mocs.Keys)
            //    {
            //        foreach (var itematt in model[item].Mocs[itemmoc].KeyAttributes)
            //        {
            //            streamWriter[2].Write("{0}\t{1}\t{2}\t{3}\t{4}\n",
            //                fileDate,
            //                "46",
            //                "\\N",
            //                 string.IsNullOrWhiteSpace(itematt.OMCName) ? "\\N" : itematt.OMCName,
            //                string.IsNullOrWhiteSpace(itematt.type) ? "\\N" : itematt.type);
            //        }
            //        foreach (var itematt in model[item].Mocs[itemmoc].NorAttributes)
            //        {
            //            streamWriter[2].Write("{0}\t{1}\t{2}\t{3}\t{4}\n",
            //                fileDate,
            //                "46",
            //                "\\N",
            //                string.IsNullOrWhiteSpace(itematt.OMCName) ? "\\N" : itematt.OMCName,
            //                string.IsNullOrWhiteSpace(itematt.type) ? "\\N" : itematt.type);
            //        }
            //    }
            //}


            var nodeList = EamInfoParser.ExtractNeList(Path.Combine(modelPath, "EAMInfo.xml"));


            foreach (string filePath in Directory.EnumerateFiles(sourcePath, sourceFileMask))
            {
                string fileName = Path.GetFileName(filePath);
                string ne = Regex.Match(fileName, @"(?<=^GExport_).+(?=_\d+\.\d+\.\d+\.\d+_)").Value;
                Console.WriteLine(fileName);
                var dateRegex = Regex.Match(fileName, @"(?<year>\d\d\d\d)(?<month>\d\d)(?<day>\d\d)");

                var dateTime = new DateTime(int.Parse(dateRegex.Groups[1].Value), int.Parse(dateRegex.Groups[2].Value), int.Parse(dateRegex.Groups[3].Value), 0, 0, 0).ToString("yyyy-MM-dd HH:mm:ss");

                if (fileName.EndsWith(".gz"))
                {
                    using (FileStream compressedStream = File.OpenRead(filePath))
                    {
                        using (Stream stream = new ICSharpCode.SharpZipLib.GZip.GZipInputStream(compressedStream))
                        {
                            GExportToKVPConverter.Convert(stream, dbFilePath, ne, true, streamWriter, columnIndices, model, nodeList, dateTime, ossid);
                        }
                    }
                }
                else
                {
                    using (FileStream stream = File.OpenRead(filePath))
                    {
                        GExportToKVPConverter.Convert(stream, dbFilePath, ne, true, streamWriter, columnIndices, model, nodeList, dateTime, ossid);
                    }
                }
                streamWriter[0].Flush();
                streamWriter[1].Flush();
                streamWriter[2].Flush();

            }

            streamWriter[0].Close();
            streamWriter[1].Close();
            streamWriter[2].Close();
        }
    }
};
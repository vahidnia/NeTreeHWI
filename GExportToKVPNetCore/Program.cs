﻿using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GExportToKVP
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 8)
            {
                Console.WriteLine("Wrong args!");
                return;
            }


            var NEList = new List<string> { "ERPHB02", "DBPHR09", "MLYPHB3", "MLYPHB4", "KYSPHR1", "LFSHR01", "LFSHR02", "GZPHBH2", "SAMPHB3", "TRPHB01", "YEPHR01", "YEPHR03", "YEPHR05", "YEPHR06", "YEPHR10", "YEPHB10", "KYSPHR2", "KYSPHR4", "GZPHRH2", "GZPHRH3", "GZPHB02", "DBPHB03", "YEPHB01", "YEPHB03", "GZPHR01", "GZPHR02", "GZPHR03", "GZPHRH1", "GZPHBH1", "YEPHR02", "YEPHR04", "YEPHB05", "BSKPHR9", "SOPHB05", "KTPHB02", "LFSHB01", "LFSHB02", "KTPHR06", "TRPHB06", "MOB84", "SOBCHB2", "KYSPHB6", "TRPHB05", "KYSPHR8", "SAMPHB2", "YEPHB04", "SOPHR04", "SAMPHR1", "SAMPHR2", "SAMPHR3", "IMPHB04", "SOPHB03", "BSKPHB2", "IMPHRM1", "IMPHBM1", "KYSPHR5", "SAMPHR4", "BSKPHB1", "SOPHB02", "IMPHB02", "KTPHB01", "SAMPHB4", "IMPHRM2", "TRPHB07", "BSKPHB8", "SOPHB06", "KYSPHB1", "SAMPHB1", "ERPHR04", "TRPHR08", "ERPHR05", "ERPHR01", "TRPHR05", "ERPHR03", "BSKPHB9", "BSKHB10", "BSKHR10", "BDPHB02", "MLYPHR2", "BSKPHR1", "BSKPHR2", "BSKPHR3", "BSKPHR4", "BSKPHR5", "BSKPHR6", "BSKPHR7", "BSKPHR8", "SOPHR01", "SOPHR02", "SOPHR03", "DBPHR01", "DBPHR02", "DBPHR06", "DBPHR03", "DBPHR04", "KTPHB03", "VAPHB02", "VAPHR01", "VAPHR02", "VAPHR03", "TRPHR06", "TRPHR07", "KTPHB04", "ANPHR01", "ANPHR02", "ANPHR03", "ANPHB01", "ANPHB02", "SOPHR06", "SOPHR05", "BSKHB11", "BSKHR11", "IMPHR05", "IMPHR06", "IMPHR07", "IMPHR08", "KYSPHB5", "TRPHB09", "DBPHR07", "VAPHB04", "BSKPHB3", "SOPHB04", "KYSPHR7", "MLYPHR4", "KYSPHB2", "KYSPHB3", "DBPHR08", "VAPHR04", "MLYPHR3", "VAPHR05", "TRPHR04", "BDPHB01", "DBPHR05", "ERPHB05", "KTPHR01", "KTPHR02", "KTPHR03", "BDPHR01", "BDPHR02", "BDPHR03", "BDPHR04", "BDPHR05", "KTPHR04", "KTPHR05", "KTPHR07", "DBPHB02", "MLYPHB1", "DBPHB06", "DBPHB07", "KYSPHR6", "TRPHB03", "IMPHRM3", "IMPHR11", "IMPHB11", "IMPHBM2", "IMPHR09", "IMPHR10", "IMPHB05", "GZPHB01", "BSKPHB4", "KYSPHB9", "DBPHB08", "IMPHR03", "IMPHR04", "IMPHB03", "BSKPHB7", "SOPHB01", "ERPHB01", "BSKPHB5", "BSKPHB6", "IMPHR01", "IMPHR02", "IMPHB01", "YEPHB02", "DBPHB09", "DBPHB10" };
            string sourcePath = "";
            string sourceFileMask = "";
            string dbFilePath = "";
            string modelPath = "";
            string ossid = "";
            string prefix = "";
            string batchCount = "";
            string eaminfoPath = "";
            string moveTo = "";

            if (args.Length == 9)
            {
                sourcePath = args[0];
                sourceFileMask = args[1];
                dbFilePath = args[2];
                modelPath = args[3];
                ossid = args[4];
                prefix = args[5];
                batchCount = args[6];
                eaminfoPath = args[7];
                moveTo = args[8];
            }
            else if (args.Length == 8)
            {
                sourcePath = args[0];
                sourceFileMask = args[1];
                dbFilePath = args[2];
                modelPath = args[3];
                ossid = args[4];
                batchCount = args[5];
                eaminfoPath = args[6];
                moveTo = args[7];

                Console.WriteLine("paramcount 8");
            }
            else
                Console.WriteLine("Error paramcount");


            List<string> fileList = new List<string>();
            if (sourceFileMask == "*.sqlite")
                //    fileList = Directory.GetFiles(sourcePath, sourceFileMask).ToList();
                fileList = new DirectoryInfo(sourcePath)
                               .GetFiles("*.sqlite", SearchOption.AllDirectories)
                               .Where(a => (DateTime.Now - a.LastWriteTime).TotalMinutes > 15)
                               .Select(a => a.FullName)
                               .ToList();

            else
                fileList = Directory.GetFiles(sourcePath, sourceFileMask).Where(a => !NEList.Any(b => a.Contains(b))).ToList();
            if (fileList.Count() == 0)
            {
                Console.WriteLine($"{DateTime.Now} no file to process {sourcePath}");
                return;
            }

            Console.WriteLine($"cm.engine started -  source path {sourcePath}");
            var model = ModelConverter.Convert(modelPath, prefix);

            var nodeList = EamInfoParser.ExtractNeList(eaminfoPath);
            int totalFileCount = fileList.Count;
            int processedFileCount = 0;
            if (fileList.Count() > 0)
            {

                List<StreamWriter> streamWriter = new List<StreamWriter>();
                var newPathTree = Path.Combine(dbFilePath, "Tree+HWI_CM_OSSID-" + ossid + "+" + System.Guid.NewGuid().ToString().Replace('-', '_') + ".csv");
                var newPathData = Path.Combine(dbFilePath, "Data+HWI_CM_OSSID-" + ossid + "+" + System.Guid.NewGuid().ToString().Replace('-', '_') + ".csv");
                streamWriter.Add(new StreamWriter(newPathData, false));
                streamWriter.Add(new StreamWriter(newPathTree, false));
                //streamWriter.Add(new StreamWriter(Path.Combine(Path.GetDirectoryName(dbFilePath), "Type" + Path.GetFileName(dbFilePath)), false));
                var f1 = streamWriter[0].FlushAsync();
                var f2 = streamWriter[1].FlushAsync();
                foreach (string filePath in fileList.Take(int.Parse(batchCount)))
                {
                    try
                    {
                        Console.WriteLine($"{DateTime.Now.ToString()}  Processing {processedFileCount++}/{totalFileCount}");
                        Task.WaitAll(new Task[] { f1, f2 });
                        string fileName = Path.GetFileName(filePath);
                        string ne = Regex.Match(fileName, @"GExport_(?<ne>.+)(?=_\d+\.\d+\.\d+\.\d+_)").Groups["ne"].Value;
                        Console.WriteLine(fileName);
                        var dateRegex = Regex.Match(fileName, @"(?<year>\d\d\d\d)(?<month>\d\d)(?<day>\d\d)");

                        var dateTime = new DateTime(int.Parse(dateRegex.Groups[1].Value), int.Parse(dateRegex.Groups[2].Value), int.Parse(dateRegex.Groups[3].Value), 0, 0, 0).ToString("yyyy-MM-dd HH:mm:ss");

                        if (string.IsNullOrEmpty(ne))
                            throw new Exception("ne is empty");

                        if (fileName.EndsWith(".gz"))
                        {
                            using (FileStream compressedStream = File.OpenRead(filePath))
                            {
                                using (Stream stream = new ICSharpCode.SharpZipLib.GZip.GZipInputStream(compressedStream))
                                {
                                    GExportToKVPConverter.Convert(stream, dbFilePath, ne, true, streamWriter, model, nodeList, dateTime, ossid);
                                }
                            }
                        }
                        else if (fileName.EndsWith(".xml"))
                        {
                            using (FileStream stream = File.OpenRead(filePath))
                            {
                                GExportToKVPConverter.Convert(stream, dbFilePath, ne, true, streamWriter, model, nodeList, dateTime, ossid);
                            }
                        }
                        else if (fileName.EndsWith(".sqlite"))
                        {

                            var datedatetime = dateTime;
                            var eamNE = nodeList.FirstOrDefault(a => a.NeName == ne);

                            if (eamNE == null)
                            {
                                Console.WriteLine($"could not find eamINFO fot this node: {ne} file: {fileName}");
                                continue;
                            };

                            var source = SourceData("SELECT treelementclass,treedepth,parentpimoname,pimoname,displayvsmoname,motype,vsmoname FROM vs_cm_tree", $"Data Source={filePath};Version=3;");
                            var result = ToCsvTree(source);

                            foreach (var item in result)
                                streamWriter[1].Write(datedatetime + "\t" + ossid + "\t" + eamNE.Folder + "\t" + item + "\n");

                            source = SourceData("SELECT vsmoname,pimoname,motype,paramname,paramvalue FROM vs_cm_data", $"Data Source={filePath};Version=3;");
                            result = ToCsvData(source);

                            foreach (var item in result)
                                streamWriter[0].Write(datedatetime + "\t" + ossid + "\t" + item + "\n");
                        }
                        else
                        {
                            Console.WriteLine($"extention not found for {fileName}");
                        }

                        f1 = streamWriter[0].FlushAsync();
                        f2 = streamWriter[1].FlushAsync();
                        //streamWriter[2].Flush();
                        string fileDestincation = Path.Combine(moveTo, Path.GetFileName(filePath));
                        if (File.Exists(fileDestincation))
                            File.Delete(fileDestincation);
                        File.Move(filePath, fileDestincation);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        Console.WriteLine(filePath);
                        string movePath = Path.Combine(Directory.GetParent(Directory.GetParent(Path.GetDirectoryName(filePath)).FullName).FullName, Path.GetFileName(filePath));

                        Console.WriteLine($"move path {movePath}");

                        try
                        {
                            File.Move(filePath, movePath);
                        }
                        catch (Exception ex1)
                        {
                            Console.WriteLine($"Unable to movefile {filePath}");
                            Console.WriteLine(ex1.ToString());
                        }
                    }
                }
                Task.WaitAll(new Task[] { f1, f2 });
                streamWriter[0].Close();
                streamWriter[1].Close();
            }
        }


        private static IEnumerable<IDataRecord> SourceData(String sql, string connString)
        {
            using (SQLiteConnection con = new SQLiteConnection(connString))
            {
                con.Open();

                using (SQLiteCommand q = new SQLiteCommand(sql, con))
                {
                    using (var reader = q.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return reader;
                        }
                    }
                }
            }
        }

        private static IEnumerable<String> ToCsvTree(IEnumerable<IDataRecord> data)
        {
            foreach (IDataRecord record in data)
            {
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < record.FieldCount; ++i)
                {
                    String chunk = "";
                    chunk = record.GetValue(i).ToString();
                    if (i > 0)
                        sb.Append('\t');

                    sb.Append(chunk);
                }

                yield return sb.ToString();
            }
        }


        private static IEnumerable<String> ToCsvData(IEnumerable<IDataRecord> data)
        {
            foreach (IDataRecord record in data)
            {
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < record.FieldCount; ++i)
                {
                    String chunk = record.GetValue(i).ToString();
                    if (i > 0)
                        sb.Append('\t');

                    //if (chunk.Contains(',') || chunk.Contains(';'))
                    //    chunk = "\"" + chunk.Replace("\"", "\"\"") + "\"";

                    sb.Append(chunk);
                }

                yield return sb.ToString();
            }
        }



    }
};
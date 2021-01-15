using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.IO.Compression;
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
                fileList = new DirectoryInfo(sourcePath)
                               .GetFiles("*.sqlite", SearchOption.AllDirectories)
                               .Where(a => (DateTime.Now - a.LastWriteTime).TotalMinutes > 15)
                               .Select(a => a.FullName)
                               .ToList();

            else
                fileList = Directory.GetFiles(sourcePath, sourceFileMask).Where(a => !a.Contains("+NETYPE=BSC")).ToList();
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
                                using (GZipStream stream = new GZipStream(compressedStream, CompressionMode.Decompress))
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

                        if (!fileName.EndsWith(".sqlite"))
                        {
                            File.Move(filePath, fileDestincation);
                            Console.WriteLine($"file {filePath} moved to {fileDestincation}");
                        }
                        else
                        {
                            File.Delete(filePath);
                            Console.WriteLine($"file {filePath} deleted");
                        }
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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


            Console.WriteLine("cm.engine started");
            var model = ModelConverter.Convert(modelPath, prefix);

            var nodeList = EamInfoParser.ExtractNeList(eaminfoPath);
            int totalFileCount = Directory.GetFiles(sourcePath).Count();
            int processedFileCount = 0;
            while (Directory.GetFiles(sourcePath).Count() > 0)
            {

                List<StreamWriter> streamWriter = new List<StreamWriter>();
                var newPathTree = Path.Combine(dbFilePath, "Tree+HWI_CM_OSSID-" + ossid + "+" + System.Guid.NewGuid().ToString().Replace('-', '_') + ".csv");
                var newPathData = Path.Combine(dbFilePath, "Data+HWI_CM_OSSID-" + ossid + "+" + System.Guid.NewGuid().ToString().Replace('-', '_') + ".csv");
                streamWriter.Add(new StreamWriter(newPathData, false));
                streamWriter.Add(new StreamWriter(newPathTree, false));
                //streamWriter.Add(new StreamWriter(Path.Combine(Path.GetDirectoryName(dbFilePath), "Type" + Path.GetFileName(dbFilePath)), false));
                var f1 = streamWriter[0].FlushAsync();
                var f2 = streamWriter[1].FlushAsync();
                foreach (string filePath in Directory.EnumerateFiles(sourcePath, sourceFileMask).Take(int.Parse(batchCount)))
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
                        else
                        {
                            using (FileStream stream = File.OpenRead(filePath))
                            {
                                GExportToKVPConverter.Convert(stream, dbFilePath, ne, true, streamWriter, model, nodeList, dateTime, ossid);
                            }
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
                    }
                }
                Task.WaitAll(new Task[] { f1, f2 });
                streamWriter[0].Close();
                streamWriter[1].Close();
                //streamWriter[2].Close();
            }


        }
    }
};
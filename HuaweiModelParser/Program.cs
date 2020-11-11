using System;
using System.Diagnostics;
using System.IO;
using SQLitePCL;

namespace HuaweiModelParser
{
    class Program
    {
        static void Main(string[] args)
        {
            string gExportFilePath = @"x:\piworks.dev\og.kuwait.2018\20200926.hwi.4.cm\GExport_RNCHH3_10.189.17.65_20200926033239.xml.gz";
            //string gExportFilePath = @"x:\piworks.dev\og.kuwait.2018\20200926.hwi.4.cm\GExport_BSCHH1_10.189.16.54_20200926033713.xml";
            //string gExportFilePath = @"x:\piworks.dev\turkcell.cm.2020\20200602.hwi.cm\GExport_BSKPHR2_10.194.6.128_20200602015715.xml.gz";
            //string gExportFilePath = @"x:\piworks.dev\turkcell.cm.2020\20200602.hwi.cm\GExport_BSKPHB1_10.194.6.192_20200602005947.xml.gz";
            string dbFilePath = Path.ChangeExtension(gExportFilePath, ".sqlite");

            //gExportFilePath = args[0];
            //dbFilePath = args[1];

            Func<string, string, HuaweiModel> createHuaweiModel = (string modelClass, string modelVersion) =>
            {
                string modelFileName = $"model.{modelClass}.{modelVersion}.xml";
                string localFolderPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName); // .NET Core Self-Contained Build work folder is in temp, so need to find real local one 
                string modelFilePath = Path.Combine(localFolderPath, modelFileName);
                return HuaweiModel.LoadFromFile(modelFilePath);
            };

            raw.SetProvider(new SQLite3Provider_e_sqlite3());

            raw.sqlite3_open(dbFilePath, out sqlite3 db);
            using (db)
            {
                GExportHelper.ConvertGExportFile(gExportFilePath, db, createHuaweiModel);
            }
        }
    }
}

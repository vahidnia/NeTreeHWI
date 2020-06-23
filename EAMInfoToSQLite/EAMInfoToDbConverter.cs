using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace EAMInfoToSQLite
{
    internal static class EAMInfoToDbConverter
    {
        private static readonly string[] ColumnNames = new[]
            {
                "NeType",
                "NeFdn",
                "ParentNeFdn",
                "NeName",
                "NeIp",
                "Version",
                "InternalId",
                "IsMatch",
                "Partition",
                "Subnet",
                "ParentSubnet",
                "TimeZone",
                "DaylightSaveInfo",
                "IsLocked",
                "Longitude",
                "Latitude",
                "GBtsFunctionName",
                "GBtsFunctionRelateNeFdn",
                "NodeBFunctionName",
                "NodeBFunctionRelateNeFdn",
                "ENodeBFunctionName"
            };
        internal static void Convert(string eaminfoFilePath, string dbFilePath)
        {
            List<EamNe> eamNeList = EamInfoParser.ExtractNeList(eaminfoFilePath);

            if (eamNeList == null || eamNeList.Count == 0)
            {
                return;
            }

            SQLiteConnectionStringBuilder dbConnectionStringBuilder = new SQLiteConnectionStringBuilder
            {
                DataSource = dbFilePath,
                Pooling = false,
                FailIfMissing = false,
                JournalMode = SQLiteJournalModeEnum.Memory,
                CacheSize = 20000,
            };
            string dbConnectionString = dbConnectionStringBuilder.ConnectionString;
            using (SQLiteConnection dbConnection = new SQLiteConnection(dbConnectionString))
            {
                dbConnection.Open();
                using (SQLiteTransaction dbTransaction = dbConnection.BeginTransaction())
                {
                    string createTableSql = $"CREATE TABLE IF NOT EXISTS EamNe({string.Join(",", ColumnNames.Select(o => $"[{o}] TEXT COLLATE NOCASE"))})";
                    using (SQLiteCommand createTableCommand = new SQLiteCommand(createTableSql, dbConnection))
                    {
                        createTableCommand.ExecuteNonQuery();
                    }

                    using (SQLiteCommand insertCommand = new SQLiteCommand($"INSERT INTO EamNe VALUES ({string.Join(",", Enumerable.Repeat("?", ColumnNames.Length))})", dbConnection))
                    {
                        for (int i = 0; i < ColumnNames.Length; i++)
                        {
                            insertCommand.Parameters.Add(new SQLiteParameter());
                        }
                        foreach (var eamNe in eamNeList)
                        {
                            insertCommand.Parameters[0].Value = eamNe.NeType;
                            insertCommand.Parameters[1].Value = eamNe.NeFdn;
                            insertCommand.Parameters[2].Value = eamNe.ParentNeFdn;
                            insertCommand.Parameters[3].Value = eamNe.NeName;
                            insertCommand.Parameters[4].Value = eamNe.NeIp;
                            insertCommand.Parameters[5].Value = eamNe.Version;
                            insertCommand.Parameters[6].Value = eamNe.InternalId;
                            insertCommand.Parameters[7].Value = eamNe.IsMatch;
                            insertCommand.Parameters[8].Value = eamNe.Partition;
                            insertCommand.Parameters[9].Value = eamNe.Subnet;
                            insertCommand.Parameters[10].Value = eamNe.ParentSubnet;
                            insertCommand.Parameters[11].Value = eamNe.TimeZone;
                            insertCommand.Parameters[12].Value = eamNe.DaylightSaveInfo;
                            insertCommand.Parameters[13].Value = eamNe.IsLocked;
                            insertCommand.Parameters[14].Value = eamNe.Longitude;
                            insertCommand.Parameters[15].Value = eamNe.Latitude;
                            insertCommand.Parameters[16].Value = eamNe.GBtsFunctionName;
                            insertCommand.Parameters[17].Value = eamNe.GBtsFunctionRelateNeFdn;
                            insertCommand.Parameters[18].Value = eamNe.NodeBFunctionName;
                            insertCommand.Parameters[19].Value = eamNe.NodeBFunctionRelateNeFdn;
                            insertCommand.Parameters[20].Value = eamNe.ENodeBFunctionName;
                            insertCommand.ExecuteNonQuery();
                        }
                    }
                    dbTransaction.Commit();
                }
            }
        }
    }
}
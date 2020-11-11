using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SQLitePCL;

namespace CnaiToSQLiteKvp
{
    internal static class CnaiExportToKvpDbConverter
    {
        /*=============================
        MSC
            INNER_CELL, OUTER_CELL
        BSC
            INTERNAL_CELL
                OVERLAID, CHANNEL_GROUP, NREL, UTRAN_NREL
            PRIORITY_PROFILE
            SITE
            TG
            EXTERNAL_CELL
            UTRAN_EXTERNAL_CELL
        RNC
            UTRAN_CELL
        FOREIGN_CELL
        ===============================*/

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
                    String chunk = "";
                    if (i == 4)
                    {
                        var dd = (record.GetValue(i) as byte[]);
                        if (dd != null)
                            chunk = System.Text.Encoding.Default.GetString(record.GetValue(i) as byte[]);
                        else
                            chunk = "";
                    }
                    else
                        chunk = record.GetValue(i).ToString();
                    if (i > 0)
                        sb.Append('\t');

                    //if (chunk.Contains(',') || chunk.Contains(';'))
                    //    chunk = "\"" + chunk.Replace("\"", "\"\"") + "\"";

                    sb.Append(chunk);
                }

                yield return sb.ToString();
            }
        }


        public static void Convert(string cnaiFilePath, string dbFilePath)
        {
            string ossid = Regex.Match(cnaiFilePath, @"OSSID-(?<ossid>\d+)").Groups["ossid"].Value;

            string sqlitePath = Path.Combine(dbFilePath, "SQLITE+HWI_CM_OSSID-" + ossid + "+" + System.Guid.NewGuid().ToString().Replace('-', '_') + ".sqlite");
            using (FileStream cnaiStream = File.Open(cnaiFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                Convert(cnaiStream, sqlitePath);


            List<StreamWriter> streamWriter = new List<StreamWriter>();
            var newPathTree = Path.Combine(dbFilePath, "Tree+ERI_CM_OSSID-" + ossid + "+" + System.Guid.NewGuid().ToString().Replace('-', '_') + ".csv");
            var newPathData = Path.Combine(dbFilePath, "Data+ERI_CM_OSSID-" + ossid + "+" + System.Guid.NewGuid().ToString().Replace('-', '_') + ".csv");
            streamWriter.Add(new StreamWriter(newPathData, false));
            streamWriter.Add(new StreamWriter(newPathTree, false));
            //streamWriter.Add(new StreamWriter(Path.Combine(Path.GetDirectoryName(dbFilePath), "Type" + Path.GetFileName(dbFilePath)), false));
            //var f1 = streamWriter[0].FlushAsync();
            //var f2 = streamWriter[1].FlushAsync();

            var mR = Regex.Match(cnaiFilePath, @"(?<day>\d\d)-(?<month>\d\d)-(?<year>\d\d\d\d)");
            var datedatetime = new DateTime(int.Parse(mR.Groups["year"].Value), int.Parse(mR.Groups["month"].Value), int.Parse(mR.Groups["day"].Value), 0, 0, 0).ToString("yyyy-MM-dd HH:mm:ss");

            var source = SourceData("SELECT netopologyfolder,treeelementclass,treedepth,parentpimoname,pimoname,displayvsmoname,motype,vsmoname  FROM vs_cm_tree", $"Data Source={sqlitePath};Version=3;");
            var result = ToCsvTree(source);

            foreach (var item in result)
                streamWriter[1].Write(datedatetime + "\t" + ossid + "\t"  + item + "\n");

            source = SourceData("SELECT vsmoname,pimoname,motype,paramname,paramvalue FROM vs_cm_data", $"Data Source={sqlitePath};Version=3;");
            result = ToCsvData(source);

            foreach (var item in result)
                streamWriter[0].Write(datedatetime + "\t" + ossid + "\t" + item + "\n");

            //Task.WaitAll(new Task[] { f1, f2 });
            streamWriter[0].Flush();
            streamWriter[0].Close();
            streamWriter[1].Flush();
            streamWriter[1].Close();

            File.Delete(sqlitePath);

        }
        public static void Convert(Stream cnaiStream, string dbFilePath)
        {
            List<CnaiParameter> cnaiParameters = CnaiParameters.Parameters;
            Dictionary<(string moType, string parameterName), CnaiParameter> cnaiParametersByPk =
                cnaiParameters.GroupBy(o => (o.MoType, o.Name))
                              .ToDictionary(g => g.Key, g => g.First());

            raw.sqlite3_open(dbFilePath, out sqlite3 db);

            using (db)
            {
                raw.sqlite3_exec(db, "PRAGMA page_size=65536");
                raw.sqlite3_exec(db, "PRAGMA journal_mode=WAL");
                raw.sqlite3_exec(db, "BEGIN TRANSACTION");

                raw.sqlite3_exec(db, @"CREATE TABLE vs_cm_tree(
                    treeelementclass TEXT,
                    netopologyfolder TEXT,
                    treedepth INTEGER,
                    parentpimoname TEXT,
                    pimoname TEXT,
                    vsmoname TEXT,
                    displayvsmoname TEXT,
                    motype TEXT)");
                raw.sqlite3_prepare_v2(db, "INSERT INTO vs_cm_tree VALUES(?,?,?,?,?,?,?,?)", out sqlite3_stmt insertTree);

                raw.sqlite3_exec(db, "CREATE TABLE vs_cm_data(motype TEXT,vsmoname TEXT,pimoname TEXT,paramname TEXT,paramvalue BLOB)");
                raw.sqlite3_prepare_v2(db, "INSERT INTO vs_cm_data VALUES(?,?,?,?,?)", out sqlite3_stmt insertData);

                using (StreamReader reader = new StreamReader(cnaiStream))
                {
                    string currentSubnetwork = null;
                    string currentDomain = null;
                    string currentMoType = null;
                    int currentTreeDepth = -1;
                    string currentMoname = null;
                    string currentPiMoname = null;
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line))
                            continue;
                        if (line.StartsWith("."))
                        {
                            if (line.StartsWith(".subnetwork ", StringComparison.Ordinal))
                            {
                                currentSubnetwork = line.Substring(".subnetwork ".Length);
                            }
                            else if (line.StartsWith(".domain ", StringComparison.Ordinal))
                            {
                                currentDomain = line.Substring(".domain ".Length);
                                currentTreeDepth = GetTreeDepth(currentDomain);
                                currentMoType = GetMoType(currentDomain);
                            }
                            else if (line.StartsWith(".set ", StringComparison.Ordinal))
                            {
                                string moname = line.Substring(".set ".Length);
                                if (moname.EndsWith(" PG"))
                                    moname = moname.Substring(0, moname.Length - " PG".Length);

                                if (!string.Equals(moname, currentMoname))
                                {
                                    currentMoname = moname;
                                    currentPiMoname = GetPiMoname(currentDomain, moname);

                                    string parentPiMoname = GetParentPiMoname(currentDomain, moname);
                                    string displayVsMoname = GetDisplayVsMoname(currentDomain, moname);
                                    raw.sqlite3_bind_text(insertTree, 1, currentDomain);
                                    raw.sqlite3_bind_text(insertTree, 2, currentSubnetwork);
                                    raw.sqlite3_bind_int(insertTree, 3, currentTreeDepth);
                                    raw.sqlite3_bind_text(insertTree, 4, parentPiMoname);
                                    raw.sqlite3_bind_text(insertTree, 5, currentPiMoname);
                                    raw.sqlite3_bind_text(insertTree, 6, moname);
                                    raw.sqlite3_bind_text(insertTree, 7, displayVsMoname);
                                    raw.sqlite3_bind_text(insertTree, 8, currentMoType);
                                    raw.sqlite3_step(insertTree);
                                    raw.sqlite3_reset(insertTree);
                                }
                            }
                            else if (line.Equals("..end", StringComparison.Ordinal))
                            {
                            }
                        }
                        else
                        {
                            int equalSignIndex = line.IndexOf('=');
                            string propertyName = line.Substring(0, equalSignIndex);

                            cnaiParametersByPk.TryGetValue((currentDomain, propertyName), out CnaiParameter cnaiParameter);
                            if (cnaiParameter != null)
                            {
                                if (cnaiParameter.ValueType == CnaiParameterValueType._int_array ||
                                    cnaiParameter.ValueType == CnaiParameterValueType._int_list ||
                                    cnaiParameter.ValueType == CnaiParameterValueType._string_array ||
                                    cnaiParameter.ValueType == CnaiParameterValueType._string_list)
                                {
                                    propertyName += "[]";
                                }
                            }

                            string propertyValue = line.Substring(equalSignIndex + 1);
                            if (cnaiParameter == null)
                            {
                                propertyValue = propertyValue.Replace("\"", string.Empty).Replace("?", string.Empty);
                                if (string.IsNullOrEmpty(propertyValue))
                                    propertyValue = null;
                            }
                            else
                            {
                                switch (cnaiParameter.ValueType)
                                {
                                    case CnaiParameterValueType._int:
                                        {
                                            propertyValue = propertyValue?.Trim()?.Trim('"'); // values like LAC or CI for some reason still come as string
                                            if (string.IsNullOrEmpty(propertyValue))
                                                propertyValue = null;
                                        }
                                        break;
                                    case CnaiParameterValueType._int_array:
                                    case CnaiParameterValueType._int_list:
                                        {
                                            propertyValue = propertyValue?.Trim();
                                            if (string.IsNullOrEmpty(propertyValue))
                                                propertyValue = null;
                                            else
                                                propertyValue = propertyValue.Replace(",", PiArraySeparator);
                                        }
                                        break;
                                    case CnaiParameterValueType._string:
                                        {
                                            propertyValue = propertyValue?.Trim();
                                        }
                                        break;
                                    case CnaiParameterValueType._string_array:
                                    case CnaiParameterValueType._string_list:
                                        {
                                            propertyValue = propertyValue?.Trim()?.Replace("\",\"", "\"" + PiArraySeparator + "\"");
                                        }
                                        break;
                                }
                            }
                            raw.sqlite3_bind_text(insertData, 1, currentMoType);
                            raw.sqlite3_bind_text(insertData, 2, currentMoname);
                            raw.sqlite3_bind_text(insertData, 3, currentPiMoname);
                            raw.sqlite3_bind_text(insertData, 4, propertyName);
                            if (propertyValue == null)
                            {
                                raw.sqlite3_bind_null(insertData, 5);
                            }
                            else
                            {
                                if (long.TryParse(propertyValue, out long propertyValueLong) &&
                                    string.Equals(propertyValue, propertyValueLong.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
                                {
                                    raw.sqlite3_bind_int64(insertData, 5, propertyValueLong);
                                }
                                else
                                {
                                    raw.sqlite3_bind_text(insertData, 5, propertyValue);
                                }
                            }
                            raw.sqlite3_step(insertData);
                            raw.sqlite3_reset(insertData);
                        }
                    }
                }

                raw.sqlite3_finalize(insertTree);
                raw.sqlite3_finalize(insertData);

                raw.sqlite3_exec(db, "COMMIT");
                raw.sqlite3_close(db);
            }
        }


        private const string PiMonameSeparator = "→";
        private const string PiArraySeparator = "ǁ";
        private static string GetPiMoname(string domain, string moname)
        {
            switch (domain)
            {
                case "MSC":
                    {
                        return $"MSC={moname}";
                    }
                case "INNER_CELL":
                    {
                        string[] monameParts = moname.Split(':');
                        return $"MSC={monameParts[0]}{PiMonameSeparator}INNER_CELL={monameParts[1]}";
                    }
                case "OUTER_CELL":
                    {
                        string[] monameParts = moname.Split(':');
                        return $"MSC={monameParts[0]}{PiMonameSeparator}OUTER_CELL={monameParts[1]}";
                    }
                case "BSC":
                    {
                        return $"BSC={moname}";
                    }
                case "INTERNAL_CELL":
                    {
                        // BRP1B08:AKBAS1
                        // BSC=BRP1B08→INTERNAL_CELL=AKBAS1
                        string[] monameParts = moname.Split(':');
                        return $"BSC={monameParts[0]}{PiMonameSeparator}INTERNAL_CELL={monameParts[1]}";
                    }
                case "OVERLAID":
                    {
                        string[] monameParts = moname.Split(':');
                        return $"BSC={monameParts[0]}{PiMonameSeparator}INTERNAL_CELL={monameParts[1]}{PiMonameSeparator}OVERLAID={monameParts[2]}";
                    }
                case "CHANNEL_GROUP":
                    {
                        // BRP1B08:AKBAS1:0
                        // BSC=BRP1B08→INTERNAL_CELL=AKBAS1→CHANNEL_GROUP=0
                        string[] monameParts = moname.Split(':');
                        return $"BSC={monameParts[0]}{PiMonameSeparator}INTERNAL_CELL={monameParts[1]}{PiMonameSeparator}CHANNEL_GROUP={monameParts[2]}";
                    }
                case "NREL":
                    {
                        // BRP1B08:GOPLT2:BRP1B08:ORENT3
                        // BSC=BRP1B08→INTERNAL_CELL=GOPLT2→NREL=ORENT3
                        string[] monameParts = moname.Split(':');
                        return $"BSC={monameParts[0]}{PiMonameSeparator}INTERNAL_CELL={monameParts[1]}{PiMonameSeparator}NREL={monameParts[3]}";
                    }
                case "UTRAN_NREL":
                    {
                        // BRP1B08:AKBAS1:505:AKBAS15
                        // BSC=BRP1B08→INTERNAL_CELL=AKBAS1→UTRAN_NREL=AKBAS15
                        string[] monameParts = moname.Split(':');
                        return $"BSC={monameParts[0]}{PiMonameSeparator}INTERNAL_CELL={monameParts[1]}{PiMonameSeparator}UTRAN_NREL={monameParts[3]}";
                    }
                case "PRIORITY_PROFILE":
                    {
                        // BRP1B08:DEFAULT
                        // BSC=BRP1B08→PRIORITY_PROFILE=DEFAULT
                        string[] monameParts = moname.Split(':');
                        return $"BSC={monameParts[0]}{PiMonameSeparator}PRIORITY_PROFILE={monameParts[1]}";
                    }
                case "SITE":
                    {
                        // BRP1B08:BALAKCAY
                        // BSC=BRP1B08→SITE=BALAKCAY
                        string[] monameParts = moname.Split(':');
                        return $"BSC={monameParts[0]}{PiMonameSeparator}SITE={monameParts[1]}";
                    }
                case "TG":
                    {
                        // BRP1B08:RXOTG-0
                        // BSC=BRP1B08→TG=RXOTG-0
                        string[] monameParts = moname.Split(':');
                        return $"BSC={monameParts[0]}{PiMonameSeparator}TG={monameParts[1]}";
                    }
                case "EXTERNAL_CELL":
                    {
                        // BRP1B08:AHIEV2
                        // BSC=BRP1B08→EXTERNAL_CELL=AHIEV2
                        string[] monameParts = moname.Split(':');
                        return $"BSC={monameParts[0]}{PiMonameSeparator}EXTERNAL_CELL={monameParts[1]}";
                    }
                case "UTRAN_EXTERNAL_CELL":
                    {
                        // BRP1B08:3KISH21
                        // BSC=BRP1B08→UTRAN_EXTERNAL_CELL=3KISH21
                        string[] monameParts = moname.Split(':');
                        return $"BSC={monameParts[0]}{PiMonameSeparator}UTRAN_EXTERNAL_CELL={monameParts[1]}";
                    }
                case "RNC":
                    {
                        // 1000
                        // RNC=1000
                        return $"RNC={moname}";
                    }
                case "UTRAN_CELL":
                    {
                        // 102:AYYEN11
                        // RNC=102→UTRAN_NCELL=AYYEN11
                        string[] monameParts = moname.Split(':');
                        return $"RNC={monameParts[0]}{PiMonameSeparator}UTRAN_CELL={monameParts[1]}";
                    }
                case "FOREIGN_CELL":
                    {
                        // ABATI2
                        // FOREIGN_CELL=ABATI2
                        return $"FOREIGN_CELL={moname}";
                    }
                default:
                    throw new InvalidDataException();
            }
        }

        private static string GetParentPiMoname(string domain, string moname)
        {
            switch (domain)
            {
                case "MSC":
                    {
                        return null;
                    }
                case "INNER_CELL":
                    {
                        string[] monameParts = moname.Split(':');
                        return $"MSC={monameParts[0]}";
                    }
                case "OUTER_CELL":
                    {
                        string[] monameParts = moname.Split(':');
                        return $"MSC={monameParts[0]}";
                    }
                case "BSC":
                    {
                        return null;
                    }
                case "INTERNAL_CELL":
                    {
                        string[] monameParts = moname.Split(':');
                        return $"BSC={monameParts[0]}";
                    }
                case "OVERLAID":
                    {
                        string[] monameParts = moname.Split(':');
                        return $"BSC={monameParts[0]}{PiMonameSeparator}INTERNAL_CELL={monameParts[1]}";
                    }
                case "CHANNEL_GROUP":
                    {
                        string[] monameParts = moname.Split(':');
                        return $"BSC={monameParts[0]}{PiMonameSeparator}INTERNAL_CELL={monameParts[1]}";
                    }
                case "NREL":
                    {
                        string[] monameParts = moname.Split(':');
                        return $"BSC={monameParts[0]}{PiMonameSeparator}INTERNAL_CELL={monameParts[1]}";
                    }
                case "UTRAN_NREL":
                    {
                        string[] monameParts = moname.Split(':');
                        return $"BSC={monameParts[0]}{PiMonameSeparator}INTERNAL_CELL={monameParts[1]}";
                    }
                case "PRIORITY_PROFILE":
                    {
                        string[] monameParts = moname.Split(':');
                        return $"BSC={monameParts[0]}";
                    }
                case "SITE":
                    {
                        string[] monameParts = moname.Split(':');
                        return $"BSC={monameParts[0]}";
                    }
                case "TG":
                    {
                        string[] monameParts = moname.Split(':');
                        return $"BSC={monameParts[0]}";
                    }
                case "EXTERNAL_CELL":
                    {
                        string[] monameParts = moname.Split(':');
                        return $"BSC={monameParts[0]}";
                    }
                case "UTRAN_EXTERNAL_CELL":
                    {
                        string[] monameParts = moname.Split(':');
                        return $"BSC={monameParts[0]}";
                    }
                case "RNC":
                    {
                        return null;
                    }
                case "UTRAN_CELL":
                    {
                        string[] monameParts = moname.Split(':');
                        return $"RNC={monameParts[0]}";
                    }
                case "FOREIGN_CELL":
                    {
                        return null;
                    }
                default:
                    throw new InvalidDataException();
            }
        }

        private static string GetDisplayVsMoname(string domain, string moname)
        {
            return GetDisplayVsMonameAsLastPartOfPiMoname(domain, moname);
        }

        private static string GetFullDisplayVsMoname(string domain, string moname)
        {
            return $"{domain}={moname}";
        }

        private static string GetDisplayVsMonameAsLastPartOfPiMoname(string domain, string moname)
        {
            string[] monameParts = moname.Split(':');
            return $"{domain}={monameParts.Last()}";
        }

        private static string GetTruncatedDisplayVsMoname(string domain, string moname)
        {
            switch (domain)
            {
                case "MSC":
                    {
                        return domain + "=" + moname;
                    }
                case "INNER_CELL":
                    {
                        string[] monameParts = moname.Split(':');
                        return domain + "=" + monameParts[1];
                    }
                case "OUTER_CELL":
                    {
                        string[] monameParts = moname.Split(':');
                        return domain + "=" + monameParts[1];
                    }
                case "BSC":
                    {
                        return domain + "=" + moname;
                    }
                case "INTERNAL_CELL":
                    {
                        // BRP1B08:AKBAS1
                        // INTERNAL_CELL=AKBAS1
                        string[] monameParts = moname.Split(':');
                        return domain + "=" + monameParts[1];
                    }
                case "OVERLAID":
                    {
                        string[] monameParts = moname.Split(':');
                        return domain + "=" + monameParts[2];
                    }
                case "CHANNEL_GROUP":
                    {
                        // BRP1B08:AKBAS1:0
                        // CHANNEL_GROUP=0
                        string[] monameParts = moname.Split(':');
                        return domain + "=" + monameParts[2];
                    }
                case "NREL":
                    {
                        // BRP1B08:GOPLT2:BRP1B08:ORENT3
                        // NREL=BRP1B08:ORENT3
                        string[] monameParts = moname.Split(':');
                        return domain + "=" + string.Join(":", monameParts[2], monameParts[3]);
                    }
                case "UTRAN_NREL":
                    {
                        // BRP1B08:AKBAS1:505:AKBAS15
                        // UTRAN_NREL=505:AKBAS15
                        string[] monameParts = moname.Split(':');
                        return domain + "=" + string.Join(":", monameParts[2], monameParts[3]);
                    }
                case "PRIORITY_PROFILE":
                    {
                        // BRP1B08:DEFAULT
                        // PRIORITY_PROFILE=DEFAULT
                        string[] monameParts = moname.Split(':');
                        return domain + "=" + monameParts[1];
                    }
                case "SITE":
                    {
                        // BRP1B08:BALAKCAY
                        // SITE=BALAKCAY
                        string[] monameParts = moname.Split(':');
                        return domain + "=" + monameParts[1];
                    }
                case "TG":
                    {
                        // BRP1B08:RXOTG-0
                        // TG=RXOTG-0
                        string[] monameParts = moname.Split(':');
                        return domain + "=" + monameParts[1];
                    }
                case "EXTERNAL_CELL":
                    {
                        // BRP1B08:AHIEV2
                        // EXTERNAL_CELL=AHIEV2
                        string[] monameParts = moname.Split(':');
                        return domain + "=" + monameParts[1];
                    }
                case "UTRAN_EXTERNAL_CELL":
                    {
                        // BRP1B08:3KISH21
                        // UTRAN_EXTERNAL_CELL=3KISH21
                        string[] monameParts = moname.Split(':');
                        return domain + "=" + monameParts[1];
                    }
                case "RNC":
                    {
                        // 1000
                        // RNC=1000
                        return domain + "=" + moname;
                    }
                case "UTRAN_CELL":
                    {
                        // 102:AYYEN11
                        // UTRAN_CELL=AYYEN11
                        string[] monameParts = moname.Split(':');
                        return domain + "=" + monameParts[1];
                    }
                case "FOREIGN_CELL":
                    {
                        // ABATI2
                        // FOREIGN_CELL=ABATI2
                        return domain + "=" + moname;
                    }
                default:
                    throw new InvalidDataException();
            }
        }

        private static int GetTreeDepth(string domain) =>
            domain switch
            {
                "MSC" => 1,
                "INNER_CELL" => 2,
                "OUTER_CELL" => 2,
                "BSC" => 1,
                "INTERNAL_CELL" => 2,
                "OVERLAID" => 3,
                "CHANNEL_GROUP" => 3,
                "NREL" => 3,
                "UTRAN_NREL" => 3,
                "PRIORITY_PROFILE" => 2,
                "SITE" => 2,
                "TG" => 2,
                "EXTERNAL_CELL" => 2,
                "UTRAN_EXTERNAL_CELL" => 2,
                "RNC" => 1,
                "UTRAN_CELL" => 2,
                "FOREIGN_CELL" => 1,
                _ => throw new InvalidDataException()
            };

        private static string GetMoType(string domain) =>
            domain switch
            {
                "MSC" => "MSC",
                "INNER_CELL" => "MSC,INNER_CELL",
                "OUTER_CELL" => "MSC,OUTER_CELL",
                "BSC" => "BSC",
                "INTERNAL_CELL" => "BSC,INTERNAL_CELL",
                "OVERLAID" => "BSC,INTERNAL_CELL,OVERLAID",
                "CHANNEL_GROUP" => "BSC,INTERNAL_CELL,CHANNEL_GROUP",
                "NREL" => "BSC,INTERNAL_CELL,NREL",
                "UTRAN_NREL" => "BSC,INTERNAL_CELL,UTRAN_NREL",
                "PRIORITY_PROFILE" => "BSC,PRIORITY_PROFILE",
                "SITE" => "BSC,SITE",
                "TG" => "BSC,TG",
                "EXTERNAL_CELL" => "BSC,EXTERNAL_CELL",
                "UTRAN_EXTERNAL_CELL" => "BSC,UTRAN_EXTERNAL_CELL",
                "RNC" => "RNC",
                "UTRAN_CELL" => "RNC,UTRAN_CELL",
                "FOREIGN_CELL" => "FOREIGN_CELL",
                _ => throw new InvalidDataException()
            };
    }
}

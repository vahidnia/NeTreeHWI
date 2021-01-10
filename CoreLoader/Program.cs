using NUnit.Framework;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CoreLoader
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = args[0];

            string workingPath = args[1];
            string clid = args[2];
            string dataConnectionString = connectionString;
            string OID = args[3];
            string mmlPattern = "";
            if (args.Count() > 4)
                mmlPattern = args[4];


            var nodes = LoadExistingNodes(connectionString, clid);
            var configs = LoadExistingConfig(connectionString, clid);
            Console.WriteLine($"node loaded {nodes.Count}");

            var fileList = new DirectoryInfo(workingPath)
                          .GetFiles("*.*", SearchOption.TopDirectoryOnly)
                          .Where(a => (DateTime.Now - a.LastWriteTime).TotalMinutes > 10)
                          .Select(a => a.FullName)
                          .ToList();


            foreach (var item in fileList)
            {
                try
                {
                    Console.WriteLine(item);
                    Match timestampMatch = Regex.Match(item, @"(?<=TIMESTAMP=)\d+");
                    string datetimestr = timestampMatch.Value;

                    Match neMatch = Regex.Match(item, @"(?<=NE=)[^\+]+");
                    string node = neMatch.Value;

                    Match mmlMatch = Regex.Match(item, @"MML\s*=\s*(?<mml>.+?)($|\+|\.response)");
                    if (!mmlMatch.Success)
                    {
                        Console.WriteLine($"Unable to process this file: {item}");
                        MoveToburnt(item);
                        continue;
                    }
                    string MML = mmlMatch.Groups["mml"].Value;
                    if (!string.IsNullOrWhiteSpace(mmlPattern))
                        MML = Regex.Match(MML, mmlPattern).Value;

                    // Match fileNameRegex = Regex.Match(item, @"TYPE=(?<TYPE>.+?)\+NE=(?<NE>.+?)\+TIMESTAMP=(?<TIMESTAMP>.+?)\+MML=(?<MML>.+?)\.response");
                    // string node = fileNameRegex.Groups["NE"].Value;
                    // string datetimestr = fileNameRegex.Groups["TIMESTAMP"].Value;
                    //string MML = fileNameRegex.Groups["MML"].Value;
                    DateTime datetime = new DateTime();
                    if (datetimestr.Length == 15)
                        datetime = new DateTime(int.Parse(datetimestr.Substring(0, 4)), int.Parse(datetimestr.Substring(4, 2)), int.Parse(datetimestr.Substring(6, 2)),
                           int.Parse(datetimestr.Substring(9, 2)), int.Parse(datetimestr.Substring(11, 2)), int.Parse(datetimestr.Substring(13, 2)));
                    else if (datetimestr.Length == 14)
                        datetime = new DateTime(int.Parse(datetimestr.Substring(0, 4)), int.Parse(datetimestr.Substring(4, 2)), int.Parse(datetimestr.Substring(6, 2)),
                          int.Parse(datetimestr.Substring(8, 2)), int.Parse(datetimestr.Substring(10, 2)), int.Parse(datetimestr.Substring(12, 2)));
                    else
                        datetime = new DateTime(int.Parse(datetimestr.Substring(0, 4)), int.Parse(datetimestr.Substring(4, 2)), int.Parse(datetimestr.Substring(6, 2)),
                       0, 0, 0);


                    Console.WriteLine($"DateTime: {datetime}   NE: {node}   MML:{MML} ");
                    using (OracleConnection con = new OracleConnection(dataConnectionString))
                    {
                        con.Open();
                        if (!nodes.ContainsKey(node))
                            AddNode(connectionString, clid, node, nodes, OID);
                        if (!configs.ContainsKey(MML))
                            AddConfig(connectionString, clid, MML, configs, OID);

                        string commandText2 = @"INSERT INTO CNFX_TXTCM_DATA (CONFIGID, CLID, PK1, PK2, PK3, PK4, TEXTUALCONFIGDUMP, INSERTTIMESTAMP, DATADATETIME) VALUES (:P0, :P1, :P2,:P3,:P4,:P5,:P6,:P7,:P8)";
                        try
                        {
                            using (OracleCommand cmd2 = new OracleCommand(commandText2, con))
                            {
                                cmd2.Parameters.Add(":P0", OracleDbType.Decimal, 10);
                                cmd2.Parameters.Add(":P1", OracleDbType.Decimal, 10);
                                cmd2.Parameters.Add(":P2", OracleDbType.Varchar2, 255);
                                cmd2.Parameters.Add(":P3", OracleDbType.Varchar2, 255);
                                cmd2.Parameters.Add(":P4", OracleDbType.Varchar2, 255);
                                cmd2.Parameters.Add(":P5", OracleDbType.Varchar2, 255);
                                cmd2.Parameters.Add(":P6", OracleDbType.Clob, int.MaxValue);
                                cmd2.Parameters.Add(":P7", OracleDbType.TimeStamp, 255);
                                cmd2.Parameters.Add(":P8", OracleDbType.TimeStamp, 255);

                                //cmd.Parameters.Add(":P3", OracleDbType.Decimal, 10);
                                //cmd.Parameters.Add(":P4", OracleDbType.Clob, 100000);

                                cmd2.Prepare();

                                string clobValue = File.ReadAllText(item);

                                cmd2.Parameters[":P0"].Value = configs[MML];
                                cmd2.Parameters[":P1"].Value = clid;
                                cmd2.Parameters[":P2"].Value = nodes[node].ToString();
                                cmd2.Parameters[":P3"].Value = "";
                                cmd2.Parameters[":P4"].Value = "";
                                cmd2.Parameters[":P5"].Value = "";

                                cmd2.Parameters[":P6"].Value = clobValue;

                                cmd2.Parameters[":P7"].Value = DateTime.Now;
                                cmd2.Parameters[":P8"].Value = datetime;

                                //cmd.Parameters[":P2"].Value = $"TEST_{DateTime.Now:yyyyMMddHHmmss}";
                                //cmd.Parameters[":P3"].Value = clobValue.Length;
                                //cmd.Parameters[":P4"].Value = clobValue;



                                cmd2.ExecuteNonQuery();
                            }


                            string archive = Path.Combine(Path.GetDirectoryName(item), "archive");
                            if (!Directory.Exists(archive))
                                Directory.CreateDirectory(archive);
                            File.Move(item, Path.Combine(archive, Path.GetFileName(item)));
                            Console.WriteLine($"move to: archive");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                        finally
                        {
                            Console.WriteLine(item);
                        }

                        con.Close();
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("main fail");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    MoveToburnt(item);
                }
            }

        }

        private static void MoveToburnt(string item)
        {
            string burnt = Path.Combine(Path.GetDirectoryName(item), "burnt");
            if (!Directory.Exists(burnt))
                Directory.CreateDirectory(burnt);
            File.Move(item, Path.Combine(burnt, Path.GetFileName(item)));
            Console.WriteLine($"move to: burnt");
        }

        private static void AddConfig(string connectionString, string clid, string node, Dictionary<string, int> nodes, string oID)
        {
            var newId = int.Parse(clid);
            if (nodes.Count > 0)
                newId = nodes.Max(a => a.Value);
            else
                newId *= 100;
            newId++;

            using (OracleConnection con = new OracleConnection(connectionString))
            {
                con.Open();
                Assert.IsNotNull(con);

                //CONFIGFULLNAME
                string commandText = @"INSERT INTO CNFX_TXTCM_CONFIG (CONFIGID,CONFIGDISPLAYNAME,CLID,CONFIGFULLNAME) VALUES (:P0, :P1, :P2, :P3)";

                using (OracleCommand cmd = new OracleCommand(commandText, con))
                {
                    cmd.Parameters.Add(":P0", OracleDbType.Decimal, 10);
                    cmd.Parameters.Add(":P1", OracleDbType.Varchar2, 200);
                    cmd.Parameters.Add(":P2", OracleDbType.Decimal, 10);
                    cmd.Parameters.Add(":P3", OracleDbType.Varchar2, 200);

                    cmd.Prepare();

                    cmd.Parameters[":P0"].Value = newId;
                    cmd.Parameters[":P1"].Value = node;
                    cmd.Parameters[":P2"].Value = clid;
                    cmd.Parameters[":P3"].Value = node;

                    cmd.ExecuteNonQuery();
                }
            }

            nodes.Add(node, newId);

        }

        private static Dictionary<string, int> LoadExistingConfig(string connectionString, string clid)
        {
            using (OracleConnection con = new OracleConnection(connectionString))
            {
                con.Open();
                Assert.IsNotNull(con);

                string commandText = $"SELECT  CONFIGFULLNAME,CONFIGID  FROM CNFX_TXTCM_CONFIG  WHERE CLID  = {clid}";
                Dictionary<string, int> configs = new Dictionary<string, int>();


                using (OracleCommand cmd = new OracleCommand(commandText, con))
                {
                    var drReader = cmd.ExecuteReader();

                    while (drReader.Read())
                    {
                        configs.Add(drReader.GetString(0), drReader.GetInt32(1));
                    }

                }
                return configs;
            }
        }

        private static Dictionary<string, int> LoadExistingNodes(string connectionString, string clid)
        {
            using (OracleConnection con = new OracleConnection(connectionString))
            {
                con.Open();
                Assert.IsNotNull(con);

                string commandText = $"SELECT NENAME,NEID  FROM CUST_ALL_CORE_NES WHERE clid = {clid}";
                Dictionary<string, int> nodes = new Dictionary<string, int>();


                using (OracleCommand cmd = new OracleCommand(commandText, con))
                {
                    var drReader = cmd.ExecuteReader();

                    while (drReader.Read())
                    {
                        nodes.Add(drReader.GetString(0), drReader.GetInt32(1));
                    }

                }
                return nodes;
            }
        }


        private static void AddNode(string connectionString, string CLID, string node, Dictionary<string, int> nodes, string OID)
        {
            var newId = 0;
            if (nodes.Count > 0)
                newId = nodes.Max(a => a.Value);
            newId++;

            using (OracleConnection con = new OracleConnection(connectionString))
            {
                con.Open();
                Assert.IsNotNull(con);

                OracleCommand loCmd = con.CreateCommand();
                loCmd.CommandType = CommandType.Text;
                loCmd.CommandText = "select CUST_ALL_CORE_NES_SEQ.NEXTVAL from dual";
                long NodeID = Convert.ToInt64(loCmd.ExecuteScalar());

                string commandText = @"INSERT INTO CUST_ALL_CORE_NES (OID,NODEID,NENAME,CLID,NEID) VALUES (:P0, :P1, :P2, :P3, :P4)";

                using (OracleCommand cmd = new OracleCommand(commandText, con))
                {
                    cmd.Parameters.Add(":P0", OracleDbType.Decimal, 10);
                    cmd.Parameters.Add(":P1", OracleDbType.Decimal, 10);
                    cmd.Parameters.Add(":P2", OracleDbType.Varchar2, 100);
                    cmd.Parameters.Add(":P3", OracleDbType.Decimal, 10);
                    cmd.Parameters.Add(":P4", OracleDbType.Decimal, 10);

                    cmd.Prepare();

                    cmd.Parameters[":P0"].Value = OID;
                    cmd.Parameters[":P1"].Value = CLID;
                    cmd.Parameters[":P2"].Value = node;
                    cmd.Parameters[":P3"].Value = CLID;
                    cmd.Parameters[":P4"].Value = newId;
                    cmd.ExecuteNonQuery();
                }
            }

            nodes.Add(node, newId);

        }
    }

}
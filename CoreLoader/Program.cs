using NUnit.Framework;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;

namespace CoreLoader
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = args[0];
            string workingPath = args[1];

            var nodes = LoadExistingNodes(connectionString);

            foreach (var item in Directory.GetFiles(workingPath))
            {
                Match fileNameRegex = Regex.Match(item, @"TYPE=(?<TYPE>.+?)\+NE=(?<NE>.+?)\+TIMESTAMP=(?<TIMESTAMP>.+?)\+MML=(?<MML>.+)");
                string node = fileNameRegex.Groups["NE"].Value;
                string datetimestr = fileNameRegex.Groups["TIMESTAMP"].Value;
                var datetime = new DateTime(int.Parse(datetimestr.Substring(0, 4)), int.Parse(datetimestr.Substring(4, 2)), int.Parse(datetimestr.Substring(6, 2)),
                    int.Parse(datetimestr.Substring(9, 2)), int.Parse(datetimestr.Substring(11, 2)), int.Parse(datetimestr.Substring(13, 2)));

                using (OracleConnection con = new OracleConnection(connectionString))
                {
                    con.Open();
                    if (!nodes.ContainsKey(node))
                        AddNode(connectionString, 400, node, nodes);

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
                            cmd2.Parameters.Add(":P6", OracleDbType.Clob, 100000);
                            cmd2.Parameters.Add(":P7", OracleDbType.TimeStamp, 255);
                            cmd2.Parameters.Add(":P8", OracleDbType.TimeStamp, 255);

                            //cmd.Parameters.Add(":P3", OracleDbType.Decimal, 10);
                            ///cmd.Parameters.Add(":P4", OracleDbType.Clob, 100000);

                            cmd2.Prepare();

                            string clobValue = File.ReadAllText(item);

                            cmd2.Parameters[":P0"].Value = nodes[node];
                            cmd2.Parameters[":P1"].Value = 400;
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

        }



        private static Dictionary<string, int> LoadExistingNodes(string connectionString)
        {
            using (OracleConnection con = new OracleConnection(connectionString))
            {
                con.Open();
                Assert.IsNotNull(con);

                string commandText = @"SELECT *  FROM CNFX_TXTCM_CONFIG";
                Dictionary<string, int> nodes = new Dictionary<string, int>();


                using (OracleCommand cmd = new OracleCommand(commandText, con))
                {
                    var drReader = cmd.ExecuteReader();

                    while (drReader.Read())
                    {
                        nodes.Add(drReader.GetString(2), drReader.GetInt32(0));
                    }

                }
                return nodes;
            }
        }


        private static Dictionary<string, int> AddNode(string connectionString, int CLID, string node, Dictionary<string, int> nodes)
        {
            var newId = nodes.Max(a => a.Value);
            newId++;


            using (OracleConnection con = new OracleConnection(connectionString))
            {
                con.Open();
                Assert.IsNotNull(con);

                //string commandText = @"INSERT INTO CNFX_TXTCM_CONFIG (CONFIGID, CLID, CONFIGDISPLAYNAME) VALUES (:P0, :P1, :P2, :P3, TO_CLOB(:P4))";
                string commandText = @"INSERT INTO CNFX_TXTCM_CONFIG (CONFIGID, CLID, CONFIGDISPLAYNAME) VALUES (:P0, :P1, :P2)";

                using (OracleCommand cmd = new OracleCommand(commandText, con))
                {
                    cmd.Parameters.Add(":P0", OracleDbType.Decimal, 10);
                    cmd.Parameters.Add(":P1", OracleDbType.Decimal, 10);
                    cmd.Parameters.Add(":P2", OracleDbType.Varchar2, 255);
                    //cmd.Parameters.Add(":P3", OracleDbType.Decimal, 10);
                    ///cmd.Parameters.Add(":P4", OracleDbType.Clob, 100000);

                    cmd.Prepare();

                    cmd.Parameters[":P0"].Value = newId;
                    cmd.Parameters[":P1"].Value = 400;
                    //cmd.Parameters[":P2"].Value = fileNameRegex.Groups["NE"].Value;
                    cmd.Parameters[":P2"].Value = node;

                    cmd.ExecuteNonQuery();
                }
            }

            nodes.Add(node, newId);
            return nodes;
        }
    }

}
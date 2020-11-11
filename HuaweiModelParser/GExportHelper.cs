using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using SQLitePCL;

namespace HuaweiModelParser
{
    public static class GExportHelper
    {
        public static void ConvertGExportFile(string gExportFilePath, sqlite3 db, Func<string, string, HuaweiModel> createHuaweiModule)
        {
            string gExportFileName = Path.GetFileName(gExportFilePath);
            string neName = Regex.Match(gExportFileName, @"(?<=GExport_).+(?=_\d+\.\d+\.\d+\.\d+_)").Value;
            if (gExportFilePath.EndsWith(".gz", StringComparison.Ordinal))
            {
                using (FileStream fileStream = File.OpenRead(gExportFilePath))
                using (GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
                    ConvertGExportStream(neName, gzipStream, db, createHuaweiModule);
            }
            else
            {
                using (FileStream fileStream = File.OpenRead(gExportFilePath))
                    ConvertGExportStream(neName, fileStream, db, createHuaweiModule);
            }

        }

        public static void ConvertGExportStream(string neName, Stream xmlStream, sqlite3 db, Func<string, string, HuaweiModel> createHuaweiModel)
        {
            Stopwatch watch = new Stopwatch();
            Guid guid;
            string tempParamTableName;
            string tempTreeTableName;
            if (true)
            {
                guid = Guid.NewGuid();
                tempParamTableName = $"temp_param_{guid:N}";
                tempTreeTableName = $"temp_tree_{guid:N}";
                raw.sqlite3_exec(db, $"CREATE TABLE {tempParamTableName}(NE TEXT,MOTYPE TEXT,MOID TEXT,PARAMETERNAME TEXT,PARAMETERVALUE TEXT)");
                raw.sqlite3_exec(db, $"CREATE TABLE {tempTreeTableName}(NE TEXT,MOTYPE TEXT,MOID TEXT,PARENTMOTYPE TEXT,PARENTMOID TEXT,MOIDORDERED TEXT,MOIDORDEREDFILTERED TEXT,MOIDFORJOIN TEXT,PARENTMOIDFORJOIN TEXT)");

                raw.sqlite3_prepare_v2(db, $"INSERT INTO {tempParamTableName} VALUES(?,?,?,?,?)", out sqlite3_stmt insertTempParamRecordStmt);
                raw.sqlite3_prepare_v2(db, $"INSERT INTO {tempTreeTableName} VALUES(?,?,?,?,?,?,?,?,?)", out sqlite3_stmt insertTempTreeRecordStmt);

                // populate raw KVP table
                raw.sqlite3_exec(db, "BEGIN TRANSACTION");
                XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Ignore,
                    IgnoreComments = true,
                    IgnoreProcessingInstructions = true,
                    IgnoreWhitespace = true
                };
                using (XmlReader xmlReader = XmlReader.Create(xmlStream, xmlReaderSettings))
                {
                    xmlReader.ReadToDescendant("bulkCmConfigDataFile");
                    xmlReader.ReadToDescendant("configData");
                    xmlReader.ReadToDescendant("class");
                    string modelName = xmlReader.GetAttribute("name");
                    xmlReader.ReadToDescendant("object");
                    string modelVersion = xmlReader.GetAttribute("version");
                    HuaweiModel huaweiModel = createHuaweiModel(modelName, modelVersion);
                    xmlReader.ReadToDescendant("class");
                    while (string.Equals(xmlReader.Name, "class", StringComparison.Ordinal))
                    {
                        string className = xmlReader.GetAttribute("name");
                        HuaweiModelClass huaweiModelClass = huaweiModel.GetHuaweiModelClassUsingGExportClassName(className);
                        if (huaweiModelClass == null)
                        {
                            Console.WriteLine($"Missed Huawei model: Class Name={className}");
                            xmlReader.Skip();
                            continue;
                        }
                        HuaweiModelClassAggr huaweiModelClassAggr = huaweiModel.GetHuaweiModelClassAggr(huaweiModelClass);
                        if (huaweiModelClassAggr == null)
                        {
                            Console.WriteLine($"Missed Huawei model aggregator: Class Name={className}");
                            xmlReader.Skip();
                            continue;
                        }
                        xmlReader.Read();
                        if (string.Equals(xmlReader.Name, "object", StringComparison.Ordinal))
                        {
                            while (string.Equals(xmlReader.Name, "object", StringComparison.Ordinal))
                            {
                                XElement xObject = (XElement)XNode.ReadFrom(xmlReader);
                                List<KeyValuePair<string, string>> parameters = new List<KeyValuePair<string, string>>();
                                foreach (XElement xParameter in xObject.Elements("parameter"))
                                {
                                    string parameterName = xParameter.Attribute("name").Value;
                                    if (string.Equals(parameterName, "OBJID", StringComparison.Ordinal) ||
                                        string.Equals(parameterName, "IDTYPE", StringComparison.Ordinal))
                                    {
                                        continue;
                                    }
                                    string parameterValue = xParameter.Attribute("value").Value;
                                    huaweiModelClass.AttrByAttrName.TryGetValue(parameterName, out HuaweiModelClassAttr huaweiModelClassAttr);
                                    bool isSwitch = huaweiModelClassAttr != null && huaweiModelClassAttr.AttrType.StartsWith("BitMap_");
                                    if (isSwitch)
                                    {
                                        foreach (string parameterSwitch in parameterValue.Split('&'))
                                        {
                                            string[] parameterSwitchParts = parameterSwitch.Split('-');
                                            string parameterSwitchName = parameterName + "." + parameterSwitchParts[0];
                                            string parameterSwitchValue = parameterSwitchParts[1];
                                            parameters.Add(new KeyValuePair<string, string>(parameterSwitchName, parameterSwitchValue));
                                        }
                                    }
                                    else
                                    {
                                        parameters.Add(new KeyValuePair<string, string>(parameterName, parameterValue));
                                    }
                                }
                                List<KeyValuePair<string, string>> moidParts = new List<KeyValuePair<string, string>>();
                                List<KeyValuePair<string, string>> moidPartsFiltered = new List<KeyValuePair<string, string>>();
                                List<KeyValuePair<string, string>> moidPartsForJoin = new List<KeyValuePair<string, string>>();
                                foreach (var keyAttr in huaweiModelClass.Attrs.Where(o => o.IsKey).OrderBy(o => o.DspOrder))
                                {
                                    KeyValuePair<string, string> moidPart;
                                    if (keyAttr.Mandatory && keyAttr.AttrName != "IDTYPE")
                                    {
                                        moidPart = parameters.Where(o => string.Equals(o.Key, keyAttr.AttrName)).Single();
                                    }
                                    else
                                    {
                                        moidPart = parameters.Where(o => string.Equals(o.Key, keyAttr.AttrName)).SingleOrDefault();
                                    }
                                    if (moidPart.Key != null)
                                    {
                                        moidParts.Add(moidPart);
                                        if (!huaweiModelClassAggr.AggrAttrs.Any(o => string.Equals(o.CAttr, moidPart.Key)))
                                            moidPartsFiltered.Add(moidPart);
                                        if (huaweiModelClass.PkAttrsUsedByChildrenPks.Any(o => o.AttrName == keyAttr.AttrName))
                                            moidPartsForJoin.Add(moidPart);
                                    }
                                }
                                // we intentionally put empty string so we can join empty moids of root objects easier
                                string moid = string.Join(",", moidParts.OrderBy(o => o.Key).Select(o => $"{o.Key}={o.Value}")); // order by key to map with parent easily
                                string moidOrdered = string.Join(",", moidParts.Select(o => $"{o.Key}={o.Value}"));
                                string moidOrderedFiltered = string.Join(",", moidPartsFiltered.Select(o => $"{o.Key}={o.Value}"));
                                string moidForJoin = string.Join(",", moidPartsForJoin.OrderBy(o => o.Key).Select(o => $"{o.Key}={o.Value}"));
                                foreach (KeyValuePair<string, string> parameter in parameters)
                                {
                                    raw.sqlite3_bind_text(insertTempParamRecordStmt, 1, neName);
                                    raw.sqlite3_bind_text(insertTempParamRecordStmt, 2, huaweiModelClass.ClassName);
                                    raw.sqlite3_bind_text(insertTempParamRecordStmt, 3, moid);
                                    raw.sqlite3_bind_text(insertTempParamRecordStmt, 4, parameter.Key);
                                    raw.sqlite3_bind_text(insertTempParamRecordStmt, 5, parameter.Value);
                                    raw.sqlite3_step(insertTempParamRecordStmt);
                                    raw.sqlite3_reset(insertTempParamRecordStmt);
                                }

                                List<KeyValuePair<string, string>> parentMoidParts = new List<KeyValuePair<string, string>>();
                                List<KeyValuePair<string, string>> parentMoidPartsForJoin = new List<KeyValuePair<string, string>>();
                                foreach (HuaweiModelClassAggrAttr parentKeyAttr in huaweiModelClassAggr.AggrAttrs.OrderBy(o => o.PAttr))
                                {
                                    var keyAttr = huaweiModelClass.Attrs.Where(o => string.Equals(o.AttrName, parentKeyAttr.CAttr)).Single();
                                    string moidPartValue;
                                    if (keyAttr.Mandatory)
                                    {
                                        moidPartValue = parameters.Where(o => o.Key == parentKeyAttr.CAttr).Select(o => o.Value).Single();
                                    }
                                    else
                                    {
                                        moidPartValue = parameters.Where(o => o.Key == parentKeyAttr.CAttr).Select(o => o.Value).SingleOrDefault();
                                    }
                                    if (moidPartValue != null)
                                    {
                                        parentMoidParts.Add(
                                            new KeyValuePair<string, string>(parentKeyAttr.PAttr, moidPartValue));
                                        if (keyAttr.IsCfgAttr)
                                            parentMoidPartsForJoin.Add(
                                                new KeyValuePair<string, string>(parentKeyAttr.PAttr, moidPartValue));
                                    }
                                }
                                // we intentionally put empty string so we can join empty moids of root objects easier
                                string parentMoid = string.Join(",", parentMoidParts.Select(o => $"{o.Key}={o.Value}"));
                                string parentMoidForJoin = string.Join(",", parentMoidPartsForJoin.Select(o => $"{o.Key}={o.Value}"));
                                raw.sqlite3_bind_text(insertTempTreeRecordStmt, 1, neName);
                                raw.sqlite3_bind_text(insertTempTreeRecordStmt, 2, huaweiModelClass.ClassName);
                                raw.sqlite3_bind_text(insertTempTreeRecordStmt, 3, moid);
                                raw.sqlite3_bind_text(insertTempTreeRecordStmt, 4, huaweiModelClassAggr.ParentClass);
                                raw.sqlite3_bind_text(insertTempTreeRecordStmt, 5, parentMoid);
                                raw.sqlite3_bind_text(insertTempTreeRecordStmt, 6, moidOrdered);
                                raw.sqlite3_bind_text(insertTempTreeRecordStmt, 7, moidOrderedFiltered);
                                raw.sqlite3_bind_text(insertTempTreeRecordStmt, 8, moidForJoin);
                                raw.sqlite3_bind_text(insertTempTreeRecordStmt, 9, parentMoidForJoin);
                                raw.sqlite3_step(insertTempTreeRecordStmt);
                                raw.sqlite3_reset(insertTempTreeRecordStmt);
                            }
                        }
                        xmlReader.ReadEndElement();
                    }
                }
                raw.sqlite3_exec(db, "COMMIT");

                raw.sqlite3_finalize(insertTempParamRecordStmt);
                raw.sqlite3_finalize(insertTempTreeRecordStmt);

                // convert temp tables to final tables
                //raw.sqlite3_exec(db, $"CREATE INDEX ix_{tempParamTableName} on {tempParamTableName}(NE,MOTYPE,MOID)");
                raw.sqlite3_exec(db, $"CREATE INDEX ix_{tempTreeTableName} on {tempTreeTableName}(NE,MOTYPE,MOIDFORJOIN)");
                raw.sqlite3_exec(db, "analyze");
            }
            else
            {
                guid = Guid.Parse("2d42953e80d24c5698e889b78aa71db3");
                tempParamTableName = $"temp_param_{guid:N}";
                tempTreeTableName = $"temp_tree_{guid:N}";
            }

            // populate vs_cm_tree
            HashSet<(string moType, string ne)> rootMos = new HashSet<(string moType, string ne)>();
            raw.sqlite3_exec(db, "CREATE TABLE vs_cm_tree(treelementclass TEXT,treedepth INTEGER,parentpimoname TEXT,pimoname TEXT,vsmoname TEXT,displayvsmoname TEXT,motype TEXT,TEMP_NE TEXT,TEMP_MOTYPE TEXT, TEMP_MOID TEXT)");
            raw.sqlite3_exec(db, "BEGIN TRANSACTION");
            raw.sqlite3_prepare_v2(db, "INSERT INTO vs_cm_tree VALUES(?,?,?,?,?,?,?,?,?,?)", out sqlite3_stmt insertVsCmTreeStmt);
            raw.sqlite3_prepare_v2(db, $"SELECT * FROM {tempTreeTableName}", out sqlite3_stmt selectTempTreeRecordStmt);
            raw.sqlite3_prepare_v2(db, $"SELECT * FROM {tempTreeTableName} WHERE NE=? AND MOTYPE=? and MOIDFORJOIN=?", out sqlite3_stmt searchTempTreeRecordStmt);
            while (raw.sqlite3_step(selectTempTreeRecordStmt) == raw.SQLITE_ROW)
            {
                string ne = raw.sqlite3_column_text(selectTempTreeRecordStmt, 0).utf8_to_string();
                string moType = raw.sqlite3_column_text(selectTempTreeRecordStmt, 1).utf8_to_string();
                string moId = raw.sqlite3_column_text(selectTempTreeRecordStmt, 2).utf8_to_string();
                string parentMoType = raw.sqlite3_column_text(selectTempTreeRecordStmt, 3).utf8_to_string();
                string parentMoId = raw.sqlite3_column_text(selectTempTreeRecordStmt, 4).utf8_to_string();
                string moIdOrdered = raw.sqlite3_column_text(selectTempTreeRecordStmt, 5).utf8_to_string();
                string moIdOrderedFiltered = raw.sqlite3_column_text(selectTempTreeRecordStmt, 6).utf8_to_string();
                string moIdForJoin = raw.sqlite3_column_text(selectTempTreeRecordStmt, 7).utf8_to_string();
                string parentMoIdForJoin = raw.sqlite3_column_text(selectTempTreeRecordStmt, 8).utf8_to_string();

                List<(string Ne, string MoType, string MoId, string ParentMoType, string ParentMoId, string MoIdOrdered, string MoIdOrderedFiltered, string MoIdForJoin, string ParentMoIdForJoin)> path =
                    new List<(string Ne, string MoType, string MoId, string ParentMoType, string ParentMoId, string MoIdOrdered, string MoIdOrderedFiltered, string MoIdForJoin, string ParentMoIdForJoin)>();
                var pathLastPart = (ne, moType, moId, parentMoType, parentMoId, moIdOrdered, moIdOrderedFiltered, moIdForJoin, parentMoIdForJoin);
                path.Add(pathLastPart);

                while (true)
                {
                    raw.sqlite3_bind_text(searchTempTreeRecordStmt, 1, ne);
                    raw.sqlite3_bind_text(searchTempTreeRecordStmt, 2, parentMoType);
                    raw.sqlite3_bind_text(searchTempTreeRecordStmt, 3, parentMoId);
                    bool parentIsFound = raw.sqlite3_step(searchTempTreeRecordStmt) == raw.SQLITE_ROW;
                    if (parentIsFound)
                    {
                        ne = raw.sqlite3_column_text(searchTempTreeRecordStmt, 0).utf8_to_string();
                        moType = raw.sqlite3_column_text(searchTempTreeRecordStmt, 1).utf8_to_string();
                        moId = raw.sqlite3_column_text(searchTempTreeRecordStmt, 2).utf8_to_string();
                        parentMoType = raw.sqlite3_column_text(searchTempTreeRecordStmt, 3).utf8_to_string();
                        parentMoId = raw.sqlite3_column_text(searchTempTreeRecordStmt, 4).utf8_to_string();
                        moIdOrdered = raw.sqlite3_column_text(searchTempTreeRecordStmt, 5).utf8_to_string();
                        moIdOrderedFiltered = raw.sqlite3_column_text(searchTempTreeRecordStmt, 6).utf8_to_string();
                        moIdForJoin = raw.sqlite3_column_text(selectTempTreeRecordStmt, 7).utf8_to_string();
                        parentMoIdForJoin = raw.sqlite3_column_text(selectTempTreeRecordStmt, 8).utf8_to_string();
                        path.Add((ne, moType, moId, parentMoType, parentMoId, moIdOrdered, moIdOrderedFiltered, moIdForJoin, parentMoIdForJoin));
                        raw.sqlite3_reset(searchTempTreeRecordStmt);
                    }
                    else
                    {
                        raw.sqlite3_reset(searchTempTreeRecordStmt);
                        break;
                    }
                }

                path.Reverse();

                string rootMoType = path[0].ParentMoType;
                // TODO: perform better validation that it is really root
                if (!rootMoType.StartsWith("BSC6"))
                    continue;
                rootMos.Add((rootMoType, ne));

                string chPiMoname =
                    rootMoType + "=" + ne + "→" +
                    string.Join("→", path.Select(o => string.IsNullOrEmpty(o.MoIdOrderedFiltered) ? o.MoType : $"{o.MoType}={o.MoIdOrderedFiltered.Replace('=', ':')}"));
                string chParentPiMoname =
                    rootMoType + "=" + ne + (path.Count > 1 ? "→" : "") +
                    string.Join("→", path.SkipLast(1).Select(o => string.IsNullOrEmpty(o.MoIdOrderedFiltered) ? o.MoType : $"{o.MoType}={o.MoIdOrderedFiltered.Replace('=', ':')}"));
                string chVsMoname =
                    string.IsNullOrEmpty(pathLastPart.moIdOrdered)
                        ? $"{ne}/{pathLastPart.moType}"
                        : $"{ne}/{pathLastPart.moType}:{pathLastPart.moIdOrdered}";
                string chMoType =
                    rootMoType + "," +
                    string.Join(",", path.Select(o => o.MoType));
                string chDisplayVsMoname =
                    string.IsNullOrEmpty(pathLastPart.moIdOrderedFiltered)
                        ? pathLastPart.moType
                        : pathLastPart.moType + "=" + pathLastPart.moIdOrderedFiltered.Replace('=', ':');

                raw.sqlite3_bind_text(insertVsCmTreeStmt, 1, pathLastPart.moType);
                raw.sqlite3_bind_int(insertVsCmTreeStmt, 2, path.Count + 1);
                raw.sqlite3_bind_text(insertVsCmTreeStmt, 3, chParentPiMoname);
                raw.sqlite3_bind_text(insertVsCmTreeStmt, 4, chPiMoname);
                raw.sqlite3_bind_text(insertVsCmTreeStmt, 5, chVsMoname);
                raw.sqlite3_bind_text(insertVsCmTreeStmt, 6, chDisplayVsMoname);
                raw.sqlite3_bind_text(insertVsCmTreeStmt, 7, chMoType);
                raw.sqlite3_bind_text(insertVsCmTreeStmt, 8, pathLastPart.ne);
                raw.sqlite3_bind_text(insertVsCmTreeStmt, 9, pathLastPart.moType);
                raw.sqlite3_bind_text(insertVsCmTreeStmt, 10, pathLastPart.moId);
                raw.sqlite3_step(insertVsCmTreeStmt);
                raw.sqlite3_reset(insertVsCmTreeStmt);
            }
            foreach (var rootMo in rootMos) // TODO: validate that there is only single root mo type
            {
                raw.sqlite3_bind_text(insertVsCmTreeStmt, 1, rootMo.moType);
                raw.sqlite3_bind_int(insertVsCmTreeStmt, 2, 1);
                raw.sqlite3_bind_text(insertVsCmTreeStmt, 3, string.Empty);
                raw.sqlite3_bind_text(insertVsCmTreeStmt, 4, rootMo.moType + "=" + rootMo.ne);
                raw.sqlite3_bind_text(insertVsCmTreeStmt, 5, rootMo.ne);
                raw.sqlite3_bind_text(insertVsCmTreeStmt, 6, string.Empty);
                raw.sqlite3_bind_text(insertVsCmTreeStmt, 7, rootMo.moType);
                raw.sqlite3_bind_null(insertVsCmTreeStmt, 8);
                raw.sqlite3_bind_null(insertVsCmTreeStmt, 9);
                raw.sqlite3_bind_null(insertVsCmTreeStmt, 10);
                raw.sqlite3_step(insertVsCmTreeStmt);
                raw.sqlite3_reset(insertVsCmTreeStmt);
            }
            raw.sqlite3_finalize(insertVsCmTreeStmt);
            raw.sqlite3_finalize(selectTempTreeRecordStmt);
            raw.sqlite3_finalize(searchTempTreeRecordStmt);
            raw.sqlite3_exec(db, "COMMIT");

            // populate vs_cm_data
            raw.sqlite3_exec(db, "CREATE TABLE vs_cm_data(pimoname TEXT,vsmoname TEXT,motype TEXT,paramname TEXT,paramvalue TEXT)");
            raw.sqlite3_exec(db, "CREATE INDEX ix_vs_cm_tree ON vs_cm_tree(TEMP_NE,TEMP_MOTYPE,TEMP_MOID)");
            raw.sqlite3_exec(db, "analyze ix_vs_cm_tree");
            Console.WriteLine("Started populating vs_cm_data");
            watch.Restart();
            raw.sqlite3_exec(db, $"INSERT INTO vs_cm_data SELECT t.pimoname,t.vsmoname,t.motype,p.PARAMETERNAME,p.PARAMETERVALUE FROM {tempParamTableName} p JOIN vs_cm_tree t ON p.NE=t.TEMP_NE and p.MOTYPE=t.TEMP_MOTYPE and p.MOID=t.TEMP_MOID");
            raw.sqlite3_exec(db, $"INSERT INTO vs_cm_data (pimoname,vsmoname,motype,paramname,paramvalue) SELECT pimoname, vsmoname,motype,'pifiller-k' paramname,'pifiller-v' paramvalue  from vs_cm_tree");
            watch.Stop();
            Console.WriteLine($"Finished populating vs_cm_data. Elapsed time: {watch.Elapsed}");
        }
    }
}

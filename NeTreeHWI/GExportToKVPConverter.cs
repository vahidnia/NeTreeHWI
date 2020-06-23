using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace GExportToKVP
{
    internal static class GExportToKVPConverter
    {
        public static void Convert(Stream xmlStream,
            string dbFilePath,
            string ne,
            bool removeClassNameSuffix,
            List<StreamWriter> streamWriter,
            Dictionary<string, Dictionary<string, int>> columnIndices,
            Dictionary<string, Model> models)
        {
            Dictionary<string, string> pimonameDic = new Dictionary<string, string>();
            string fileDate = "2020-04-06 06:00:00";
            var ossid = 46; //HAVA


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
                xmlReader.ReadToDescendant("object"); // TODO: extract 'technique'?

                string version = xmlReader.GetAttribute("version");

                xmlReader.ReadToDescendant("class");



                while (string.Equals(xmlReader.Name, "class", StringComparison.Ordinal))
                {
                    string className = xmlReader.GetAttribute("name");
                    if (removeClassNameSuffix)
                    {
                        className = className.Split('_')[0];
                    }
                    xmlReader.Read();
                    if (string.Equals(xmlReader.Name, "object", StringComparison.Ordinal))
                    {
                        while (string.Equals(xmlReader.Name, "object", StringComparison.Ordinal))
                        {
                            XElement xObject = (XElement)XNode.ReadFrom(xmlReader);
                            Dictionary<string, string> parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "NE", ne } };
                            foreach (XElement xParameter in xObject.Elements("parameter"))
                            {
                                string parameterName = xParameter.Attribute("name").Value;
                                string parameterValue = xParameter.Attribute("value").Value;
                                parameters.Add(parameterName, parameterValue);
                                bool isSwitchParameter =
                                    parameterValue.EndsWith("SW-0") ||
                                    parameterValue.EndsWith("SW-1") ||
                                    parameterValue.EndsWith("SWITCH-0") ||
                                    parameterValue.EndsWith("SWITCH-1") ||
                                    // handle CAALGOSWITCH having value INTRA_BAND_CA_SW-0 in NRDUCELLALGOSWITCH_BTS59005G
                                    (parameterName.EndsWith("SWITCH", StringComparison.OrdinalIgnoreCase) && (parameterValue.EndsWith("-0", StringComparison.Ordinal) || parameterValue.EndsWith("-1", StringComparison.Ordinal))) ||
                                    // handle SRSCOORDSCHSW having value UL_SRSCOORDSCH_SW-0 in NRDUCELLSRS_BTS59005G
                                    (parameterName.EndsWith("SW", StringComparison.OrdinalIgnoreCase) && (parameterValue.EndsWith("_SW-0", StringComparison.OrdinalIgnoreCase) || parameterValue.EndsWith("_SW-1", StringComparison.OrdinalIgnoreCase)));
                                bool needToSplitParameter =
                                    isSwitchParameter &&
                                    !parameterName.StartsWith("RSVSWITCH", StringComparison.Ordinal) &&
                                    !parameterName.StartsWith("RSVDPARA", StringComparison.Ordinal);
                                if (needToSplitParameter)
                                {
                                    foreach (string parameterSwitch in parameterValue.Split('&'))
                                    {
                                        string[] parameterSwitchNameAndValue = parameterSwitch.Split('-');
                                        string parameterSwitchName = parameterSwitchNameAndValue[0];
                                        string parameterSwitchValue = parameterSwitchNameAndValue[1];
                                        parameters.Add(parameterName + "." + parameterSwitchName, parameterSwitchValue);
                                    }
                                }
                            }
                            if (parameters.Count > 1) // > 1 because NE is always there
                            {

                                string omcName = "NA";
                                string neName = "NA";


                                foreach (KeyValuePair<string, string> parameter in parameters)
                                {
                                    string vsmoname = "NA";
                                    Boolean key = false;
                                    string pimoname = "NA";
                                    string motype = "NA";



                                    // foreach (var item in models.Keys.Where(a => a.Contains("OSS_BTS3900_MATCH_ENG_V300R019C10SPC210")))
                                    foreach (var item in models.Values.Where(a => a.DisplayVersion == version))

                                    {
                                        //var model = models[item];
                                        var model = item;
                                        if (!model.Mocs.ContainsKey(className.ToUpper()))
                                            continue;
                                        var moc = model.Mocs[className.ToUpper()];

                                        if (moc.KeyAttributes.Any(a => a.name == parameter.Key))
                                        {
                                            key = true;
                                            continue;
                                        }
                                        neName = moc.NeName;
                                        omcName = moc.OMCName;

                                        var searchTree = model.ModelTree.Descendants().Where(node => node.Name == omcName);
                                        var searchTreeItem = searchTree.FirstOrDefault();
                                        if (searchTreeItem != null)
                                        {
                                            HashSet<string> exsitingAtt = new HashSet<string>();
                                            pimoname = searchTreeItem.GetPiMoname(model.Mocs, parameters, exsitingAtt, ne, out vsmoname);
                                            //vsmoname = string.Join(",", pimoname.Split(new char[] { '→' }).Skip(1));
                                            vsmoname = ne + "/" + className + (string.IsNullOrWhiteSpace(vsmoname) ? "" : ":" + vsmoname);
                                            motype = searchTreeItem.Getmotype();
                                            break;
                                        }
                                    }

                                    if (key || parameter.Key == "NE" || pimoname == "NA")
                                        continue;
                                    if (pimoname.Split('→')[pimoname.Count(a => a == '→')].Contains("LIOPTRULE"))
                                    { }

                                    if (!pimonameDic.ContainsKey(pimoname))
                                        pimonameDic.Add(pimoname, vsmoname);

                                    //→
                                    //Console.WriteLine($"NeName:{neName} omcName:{omcName} NEType{neType} Key:{key}");
                                    //TSV prefered to help Click House importer 
                                    //CMDATA => datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue
                                    streamWriter[0].Write("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\n",
                                    //streamWriter.Write("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}\n",
                                                        fileDate,
                                                        "\\N",
                                                        "\\N",
                                                        "\\N",
                                                        "\\N",
                                                        "\\N",
                                                        ossid,
                                                        vsmoname,
                                                        pimoname,
                                                        motype,
                                                        parameter.Key,
                                                        parameter.Value);

                                }
                            }
                        }
                    }
                    xmlReader.ReadEndElement();
                }
            }

            foreach (var item in pimonameDic.Keys)
            {
                int level = item.Count(a => a == '→');
                if (level == 0)
                    continue;

                string parentpimoname = string.Join("→", item.Split('→').ToArray<string>().Take(item.Count(a => a == '→')));

                //CMTREE => datadatetime,ossid,netopologyfolder,treeelementclass,treedepth,parentpimoname,pimoname,displayvsmoname,motype,vsmoname
                streamWriter[1].Write("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\n",
                   fileDate,
                   ossid,
                   item.Split('→')[0],//netopologyfolder
                   item.Split('→')[item.Count(a => a == '→')].Split('=')[0],//treeelementclass
                   level,
                   level > 1 ? parentpimoname : "",//parentpimoname
                   item,//pimoname
                   item.Split('→')[item.Count(a => a == '→')],//displayvsmoname
                   string.Join(",", item.Split('→').Select(a => a.Split('=')[0]).ToArray<string>()),
                   pimonameDic[item] //vsmoname 
                   );
            }
        }
    }
}

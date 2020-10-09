using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            Dictionary<string, Model> models,
            List<EamNe> eamNElist,
            string fileDate,
            string ossid)
        {
            Dictionary<string, string> pimonameDic = new Dictionary<string, string>();
            var eamNE = eamNElist.FirstOrDefault(a => a.NeName == ne);

            if(eamNE == null)
                throw new Exception("eamNE is null");

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
                string mainClassName = xmlReader.GetAttribute("name");
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
                            Dictionary<string, string> parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            foreach (XElement xParameter in xObject.Elements("parameter"))
                            {
                                string parameterName = xParameter.Attribute("name").Value;
                                string parameterValue = xParameter.Attribute("value").Value;
                                parameters.Add(parameterName, parameterValue);
                            }
                            if (parameters.Count > 1) // > 1 because NE is always there
                            {

                                string omcName = "NA";
                                string neName = "NA";


                                foreach (KeyValuePair<string, string> parameter in parameters)
                                {
                                    string vsmoname = "NA";
                                    Dictionary<string, string> vsmonameDic = new Dictionary<string, string>();
                                    string pimoname = "NA";
                                    string motype = "NA";
                                    string paramvaluetype = "\\N";
                                    string paramValue = parameter.Value;

                                    Dictionary<string, string> switchparameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                                    var model = models.Values.Where(a => a.DisplayVersion == version && a.NeTypeName == mainClassName).FirstOrDefault(a => a.Mocs.ContainsKey(className.ToUpper()));

                                    //foreach (var model in models.Values.Where(a => a.DisplayVersion == version && a.NeTypeName == mainClassName))

                                    if (model != null)
                                    {
                                        //if (!model.Mocs.ContainsKey(className.ToUpper()))
                                        //    continue;
                                        var moc = model.Mocs[className.ToUpper()];

                                        //Console.WriteLine()
                                        // if (moc.KeyAttributes.Any(a => a.name == parameter.Key))

                                        neName = moc.NeName;
                                        omcName = moc.OMCName;

                                        bool needToSplitParameter = false;

                                        if (moc.Attributes.ContainsKey(parameter.Key))
                                        {
                                            bool isEnum = false;
                                            var att = moc.Attributes[parameter.Key];
                                            var fileType = att.type;
                                            if (fileType.Contains("enum"))
                                                isEnum = true;

                                            if (CHType.CHTypesDic.ContainsKey(fileType))
                                                paramvaluetype = CHType.CHTypesDic[fileType];
                                            else
                                                paramvaluetype = fileType;



                                            needToSplitParameter = (parameter.Value.Contains("-1&") || parameter.Value.Contains("-0&")) && isEnum;
                                            if (att.ExternalRef != "" && att.ExternalRef != "IPV4" && att.ExternalRef != null && needToSplitParameter == false)
                                            {
                                                if (model.ExternalTypesEnums.ContainsKey(att.ExternalRef))
                                                {
                                                    if (model.ExternalTypesEnums[att.ExternalRef].ExternalTypesEnumItemList.Count > 0)
                                                    {
                                                        var pv = model.ExternalTypesEnums[att.ExternalRef].ExternalTypesEnumItemList.FirstOrDefault(a => a.name == parameter.Value);
                                                        if (pv == null)
                                                        {
                                                            paramValue = parameter.Value;
                                                            //Console.WriteLine(parameter.Key);
                                                        }
                                                        else
                                                            paramValue = pv.Value;
                                                    }
                                                    else
                                                        paramValue = model.ExternalTypesEnums[att.ExternalRef].ExternalTypesBitEnumItemList.FirstOrDefault(a => a.name == parameter.Value.Split('-')[0]).index;
                                                }
                                                else
                                                {
                                                    //mayne this is struct and we are not processing it 
                                                }
                                            }


                                        }
                                        else
                                        /// { if (parameter.Key != "NE") { Console.WriteLine(parameter.Key); continue; } }
                                        {
                                            if (parameter.Key != "NE" && parameter.Key != "OBJID")
                                            {

                                                //Console.WriteLine("param not find in mode: " + parameter.Key);
                                                //Console.WriteLine(model.Path);
                                                //continue;
                                            }
                                        }



                                        if (needToSplitParameter)
                                        {
                                            foreach (string parameterSwitch in parameter.Value.Split('&'))
                                            {
                                                string[] parameterSwitchNameAndValue = parameterSwitch.Split('-');
                                                string parameterSwitchName = parameterSwitchNameAndValue[0];
                                                string parameterSwitchValue = parameterSwitchNameAndValue[1];
                                                switchparameters.Add(parameter.Key + "." + parameterSwitchName, parameterSwitchValue);
                                            }
                                        }
                                        else
                                        {
                                            switchparameters.Add(parameter.Key, paramValue);
                                        }

                                        paramvaluetype = paramvaluetype == null ? "\\N" : paramvaluetype;
                                        //if (paramvaluetype != "\\N") { }
                                        //var searchTreeItem = model.ModelTree.Descendants().FirstOrDefault(node => node.Name == omcName);

                                        //var searchTreeItem = searchTree.FirstOrDefault();
                                        //if (searchTreeItem != null)
                                        if (model.FlattenTree.ContainsKey(omcName))
                                        {
                                            var searchTreeItem = model.FlattenTree[omcName];
                                            HashSet<string> exsitingAtt = new HashSet<string>();
                                            vsmonameDic = new Dictionary<string, string>();
                                            pimoname = searchTreeItem.GetPiMoname(model.Mocs, parameters, exsitingAtt, ne, vsmonameDic);
                                            //vsmoname = string.Join(",", pimoname.Split(new char[] { '→' }).Skip(1));
                                            vsmoname = ne + "/" + className + (vsmonameDic.Count == 0 ? "" : "=" + string.Join(",", vsmonameDic.Select(a => a.Key + ":" + a.Value)));
                                            motype = searchTreeItem.Getmotype();
                                            //break;
                                        }
                                    }
                                    else
                                    {
                                        //Console.WriteLine($"Model not find for {parameter}");
                                        //Console.WriteLine(parameter);
                                        continue;
                                    }

                                    if (parameter.Key == "NE" || pimoname == "NA" || parameter.Key == "OBJID")
                                        continue;

                                    foreach (var paramaterex in switchparameters)
                                    {
                                        if (!pimonameDic.ContainsKey(pimoname))
                                            pimonameDic.Add(pimoname, vsmoname);

                                        //→
                                        //cmdata => {datadatetime},{ossid},{vsmoname},{pimoname},{motype},{paramname},{paramvalue}
                                        streamWriter[0].Write(string.Join("\t", fileDate, ossid, vsmoname, pimoname, motype, paramaterex.Key, paramaterex.Value));
                                        streamWriter[0].Write("\n");
                                    }
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
                //if (level == 1) { }
                string parentpimoname = string.Join("→", item.Split('→').ToArray<string>().Take(item.Count(a => a == '→')));

                ////cmtree => {0-datadatetime},{1-ossid},{2-netopologyfolder},{3-treeelementclass},{4-treedepth},{5-parentpimoname},{6-pimoname},{7-displayvsmoname},{8-motype},{9-vsmoname}
                streamWriter[1].Write("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\n",
                   fileDate,
                   ossid,
                   eamNE == null ? "" : eamNE.Folder,//netopologyfolder
                   item.Split('→')[item.Count(a => a == '→')].Split('=')[0],//treeelementclass
                   level,
                   level > 1 ? parentpimoname : "",//parentpimoname
                   item,//pimoname
                   level == 1 ? item.Split('→')[item.Count(a => a == '→') - 1] : item.Split('→')[item.Count(a => a == '→')],//displayvsmoname
                   string.Join(",", item.Split('→').Select(a => a.Split('=')[0]).ToArray<string>()),//motype
                   pimonameDic[item] //vsmoname 
                   );
            }
        }
    }
}

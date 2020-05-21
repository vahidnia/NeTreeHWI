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
        public static void Convert(Stream xmlStream, string dbFilePath, string ne, bool removeClassNameSuffix, StreamWriter streamWriter, Dictionary<string, Dictionary<string, int>> columnIndices, Model model, Tree tree)
        {

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
                                foreach (KeyValuePair<string, string> parameter in parameters)
                                {

                                    var fileDateTime = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
                                    var ossid = -1;
                                    string omcName = "";
                                    string neName = "";
                                    string neType = "NA";
                                    Boolean key = false;
                                    var KeyAtt = model.Mocs.FirstOrDefault(a => a.KeyAttributes.Any(b => b.NeName == parameter.Key));
                                    var NoKeyAtt = model.Mocs.FirstOrDefault(a => a.NorAttributes.Any(b => b.NeName == parameter.Key));
                                    if (KeyAtt != null)
                                    {
                                        neName = KeyAtt.NeName;
                                        omcName = KeyAtt.OMCName;
                                        key = true;
                                    }
                                    else if (NoKeyAtt != null)
                                    {
                                        neName = NoKeyAtt.NeName;
                                        omcName = NoKeyAtt.OMCName;
                                    }


                                    var searchTree = tree.Descendants().Where(node => node.Name == omcName);
                                    if (searchTree.Any())
                                        neType = searchTree.FirstOrDefault().ToString();
                                    else if (parameter.Key != "NE")
                                    { }

                                    //Console.WriteLine($"NeName:{neName} omcName:{omcName} NEType{neType} Key:{key}");

                                    //streamWriter.Write("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\n",
                                    streamWriter.Write("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}\n",
                                                        fileDateTime,
                                                        "-1",
                                                        "-1",
                                                        "-1",
                                                        "-1",
                                                        "-1",
                                                        ossid,
                                                        ne,
                                                        neType,
                                                        parameter.Key,
                                                        parameter.Value,
                                                        "0000-00-00 00:00:00"
                                                        );
                                }
                            }
                        }
                    }
                    xmlReader.ReadEndElement();
                }
            }


        }
    }
}

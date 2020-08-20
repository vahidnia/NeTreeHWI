using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Xml;
using System.Xml.Schema;

namespace Enm3gppToCsvKVP
{
    internal static class Enm3GPPToCsvKVPConverter
    {
        private const string NednColumnName = "Nedn";
        private const int NednColumnIndex = 0;
        private const string MonameColumnName = "MONAME";
        private const int MonameColumnIndex = 1;
        private const string MimNameColumnName = "mimName";
        private const int MimNameColumnIndex = 2;
        private const string ArraySeparator = "|";

        private const string ReservedByColumnName = "reservedBy";
        private const string AttributesColumnName = "attributes";


        public static void Convert(string inputFilePath, string dbFilePath, string ossid, IEnumerable<string> moTypeFilter)
        {
            using (FileStream fileStream = File.Open(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                string dateStr = System.Text.RegularExpressions.Regex.Match(inputFilePath, @"\d\d\d\d-\d\d-\d\dT\d\d-\d\d-\d\d").Value;
                string FileDateTime = dateStr.Substring(0, dateStr.IndexOf('T')) + " " + dateStr.Substring(dateStr.IndexOf('T') + 1, 8).Replace('-', ':');

                if (inputFilePath.EndsWith(".gz") || inputFilePath.EndsWith(".zip"))
                {
                    using (GZipStream decompressedStream = new GZipStream(fileStream, CompressionMode.Decompress))
                    {
                        Convert(decompressedStream, dbFilePath, ossid, moTypeFilter, FileDateTime);
                    }
                }
                else
                {
                    Convert(fileStream, dbFilePath, ossid, moTypeFilter, FileDateTime);
                }
            }
        }

        public static void Convert(Stream stream, string csv, string ossid, IEnumerable<string> moTypeFilter, string fileDateTime)
        {
            using (XmlReader xmlReader = XmlReader.Create(stream, new XmlReaderSettings { IgnoreWhitespace = true, IgnoreComments = true, IgnoreProcessingInstructions = true, CheckCharacters = false, ValidationFlags = XmlSchemaValidationFlags.None }))
            {
                xmlReader.MoveToContent();
                if (!xmlReader.LocalName.Equals("bulkCmConfigDataFile", StringComparison.Ordinal))
                    throw new Exception("Cannot find the <bulkCmConfigDataFile> tag.");

                string esNamespaceUri = xmlReader.GetAttribute("xmlns:es");

                if (!xmlReader.ReadToDescendant("configData"))
                    throw new Exception("Cannot find the <configData> tag.");

                List<StreamWriter> streamWriter = new List<StreamWriter>();

                streamWriter.Add(new StreamWriter(csv, false));
                streamWriter.Add(new StreamWriter(Path.Combine(Path.GetDirectoryName(csv), "Tree" + Path.GetFileName(csv)), false));

                List<KeyValuePair<string, string>> ossPrefixes = new List<KeyValuePair<string, string>>();
                List<KeyValuePair<string, string>> path = new List<KeyValuePair<string, string>>();
                xmlReader.ReadStartElement();
                while (!xmlReader.LocalName.Equals("configData", StringComparison.Ordinal))
                    ProcessGenericContainer(
                        ossid,
                        xmlReader,
                        path,
                        ossPrefixes,
                        moTypeFilter,
                        streamWriter,
                        fileDateTime);
                streamWriter[0].Flush();
                streamWriter[0].Close();
                streamWriter[1].Flush();
                streamWriter[1].Close();
            }
        }

        private static void ProcessAttributes(
            XmlReader xmlReader,
            List<KeyValuePair<string, string>> attributes,
            string baseAttributeName = null)
        {
            while (xmlReader.IsStartElement())
            {
                string attributeName = xmlReader.LocalName;
                string attributePath = attributeName;
                if (attributeName.Equals(ReservedByColumnName, StringComparison.Ordinal))
                {
                    xmlReader.Skip();
                    continue;
                }
                // todo: skip if XElement is empty
                if (!string.IsNullOrEmpty(baseAttributeName))
                {
                    attributePath = $"{baseAttributeName}.{attributeName}";
                }
                if (xmlReader.IsEmptyElement)
                {
                    xmlReader.Skip();
                    attributes.Add(new KeyValuePair<string, string>(attributePath, string.Empty));
                }
                else
                {
                    xmlReader.ReadStartElement();
                    if (xmlReader.NodeType == XmlNodeType.Text || xmlReader.NodeType == XmlNodeType.EndElement)
                    {
                        string attributeValue;
                        if (xmlReader.NodeType == XmlNodeType.Text)
                        {
                            attributeValue = xmlReader.ReadContentAsString();
                        }
                        else
                        {
                            attributeValue = string.Empty;
                        }
                        if (attributePath.Contains("trafficModelPrb")) { }
                        attributes.Add(new KeyValuePair<string, string>(attributePath, attributeValue));
                    }
                    else
                    {
                        ProcessAttributes(
                            xmlReader,
                            attributes,
                            attributePath);
                    }
                    xmlReader.ReadEndElement();
                }
            }
        }

        private static void ProcessGenericContainer(
            string ossid,
            XmlReader xmlReader,
            List<KeyValuePair<string, string>> path,
            List<KeyValuePair<string, string>> ossPrefixes,
            IEnumerable<string> moTypeFilter,
            List<StreamWriter> streamWriter,
            string fileDateTime)
        {

            int level = 1;
            string parent = "";
            // in the end xmlReader points right after the </??:Object>
            while (xmlReader.LocalName.Equals("SubNetwork", StringComparison.Ordinal)
                  || xmlReader.LocalName.Equals("MeContext", StringComparison.Ordinal))
            {

                if (xmlReader.IsStartElement())
                {
                    if (xmlReader.LocalName.Equals("MeContext", StringComparison.Ordinal))
                    {
                        if (string.IsNullOrWhiteSpace(parent))
                        {

                            //CMTREE
                            // datadatetime,ossid,netopologyfolder,treeelementclass,treedepth,parentpimoname,pimoname,displayvsmoname,motype,vsmoname
                            streamWriter[1].Write("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\n",
                                     fileDateTime,
                                     ossid,
                                     string.Join("→", ossPrefixes.Select(o => $"{o.Key}={o.Value}")),
                                     xmlReader.LocalName,
                                     level++,
                                     parent,
                                     string.Join("→", ossPrefixes.Select(o => $"{o.Key}={o.Value}")) + "→" + xmlReader.LocalName + "=" + xmlReader.GetAttribute("id"),
                                     xmlReader.LocalName + "=" + xmlReader.GetAttribute("id"),
                                     "",
                                     xmlReader.LocalName + "=" + xmlReader.GetAttribute("id"));

                            parent = string.Join("→", ossPrefixes.Select(o => $"{o.Key}={o.Value}")) + "→" + xmlReader.LocalName + "=" + xmlReader.GetAttribute("id");

                        }
                        else
                        {
                            streamWriter[1].Write("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\n",
                                        fileDateTime,
                                        ossid,
                                        string.Join("→", ossPrefixes.Select(o => $"{o.Key}={o.Value}")),
                                        xmlReader.LocalName,
                                        level++,
                                        parent,
                                        parent + "→" + xmlReader.LocalName + "=" + xmlReader.GetAttribute("id"),
                                        parent + "→" + xmlReader.LocalName + "=" + xmlReader.GetAttribute("id"),
                                        "");

                            parent = parent + "→" + xmlReader.LocalName + "=" + xmlReader.GetAttribute("id");

                        }
                    }

                    ossPrefixes.Add(new KeyValuePair<string, string>(xmlReader.LocalName.Replace("\\", "\\\\"), xmlReader.GetAttribute("id").Replace("\\", "\\\\")));
                    xmlReader.ReadStartElement();
                }
                else
                {
                    if (ossPrefixes.Count > 0)
                        ossPrefixes.RemoveAt(ossPrefixes.Count - 1);
                    xmlReader.ReadEndElement();
                }
                if (xmlReader.LocalName.Equals(AttributesColumnName, StringComparison.Ordinal))
                {
                    xmlReader.ReadStartElement();
                    while (xmlReader.IsStartElement())
                    {
                        xmlReader.Skip();
                    }
                    xmlReader.ReadEndElement();
                }
            }

            if (xmlReader.LocalName.Equals("configData", StringComparison.Ordinal))
            {
                if (xmlReader.IsStartElement())
                    xmlReader.ReadStartElement();
                else
                    return;
            }


            while (xmlReader.LocalName.Equals("VsDataContainer", StringComparison.Ordinal))
            {
                ProcessVsDataContainer(
                    ossid,
                    level,
                    parent,
                    xmlReader,
                    path,
                    ossPrefixes,
                    moTypeFilter,
                    streamWriter,
                    fileDateTime);
            }

            // if there is no Object under xmlReader then return
            if (!xmlReader.IsStartElement() &&
                path.Count == 0)
            {
                return;
            }

            // in the beginning xmlReader points to <??:Object>
            // in the end xmlReader points right after the </??:Object>
            Dictionary<string, string> currentObjectAttributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string currentObjectType = xmlReader.LocalName.Replace("\\", "\\\\");
            string id = xmlReader.GetAttribute("id").Replace("\\", "\\\\");
            path.Add(new KeyValuePair<string, string>(currentObjectType, id));

            if (xmlReader.LocalName == "ManagedElement")
            {
                string currentObjectFullType = string.Join(",", path.Select(o => o.Key));
                string ossPrefix = string.Join(",", ossPrefixes.Select(o => $"{o.Key}={o.Value}"));
                string monamet = string.Join(",", path.Select(o => o.Key + "=" + o.Value));
                string moname = $"{ossPrefixes[ossPrefixes.Count - 1].Key}={ossPrefixes[ossPrefixes.Count - 1].Value},{string.Join(",", path.Select(o => o.Key + "=" + o.Value))}";
                string pimoname = monamet.Replace(',', '→');
                if (!string.IsNullOrEmpty(ossPrefix))
                    pimoname = string.Join("→", ossPrefix.Replace(',', '→'), pimoname);


                string displayvsmoname = string.Join(",", path.Last().Key + "=" + path.Last().Value);
                string netopologyfolder = string.Join("→", ossPrefixes.Take(ossPrefixes.Count - 1).Select(o => $"{o.Key}={o.Value}"));


                //CMTREE
                // datadatetime,ossid,netopologyfolder,treeelementclass,treedepth,parentpimoname,pimoname,displayvsmoname,motype,vsmoname
                streamWriter[1].Write("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\n",
                         fileDateTime,
                         ossid,
                         netopologyfolder,
                         xmlReader.LocalName,
                         level++,
                         parent,
                         pimoname,
                         xmlReader.LocalName + "=" + xmlReader.GetAttribute("id"),
                         "",
                         moname);

                parent = string.Join("→", ossPrefixes.Select(o => $"{o.Key}={o.Value}")) + "→" + xmlReader.LocalName.Replace("\\", "\\\\") + "=" + xmlReader.GetAttribute("id").Replace("\\", "\\\\");

                //CMDATA dummy insert for pifiller
                //CMDATA => datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue
                streamWriter[0].Write("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\n",
                         fileDateTime,
                         "\\N",
                         "\\N",
                         "\\N",
                         "\\N",
                         "\\N",
                         ossid,
                         moname,
                         pimoname,
                         "",
                         "pifiller-k",
                         "pifiller-v"
                         );
            }

            xmlReader.ReadStartElement();
            if (xmlReader.LocalName.Equals(AttributesColumnName, StringComparison.Ordinal))
            {
                xmlReader.ReadStartElement();

                var attributes = new List<KeyValuePair<string, string>>();
                ProcessAttributes(xmlReader, attributes);
                if (attributes.Select(a => a.Key).Contains("trafficModelPrb")) { }
                currentObjectAttributes =
                    attributes.GroupBy(o => o.Key).ToDictionary(o => o.Key, o => string.Join(ArraySeparator, o.Select(r => r.Value)));
                xmlReader.ReadEndElement();
            }


            string mimName = currentObjectAttributes.ContainsKey(MimNameColumnName) ? currentObjectAttributes[MimNameColumnName] : string.Empty;

            if (xmlReader.LocalName.Equals("VsDataContainer", StringComparison.Ordinal)) // may be an extension
            {
                string vsDataContainerId = xmlReader.GetAttribute("id");
                string vsDataContainerMimName = xmlReader.GetAttribute("mimName");
                xmlReader.ReadStartElement();
                Debug.Assert(xmlReader.LocalName.Equals(AttributesColumnName, StringComparison.Ordinal));
                xmlReader.ReadStartElement();
                Debug.Assert(xmlReader.LocalName.Equals("vsDataType", StringComparison.Ordinal));
                string vsDataType = xmlReader.ReadElementContentAsString();
                bool isExtensionOfCurrentObject = vsDataType.Equals($"vsData{currentObjectType}", StringComparison.Ordinal);
                if (isExtensionOfCurrentObject)
                {
                    Debug.Assert(xmlReader.LocalName.Equals("vsDataFormatVersion", StringComparison.Ordinal));
                    xmlReader.Skip();
                    Debug.Assert(xmlReader.LocalName.Equals(vsDataType, StringComparison.Ordinal));
                    if (!xmlReader.IsEmptyElement)
                    {
                        xmlReader.ReadStartElement(); // should point to first child of <??:{vsDataType}> after execution
                        var attributes = new List<KeyValuePair<string, string>>();

                        ProcessAttributes(xmlReader, attributes);
                        if (attributes.Select(a => a.Key).Contains("trafficModelPrb")) { }
                        currentObjectAttributes =
                            attributes.GroupBy(o => o.Key).ToDictionary(o => o.Key, o => string.Join(ArraySeparator, o.Select(r => r.Value)));
                    }
                    Debug.Assert(xmlReader.LocalName.Equals(vsDataType, StringComparison.Ordinal));
                    xmlReader.Read();
                    Debug.Assert(xmlReader.LocalName.Equals(AttributesColumnName, StringComparison.Ordinal));
                    xmlReader.ReadEndElement();
                    Debug.Assert(xmlReader.LocalName.Equals("VsDataContainer", StringComparison.Ordinal));
                    xmlReader.ReadEndElement();
                }
                else
                {
                    ProcessVsDataContainer(
                        ossid,
                        level,
                        parent,
                        xmlReader,
                        path,
                        ossPrefixes,
                        moTypeFilter,
                        streamWriter,
                        vsDataContainerId,
                        vsDataContainerMimName,
                        vsDataType);
                }
            }



            while (xmlReader.IsStartElement())
            {
                if (xmlReader.LocalName.Equals("VsDataContainer", StringComparison.Ordinal))
                {
                    ProcessVsDataContainer(
                        ossid,
                        level,
                        parent,
                        xmlReader,
                        path,
                        ossPrefixes,
                        moTypeFilter,
                        streamWriter,
                        fileDateTime);
                }
                else
                {
                    ProcessGenericContainer(
                        ossid,
                        xmlReader,
                        path,
                        ossPrefixes,
                        moTypeFilter,
                        streamWriter,
                        fileDateTime);
                }
            }
            path.RemoveAt(path.Count - 1);
            xmlReader.ReadEndElement();
        }

        private static void ProcessVsDataContainer(
            string ossid,
            int level,
            string parent,
            XmlReader xmlReader,
            List<KeyValuePair<string, string>> path,
            List<KeyValuePair<string, string>> ossPrefixes,
            IEnumerable<string> moTypeFilter,
            List<StreamWriter> streamWriter,
            string fileDateTime,
            string id = null,
            string mimName = null,
            string vsDataType = null)
        {
            // in the beginning xmlReader points to <??:VsDataContainer>
            // in the end xmlReader points right after the </??:vsDataContainer>
            Dictionary<string, string> currentObjectAttributes = new Dictionary<string, string>();
            if (id == null && mimName == null && vsDataType == null)
            {
                id = xmlReader.GetAttribute("id");
                mimName = xmlReader.GetAttribute("mimName");
                xmlReader.ReadStartElement();
                Debug.Assert(xmlReader.LocalName.Equals(AttributesColumnName, StringComparison.Ordinal));
                xmlReader.ReadStartElement();
                Debug.Assert(xmlReader.LocalName.Equals("vsDataType", StringComparison.Ordinal));
                vsDataType = xmlReader.ReadElementContentAsString();
            }
            path.Add(new KeyValuePair<string, string>(vsDataType.Replace("\\", "\\\\"), id.Replace("\\", "\\\\")));
            Debug.Assert(xmlReader.LocalName.Equals("vsDataFormatVersion", StringComparison.Ordinal));
            xmlReader.Skip();

            Debug.Assert(xmlReader.LocalName.Equals(vsDataType, StringComparison.Ordinal));
            while (!xmlReader.IsEmptyElement && xmlReader.IsStartElement())
            {
                xmlReader.ReadStartElement(); // should point to first child of <??:{vsDataType}> after execution
                var attributes = new List<KeyValuePair<string, string>>();
                ProcessAttributes(xmlReader, attributes);
                if (attributes.Select(a => a.Key).Contains("trafficModelPrb")) { }
                currentObjectAttributes =
                    attributes.GroupBy(o => o.Key).ToDictionary(o => o.Key, o => string.Join(ArraySeparator, o.Select(r => r.Value)));
            }
            Debug.Assert(xmlReader.LocalName.Equals(vsDataType, StringComparison.Ordinal));
            xmlReader.Read();
            Debug.Assert(xmlReader.LocalName.Equals(AttributesColumnName, StringComparison.Ordinal));
            xmlReader.ReadEndElement();


            string currentObjectFullType = string.Join(",", path.Select(o => o.Key));
            string ossPrefix = string.Join(",", ossPrefixes.Select(o => $"{o.Key}={o.Value}"));
            string monamet = string.Join(",", path.Select(o => o.Key + "=" + o.Value));
            string moname = $"{ossPrefixes[ossPrefixes.Count - 1].Key}={ossPrefixes[ossPrefixes.Count - 1].Value},{string.Join(",", path.Select(o => o.Key + "=" + o.Value))}";
            string pimoname = monamet.Replace(',', '→');
            if (!string.IsNullOrEmpty(ossPrefix))
                pimoname = string.Join("→", ossPrefix.Replace(',', '→'), pimoname);


            string displayvsmoname = string.Join(",", path.Last().Key + "=" + path.Last().Value);
            string netopologyfolder = string.Join("→", ossPrefixes.Take(ossPrefixes.Count - 1).Select(o => $"{o.Key}={o.Value}"));

            //CMTREE => datadatetime,ossid,netopologyfolder,treeelementclass,treedepth,parentpimoname,pimoname,displayvsmoname,motype,vsmoname
            streamWriter[1].Write("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\n",
               fileDateTime,
               ossid,
               netopologyfolder,
               vsDataType,
               level,
               parent,
               pimoname,
               displayvsmoname,
               currentObjectFullType,
               moname
               );


            //CMDATA dummy insert for pifiller
            //CMDATA => datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue
            streamWriter[0].Write("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\n",
                     fileDateTime,
                     "\\N",
                     "\\N",
                     "\\N",
                     "\\N",
                     "\\N",
                     ossid,
                     moname,
                     pimoname,
                     currentObjectFullType,
                     "pifiller-k",
                     "pifiller-v"
                     );


            foreach (KeyValuePair<string, string> attribute in currentObjectAttributes)
            {
                if (string.IsNullOrWhiteSpace(attribute.Key))
                    continue;


                string key = attribute.Key.Replace('\n', ' ').Replace('\t', ' ').Replace('\r', ' ').Replace("\\", "\\\\");
                string value = attribute.Value.Replace('\n', ' ').Replace('\t', ' ').Replace('\r', ' ').Replace("\\", "\\\\");
                if (value.Contains('|'))
                {
                    int i = 0;
                    foreach (var splitedValue in value.Split('|'))
                    {
                        //TSV prefered to help Click House importer 
                        //CMDATA => datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue
                        streamWriter[0].Write("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\n",
                            fileDateTime,
                            "\\N",
                            "\\N",
                            "\\N",
                            "\\N",
                            "\\N",
                            ossid,
                            moname,
                            pimoname,
                            currentObjectFullType,
                            $"{key}[{i++}]",
                            splitedValue);
                    }
                }
                else
                {
                    //TSV prefered to help Click House importer 
                    //CMDATA => datadatetime,pk1,pk2,pk3,pk4,clid,ossid,vsmoname,pimoname,motype,paramname,paramvalue
                    streamWriter[0].Write("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\n",
                        fileDateTime,
                        "\\N",
                        "\\N",
                        "\\N",
                        "\\N",
                        "\\N",
                        ossid,
                        moname,
                        pimoname,
                        currentObjectFullType,
                        key,
                        value);
                }
            }

            while (xmlReader.IsStartElement())
            {
                ProcessVsDataContainer(
                    ossid,
                    level + 1,
                    pimoname,
                    xmlReader,
                    path,
                    ossPrefixes,
                    moTypeFilter,
                    streamWriter,
                    fileDateTime);
            }
            path.RemoveAt(path.Count - 1);
            xmlReader.ReadEndElement();
        }
        static Dictionary<string, List<Tuple<string, string>>> cmData = new Dictionary<string, List<Tuple<string, string>>>();

    }
}

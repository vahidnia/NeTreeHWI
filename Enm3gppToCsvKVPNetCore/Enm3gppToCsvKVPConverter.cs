using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;

namespace Enm3gppToCsvKVPNetCore
{
    internal static class Enm3GPPToCsvKVPConverter
    {
        private const string NednColumnName = "Nedn";
        private const int NednColumnIndex = 0;
        private const string MonameColumnName = "MONAME";
        private const int MonameColumnIndex = 1;
        private const string MimNameColumnName = "mimName";
        private const int MimNameColumnIndex = 2;
        private const string ArraySeparator = "ǁ";

        private const string ReservedByColumnName = "reservedBy";
        private const string AttributesColumnName = "attributes";


        public static void Convert(string inputFilePath, string dbFilePath, string modelPath)
        {
            using (FileStream fileStream = File.Open(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                string ossId = Regex.Match(inputFilePath, @"OSSID-(?<ossid>\d+)").Groups["ossid"].Value;
                string strDate = System.Text.RegularExpressions.Regex.Match(inputFilePath, @"\d\d\d\d-\d\d-\d\dT\d\d-\d\d-\d\d").Value;
                string fileDateTime = strDate.Substring(0, strDate.IndexOf('T')) + " " + strDate.Substring(strDate.IndexOf('T') + 1, 8).Replace('-', ':');
                var model = ExtractModel(modelPath);

                if (inputFilePath.EndsWith(".gz") || inputFilePath.EndsWith(".zip"))
                {
                    using (GZipStream decompressedStream = new GZipStream(fileStream, CompressionMode.Decompress))
                    {
                        Convert(decompressedStream, dbFilePath, ossId, model, fileDateTime);
                    }
                }
                else
                {
                    Convert(fileStream, dbFilePath, ossId, model, fileDateTime);
                }
            }
        }

        private static Dictionary<string, bool> ExtractModel(string modelPath)
        {
            Dictionary<string, Boolean> attDataType = new Dictionary<string, Boolean>();
            foreach (var item in Directory.GetFiles(modelPath, "*MODEL*.xml"))
                ProcessModel(attDataType, item);

            foreach (var item in Directory.GetFiles(modelPath, "*com*.xml"))
                ProcessModel(attDataType, item);
            return attDataType;
        }

        private static void ProcessModel(Dictionary<string, bool> attDataType, string item)
        {
            Match match = Regex.Match(File.ReadAllText(item), @"<attribute\sname=\""(?<attname>\w+)\"">.+?<dataType>(?<dataType>.+?)</dataType>", RegexOptions.Singleline);
            while (match.Success)
            {
                var isArray = match.Groups["dataType"].Value.Contains("<sequence>");
                if (!attDataType.ContainsKey(match.Groups["attname"].Value))
                    attDataType.Add(match.Groups["attname"].Value, isArray);
                match = match.NextMatch();
            }
        }

        public static void Convert(Stream stream, string csv, string ossid, Dictionary<string, Boolean> model, string fileDateTime)
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

                string dataFilePath = Path.Combine(Path.GetDirectoryName(csv), "Data+" + Path.GetFileName(csv));
                string treeFilePath = Path.Combine(Path.GetDirectoryName(csv), "Tree+" + Path.GetFileName(csv));

                FileStream dataFileStream = File.Create(dataFilePath + ".gz");
                FileStream treeFileStream = File.Create(treeFilePath + ".gz");

                GZipStream compressData = new GZipStream(dataFileStream, CompressionLevel.Fastest);
                GZipStream compressTree = new GZipStream(treeFileStream, CompressionLevel.Fastest);

                streamWriter.Add(new StreamWriter(compressData));
                streamWriter.Add(new StreamWriter(compressTree));

                List<KeyValuePair<string, string>> ossPrefixes = new List<KeyValuePair<string, string>>();
                List<KeyValuePair<string, string>> path = new List<KeyValuePair<string, string>>();
                xmlReader.ReadStartElement();
                while (!xmlReader.LocalName.Equals("configData", StringComparison.Ordinal))
                    ProcessGenericContainer(
                        ossid,
                        xmlReader,
                        path,
                        ossPrefixes,
                        model,
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
            Dictionary<string, List<int>> attributesLevel,
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


                    if (!attributesLevel.ContainsKey(attributeName))
                        attributesLevel.Add(attributeName, new List<int>());
                    attributesLevel[attributeName].Add(string.IsNullOrEmpty(baseAttributeName) ? 1 : 0);

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
                        attributes.Add(new KeyValuePair<string, string>(attributePath, attributeValue));

                    }
                    else
                    {
                        ProcessAttributes(
                            xmlReader,
                            attributes,
                            attributesLevel,
                            attributePath
                            );
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
            Dictionary<string, Boolean> moTypeFilter,
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
                            string netopologyfolder = string.Join("→", ossPrefixes.Select(o => $"{o.Key}={o.Value}"));
                            string vsDataType = xmlReader.LocalName;
                            string pimoname = string.Join("→", ossPrefixes.Select(o => $"{o.Key}={o.Value}")) + "→" + xmlReader.LocalName + "=" + xmlReader.GetAttribute("id");
                            string displayvsmoname = xmlReader.LocalName + "=" + xmlReader.GetAttribute("id");
                            string moname = xmlReader.LocalName + "=" + xmlReader.GetAttribute("id");
                            if (!string.IsNullOrWhiteSpace(netopologyfolder))
                            {
                                // cmtree => {0-datadatetime},{1-ossid},{2-netopologyfolder},{3-treeelementclass},{4-treedepth},{5-parentpimoname},{6-pimoname},{7-displayvsmoname},{8-motype},{9-vsmoname}
                                streamWriter[1].Write(string.Join("\t", fileDateTime, ossid, netopologyfolder, vsDataType, level++, parent, pimoname, displayvsmoname, string.Empty, moname));
                                streamWriter[1].Write("\n");
                            }
                            parent = string.Join("→", ossPrefixes.Select(o => $"{o.Key}={o.Value}")) + "→" + xmlReader.LocalName + "=" + xmlReader.GetAttribute("id");
                        }
                        else
                        {
                            throw new Exception("Parent is not empty for <MeContext>");
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


                string displayvsmoname = currentObjectType + "=" + id;
                string netopologyfolder = string.Join("→", ossPrefixes.Take(ossPrefixes.Count - 1).Select(o => $"{o.Key}={o.Value}"));
                string vsDataType = currentObjectType;
                if (!string.IsNullOrWhiteSpace(netopologyfolder))
                {
                    // cmtree => {0-datadatetime},{1-ossid},{2-netopologyfolder},{3-treeelementclass},{4-treedepth},{5-parentpimoname},{6-pimoname},{7-displayvsmoname},{8-motype},{9-vsmoname}
                    streamWriter[1].Write(string.Join("\t", fileDateTime, ossid, netopologyfolder, vsDataType, level++, parent, pimoname, displayvsmoname, string.Empty, moname));
                    streamWriter[1].Write("\n");



                    //cmdata => {datadatetime},{ossid},{vsmoname},{pimoname},{motype},{paramname},{paramvalue}
                    streamWriter[0].Write(string.Join("\t", fileDateTime, ossid, moname, pimoname, "ManagedElement", "pifiller-k", "pifiller-v"));
                    streamWriter[0].Write("\n");
                }
                parent = string.Join("→", ossPrefixes.Select(o => $"{o.Key}={o.Value}")) + "→" + displayvsmoname;

            }

            xmlReader.ReadStartElement();
            if (xmlReader.LocalName.Equals(AttributesColumnName, StringComparison.Ordinal))
            {
                xmlReader.ReadStartElement();

                var attributes = new List<KeyValuePair<string, string>>();
                Dictionary<string, List<int>> attributesLevel = new Dictionary<string, List<int>>();
                ProcessAttributes(xmlReader, attributes, attributesLevel);
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
                        var attributesLevel = new Dictionary<string, List<int>>();
                        ProcessAttributes(xmlReader, attributes, attributesLevel);
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
            Dictionary<string, Boolean> model,
            List<StreamWriter> streamWriter,
            string fileDateTime,
            string id = null,
            string mimName = null,
            string vsDataType = null)
        {
            // in the beginning xmlReader points to <??:VsDataContainer>
            // in the end xmlReader points right after the </??:vsDataContainer>
            Dictionary<string, string> currentObjectAttributes = new Dictionary<string, string>();
            HashSet<string> arrayKeys = new HashSet<string>();
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
                Dictionary<string, List<int>> attributesLevel = new Dictionary<string, List<int>>();

                ProcessAttributes(xmlReader, attributes, attributesLevel);
                arrayKeys = attributesLevel.Where(a => a.Value.Count > 1).Select(a => a.Key).ToHashSet<string>();
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

            if (!string.IsNullOrWhiteSpace(netopologyfolder))
            {
                // cmtree => {0-datadatetime},{1-ossid},{2-netopologyfolder},{3-treeelementclass},{4-treedepth},{5-parentpimoname},{6-pimoname},{7-displayvsmoname},{8-motype},{9-vsmoname}
                streamWriter[1].Write(string.Join("\t", fileDateTime, ossid, netopologyfolder, vsDataType, level, parent, pimoname, displayvsmoname, currentObjectFullType, moname));
                streamWriter[1].Write("\n");


                streamWriter[0].Write(string.Join("\t", fileDateTime, ossid, moname, pimoname, currentObjectFullType, "pifiller-k", "pifiller-v"));
                streamWriter[0].Write("\n");
            }

            foreach (KeyValuePair<string, string> attribute in currentObjectAttributes)
            {
                if (string.IsNullOrWhiteSpace(attribute.Key))
                    continue;

                string key = attribute.Key.Replace('\n', ' ').Replace('\t', ' ').Replace('\r', ' ').Replace("\\", "\\\\");
                string value = attribute.Value.Replace('\n', ' ').Replace('\t', ' ').Replace('\r', ' ').Replace("\\", "\\\\");
                if (value.Contains(ArraySeparator))
                {
                    if (key.Contains('.'))
                    {
                        var splittedArr = key.Split('.');
                        var keySplitedBase = splittedArr[0];
                        var keySplited = splittedArr[1];
                        if (arrayKeys.Contains(keySplitedBase))
                            key = $"{keySplitedBase}[].{keySplited}";
                        else
                            key = $"{keySplitedBase}.{keySplited}[]";
                    }
                    else
                        key = $"{key}[]";
                }
                else
                {
                    Boolean isArrayParam1 = false, isArrayParam2 = false;
                    if (key.Contains('.'))
                    {
                        var p1 = key.Split('.')[0];
                        var p2 = key.Split('.')[1];

                        Boolean p1Find = model.TryGetValue(p1, out isArrayParam1);
                        Boolean p2Find = model.TryGetValue(p2, out isArrayParam2);

                        p1 += isArrayParam1 ? "[]" : "";
                        p2 += isArrayParam2 ? "[]" : "";
                        key = $"{p1}.{p2}";
                    }
                    else
                    {

                        model.TryGetValue(key, out isArrayParam1);
                        key += isArrayParam1 ? "[]" : "";
                    }
                }


                //cmdata => {datadatetime},{ossid},{vsmoname},{pimoname},{motype},{paramname},{paramvalue}
                streamWriter[0].Write(string.Join("\t", fileDateTime, ossid, moname, pimoname, currentObjectFullType, key, value));
                streamWriter[0].Write("\n");
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
                    model,
                    streamWriter,
                    fileDateTime);
            }
            path.RemoveAt(path.Count - 1);
            xmlReader.ReadEndElement();
        }


    }
}
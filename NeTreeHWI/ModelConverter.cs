using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace GExportToKVP
{
    internal static class ModelConverter
    {
        public static Dictionary<string, Model> Convert(string modelPath)
        {
            Dictionary<string, Model> modelDic = new Dictionary<string, Model>();
            Dictionary<string, string> files = new Dictionary<string, string>();
            Dictionary<string, NeVersionDef> NeVersionDic = new Dictionary<string, NeVersionDef>();


            foreach (var item in Directory.GetFiles(modelPath, "Model.xml", SearchOption.AllDirectories))
                files.Add(item, Regex.Match(item, @"model\\(?<m>(?<m1>\w+)\\(?<m2>\w+)\\(?<m3>\w+))").Groups["m"].Value);

            foreach (var item in Directory.GetFiles(modelPath, "NeVersion.def", SearchOption.AllDirectories))
            {
                NeVersionDef neVersion = new NeVersionDef();
                var lines = File.ReadAllLines(item);
                neVersion.NeType = lines[0].Split('=')[1];
                neVersion.DisplayVersion = lines[1].Split('=')[1];
                neVersion.NeVersion = lines[2].Split('=')[1];
                neVersion.MatchVersion = lines[3].Split('=')[1];
                neVersion.NeTypeId = lines[4].Split('=')[1];
                neVersion.IsRussFun = (lines.Count() > 5 && !string.IsNullOrWhiteSpace(lines[5])) ? lines[5].Split('=')[1] : "NA";
                neVersion.NermVersion = (lines.Count() > 6 && !string.IsNullOrWhiteSpace(lines[6])) ? lines[6].Split('=')[1] : "NA";
                neVersion.SoftVersion = (lines.Count() > 7 && !string.IsNullOrWhiteSpace(lines[7])) ? lines[7].Split('=')[1] : "NA";
                neVersion.FilePath = item;
                if (!NeVersionDic.ContainsKey(lines[1].Split('=')[1]))
                    NeVersionDic.Add(lines[1].Split('=')[1], neVersion);
                else
                    Console.WriteLine(item);

            }




            foreach (var file in files.Keys)
            {
                string neTreeFilePath = Path.Combine(Path.GetDirectoryName(file), "NeTree.xml");
                if (!File.Exists(neTreeFilePath))
                {
                    Console.WriteLine("Tree not found");
                    continue;
                }

                Model model = new Model();

                using (FileStream stream = File.OpenRead(file))
                {
                    XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
                    {
                        DtdProcessing = DtdProcessing.Ignore,
                        IgnoreComments = true,
                        IgnoreProcessingInstructions = true,
                        IgnoreWhitespace = true
                    };
                    using (XmlReader xmlReader = XmlReader.Create(stream, xmlReaderSettings))
                    {
                        try
                        {
                            xmlReader.ReadToDescendant("DataFile");
                            xmlReader.ReadToDescendant("NeTypeName");
                            model.NeTypeName = xmlReader.ReadElementContentAsString();
                            model.Version = xmlReader.ReadElementContentAsString();
                            model.Path = file;
                            //Console.WriteLine(file);
                            if (xmlReader.LocalName == "ExternalTypes")
                            {
                                xmlReader.Read();
                                while (xmlReader.Name == "Enum" || xmlReader.LocalName == "BitDomain")
                                {
                                    try
                                    {
                                        ExternalTypesEnum exTypeEnum = new ExternalTypesEnum();
                                        exTypeEnum.BasicId = xmlReader.GetAttribute("basicId");
                                        exTypeEnum.dispUse = xmlReader.GetAttribute("dispUse");
                                        exTypeEnum.Name = xmlReader.GetAttribute("name");
                                        exTypeEnum.mmlUse = xmlReader.GetAttribute("mmlUse");
                                        xmlReader.Read();
                                        while (xmlReader.LocalName == "EnumItem")
                                        {
                                            exTypeEnum.ExternalTypesEnumItemList.Add(new ExternalTypesEnumItem()
                                            {
                                                desId = (xmlReader.GetAttribute("desId")),
                                                name = xmlReader.GetAttribute("name"),
                                                Value = (xmlReader.GetAttribute("value"))
                                            });
                                            xmlReader.Read();
                                        }

                                        while (xmlReader.LocalName == "BitEnumItem")
                                        {
                                            exTypeEnum.ExternalTypesBitEnumItemList.Add(new ExternalTypesBitEnumItem()
                                            {
                                                desId = (xmlReader.GetAttribute("desId")),
                                                name = xmlReader.GetAttribute("name"),
                                                index = (xmlReader.GetAttribute("index"))
                                            });
                                            xmlReader.Read();
                                        }

                                        //if (xmlReader.LocalName == "Sequence")
                                        //{
                                        //    exTypeEnum.ExternalTypesBitEnumItemList.Add(new ExternalTypesBitEnumItem()
                                        //    {
                                        //        desId = (xmlReader.GetAttribute("desId")),
                                        //        name = xmlReader.GetAttribute("name"),
                                        //        index = (xmlReader.GetAttribute("index"))
                                        //    });
                                        //    xmlReader.Read();
                                        //}

                                        model.ExternalTypesEnums.Add(exTypeEnum.Name.ToUpper(), exTypeEnum);
                                        xmlReader.Read();
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.ToString());
                                    }
                                }
                            }


                            xmlReader.ReadToFollowing("MocDefs");

                            xmlReader.Read();
                            int level = 1;
                            while (xmlReader.Name == "Moc")
                            {
                                Moc moc = new Moc();
                                ReadMocs(xmlReader, level, moc, model.ExternalTypesEnums);
                                model.Mocs.Add(moc.NeName.ToUpper(), moc);
                                xmlReader.Read();
                            }
                            if (NeVersionDic.Values.Where(a => a.FilePath.Contains(string.Join("\\", files[file].Split('\\').Take(2)))).Any())
                                model.DisplayVersion = NeVersionDic.Values.Where(a => a.FilePath.Contains(string.Join("\\", files[file].Split('\\').Take(2)))).FirstOrDefault().DisplayVersion;
                            else
                                Console.WriteLine("Unable to find version");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(file);
                        }
                    }

                    modelDic.Add(files[file], model);
                }
                string ne = Regex.Match(file, @"(?<=^GExport_).+(?=_\d+\.\d+\.\d+\.\d+_)").Value;
                var tree = NeTreeConverter.Convert(neTreeFilePath, ne);
                if (tree != null)
                {
                    model.ModelTree = tree;
                    model.DescendantsTree();
                }
                else
                {
                    Console.WriteLine("error creating tree");
                    continue;
                }
            }

            return modelDic;

        }


        private static void ReadMocs(XmlReader xmlReader, int level, Moc moc, Dictionary<string, ExternalTypesEnum> externalTypesEnum)
        {
            moc.name = xmlReader.GetAttribute("name");
            moc.NeName = xmlReader.GetAttribute("NeName");
            moc.OMCName = xmlReader.GetAttribute("OMCName");
            moc.isVirtual = xmlReader.GetAttribute("isVirtual");
            moc.category = xmlReader.GetAttribute("category");
            moc.type = xmlReader.GetAttribute("type");

            //xmlReader.ReadToDescendant("KeyAttrGroup");
            //ReadAttrGroup(xmlReader, moc.KeyAttributes, true, externalTypesEnum);
            //xmlReader.ReadToFollowing("AttributeGroup");
            //ReadAttrGroup(xmlReader, moc.NorAttributes, false, externalTypesEnum);

            ReadAttrGroup(xmlReader, moc.Attributes, externalTypesEnum);
        }

        private static void ReadAttrGroup(XmlReader xmlReader, Dictionary<string, Attribute> attList, Dictionary<string, ExternalTypesEnum> externalTypesEnum)
        {
            if (xmlReader.IsEmptyElement == false)
            {
                xmlReader.Read();
                if (xmlReader.LocalName == "KeyAttrGroup" && xmlReader.IsEmptyElement == false)
                    getAtt(xmlReader, attList, externalTypesEnum);
                xmlReader.Read();
                if (xmlReader.LocalName == "AttributeGroup" && xmlReader.IsEmptyElement == false)
                    getAtt(xmlReader, attList, externalTypesEnum);
                xmlReader.Read();

                if (attList.Count == 0) { }
            }
        }

        private static void getAtt(XmlReader xmlReader, Dictionary<string, Attribute> attList, Dictionary<string, ExternalTypesEnum> externalTypesEnum)
        {
            xmlReader.ReadStartElement();
            while ((xmlReader.Name == "KeyAttribute") || (xmlReader.Name == "NorAttribute"))
            {
                Attribute att = new Attribute();

                att.IsKeyAttribute = (xmlReader.Name == "KeyAttribute");
                att.name = xmlReader.GetAttribute("name");
                att.NeName = xmlReader.GetAttribute("NeName");
                att.OMCName = xmlReader.GetAttribute("OMCName");
                att.mmlDisNameId = xmlReader.GetAttribute("mmlDisNameId");

                var xmlInnet = xmlReader.ReadInnerXml();
                var match = Regex.Match(xmlInnet, @"basicId=""(?<type>\w+)""");
                if (match.Success)
                {
                    att.IsString = match.Groups[1].Value == "string";
                    att.type = match.Groups[1].Value;
                }
                else
                {
                    var match2 = Regex.Match(xmlInnet, @"name=\""(?<name>.+?)\""");
                    if (match2.Success)
                    {
                        att.IsString = match.Groups[1].Value == "string";
                        string exEnumName = match2.Groups[1].Value.ToUpper();
                        att.ExternalRef = exEnumName;
                        if (externalTypesEnum.ContainsKey(exEnumName))
                            att.type = "enum(" + externalTypesEnum[exEnumName].BasicId + ")";
                        else if (exEnumName == "IPV4")
                            att.type = "string";
                        else //structs 
                            att.type = "string";

                    }
                    else
                    { }

                }
                if (att.OMCName != "OBJID")
                    attList.Add(att.OMCName, att);
            }
        }
    }


    internal class NeVersionDef
    {
        public NeVersionDef()
        {

        }
        public string NeType { get; set; }
        public string DisplayVersion { get; set; }
        public string NeVersion { get; set; }
        public string MatchVersion { get; set; }
        public string NeTypeId { get; set; }
        public string IsRussFun { get; set; }
        public string NermVersion { get; set; }
        public string SoftVersion { get; set; }

        public string FilePath { get; set; }
    }
}

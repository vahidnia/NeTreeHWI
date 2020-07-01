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

                            xmlReader.ReadToFollowing("MocDefs");

                            xmlReader.Read();
                            int level = 1;
                            while (xmlReader.Name == "Moc")
                            {
                                Moc moc = new Moc();
                                ReadMocs(xmlReader, level, moc);
                                model.Mocs.Add(moc.NeName.ToUpper(), moc);
                                xmlReader.Read();
                            }

                            model.DisplayVersion = NeVersionDic.Values.Where(a => a.FilePath.Contains(string.Join("\\", files[file].Split('\\').Take(2)))).FirstOrDefault().DisplayVersion;
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
                }
                else
                {
                    Console.WriteLine("error creating tree");
                    continue;
                }
            }
 
            return modelDic;

        }


        private static void ReadMocs(XmlReader xmlReader, int level, Moc moc)
        {
            moc.name = xmlReader.GetAttribute("name");
            moc.NeName = xmlReader.GetAttribute("NeName");
            moc.OMCName = xmlReader.GetAttribute("OMCName");
            moc.isVirtual = xmlReader.GetAttribute("isVirtual");
            moc.category = xmlReader.GetAttribute("category");
            moc.type = xmlReader.GetAttribute("type");

            xmlReader.ReadToDescendant("KeyAttrGroup");
            ReadAttrGroup(xmlReader, moc.KeyAttributes, true);
            //xmlReader.ReadToFollowing("AttributeGroup");
            ReadAttrGroup(xmlReader, moc.NorAttributes, false);

        }

        private static void ReadAttrGroup(XmlReader xmlReader, List<Attribute> attList, Boolean IsKey)
        {
            xmlReader.ReadStartElement();
            if (xmlReader.HasAttributes)
            {
                while (xmlReader.Name == "KeyAttribute" || xmlReader.Name == "NorAttribute")
                {
                    Attribute att = new Attribute();

                    att.name = xmlReader.GetAttribute("name");
                    att.NeName = xmlReader.GetAttribute("NeName");
                    att.OMCName = xmlReader.GetAttribute("OMCName");
                    att.mmlDisNameId = xmlReader.GetAttribute("mmlDisNameId");

                    var xmlInnet = xmlReader.ReadInnerXml();
                    var match = Regex.Match(xmlInnet, @"basicId=""(?<type>\w+)""");
                    if (match.Success)
                        att.IsString = match.Groups[1].Value == "string";

                    if (IsKey == true && att.OMCName != "OBJID")
                        attList.Add(att);
                }
                xmlReader.ReadEndElement();
            }
            else
            { }
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

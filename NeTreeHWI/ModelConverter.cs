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
        public static Dictionary<string, Model> Convert(string ne)
        {
            Dictionary<string, Model> modelDic = new Dictionary<string, Model>();
            Dictionary<string, string> files = new Dictionary<string, string>();

            foreach (var item in Directory.GetFiles(@"C:\!working\CM\HWI\model", "Model.xml", SearchOption.AllDirectories))
                files.Add(item, Regex.Match(item, @"model\\(?<m>(?<m1>\w+)\\(?<m2>\w+)\\(?<m3>\w+))").Groups["m"].Value);


            foreach (var file in files.Keys)
            {
                string neTreeFilePath = Path.Combine(Path.GetDirectoryName(file), "NeTree.xml");
                if (!File.Exists(neTreeFilePath))
                {
                    Console.WriteLine("Tree not found");
                    continue;
                }

                Model model = new Model();
                model.NeName = ne;
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
                                model.Mocs.Add(moc);
                                ReadMocs(xmlReader, level, moc);
                                xmlReader.Read();
                            }
                        }
                        catch
                        {
                            Console.WriteLine(file);
                        }
                    }
                    modelDic.Add(files[file], model);
                }
                var tree = NeTreeConverter.Convert(neTreeFilePath, model.NeName);
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
            ReadAttrGroup(xmlReader, moc.KeyAttributes);
            //xmlReader.ReadToFollowing("AttributeGroup");
            ReadAttrGroup(xmlReader, moc.NorAttributes);

        }

        private static void ReadAttrGroup(XmlReader xmlReader, List<Attribute> attList)
        {
            xmlReader.ReadStartElement();
            if (xmlReader.HasAttributes)
            {
                while (xmlReader.Name == "KeyAttribute" || xmlReader.Name == "NorAttribute")
                {
                    Attribute att = new Attribute();
                    attList.Add(att);
                    att.name = xmlReader.GetAttribute("name");
                    att.NeName = xmlReader.GetAttribute("NeName");
                    att.OMCName = xmlReader.GetAttribute("OMCName");
                    att.mmlDisNameId = xmlReader.GetAttribute("mmlDisNameId");

                    xmlReader.Skip();
                    //xmlReader.ReadEndElement();
                }
                xmlReader.ReadEndElement();
            }
            else
            { }
        }


    }
}

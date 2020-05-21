using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace GExportToKVP
{
    internal static class ModelConverter
    {
        public static Model Convert()
        {
            Model model = new Model();

            using (FileStream stream = File.OpenRead(@"C:\Users\vahid\Downloads\OneDrive_1_5-20-2020\Model.xml"))
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
            }

            return model;
        }

        private static void ReadMocs(XmlReader xmlReader, int level, Moc moc)
        {
            moc.name = xmlReader.GetAttribute(0);
            moc.NeName = xmlReader.GetAttribute(1);
            moc.OMCName = xmlReader.GetAttribute(2);
            moc.isVirtual = xmlReader.GetAttribute(3);
            moc.category = xmlReader.GetAttribute(4);
            moc.type = xmlReader.GetAttribute(5);

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
                    att.name = xmlReader.GetAttribute(0);
                    att.NeName = xmlReader.GetAttribute(1);
                    att.OMCName = xmlReader.GetAttribute(2);
                    att.mmlDisNameId = xmlReader.GetAttribute(3);

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

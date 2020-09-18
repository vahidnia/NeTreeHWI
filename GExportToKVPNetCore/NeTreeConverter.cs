using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace GExportToKVP
{
    internal static class NeTreeConverter
    {

        public static Tree Convert(string path, string neName)
        {
            Tree tree = new Tree();
            try
            {
                using (FileStream stream = File.OpenRead(path))
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
                        xmlReader.ReadToDescendant("ROOT");
                        tree.Name = xmlReader.GetAttribute(0);
                        xmlReader.Read();
                        int level = 1;
                        while (xmlReader.Name == "ChildMocs")
                        {
                            ReadChildMocs(xmlReader, level, tree);
                            xmlReader.ReadEndElement();
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
            return tree;
        }

        private static void ReadChildMocs(XmlReader xmlReader, int level, Tree tree)
        {
            level++;
            if (xmlReader.NodeType == XmlNodeType.EndElement)
                return;
            xmlReader.ReadStartElement();
            while (xmlReader.Name == "Moc")
            {
                ReadMocs(xmlReader, level, tree);
                xmlReader.ReadEndElement();
            }

        }

        private static void ReadMocs(XmlReader xmlReader, int level, Tree ptree)
        {
            string MocName = xmlReader.GetAttribute(0);
            //Console.WriteLine($"level:{level} Moc:{MocName}");
            Tree tree = new Tree(ptree);
            tree.Name = MocName;
            tree.Level = level;
            ptree.Children.Add(tree);
            if (xmlReader.ReadToDescendant("ChildMocs"))
            {
                ReadChildMocs(xmlReader, level++, tree);
                xmlReader.ReadEndElement();
            }
        }
    }
}
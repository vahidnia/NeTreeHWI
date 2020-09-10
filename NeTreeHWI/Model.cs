using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GExportToKVP
{
    public class Model
    {
        public Tree ModelTree { get; set; }
        public Model()
        {
            Mocs = new Dictionary<string, Moc>();
            ExternalTypesEnums = new Dictionary<string, ExternalTypesEnum>();
        }
        public string NeTypeName { get; set; }
        public string Version { get; set; }


        public string name { get; set; }
        public string NeName { get; set; }
        public string OMCName { get; set; }
        public string isVirtual { get; set; }
        public string category { get; set; }
        public string type { get; set; }

        public string DisplayVersion { get; set; }
        public Dictionary<string, Moc> Mocs { get; set; }
        public Dictionary<string, ExternalTypesEnum> ExternalTypesEnums { get; set; }
        public string Path { get; set; }

        public Dictionary<string, Tree> FlattenTree { get; set; }

        public void DescendantsTree()
        {
            FlattenTree = new Dictionary<string, Tree>(); 
            foreach (var item in this.ModelTree.Descendants())
            {
                string key = item.ToString().Split(',').Last();

                if (FlattenTree.ContainsKey(key))
                    Console.WriteLine($"key already exist in f-tree {key} \r\n {item} \r\n {FlattenTree[key]}");
                else
                    FlattenTree.Add(key, item);
            }
        }
    }

    public class Moc
    {

        public Moc()
        {
            //KeyAttributes = new List<Attribute>();
            //NorAttributes = new List<Attribute>();
            Attributes = new Dictionary<string, Attribute>();
        }
        public string name { get; set; }
        public string NeName { get; set; }
        public string OMCName { get; set; }
        public string isVirtual { get; set; }
        public string category { get; set; }
        public string type { get; set; }
        //public List<Attribute> KeyAttributes { get; set; }
        //public List<Attribute> NorAttributes { get; set; }

        public Dictionary<string, Attribute> Attributes { get; set; }

    }

    public class Attribute
    {
        public Attribute()
        {
            IsString = false;
        }
        public string name { get; set; }
        public string NeName { get; set; }
        public string OMCName { get; set; }
        public string mmlDisNameId { get; set; }
        public Boolean IsString { get; set; }

        public string type { get; set; }

        public string ExternalRef { get; set; }

        public Boolean IsKeyAttribute { get; set; }
    }

    public class ExternalTypesEnum
    {
        public ExternalTypesEnum()
        {
            ExternalTypesEnumItemList = new List<ExternalTypesEnumItem>();
            ExternalTypesBitEnumItemList = new List<ExternalTypesBitEnumItem>();
        }
        public string Name { get; set; }
        public string BasicId { get; set; }
        public string dispUse { get; set; }
        public string mmlUse { get; set; }
        public List<ExternalTypesEnumItem> ExternalTypesEnumItemList { get; set; }
        public List<ExternalTypesBitEnumItem> ExternalTypesBitEnumItemList { get; set; }
    }

    public class ExternalTypesEnumItem
    {
        // name="OFF" value="0" desId="0"/>
        public string name { get; set; }
        public string desId { get; set; }
        public string Value { get; set; }
    }

    public class ExternalTypesBitEnumItem
    {
        //<BitEnumItem name="TS21" index="21" desId="247"/>
        public string name { get; set; }
        // name="OFF" value="0" desId="0"/>
        public string index { get; set; }
        public string desId { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GExportToKVP
{
    public class Model
    {

        public Model()
        {
            Mocs = new List<Moc>();
        }
        public string NeTypeName { get; set; }
        public string Version { get; set; }


        public string name { get; set; }
        public string NeName { get; set; }
        public string OMCName { get; set; }
        public string isVirtual { get; set; }
        public string category { get; set; }
        public string type { get; set; }

        public List<Moc> Mocs { get; set; }
    }

    public class Moc
    {

        public Moc()
        {
            KeyAttributes = new List<Attribute>();
            NorAttributes = new List<Attribute>();
        }
        public string name { get; set; }
        public string NeName { get; set; }
        public string OMCName { get; set; }
        public string isVirtual { get; set; }
        public string category { get; set; }
        public string type { get; set; }
        public List<Attribute> KeyAttributes { get; set; }
        public List<Attribute> NorAttributes { get; set; }

    }

    public class Attribute
    {
        public string name { get; set; }
        public string NeName { get; set; }
        public string OMCName { get; set; }
        public string mmlDisNameId { get; set; }
    }
}

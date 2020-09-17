using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GExportToKVP
{
    public sealed class EamNe
    {
        public string NeType { get; set; }
        public string NeFdn { get; set; }
        public string ParentNeFdn { get; set; }
        public string NeName { get; set; }
        public string NeIp { get; set; }
        public string Version { get; set; }
        public string InternalId { get; set; }
        public string IsMatch { get; set; }
        public string Partition { get; set; }
        public string Subnet { get; set; }
        public string ParentSubnet { get; set; }
        public string TimeZone { get; set; }
        public string DaylightSaveInfo { get; set; }
        public string IsLocked { get; set; }
        public string Longitude { get; set; }
        public string Latitude { get; set; }
        public string GBtsFunctionName { get; set; }
        public string GBtsFunctionRelateNeFdn { get; set; }
        public string NodeBFunctionName { get; set; }
        public string NodeBFunctionRelateNeFdn { get; set; }
        public string ENodeBFunctionName { get; set; }

        public string Folder { get; set; }


    }
}

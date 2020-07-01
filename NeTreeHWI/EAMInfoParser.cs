using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GExportToKVP
{
    internal static class EamInfoParser
    {
        static Dictionary<string, Tuple<string, string>> eamInfoDic = new Dictionary<string, Tuple<string, string>>();
        public static List<EamNe> ExtractNeList(string filePath)
        {
            List<EamNe> neList = new List<EamNe>();

            XDocument xDoc = XDocument.Load(filePath);

            foreach (XElement xSubnet in xDoc.Root.Descendants("Subnets").Descendants("Subnet"))
            {
                eamInfoDic.Add(xSubnet.Attribute("ProductFdn").Value, new Tuple<string, string>(xSubnet.Attribute("ParentProductFdn").Value, xSubnet.Attribute("SubnetName").Value));
            }

            foreach (XElement xNe in xDoc.Root.Descendants("Ne"))
            {
                // <Ne fdn="NE=470" name="R8MON1MME" type="USN"/>
                // it comes under Pool and doesn't make any sense
                XAttribute xNeType = xNe.Attribute("NeType");
                if (xNeType == null)
                    continue;

                EamNe ne = new EamNe();
                neList.Add(ne);
                ne.NeType = xNeType.Value;
                ne.NeFdn = xNe.Attribute("fdn").Value;
                XElement xParent = xNe.Parent;
                if (string.Equals(xParent.Name.LocalName, "Ne", StringComparison.Ordinal))
                {
                    ne.ParentNeFdn = xParent.Attribute("fdn").Value;
                }
                XAttribute xNeName = xNe.Attribute("name");
                ne.NeName = xNeName?.Value;
                XElement xNeIp = xNe.Element("NeIP");
                ne.NeIp = xNeIp?.Value;
                XElement xVersion = xNe.Element("Version");
                ne.Version = xVersion?.Value;
                XElement xInternalId = xNe.Element("InternalId");
                ne.InternalId = xInternalId?.Value;
                XElement xIsMatch = xNe.Element("IsMatch");
                ne.IsMatch = xIsMatch?.Value;
                XElement xPartition = xNe.Element("Partition");
                ne.Partition = xPartition?.Value;
                XElement xSubnet = xNe.Element("Subnet");
                if (xSubnet != null)
                {
                    string subnet = xSubnet.Value;
                    ne.Subnet = subnet;
                    string[] subnetParts = subnet.Split(new[] { '@' }, 2);
                    if (subnetParts.Length == 2)
                    {
                        ne.ParentSubnet = subnetParts[1];
                    }
                }
                XElement xTimeZone = xNe.Element("TimeZone");
                ne.TimeZone = xTimeZone?.Value;
                XElement xDaylightSaveInfo = xNe.Element("DaylightSaveInfo");
                ne.DaylightSaveInfo = xDaylightSaveInfo?.Value;
                XElement xIsLocked = xNe.Element("isLocked");
                ne.IsLocked = xIsLocked?.Value;
                XElement xLongitude = xNe.Element("Longitude");
                ne.Longitude = xLongitude?.Value;
                XElement xLatitude = xNe.Element("Latitude");
                ne.Latitude = xLatitude?.Value;
                XElement xFunctions = xNe.Element("Functions");
                if (xFunctions != null)
                {
                    foreach (XElement xFunction in xFunctions.Elements("Function"))
                    {
                        string functionType = xFunction.Attribute("FunctionType").Value;
                        string functionName = xFunction.Attribute("name").Value;
                        string relateNeFdn = xFunction.Attribute("RelateNEDN")?.Value;
                        if (string.Equals(functionType, "GBTSFUNCTION", StringComparison.Ordinal))
                        {
                            ne.GBtsFunctionName = functionName;
                            ne.GBtsFunctionRelateNeFdn = relateNeFdn;
                        }
                        else if (string.Equals(functionType, "NODEBFUNCTION", StringComparison.Ordinal))
                        {
                            ne.NodeBFunctionName = functionName;
                            ne.NodeBFunctionRelateNeFdn = relateNeFdn;
                        }
                        else if (string.Equals(functionType, "ENODEBFUNCTION", StringComparison.Ordinal))
                        {
                            ne.ENodeBFunctionName = functionName;
                        }
                    }
                }
                string folder = "";
                if (ne.Subnet != null)
                    foreach (var item in ne.Subnet.Split('@'))
                        if (eamInfoDic.ContainsKey(item))
                            folder += eamInfoDic[item].Item2 + "→";

                ne.Folder = folder.TrimEnd('→');

            }

            return neList;
        }


    }
}


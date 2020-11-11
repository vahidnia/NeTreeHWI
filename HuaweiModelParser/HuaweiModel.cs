using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace HuaweiModelParser
{
    public class HuaweiModel
    {
        private readonly IReadOnlyList<HuaweiModelClass> _huaweiModelClassList;
        private readonly IReadOnlyList<HuaweiModelClassAggr> _huaweModelClassAggr;
        private readonly IReadOnlyDictionary<string, HuaweiModelClass> _huaweiModelClassById;
        private readonly IReadOnlyDictionary<string, HuaweiModelClass> _huaweiModelClassByClassName;
        private readonly IReadOnlyDictionary<string, TreeItem> _treeItemByClassName;

        public HuaweiModel(
            IReadOnlyDictionary<string, HuaweiModelClass> huaweiModelClassById,
            IReadOnlyDictionary<string, HuaweiModelClass> huaweiModelClassByClassName,
            IReadOnlyDictionary<string, TreeItem> treeItemByClassName)
        {
            _huaweiModelClassById = huaweiModelClassById;
            _huaweiModelClassByClassName = huaweiModelClassByClassName;
            _treeItemByClassName = treeItemByClassName;
        }

        public HuaweiModelClass GetHuaweiModelClassUsingGExportClassName(string gExportClassName)
        {
            string classId = gExportClassName.Split('_')[0]; // throw away _BTS3900, _BSC6910UMTS, etc. suffixes
            _huaweiModelClassById.TryGetValue(classId, out HuaweiModelClass huaweiModelClass);
            // UCELL comes as UCELL in GExport, but it has ID=CELL in model.xml
            if (huaweiModelClass == null)
                _huaweiModelClassByClassName.TryGetValue(classId, out huaweiModelClass);
            return huaweiModelClass;
        }

        public HuaweiModelClassAggr GetHuaweiModelClassAggr(HuaweiModelClass huaweiModelClass)
        {
            _treeItemByClassName.TryGetValue(huaweiModelClass.ClassName, out TreeItem treeItem);
            return treeItem?.ClassAggr;
        }

        // GExport-object based methods
        public string GetCmPiMoname(string neName, string gExportClassName, IEnumerable<KeyValuePair<string, string>> parameters)
        {
            // BTS3900=BAGCY→NE→ENODEBFUNCTION→CELL=LOCALCELLID:33→EUTRANINTERFREQNCELL=MCC:286,MNC:01,ENODEBID:45843,CELLID:34

            string classId = gExportClassName.Split('_')[0]; // throw away _BTS3900, _BSC6910UMTS, etc. suffixes

            if (!_huaweiModelClassById.TryGetValue(classId, out HuaweiModelClass huaweiModelClass))
                return null;
            _treeItemByClassName.TryGetValue(huaweiModelClass.ClassName, out TreeItem treeItem);
            if (treeItem == null)
                return null;



            throw new NotImplementedException();

        }

        public static HuaweiModel LoadFromFile(string modelFilePath)
        {
            List<HuaweiModelClass> huaweiModelClassList = new List<HuaweiModelClass>();
            List<HuaweiModelClassAsso> huaweiModelClassAssoList = new List<HuaweiModelClassAsso>();
            List<HuaweiModelClassAggr> huaweiModelClassAggrList = new List<HuaweiModelClassAggr>();

            XDocument xModelDoc = XDocument.Load(modelFilePath);
            XElement xModel = xModelDoc.Root;
            XNamespace xNs = xModel.GetDefaultNamespace();
            foreach (XElement xModule in xModel.Elements(xNs + "Module"))
            {
                foreach (XElement xClass in xModule.Elements(xNs + "Class"))
                {
                    string className = xClass.Attribute("ClassName").Value;
                    string id = xClass.Attribute("ID").Value;
                    string isAbstractString = xClass.Attribute("IsAbstract").Value;
                    bool isAbstract = string.Equals(isAbstractString, "true");
                    string name = xClass.Attribute("Name").Value;

                    List<HuaweiModelClassAttr> attrs = new List<HuaweiModelClassAttr>();
                    foreach (XElement xAttr in xClass.Elements(xNs + "Attr"))
                    {
                        string attrName = xAttr.Attribute("AttrName").Value;
                        string attrType = xAttr.Attribute("AttrType").Value;
                        string isKeyString = xAttr.Attribute("IsKey").Value;
                        bool isKey = string.Equals(isKeyString, "true");
                        string dspOrderString = xAttr.Attribute("DspOrder").Value;
                        int.TryParse(dspOrderString, out int dspOrder);
                        string _name = xAttr.Attribute("Name").Value;
                        string mandatoryString = xAttr.Attribute("Mandatory").Value;
                        bool mandatory = string.Equals(mandatoryString, "true");
                        string defaultValue = xAttr.Attribute("DefaultValue").Value;
                        string isCfgAttrString = xAttr.Attribute("IsCfgAttr").Value;
                        bool isCfgAttr = string.Equals(isCfgAttrString, "true");
                        HuaweiModelClassAttr attr = new HuaweiModelClassAttr(attrName, attrType, isKey, dspOrder, _name, mandatory, defaultValue, isCfgAttr);
                        attrs.Add(attr);
                    }

                    HuaweiModelClass huaweiModelClass = new HuaweiModelClass(className, id, isAbstract, name, attrs);
                    huaweiModelClassList.Add(huaweiModelClass);
                }

                foreach (XElement xClassAsso in xModule.Elements(xNs + "ClassAsso"))
                {
                    string classA = xClassAsso.Attribute("ClassA").Value;
                    string classZ = xClassAsso.Attribute("ClassZ").Value;
                    List<HuaweiModelClassAssoAttr> assoAttrs = new List<HuaweiModelClassAssoAttr>();
                    foreach (XElement xAssoAttr in xClassAsso.Elements(xNs + "AssoAttr"))
                    {
                        string aAttr = xAssoAttr.Attribute("AAttr").Value;
                        string zAttr = xAssoAttr.Attribute("ZAttr").Value;
                        HuaweiModelClassAssoAttr assoAttr = new HuaweiModelClassAssoAttr(aAttr, zAttr);
                        assoAttrs.Add(assoAttr);
                    }

                    HuaweiModelClassAsso huaweiModelClassAsso = new HuaweiModelClassAsso(classA, classZ, assoAttrs);
                    huaweiModelClassAssoList.Add(huaweiModelClassAsso);
                }

                foreach (XElement xClassAggr in xModule.Elements(xNs + "ClassAggr"))
                {
                    //string assoClass = xClassAggr.Attribute("AssoClass").Value;
                    string childClass = xClassAggr.Attribute("ChildClass").Value;
                    string parentClass = xClassAggr.Attribute("ParentClass").Value;
                    List<HuaweiModelClassAggrAttr> aggrAttrs = new List<HuaweiModelClassAggrAttr>();
                    foreach (XElement xAggrAttr in xClassAggr.Elements(xNs + "AggrAttr"))
                    {
                        string cAttr = xAggrAttr.Attribute("CAttr").Value;
                        string pAttr = xAggrAttr.Attribute("PAttr").Value;
                        HuaweiModelClassAggrAttr aggrAttr = new HuaweiModelClassAggrAttr(cAttr, pAttr);
                        aggrAttrs.Add(aggrAttr);
                    }

                    HuaweiModelClassAggr classAggr = new HuaweiModelClassAggr(childClass, parentClass, aggrAttrs);
                    huaweiModelClassAggrList.Add(classAggr);
                }
            }


            // build tree
            // TODO: there is some f* up with CLK class, it has 2 parents - BRD and RNCBRD - need to handle it later
            Dictionary<string, TreeItem> treeItemByClassName = new Dictionary<string, TreeItem>(StringComparer.Ordinal);
            foreach (HuaweiModelClassAggr classAggr in huaweiModelClassAggrList)
            {
                TreeItem treeItem = new TreeItem(classAggr);
                if (!treeItemByClassName.ContainsKey(treeItem.ClassAggr.ChildClass))
                    treeItemByClassName.Add(treeItem.ClassAggr.ChildClass, treeItem);
            }
            foreach (HuaweiModelClassAggr classAggr in huaweiModelClassAggrList)
            {
                TreeItem treeItem = treeItemByClassName[classAggr.ChildClass];
                if (treeItemByClassName.TryGetValue(classAggr.ParentClass, out TreeItem parentTreeItem))
                {
                    treeItem.Parent = parentTreeItem;
                    parentTreeItem.Children.Add(treeItem);
                }
            }

            // Handle problem with UFACH/URACH children
            Dictionary<string, HuaweiModelClass> huaweiModelClassByClassName =
                huaweiModelClassList.ToDictionary(o => o.ClassName);
            foreach (var treeItem in treeItemByClassName.Values)
            {
                string className = treeItem.ClassAggr.ChildClass;
                HuaweiModelClass huaweiModelClass = huaweiModelClassByClassName[className];
                huaweiModelClass.PkAttrs.AddRange(huaweiModelClass.Attrs.Where(o => o.IsKey));
                if (treeItem.Children.Any())
                {
                    HashSet<string> pkAttrNames = new HashSet<string>();
                    foreach (TreeItem child in treeItem.Children)
                    {
                        string childClassName = child.ClassAggr.ChildClass;
                        HuaweiModelClass huaweiModelClassChild = huaweiModelClassByClassName[childClassName];
                        foreach (var aggrAttr in child.ClassAggr.AggrAttrs)
                        {
                            if (huaweiModelClassChild.AttrByAttrName[aggrAttr.CAttr].IsCfgAttr)
                            {
                                pkAttrNames.Add(aggrAttr.PAttr);
                            }
                        }
                    }
                    huaweiModelClass.PkAttrsUsedByChildrenPks.AddRange(huaweiModelClass.Attrs.Where(o => pkAttrNames.Contains(o.AttrName)));
                }
                else
                {
                    huaweiModelClass.PkAttrsUsedByChildrenPks.AddRange(huaweiModelClass.Attrs.Where(o => o.IsKey));
                }
            }

            return new HuaweiModel(
                huaweiModelClassList.ToDictionary(o => o.ID),
                huaweiModelClassByClassName,
                treeItemByClassName);
        }
    }
}

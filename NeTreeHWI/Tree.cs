﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace GExportToKVP
{
    public class Tree
    {
        public Tree()
        {
            this.Parent = null;
        }

        public Tree(Tree Parent)
        {
            this.Parent = Parent;
        }

        private Tree Parent;
        public string Name { get; set; }
        public int Level { get; set; }
        public List<Tree> Children = new List<Tree>();
        public override string ToString()
        {
            return GetPname();
        }

        public string GetPname()
        {
            if (this.Parent == null)
                return this.Name;
            else
                return Parent.GetPname() + "," + this.Name;
        }

        public string GetPiMoname(Dictionary<string, Moc> mocs, Dictionary<string, string> parameters, HashSet<string> existingAtt, string ne, out string vsmoname)
        {
            string att = "";
            //var moc = mocs[this.Name.ToUpper()];
            vsmoname = "";
            if (this.Parent == null)
                return this.Name + "=" + ne;

            string ppmoname = Parent.GetPiMoname(mocs, parameters, existingAtt, ne, out vsmoname);
            vsmoname = "";
            if (mocs.ContainsKey(this.Name.ToUpper()))
            {
                //var paramList = parameters.Where(a => mocs[this.Name].KeyAttributes.Select(b => b.OMCName).Contains(a.Key)).ToList();


                //var paramListParentRemoved = paramList.Where(a => !existingAtt.Contains(a.Key)).ToList();
                //att = string.Join(",", paramListParentRemoved.Select(a => string.Join(":", a.Key, a.Value)));
                //foreach (var item in paramListParentRemoved)
                //    existingAtt.Add(item.Key);


                foreach (var item in mocs[this.Name].KeyAttributes)
                {
                    if (parameters.ContainsKey(item.OMCName))
                        if (!existingAtt.Contains(parameters[item.OMCName]))
                        {
                            att += string.Join(":", item.OMCName, parameters[item.OMCName]) + ",";
                            vsmoname += string.Join("=", item.OMCName, item.IsString ? "\"" + parameters[item.OMCName] + "\"" : parameters[item.OMCName]) + ",";
                        }
                }
                att = att.TrimEnd(new char[] { ',' });
                vsmoname = vsmoname.TrimEnd(new char[] { ',' });
            }

            if (string.IsNullOrWhiteSpace(att))
                return ppmoname + "→" + this.Name;
            else
                return ppmoname + "→" + this.Name + "=" + att;

        }

        public string Getmotype()
        {
            if (this.Parent == null)
                return this.Name;
            else
                return Parent.Getmotype() + "," + this.Name.Split('=')[0];
        }

    }

    public static class Extentions
    {
        public static IEnumerable<Tree> Descendants(this Tree root)
        {
            var nodes = new Stack<Tree>(new[] { root });
            while (nodes.Any())
            {
                Tree node = nodes.Pop();
                yield return node;
                foreach (var n in node.Children) nodes.Push(n);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace GExportToKVP
{
    public class Tree
    {
        public Tree(string neName)
        {
            this.NE = neName;
            this.Parent = null;
        }
        public Tree(Tree Parent, string neName)
        {
            this.Parent = Parent;
            this.NE = neName;
        }

        public string NE { get; set; }
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

        public string GetPiMoname(Dictionary<string, Moc> mocs, Dictionary<string, string> parameters, HashSet<string> existingAtt)
        {
            string att = "";
            //var moc = mocs[this.Name.ToUpper()];
            if (this.Parent == null)
                return this.Name + "=" + NE;

            string ppmoname  = Parent.GetPiMoname(mocs, parameters, existingAtt);

            if (mocs.ContainsKey(this.Name.ToUpper()))
            {
                var paramList = parameters.Where(a => mocs[this.Name.ToUpper()].KeyAttributes.Select(b => b.OMCName).Contains(a.Key));

              
                var paramListParentRemoved = paramList.Where(a => !existingAtt.Contains(a.Key));
                att = string.Join(",", paramListParentRemoved.Select(a => string.Join(":", a.Key, a.Value)));
                foreach (var item in paramListParentRemoved)
                    existingAtt.Add(item.Key);
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

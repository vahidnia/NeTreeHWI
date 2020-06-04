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

        public string GetPiMoname(List<Moc> mocs, Dictionary<string, string> parameters)
        {
            string att = "";
            var moc = mocs.FirstOrDefault(a => a.NeName.ToUpper() == this.Name.ToUpper());
            if (moc != null)
                att = string.Join(",", parameters.Where(a => moc.KeyAttributes.Select(b => b.OMCName).Contains(a.Key)).Select(a => string.Join(":", a.Key, a.Value)));
            if (this.Parent == null)
                return "ManagedElement=" + NE + "→" + this.Name;
            else
                if (string.IsNullOrWhiteSpace(att))
                return Parent.GetPiMoname(mocs, parameters) + "→" + this.Name;
            else
                return Parent.GetPiMoname(mocs, parameters) + "→" + this.Name + "=" + att;
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

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

        private string GetPname()
        {
            if (this.Parent == null)
                return this.Name;
            else
                return Parent.GetPname() + "->" + this.Name;
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

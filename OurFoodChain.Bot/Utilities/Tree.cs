using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class TreeNode<T> {

        public T Value { get; set; }

        public int Depth { get; set; } = 0;
        public TreeNode<T> Parent { get; set; } = null;

        public void AddChild(TreeNode<T> node) {

            node.Depth = Depth + 1;
            node.Parent = this;

            _children.Add(node);

        }

        public IReadOnlyList<TreeNode<T>> Children {
            get {
                return _children;
            }
        }

        private List<TreeNode<T>> _children = new List<TreeNode<T>>();

    }

}
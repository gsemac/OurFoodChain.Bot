using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class Tree<T> {

        public class TreeNode {

            public TreeNode() {
                childNodes = new List<TreeNode>();
            }

            public T value;
            public List<TreeNode> childNodes;

        }

        public static int Depth(TreeNode root) {

            if (root.childNodes.Count() <= 0)
                return 1;

            int max_child_depth = 1;

            foreach (TreeNode i in root.childNodes) {

                int d = Depth(i);

                max_child_depth = Math.Max(max_child_depth, d);

            }

            return 1 + max_child_depth;

        }

    }

}
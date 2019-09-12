using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class AncestryTreeTextRenderer {

        // Public members

        public TreeNode<AncestryTree.NodeData> Tree { get; set; } = null;
        public int MaxLength { get; set; } = int.MaxValue;

        public AncestryTreeTextRenderer() {
        }
        public AncestryTreeTextRenderer(TreeNode<AncestryTree.NodeData> tree) {
            Tree = tree;
        }

        public override string ToString() {
            return _treeToString();
        }

        // Private members

        private string _treeToString() {

            if (Tree is null)
                return string.Empty;

            List<string> lines = new List<string>();
            Stack<Tuple<int, int>> sibling_line_positions = new Stack<Tuple<int, int>>();

            TreeUtils.PreOrderTraverse(Tree, x => {

                string line = "";

                line += x.Value.Species.GetTimeStampAsDateString();
                line += " -";

                if (x.Parent != null)
                    for (int i = 0; i < x.Depth * 2 - 1; ++i) {

                        if (sibling_line_positions.Count() > 0 && sibling_line_positions.Any(y => y.Item2 == line.Length + 1))
                            line += "│";
                        else
                            line += " ";

                    }

                else
                    line += " ";

                if (x.Parent != null) {

                    // If this node has a parent, draw a branch leading down it.

                    if (x.Parent.Children.Count() > 1 && x.Parent.Children.Last() != x) {

                        line += "├─";

                        // If this is the first sibling, take note of the index to draw connecting lines.

                        if (sibling_line_positions.Count() == 0 || sibling_line_positions.First().Item1 != x.Depth)
                            sibling_line_positions.Push(new Tuple<int, int>(x.Depth, line.Length - 1));

                    }
                    else if (x.Parent.Children.Last() == x) {

                        line += "└─";

                        // If this is the last sibling, remove the stored index.

                        if (sibling_line_positions.Count() > 0 && sibling_line_positions.First().Item1 == x.Depth)
                            sibling_line_positions.Pop();

                    }

                }

                line += x.Value.Species.ShortName;

                lines.Add(line);

            });

            StringBuilder sb = new StringBuilder();

            foreach (string line in lines) {

                if (sb.Length + line.Length > MaxLength) {

                    sb.AppendLine("...");

                    break;

                }
                else
                    sb.AppendLine(line);

            }

            return sb.ToString();

        }

    }

}
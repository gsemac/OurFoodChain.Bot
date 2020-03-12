using OurFoodChain.Common.Collections;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OurFoodChain {

    public class AncestryTreeTextRenderer {

        // Public members

        public TreeNode<AncestryTree.NodeData> Tree { get; set; } = null;

        public int MaxLength { get; set; } = int.MaxValue;
        public bool DrawLines { get; set; } = true;
        public Func<long, string> TimestampFormatter { get; set; }

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

            // If no tree has been provided, there is nothing to render.

            if (Tree is null)
                return string.Empty;

            // Generate timestamp strings for each entry ahead of time, so we can make sure that they're all the same length.

            int maxTimestampLength = 0;

            Tree.PreOrderTraverse(x => {

                int length = _timestampToString(DateUtilities.GetTimestampFromDate(x.Value.Species.CreationDate)).Length;

                if (length > maxTimestampLength)
                    maxTimestampLength = length;

            });

            // Render the tree.

            List<string> lines = new List<string>();
            Stack<Tuple<int, int>> sibling_line_positions = new Stack<Tuple<int, int>>();

            Tree.PreOrderTraverse(x => {

                string line = "";

                line += _timestampToString(DateUtilities.GetTimestampFromDate(x.Value.Species.CreationDate)).PadRight(maxTimestampLength);
                line += " " + (x.Value.Species.IsExtinct() ? "*" : "-");

                if (DrawLines && x.Parent != null)
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

                        if (DrawLines)
                            line += "├─";

                        // If this is the first sibling, take note of the index to draw connecting lines.

                        if (sibling_line_positions.Count() == 0 || sibling_line_positions.First().Item1 != x.Depth)
                            sibling_line_positions.Push(new Tuple<int, int>(x.Depth, line.Length - 1));

                    }
                    else if (x.Parent.Children.Last() == x) {

                        if (DrawLines)
                            line += "└─";

                        // If this is the last sibling, remove the stored index.

                        if (sibling_line_positions.Count() > 0 && sibling_line_positions.First().Item1 == x.Depth)
                            sibling_line_positions.Pop();

                    }

                }

                line += x.Value.Species.GetShortName();

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
        private string _timestampToString(long timestamp) {

            return TimestampFormatter is null ? DateUtilities.GetDateString(timestamp, DateStringFormat.Short) : TimestampFormatter(timestamp);

        }

    }

}
using OurFoodChain.Common.Collections;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OurFoodChain.Drawing {

    public class DefaultCladogramRenderer :
        CladogramRendererBase {

        // Public members

        public DefaultCladogramRenderer(ICladogram cladogram) {

            this.cladogram = cladogram;

        }

        public override void Render(ICanvas canvas) {

            RenderNode(canvas, BuildTree(cladogram.Root), GetFont());

        }

        // Protected members

        protected class NodeData {

            public CladogramNodeData Data { get; set; }
            public RectangleF Bounds { get; set; } = new RectangleF();

        }

        protected override RectangleF GetTreeBounds() {

            return CalculateTreeBounds(BuildTree(cladogram.Root));

        }

        protected virtual void RenderNode(ICanvas canvas, TreeNode<NodeData> node, Font font) {

            // Cross-out the species if it's extinct.

            string speciesName = TaxonFormatter.GetString(node.Value.Data.Species);
            bool isHighlighted = node.Value.Data.IsAncestor && !node.Children.Any(child => child.Value.Data.IsAncestor);

            if (node.Value.Data.Species.Status.IsExinct && StringUtilities.GetMarkupProperties(speciesName).HasFlag(MarkupProperties.Strikethrough)) {

                using (Brush brush = new SolidBrush(TextColor))
                using (Pen pen = new Pen(brush, 1.0f)) {

                    canvas.DrawLine(pen,
                        new PointF(node.Value.Bounds.X, (float)Math.Round(node.Value.Bounds.Y + node.Value.Bounds.Height / 2.0f)),
                        new PointF(node.Value.Bounds.X + (node.Value.Bounds.Width - 10.0f), (float)Math.Round(node.Value.Bounds.Y + node.Value.Bounds.Height / 2.0f)));

                }

            }

            // Draw the name of the species.

            RenderText(canvas, StringUtilities.StripMarkup(speciesName), new PointF(node.Value.Bounds.X, node.Value.Bounds.Y), font, isHighlighted ? HighlightColor : TextColor);

            // Draw child nodes.

            RenderChildNodes(canvas, node, font);

        }
        protected virtual void RenderChildNodes(ICanvas canvas, TreeNode<NodeData> node, Font font) {

            foreach (TreeNode<NodeData> child in node.Children) {

                using (Brush brush = new SolidBrush(child.Value.Data.IsAncestor ? HighlightColor : TextColor))
                using (Pen pen = new Pen(brush, 2.0f)) {

                    pen.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;

                    canvas.DrawLine(pen,
                        new PointF(node.Value.Bounds.X + (node.Value.Bounds.Width / 2.0f), node.Value.Bounds.Y + node.Value.Bounds.Height),
                        new PointF(child.Value.Bounds.X + (child.Value.Bounds.Width / 2.0f), child.Value.Bounds.Y));

                }

                RenderNode(canvas, child, font);

            }

        }
        protected virtual void RenderText(ICanvas canvas, string text, PointF position, Font font, Color color) {

            canvas.DrawText(text, position, font, color);

        }

        // Private members

        private class NodeBoundsPair {

            public TreeNode<NodeData> Node { get; set; }
            public RectangleF Bounds { get; set; } = new RectangleF();

        }

        private readonly ICladogram cladogram;

        private static Font GetFont() {

            return new Font("Calibri", 12);

        }

        private TreeNode<NodeData> BuildTree(CladogramNode cladogramRoot) {

            TreeNode<NodeData> root = cladogramRoot.Copy(cladogramNodeData => new NodeData {
                Data = cladogramNodeData
            });

            using (Font font = GetFont()) {

                // Measure the size of each node.

                float horizontalPadding = 5.0f;

                root.PostOrderTraverse(node => {

                    SizeF size = DrawingUtilities.MeasureText(StringUtilities.StripMarkup(TaxonFormatter.GetString(node.Value.Data.Species)), font);

                    node.Value.Bounds = new RectangleF(node.Value.Bounds.X, node.Value.Bounds.Y, size.Width + horizontalPadding, size.Height);

                });

                // Calculate node positions.

                CalculateNodePositions(root);

                // Calculate the size of the tree.

                RectangleF bounds = CalculateTreeBounds(root);

                // Shift the tree so that the entire thing is visible.

                float minX = 0.0f;

                root.PostOrderTraverse(node => {

                    if (node.Value.Bounds.X < minX)
                        minX = bounds.X;

                });

                ShiftTree(root, -minX, 0.0f);

                return root;

            }

        }
        private static void ShiftTree(TreeNode<NodeData> root, float deltaX, float deltaY) {

            root.PostOrderTraverse(node => {

                node.Value.Bounds = new RectangleF(
                    node.Value.Bounds.X + deltaX,
                    node.Value.Bounds.Y + deltaY,
                    node.Value.Bounds.Width,
                    node.Value.Bounds.Height
                    );

            });

        }

        private static void CalculateNodePositions(TreeNode<NodeData> node) {

            InitializeNodePositions(node);

            CorrectSubtreeOverlap(node);

        }
        private static void InitializeNodePositions(TreeNode<NodeData> node) {

            // Get the widths of this node's children, and space them out evenly.

            float totalWidth = 0.0f;

            foreach (TreeNode<NodeData> child in node.Children)
                totalWidth += child.Value.Bounds.Width;

            float childX = node.Value.Bounds.X + (node.Value.Bounds.Width / 2.0f) - (totalWidth / 2.0f);
            float childY = node.Value.Bounds.Y + node.Value.Bounds.Height;

            float vertical_spacing = node.Value.Bounds.Height * 1.5f;

            foreach (TreeNode<NodeData> child in node.Children) {

                child.Value.Bounds = new RectangleF(childX, childY + vertical_spacing, child.Value.Bounds.Width, child.Value.Bounds.Height);

                childX += child.Value.Bounds.Width;

            }

            // Do this for each child node as well.

            foreach (TreeNode<NodeData> child in node.Children)
                CalculateNodePositions(child);

        }
        private static void CorrectSubtreeOverlap(TreeNode<NodeData> node) {

            node.PostOrderTraverse(n => {

                // Check if this node has overlapping subtrees.

                if (n.Children.Count() <= 1)
                    return;

                bool has_overlapping = false;
                List<NodeBoundsPair> bounds = new List<NodeBoundsPair>();

                foreach (var child in node.Children)
                    bounds.Add(new NodeBoundsPair {
                        Node = child,
                        Bounds = CalculateTreeBounds(child)
                    });

                for (int i = 1; i < bounds.Count(); ++i) {

                    if (bounds[i].Bounds.Left < bounds[i - 1].Bounds.Right) {
                        has_overlapping = true;

                        break;

                    }

                }

                // If there are overlapping subtrees, move them so that they are no longer overlapping.

                if (has_overlapping) {

                    int left = 0;
                    int right = 0;

                    if (bounds.Count() % 2 == 1) {

                        int middle = (int)Math.Ceiling(bounds.Count() / 2.0f);

                        left = middle - 1;
                        right = middle + 1;

                    }
                    else {

                        left = (bounds.Count() / 2) - 1;
                        right = bounds.Count() / 2;

                    }

                    for (int i = left; i >= 0; --i) {

                        float overlap = bounds[i].Bounds.Right - bounds[i + 1].Bounds.Left;

                        if (overlap <= 0.0f)
                            continue;

                        if (i == left)
                            overlap /= 2.0f;

                        bounds[i].Bounds = new RectangleF(
                            bounds[i].Bounds.X - overlap,
                            bounds[i].Bounds.Y,
                            bounds[i].Bounds.Width,
                            bounds[i].Bounds.Height
                            );

                        ShiftTree(bounds[i].Node, -overlap, 0.0f);

                    }

                    for (int i = right; i < bounds.Count(); ++i) {

                        float overlap = bounds[i - 1].Bounds.Right - bounds[i].Bounds.Left;

                        if (overlap <= 0.0f)
                            continue;

                        if (i == right)
                            overlap /= 2.0f;

                        bounds[i].Bounds = new RectangleF(
                            bounds[i].Bounds.X + overlap,
                            bounds[i].Bounds.Y,
                            bounds[i].Bounds.Width,
                            bounds[i].Bounds.Height
                            );

                        ShiftTree(bounds[i].Node, overlap, 0.0f);

                    }

                }

                // Finally, center the parent node over its children.

                if (node.Children.Any()) {

                    float midpoint = 0.0f;

                    if (node.Children.Count() % 2 == 1) {

                        // Center over the middle child.

                        var centerChild = node.Children.ElementAt((int)Math.Truncate(node.Children.Count() / 2.0f));

                        midpoint = centerChild.Value.Bounds.X + (centerChild.Value.Bounds.Width / 2.0f) - (node.Value.Bounds.Width / 2.0f);

                    }
                    else {

                        // Center between two middle children.

                        var leftCenterChild = node.Children.ElementAt((node.Children.Count() / 2) - 1);
                        var rightCenterChild = node.Children.ElementAt(node.Children.Count() / 2);

                        float leftCenterChildMidpoint = leftCenterChild.Value.Bounds.X + (leftCenterChild.Value.Bounds.Width / 2.0f);
                        float rightCenterChildMidpoint = rightCenterChild.Value.Bounds.X + (rightCenterChild.Value.Bounds.Width / 2.0f);

                        float totalWidth = rightCenterChildMidpoint - leftCenterChildMidpoint;

                        midpoint = leftCenterChildMidpoint + (totalWidth / 2.0f) - (node.Value.Bounds.Width / 2.0f);

                    }

                    node.Value.Bounds = new RectangleF(midpoint, node.Value.Bounds.Y, node.Value.Bounds.Width, node.Value.Bounds.Height);

                }

                //float child_min_x = node.Children.First().Value.Bounds.X;
                //float child_max_x = node.Children.Last().Value.Bounds.X + node.Children.Last().Value.Bounds.Width;
                //float child_width = child_max_x - child_min_x;

                //node.Value.Bounds = new RectangleF(
                //    child_min_x + (child_width / 2.0f) - (node.Value.Bounds.Width / 2.0f),
                //    node.Value.Bounds.Y,
                //    node.Value.Bounds.Width,
                //    node.Value.Bounds.Height
                //    );

            });

        }

        private static RectangleF CalculateTreeBounds(TreeNode<NodeData> node) {

            float minX = node.Value.Bounds.X;
            float maxX = node.Value.Bounds.X + node.Value.Bounds.Width;
            float minY = node.Value.Bounds.Y;
            float maxY = node.Value.Bounds.Y + node.Value.Bounds.Height;

            foreach (TreeNode<NodeData> child in node.Children) {

                RectangleF bounds = CalculateTreeBounds(child);

                minX = Math.Min(bounds.X, minX);
                maxX = Math.Max(bounds.X + bounds.Width, maxX);
                minY = Math.Min(bounds.Y, minY);
                maxY = Math.Max(bounds.Y + bounds.Height, maxY);

            }

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);

        }

    }

}
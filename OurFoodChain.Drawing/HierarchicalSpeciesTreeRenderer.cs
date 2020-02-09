using OurFoodChain.Common;
using OurFoodChain.Common.Collections;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace OurFoodChain.Drawing {

    public sealed class HierarchicalSpeciesTreeRenderer :
        ISpeciesTreeRenderer {

        // Public members

        public Color BackgroundColor { get; set; } = Color.FromArgb(54, 57, 63);
        public Color TextColor { get; set; } = Color.White;
        public Color LineColor { get; set; } = Color.White;
        public Color HighlightColor { get; set; } = Color.Yellow;
        public SizeF Size => CalculateTreeBounds(tree).Size;

        public ISpecies HighlightedSpecies { get; set; }

        public HierarchicalSpeciesTreeRenderer(TreeNode<ISpecies> tree) {

            this.tree = BuildTree(tree);

        }

        public void DrawTo(ICanvas canvas) {

            DrawNode(canvas, tree);

        }

        public void Dispose() {

            if (font != null)
                font.Dispose();

        }

        // Private members

        private class NodeData {

            public ISpecies Species { get; set; }
            public RectangleF Bounds { get; set; } = new RectangleF();

        }

        private class NodeBoundsPair {

            public TreeNode<NodeData> Node { get; set; }
            public RectangleF Bounds { get; set; } = new RectangleF();

        }

        private readonly TreeNode<NodeData> tree;
        private readonly Font font = new Font("Calibri", 12);

        private static TreeNode<NodeData> BuildTree(TreeNode<ISpecies> tree) {

            TreeNode<NodeData> root = tree.Copy(n => new NodeData {
                Species = n
            });

            using (Font font = new Font("Calibri", 12)) {

                // Measure the size of each node.

                float nodePaddingX = 5.0f;

                root.PostOrderTraverse(node => {

                    SizeF textSize = DrawingUtilities.MeasureText(node.Value.Species.BinomialName.ToString(BinomialNameFormat.Abbreviated), font);

                    node.Value.Bounds = new RectangleF(node.Value.Bounds.X, node.Value.Bounds.Y, textSize.Width + nodePaddingX, textSize.Height);

                });

                // Calculate positions for each node.

                CalculateNodePositions(root);

                // Calculate the size of the tree.

                RectangleF bounds = CalculateTreeBounds(root);

                // Shift the tree so that it is within the bounds of the image.

                float minX = 0.0f;

                root.PostOrderTraverse(node => {

                    if (node.Value.Bounds.X < minX)
                        minX = bounds.X;

                });

                ShiftTree(root, -minX, 0.0f);

                return root;

            }

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

                float child_min_x = node.Children.First().Value.Bounds.X;
                float child_max_x = node.Children.Last().Value.Bounds.X + node.Children.Last().Value.Bounds.Width;
                float child_width = child_max_x - child_min_x;

                node.Value.Bounds = new RectangleF(
                    child_min_x + (child_width / 2.0f) - (node.Value.Bounds.Width / 2.0f),
                    node.Value.Bounds.Y,
                    node.Value.Bounds.Width,
                    node.Value.Bounds.Height
                    );

            });

        }
        private static RectangleF CalculateTreeBounds(TreeNode<NodeData> node) {

            float min_x = node.Value.Bounds.X;
            float max_x = node.Value.Bounds.X + node.Value.Bounds.Width;
            float min_y = node.Value.Bounds.Y;
            float max_y = node.Value.Bounds.Y + node.Value.Bounds.Height;

            foreach (TreeNode<NodeData> child in node.Children) {

                RectangleF bounds = CalculateTreeBounds(child);

                min_x = Math.Min(bounds.X, min_x);
                max_x = Math.Max(bounds.X + bounds.Width, max_x);
                min_y = Math.Min(bounds.Y, min_y);
                max_y = Math.Max(bounds.Y + bounds.Height, max_y);

            }

            return new RectangleF(min_x, min_y, max_x - min_x, max_y - min_y);

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

        private void DrawNode(ICanvas canvas, TreeNode<NodeData> node) {

            // Cross-out the species if it's extinct.

            if (node.Value.Species.Status.IsExinct) {

                using (Brush brush = new SolidBrush(TextColor))
                using (Pen pen = new Pen(brush, 1.0f)) {

                    canvas.DrawLine(pen,
                        new PointF(node.Value.Bounds.X, node.Value.Bounds.Y + node.Value.Bounds.Height / 2.0f),
                        new PointF(node.Value.Bounds.X + node.Value.Bounds.Width - 5.0f, node.Value.Bounds.Y + node.Value.Bounds.Height / 2.0f));

                }

            }

            // Draw the name of the species.

            canvas.DrawText(node.Value.Species.ShortName,
                new PointF(node.Value.Bounds.X, node.Value.Bounds.Y),
                font,
                node.Value.Species.Id == HighlightedSpecies.Id ? HighlightColor : TextColor);

            // Draw child nodes.

            foreach (TreeNode<NodeData> child in node.Children) {

                using (Brush brush = new SolidBrush(false ? HighlightColor : TextColor)) // child.Value.IsAncestor
                using (Pen pen = new Pen(brush, 2.0f)) {

                    pen.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;

                    canvas.DrawLine(pen,
                        new PointF(node.Value.Bounds.X + (node.Value.Bounds.Width / 2.0f), node.Value.Bounds.Y + node.Value.Bounds.Height),
                        new PointF(child.Value.Bounds.X + (child.Value.Bounds.Width / 2.0f), child.Value.Bounds.Y));

                }

                DrawNode(canvas, child);

            }

        }

    }

}
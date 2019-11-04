using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class AncestryTreeImageRenderer {

        // Public members

        public static async Task<string> Save(Species species, AncestryTreeGenerationFlags flags) {

            // Generate the ancestry tree.

            TreeNode<AncestryTree.NodeData> ancestry_tree_root = await AncestryTree.GenerateTreeAsync(species, flags);
            TreeNode<AncestryTreeRendererNodeData> root = TreeUtils.CopyAs(ancestry_tree_root, x => {
                return new AncestryTreeRendererNodeData {
                    Species = x.Value.Species,
                    IsAncestor = x.Value.IsAncestor,
                    Bounds = new RectangleF()
                };
            });

            // Generate the evolution tree image.

            using (Font font = new Font("Calibri", 12)) {

                // Calculate the size of each node.

                float horizontal_padding = 5.0f;

                TreeUtils.PostOrderTraverse(root, (node) => {

                    SizeF size = GraphicsUtils.MeasureString(node.Value.Species.ShortName, font);

                    node.Value.Bounds.Width = size.Width + horizontal_padding;
                    node.Value.Bounds.Height = size.Height;

                });

                // Calculate node placements.

                _calculateNodePlacements(root);

                // Calculate the size of the tree.

                RectangleF bounds = _calculateTreeBounds(root);

                // Shift the tree so that the entire thing is visible.

                float min_x = 0.0f;

                TreeUtils.PostOrderTraverse(root, (node) => {

                    if (node.Value.Bounds.X < min_x)
                        min_x = bounds.X;

                });

                _shiftTree(root, -min_x, 0.0f);

                // Create the bitmap.

                using (Bitmap bmp = new Bitmap((int)bounds.Width, (int)bounds.Height))
                using (Graphics gfx = Graphics.FromImage(bmp)) {

                    gfx.Clear(Color.FromArgb(54, 57, 63));
                    gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    _drawSpeciesTreeNode(gfx, root, species, font);

                    // Save the result.

                    string out_dir = Global.TempDirectory + "anc";

                    if (!System.IO.Directory.Exists(out_dir))
                        System.IO.Directory.CreateDirectory(out_dir);

                    string fpath = System.IO.Path.Combine(out_dir, species.ShortName + ".png");

                    bmp.Save(fpath);

                    return fpath;

                }

            }

        }

        // Private members

        private class AncestryTreeRendererNodeData {

            public Species Species { get; set; } = null;
            public bool IsAncestor { get; set; } = false;

            public RectangleF Bounds = new RectangleF();

        }

        private class NodeBoundsPair<T> {
            public TreeNode<T> Node;
            public RectangleF Bounds;
        }

        private static void _drawSpeciesTreeNode(Graphics gfx, TreeNode<AncestryTreeRendererNodeData> node, Species selectedSpecies, Font font) {

            // Cross-out the species if it's extinct.

            if (node.Value.Species.IsExtinct)
                using (Brush brush = new SolidBrush(Color.White))
                using (Pen pen = new Pen(brush, 1.0f))
                    gfx.DrawLine(pen,
                        new PointF(node.Value.Bounds.X, node.Value.Bounds.Y + node.Value.Bounds.Height / 2.0f),
                        new PointF(node.Value.Bounds.X + node.Value.Bounds.Width - 5.0f, node.Value.Bounds.Y + node.Value.Bounds.Height / 2.0f));

            // Draw the name of the species.

            using (Brush brush = new SolidBrush(node.Value.Species.Id == selectedSpecies.Id ? Color.Yellow : Color.White))
                gfx.DrawString(node.Value.Species.ShortName, font, brush, new PointF(node.Value.Bounds.X, node.Value.Bounds.Y));

            // Draw child nodes.

            foreach (TreeNode<AncestryTreeRendererNodeData> child in node.Children) {

                using (Brush brush = new SolidBrush(child.Value.IsAncestor ? Color.Yellow : Color.FromArgb(162, 164, 171)))
                using (Pen pen = new Pen(brush, 2.0f)) {

                    pen.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;

                    gfx.DrawLine(pen,
                        new PointF(node.Value.Bounds.X + (node.Value.Bounds.Width / 2.0f), node.Value.Bounds.Y + node.Value.Bounds.Height),
                        new PointF(child.Value.Bounds.X + (child.Value.Bounds.Width / 2.0f), child.Value.Bounds.Y));

                }

                _drawSpeciesTreeNode(gfx, child, selectedSpecies, font);

            }

        }

        private static float _calculateWidth(TreeNode<AncestryTreeRendererNodeData> root) {

            float total_width = 0.0f;

            foreach (TreeNode<AncestryTreeRendererNodeData> child in root.Children)
                total_width += _calculateWidth(child);

            return Math.Max(total_width, root.Value.Bounds.Width);

        }
        private static RectangleF _calculateTreeBounds(TreeNode<AncestryTreeRendererNodeData> root) {

            float min_x = root.Value.Bounds.X;
            float max_x = root.Value.Bounds.X + root.Value.Bounds.Width;
            float min_y = root.Value.Bounds.Y;
            float max_y = root.Value.Bounds.Y + root.Value.Bounds.Height;

            foreach (TreeNode<AncestryTreeRendererNodeData> child in root.Children) {

                RectangleF bounds = _calculateTreeBounds(child);

                min_x = Math.Min(bounds.X, min_x);
                max_x = Math.Max(bounds.X + bounds.Width, max_x);
                min_y = Math.Min(bounds.Y, min_y);
                max_y = Math.Max(bounds.Y + bounds.Height, max_y);

            }

            return new RectangleF(min_x, min_y, max_x - min_x, max_y - min_y);

        }
        private static void _calculateNodePlacements(TreeNode<AncestryTreeRendererNodeData> root) {

            _initializeNodePlacements(root);

            _fixSubTreeOverlap(root);

        }

        private static void _initializeNodePlacements(TreeNode<AncestryTreeRendererNodeData> node) {

            // Get the widths of this node's children, and space them out evenly.

            float total_width = 0.0f;

            foreach (TreeNode<AncestryTreeRendererNodeData> child in node.Children)
                total_width += child.Value.Bounds.Width;

            float child_x = node.Value.Bounds.X + (node.Value.Bounds.Width / 2.0f) - (total_width / 2.0f);
            float child_y = node.Value.Bounds.Y + node.Value.Bounds.Height;

            float vertical_spacing = node.Value.Bounds.Height * 1.5f;

            foreach (TreeNode<AncestryTreeRendererNodeData> child in node.Children) {

                child.Value.Bounds.X = child_x;
                child.Value.Bounds.Y = child_y + vertical_spacing;
                child_x += child.Value.Bounds.Width;

            }

            // Do this for each child node as well.

            foreach (TreeNode<AncestryTreeRendererNodeData> child in node.Children)
                _calculateNodePlacements(child);

        }
        private static void _fixSubTreeOverlap(TreeNode<AncestryTreeRendererNodeData> node) {

            TreeUtils.PostOrderTraverse(node, (n) => {

                // Check if this node has overlapping subtrees.

                if (n.Children.Count() <= 1)
                    return;

                bool has_overlapping = false;
                List<NodeBoundsPair<AncestryTreeRendererNodeData>> bounds = new List<NodeBoundsPair<AncestryTreeRendererNodeData>>();

                foreach (TreeNode<AncestryTreeRendererNodeData> child in node.Children)
                    bounds.Add(new NodeBoundsPair<AncestryTreeRendererNodeData> {
                        Node = child,
                        Bounds = _calculateTreeBounds(child)
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

                        bounds[i].Bounds.X -= overlap;
                        _shiftTree(bounds[i].Node, -overlap, 0.0f);

                    }

                    for (int i = right; i < bounds.Count(); ++i) {

                        float overlap = bounds[i - 1].Bounds.Right - bounds[i].Bounds.Left;

                        if (overlap <= 0.0f)
                            continue;

                        if (i == right)
                            overlap /= 2.0f;

                        bounds[i].Bounds.X += overlap;
                        _shiftTree(bounds[i].Node, overlap, 0.0f);

                    }

                }

                // Finally, center the parent node over its children.

                float child_min_x = node.Children.First().Value.Bounds.X;
                float child_max_x = node.Children.Last().Value.Bounds.X + node.Children.Last().Value.Bounds.Width;
                float child_width = (child_max_x - child_min_x);

                node.Value.Bounds.X = child_min_x + (child_width / 2.0f) - (node.Value.Bounds.Width / 2.0f);

            });

        }

        private static void _shiftTree(TreeNode<AncestryTreeRendererNodeData> root, float deltaX, float deltaY) {

            TreeUtils.PostOrderTraverse(root, (node) => {

                node.Value.Bounds.X += deltaX;
                node.Value.Bounds.Y += deltaY;

            });

        }

    }

}
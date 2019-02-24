using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class TreeNode<T> {

        public RectangleF bounds = new RectangleF();
        public float mod = 0;

        public T value;
        public TreeNode<T> parent = null;
        public List<TreeNode<T>> children = new List<TreeNode<T>>();

    }

    public class TreeUtils {

        public static int GetDepth<T>(TreeNode<T> root) {

            if (root.children.Count() <= 0)
                return 1;

            int max_child_depth = 1;

            foreach (TreeNode<T> i in root.children) {

                int d = GetDepth(i);

                max_child_depth = Math.Max(max_child_depth, d);

            }

            return 1 + max_child_depth;

        }

        public static void ShiftTree<T>(TreeNode<T> root, float deltaX, float deltaY) {

            PostOrderTraverse(root, (node) => {

                node.bounds.X += deltaX;
                node.bounds.Y += deltaY;

            });

        }

        public static void PostOrderTraverse<T>(TreeNode<T> root, Action<TreeNode<T>> callback) {

            foreach (TreeNode<T> child in root.children)
                PostOrderTraverse(child, callback);

            callback(root);

        }

        public static float CalculateWidth<T>(TreeNode<T> root) {

            float total_width = 0.0f;

            foreach (TreeNode<T> child in root.children)
                total_width += CalculateWidth(child);

            return Math.Max(total_width, root.bounds.Width);

        }
        public static RectangleF CalculateTreeBounds<T>(TreeNode<T> root) {

            float min_x = root.bounds.X;
            float max_x = root.bounds.X + root.bounds.Width;
            float min_y = root.bounds.Y;
            float max_y = root.bounds.Y + root.bounds.Height;

            foreach (TreeNode<T> child in root.children) {

                RectangleF bounds = CalculateTreeBounds(child);

                min_x = Math.Min(bounds.X, min_x);
                max_x = Math.Max(bounds.X + bounds.Width, max_x);
                min_y = Math.Min(bounds.Y, min_y);
                max_y = Math.Max(bounds.Y + bounds.Height, max_y);

            }

            return new RectangleF(min_x, min_y, max_x - min_x, max_y - min_y);

        }
        public static void CalculateNodePlacements<T>(TreeNode<T> root) {

            _initializeNodePlacements(root);

            _fixSubTreeOverlap(root);

        }

        private static void _initializeNodePlacements<T>(TreeNode<T> node) {

            // Get the widths of this node's children, and space them out evenly.

            float total_width = 0.0f;

            foreach (TreeNode<T> child in node.children)
                total_width += child.bounds.Width;

            float child_x = node.bounds.X + (node.bounds.Width / 2.0f) - (total_width / 2.0f);
            float child_y = node.bounds.Y + node.bounds.Height;

            float vertical_spacing = node.bounds.Height * 1.5f;

            foreach (TreeNode<T> child in node.children) {

                child.bounds.X = child_x;
                child.bounds.Y = child_y + vertical_spacing;
                child_x += child.bounds.Width;

            }

            // Do this for each child node as well.

            foreach (TreeNode<T> child in node.children)
                CalculateNodePlacements(child);

        }
        private static void _fixSubTreeOverlap<T>(TreeNode<T> node) {

            PostOrderTraverse(node, (n) => {

                // Check if this node has overlapping subtrees.

                if (n.children.Count() <= 1)
                    return;

                bool has_overlapping = false;
                List<Tuple<TreeNode<T>, RectangleF>> bounds = new List<Tuple<TreeNode<T>, RectangleF>>();

                foreach (TreeNode<T> child in node.children)
                    bounds.Add(new Tuple<TreeNode<T>, RectangleF>(child, CalculateTreeBounds(child)));

                for (int i = 1; i < bounds.Count(); ++i) {

                    if (bounds[i].Item2.Left < bounds[i - 1].Item2.Right) {
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

                        float overlap = bounds[i].Item2.Right - bounds[i + 1].Item2.Left;

                        if (overlap <= 0.0f)
                            continue;

                        if (i == left)
                            overlap /= 2.0f;

                        ShiftTree(bounds[i].Item1, -overlap, 0.0f);

                    }

                    for (int i = right; i < bounds.Count(); ++i) {

                        float overlap = bounds[i - 1].Item2.Right - bounds[i].Item2.Left;

                        if (overlap <= 0.0f)
                            continue;

                        if (i == right)
                            overlap /= 2.0f;

                        ShiftTree(bounds[i].Item1, overlap, 0.0f);

                    }

                }

                // Finally, center the parent node over its children.

                float child_min_x = node.children.First().bounds.X;
                float child_max_x = node.children.Last().bounds.X + node.children.Last().bounds.Width;
                float child_width = (child_max_x - child_min_x);

                node.bounds.X = child_min_x + (child_width / 2.0f) - (node.bounds.Width / 2.0f);

            });

        }

    }

}
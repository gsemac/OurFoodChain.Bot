using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public static class TreeUtils {

        public static int GetDepth<T>(TreeNode<T> root) {

            if (root.Children.Count() <= 0)
                return 1;

            int max_child_depth = 1;

            foreach (TreeNode<T> i in root.Children) {

                int d = GetDepth(i);

                max_child_depth = Math.Max(max_child_depth, d);

            }

            return 1 + max_child_depth;

        }
        public static TreeNode<U> CopyAs<T, U>(TreeNode<T> root, Func<TreeNode<T>, U> func) {

            if (root is null)
                return null;

            TreeNode<U> copy_root = new TreeNode<U> {
                Value = func(root)
            };

            foreach (var child in root.Children)
                copy_root.AddChild(CopyAs(child, func));

            return copy_root;

        }

        public static void PostOrderTraverse<T>(TreeNode<T> root, Action<TreeNode<T>> callback) {

            foreach (TreeNode<T> child in root.Children)
                PostOrderTraverse(child, callback);

            callback(root);

        }
        public static void PreOrderTraverse<T>(TreeNode<T> root, Action<TreeNode<T>> callback) {

            callback(root);

            foreach (TreeNode<T> child in root.Children)
                PreOrderTraverse(child, callback);

        }

    }

}

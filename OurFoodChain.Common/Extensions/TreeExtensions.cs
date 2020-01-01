using OurFoodChain.Common.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OurFoodChain.Common.Extensions {

    public static class TreeExtensions {

        public static int Height<T>(this TreeNode<T> node) {

            if (node.Children.Count() <= 0)
                return 1;

            int maxSubtreeDepth = 1;

            foreach (TreeNode<T> childNode in node.Children) {

                int subtreeDepth = childNode.Height();

                maxSubtreeDepth = Math.Max(maxSubtreeDepth, subtreeDepth);

            }

            return 1 + maxSubtreeDepth;

        }
        public static TreeNode<T> Copy<T>(this TreeNode<T> sourceNode) {

            return Copy(sourceNode, v => v);

        }
        public static TreeNode<U> Copy<T, U>(this TreeNode<T> sourceNode, Func<T, U> transformFunc) {

            if (sourceNode is null)
                return null;

            TreeNode<U> copiedNode = new TreeNode<U> {
                Value = transformFunc(sourceNode.Value)
            };

            foreach (TreeNode<T> childNode in sourceNode.Children)
                copiedNode.Children.Add(Copy(childNode, transformFunc));

            return copiedNode;

        }

        public static void PostOrderTraverse<T>(this TreeNode<T> node, Action<TreeNode<T>> action) {

            foreach (TreeNode<T> child in node.Children)
                PostOrderTraverse(child, action);

            action(node);

        }
        public static void PreOrderTraverse<T>(this TreeNode<T> node, Action<TreeNode<T>> action) {

            action(node);

            foreach (TreeNode<T> child in node.Children)
                PreOrderTraverse(child, action);

        }

    }

}
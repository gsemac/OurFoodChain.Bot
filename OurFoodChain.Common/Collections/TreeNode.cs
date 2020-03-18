using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OurFoodChain.Common.Collections {

    public static class TreeNode {

        public static TreeNode<T> Empty<T>() {

            return new TreeNode<T>();

        }

    }

    public class TreeNode<T> :
        ICollection<TreeNode<T>> {

        // Public members

        public T Value { get; set; }
        public int Depth => Parent is null ? 0 : Parent.Depth + 1;
        public TreeNode<T> Parent { get; set; }
        public ICollection<TreeNode<T>> Children {
            get {

                if (_children is null)
                    _children = new TreeNodeCollection<T>(this);

                return _children;

            }
        }

        public int Count => Children.Sum(node => node.Count) + 1;
        public bool IsReadOnly => false;

        public void Add(TreeNode<T> item) {

            Children.Add(item);

        }
        public void Clear() {

            Children.Clear();

        }
        public bool Contains(TreeNode<T> item) {

            return Children.Contains(item) || Children.Any(node => node.Contains(item));

        }
        public bool Contains(T item) {

            return Children.Any(node => node.Value.Equals(item)) || Children.Any(node => node.Contains(item));

        }
        public void CopyTo(TreeNode<T>[] array, int arrayIndex) {

            Children.CopyTo(array, arrayIndex);

        }
        public bool Remove(TreeNode<T> item) {

            return Children.Remove(item);

        }

        public IEnumerator<TreeNode<T>> GetEnumerator() {

            return Children.GetEnumerator();

        }
        IEnumerator IEnumerable.GetEnumerator() {

            return Children.GetEnumerator();

        }

        // Private members

        private TreeNodeCollection<T> _children = null;

    }

}
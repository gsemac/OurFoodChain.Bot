using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Collections {

    public class TreeNodeCollection<T> :
        ICollection<TreeNode<T>> {

        // Public members

        public int Count => nodes.Count;
        public bool IsReadOnly => false;

        public TreeNodeCollection() :
            this(null) {
        }
        public TreeNodeCollection(TreeNode<T> parentNode) {

            this.parentNode = parentNode;

        }

        public void Add(TreeNode<T> item) {

            if (parentNode != null)
                item.Parent = parentNode;

            nodes.Add(item);

        }
        public void Clear() {

            nodes.Clear();

        }
        public bool Contains(TreeNode<T> item) {

            return nodes.Contains(item);

        }
        public void CopyTo(TreeNode<T>[] array, int arrayIndex) {

            nodes.CopyTo(array, arrayIndex);

        }
        public IEnumerator<TreeNode<T>> GetEnumerator() {

            return nodes.GetEnumerator();

        }
        public bool Remove(TreeNode<T> item) {

            return nodes.Remove(item);

        }
        IEnumerator IEnumerable.GetEnumerator() {

            return nodes.GetEnumerator();

        }

        // Private members

        private readonly TreeNode<T> parentNode = null;
        private readonly List<TreeNode<T>> nodes = new List<TreeNode<T>>();

    }


}
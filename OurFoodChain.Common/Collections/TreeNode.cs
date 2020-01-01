using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Collections {

    public class TreeNode<T> :
        IEnumerable<TreeNode<T>> {

        // Public members

        public T Value { get; set; }
        public int Depth { get; private set; } = 0;
        public TreeNode<T> Parent { get; private set; }
        public ICollection<TreeNode<T>> Children => _children;

        public IEnumerator<TreeNode<T>> GetEnumerator() {

            return _children.GetEnumerator();

        }
        IEnumerator IEnumerable.GetEnumerator() {

            return _children.GetEnumerator();

        }

        // Private members

        private readonly List<TreeNode<T>> _children = new List<TreeNode<T>>();

    }

}
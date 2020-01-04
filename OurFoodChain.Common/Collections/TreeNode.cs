using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Collections {

    public class TreeNode<T> :
        IEnumerable<TreeNode<T>> {

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
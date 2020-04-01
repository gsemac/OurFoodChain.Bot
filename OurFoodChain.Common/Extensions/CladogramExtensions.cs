using OurFoodChain.Common.Collections;
using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OurFoodChain.Common.Extensions {

    public static class CladogramExtensions {

        // Public members

        public static void Trim(this ICladogram cladogram) {

            // Trim all extinct branches from the cladogram.

            RemoveExtinctBranches(cladogram.Root);

        }

        // Private members

        private static void RemoveExtinctBranches(TreeNode<CladogramNodeData> root) {

            foreach (var node in root.Children)
                RemoveExtinctBranches(node);

            var trimmedChildNodes = root.Children.Where(child => child.Children.Count() <= 0 && child.Value.Species.IsExtinct()).
              ToArray();

            foreach (var node in trimmedChildNodes)
                root.Remove(node);

        }

    }

}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public enum AncestryTreeGenerationFlags {
        None = 0,
        Full = 1,
        AncestorsOnly = 2,
        DescendantsOnly = 4
    }

    public class AncestryTree {

        public class NodeData {

            public Species Species { get; set; } = null;
            public bool IsAncestor { get; set; } = false;

        }

        public static async Task<TreeNode<NodeData>> GenerateTreeAsync(Species species, AncestryTreeGenerationFlags flags) {

            // Start by finding the earliest ancestor of this species.

            List<long> ancestor_ids = new List<long>();

            if (!flags.HasFlag(AncestryTreeGenerationFlags.DescendantsOnly))
                ancestor_ids.AddRange(await SpeciesUtils.GetAncestorIdsAsync(species.Id));

            ancestor_ids.Add(species.Id);

            // Starting from the earliest ancestor, generate all tiers, down to the latest descendant.

            TreeNode<NodeData> root = new TreeNode<NodeData> {
                Value = new NodeData {
                    Species = await SpeciesUtils.GetSpeciesAsync(ancestor_ids.First()),
                    IsAncestor = true
                }
            };

            Queue<TreeNode<NodeData>> queue = new Queue<TreeNode<NodeData>>();
            queue.Enqueue(root);

            while (queue.Count() > 0) {

                Species[] descendants = await SpeciesUtils.GetDirectDescendantsAsync(queue.First().Value.Species);

                foreach (Species descendant in descendants) {

                    TreeNode<NodeData> node = new TreeNode<NodeData> {
                        Value = new NodeData {
                            Species = descendant,
                            IsAncestor = ancestor_ids.Contains(descendant.Id)
                        }
                    };

                    if (!flags.HasFlag(AncestryTreeGenerationFlags.AncestorsOnly) || node.Value.IsAncestor) {

                        queue.First().AddChild(node);
                        queue.Enqueue(node);

                    }

                }

                queue.Dequeue();

            }

            return root;

        }

    }

}
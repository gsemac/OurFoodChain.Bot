using OurFoodChain.Common.Collections;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Taxa {

    public class CladogramNodeData {

        public ISpecies Species { get; }
        public bool IsAncestor { get; }

        public CladogramNodeData(ISpecies species, bool isAncestor) {

            this.Species = species;
            this.IsAncestor = isAncestor;

        }

    }

    public class CladogramNode :
        TreeNode<CladogramNodeData> {

        public CladogramNode(ISpecies species, bool isAncestor) {

            Value = new CladogramNodeData(species, isAncestor);

        }

    }

}
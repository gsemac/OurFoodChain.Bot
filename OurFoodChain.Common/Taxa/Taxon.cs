using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OurFoodChain.Common.Taxa {

    public class Taxon :
        TaxonBase {

        // Public members

        public override ITaxonRank Rank { get; protected set; }

        public Taxon(string name, TaxonRankType rank) {

            this.Name = name;
            this.Rank = new TaxonRank(rank);

        }

    }

}
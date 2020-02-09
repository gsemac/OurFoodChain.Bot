using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Taxa {

    public class Taxon :
        ITaxon {

        // Public members

        public long? Id { get; set; }
        public long? ParentId { get; set; }
        public ITaxonRank Rank { get; private set; }
        public string Name { get; set; }

        public Taxon(string name, TaxonRankType rank) {

            this.Name = name;
            this.Rank = new TaxonRank(rank);

        }

    }

}
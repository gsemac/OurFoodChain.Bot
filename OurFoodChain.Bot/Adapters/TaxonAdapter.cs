using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Adapters {

    public class TaxonAdapter :
        ITaxon {

        // Public members

        public long? Id {
            get => taxon.id;
            set => taxon.id = value.GetValueOrDefault(Taxon.NullId);
        }
        public long? ParentId {
            get => taxon.parent_id;
            set => taxon.parent_id = value.GetValueOrDefault(Taxon.NullId);
        }
        public ITaxonRank Rank {
            get {

                switch (taxon.type) {

                    case TaxonRank.Domain:
                        return new Common.Taxa.TaxonRank(TaxonRankType.Domain);

                    case TaxonRank.Kingdom:
                        return new Common.Taxa.TaxonRank(TaxonRankType.Kingdom);

                    case TaxonRank.Phylum:
                        return new Common.Taxa.TaxonRank(TaxonRankType.Phylum);

                    case TaxonRank.Class:
                        return new Common.Taxa.TaxonRank(TaxonRankType.Class);

                    case TaxonRank.Order:
                        return new Common.Taxa.TaxonRank(TaxonRankType.Order);

                    case TaxonRank.Family:
                        return new Common.Taxa.TaxonRank(TaxonRankType.Family);

                    case TaxonRank.Genus:
                        return new Common.Taxa.TaxonRank(TaxonRankType.Genus);

                    case TaxonRank.Species:
                        return new Common.Taxa.TaxonRank(TaxonRankType.Species);

                    default:
                        return new Common.Taxa.TaxonRank(TaxonRankType.None);

                }

            }
        }
        public string Name {
            get => taxon.GetName();
            set => taxon.name = value;
        }

        public TaxonAdapter(Taxon taxon) {

            this.taxon = taxon;

        }

        // Private members

        private readonly Taxon taxon;

    }

}
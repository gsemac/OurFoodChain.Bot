using OurFoodChain.Common;
using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Adapters {

    public class TaxonAdapter :
        TaxonBase {

        // Public members

        public override long? Id {
            get => taxon.id;
            set => taxon.id = value.GetValueOrDefault(Taxon.NullId);
        }
        public override long? ParentId {
            get => taxon.parent_id;
            set => taxon.parent_id = value.GetValueOrDefault(Taxon.NullId);
        }
        public override ITaxonRank Rank {
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
        public override string Name {
            get => taxon.GetName();
            set => taxon.name = value;
        }
        public override string Description {
            get => taxon.description;
            set => taxon.description = value;
        }

        public TaxonAdapter(Taxon taxon) {

            this.taxon = taxon;

            if (!string.IsNullOrEmpty(taxon.CommonName))
                CommonNames.Add(taxon.CommonName);

            if (!string.IsNullOrEmpty(taxon.pics))
                Pictures.Add(new Picture(taxon.pics));

        }

        // Private members

        private readonly Taxon taxon;

    }

}
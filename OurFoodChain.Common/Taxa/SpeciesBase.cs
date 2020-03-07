using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OurFoodChain.Common.Taxa {

    public abstract class SpeciesBase :
        ISpecies {

        public virtual ITaxon Genus { get; set; }
        public virtual ICreator Creator { get; set; }
        public virtual DateTimeOffset CreationDate { get; set; } = DateUtilities.GetCurrentDateUtc();
        public virtual IConservationStatus Status { get; set; } = new ConservationStatus();
        public virtual long? Id { get; set; }
        public virtual string Name { get; set; }
        public virtual ICollection<string> CommonNames { get; set; } = new List<string>();
        public virtual string Description { get; set; }
        public virtual ICollection<IPicture> Pictures { get; set; } = new List<IPicture>();

        public BinomialName BinomialName => new BinomialName(Genus.Name, Name);
        public long? ParentId {
            get => Genus.Id;
            set => throw new InvalidOperationException("Set the parent taxon directly through the Genus property.");
        }
        public ITaxonRank Rank => new TaxonRank(TaxonRankType.Species);

        public override string ToString() {

            return BinomialName.ToString();

        }

    }

}
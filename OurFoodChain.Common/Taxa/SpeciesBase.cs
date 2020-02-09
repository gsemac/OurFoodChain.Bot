using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Taxa {

    public abstract class SpeciesBase :
        ISpecies {

        public abstract ITaxon Genus { get; set; }
        public abstract ICreator Creator { get; set; }
        public abstract DateTimeOffset CreationDate { get; set; }
        public abstract string Description { get; set; }
        public abstract IConservationStatus Status { get; set; }
        public abstract long? Id { get; set; }
        public abstract string Name { get; set; }
        public abstract IPicture Picture { get; set; }

        public BinomialName BinomialName => new BinomialName(Genus.Name, Name);
        public string FullName => BinomialName.ToString(BinomialNameFormat.Full);
        public string ShortName => BinomialName.ToString(BinomialNameFormat.Abbreviated);
        public long? ParentId {
            get => Genus.Id;
            set => throw new InvalidOperationException("Set the parent taxon directly through the Genus property.");
        }
        public ITaxonRank Rank => new TaxonRank(TaxonRankType.Species);

    }

}
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Taxa {

    public abstract class SpeciesBase :
        ISpecies {

        public virtual ITaxon Genus { get; set; }
        public virtual ICreator Creator { get; set; }
        public virtual DateTimeOffset CreationDate { get; set; } = DateUtilities.GetCurrentUtcDate();
        public virtual string Description { get; set; }
        public virtual IConservationStatus Status { get; set; } = new ConservationStatus();
        public virtual long? Id { get; set; }
        public virtual string Name { get; set; }
        public virtual IPicture Picture { get; set; }

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
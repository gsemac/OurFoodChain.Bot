using OurFoodChain.Common;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Taxa {

    public class Species :
        ISpecies {

        public ITaxon Genus { get; set; }
        public BinomialName BinomialName => new BinomialName(Genus.Name, Name);
        public string FullName => BinomialName.ToString(BinomialNameFormat.Full);
        public string ShortName => BinomialName.ToString(BinomialNameFormat.Abbreviated);
        public ICreator Creator { get; set; }
        public DateTimeOffset CreationDate { get; set; } = DateUtilities.GetCurrentUtcDate();
        public string Description { get; set; }
        public IConservationStatus Status { get; set; } = new ConservationStatus();
        public int? Id { get; set; }
        public int? ParentId { get; set; }
        public ITaxonRank Rank => new TaxonRank(TaxonRankType.Species);
        public string Name { get; set; }

    }

}
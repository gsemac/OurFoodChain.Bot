using OurFoodChain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Taxa {

    public interface ISpecies :
        ITaxon {

        ITaxon Genus { get; set; }
        BinomialName BinomialName { get; }
        string FullName { get; }
        string ShortName { get; }
        ICreator Creator { get; set; }
        DateTimeOffset CreationDate { get; set; }
        string Description { get; set; }
        IConservationStatus Status { get; set; }
        IPicture Picture { get; set; }

    }

}
using OurFoodChain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Taxa {

    public interface ISpecies :
        ITaxon {

        ITaxon Genus { get; set; }
        BinomialName BinomialName { get; }
        IUser Creator { get; set; }
        DateTimeOffset CreationDate { get; set; }
        IConservationStatus Status { get; set; }

    }

}
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common {

    public enum BinomialNameFormat {
        Full,
        Abbreviated,
        Species
    }

    public interface IBinomialName {

        string GenusName { get; }
        string SpeciesName { get; }
        bool IsAbbreviated { get; }

    }

}
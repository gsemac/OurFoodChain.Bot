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

        string Genus { get; }
        string Species { get; }
        bool IsAbbreviated { get; }

    }

}
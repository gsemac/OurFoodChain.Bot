using OurFoodChain.Common.Collections;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Taxa {

    [Flags]
    public enum CladogramOptions {
        None = 0,
        Ancestors = 1,
        Descendants = 2,
        Full = Ancestors | Descendants,
        Default = Full
    }

    public interface ICladogram {

        CladogramNode Root { get; }

    }

}
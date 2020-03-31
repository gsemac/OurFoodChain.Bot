using OurFoodChain.Common.Collections;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Taxa {

    public enum CladogramType {
        Full,
        Ancestors,
        Descendants,
        Default = Full
    }

    public interface ICladogram {

        CladogramNode Root { get; }

    }

}
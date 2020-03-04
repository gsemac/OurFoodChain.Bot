using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Data.Queries {

    public interface ISearchQuery {

        IEnumerable<string> Keywords { get; }
        IEnumerable<ISearchModifier> Modifiers { get; }

    }

}
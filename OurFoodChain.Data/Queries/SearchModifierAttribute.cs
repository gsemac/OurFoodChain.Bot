using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Data.Queries {

    public sealed class SearchModifierAttribute :
        Attribute {

        public IEnumerable<string> Aliases { get; }

        public SearchModifierAttribute(params string[] aliases) {

            this.Aliases = aliases;

        }

    }

}
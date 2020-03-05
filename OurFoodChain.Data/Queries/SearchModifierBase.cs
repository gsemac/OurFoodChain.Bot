using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    public abstract class SearchModifierBase :
        ISearchModifier {

        public string Name { get; set; }
        public string Value { get; set; }
        public bool Invert { get; set; } = false;

        public abstract Task ApplyAsync(ISearchContext context, ISearchResult result);

    }

}

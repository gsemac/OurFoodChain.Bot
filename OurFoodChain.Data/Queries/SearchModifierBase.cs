using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    public abstract class SearchModifierBase :
        ISearchModifier {

        public string Name { get; set; }
        public string Value { get; set; }
        public IEnumerable<string> Values => Value.Split(',').Select(v => v.Trim()).Where(v => !string.IsNullOrWhiteSpace(v));
        public bool Invert { get; set; } = false;

        public abstract Task ApplyAsync(ISearchContext context, ISearchResult result);

    }

}
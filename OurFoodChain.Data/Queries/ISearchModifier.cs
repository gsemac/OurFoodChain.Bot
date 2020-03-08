using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    public interface ISearchModifier {

        string Name { get; set; }
        string Value { get; set; }
        IEnumerable<string> Values { get; }
        bool Invert { get; set; }

        Task ApplyAsync(ISearchContext context, ISearchResult result);

    }

}
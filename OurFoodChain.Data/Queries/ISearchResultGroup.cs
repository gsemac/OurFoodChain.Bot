using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    public interface ISearchResultGroup :
    IEnumerable<ISpecies> {

        string Name { get; set; }
        ICollection<ISpecies> Items { get; }

        Task SortByAsync(IComparer<ISpecies> resultComparer);
        Task FormatByAsync(Func<ISpecies, Task<string>> formatterFunction);

        Task<IEnumerable<ISpecies>> GetResultsAsync();
        Task<IEnumerable<string>> GetStringResultsAsync();

    }

}
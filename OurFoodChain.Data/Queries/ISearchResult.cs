using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    public enum SearchResultDisplayFormat {
        None,
        Gallery,
        Leaderboard
    }

    public interface ISearchResult :
        IEnumerable<ISearchResultGroup> {

        ISearchResultGroup DefaultGroup { get; }
        IEnumerable<ISearchResultGroup> Groups { get; }
        SearchResultDisplayFormat DisplayFormat { get; set; }

        bool HasDefaultOrdering { get; }
        bool HasDefaultGrouping { get; }

        ISearchResultGroup Add(string groupName, ISpecies species);
        ISearchResultGroup Add(string groupName, IEnumerable<ISpecies> species);
        int Count();

        bool ContainsGroup(string groupName);

        Task GroupByAsync(Func<ISpecies, Task<IEnumerable<string>>> groupingFunction);
        Task FilterByAsync(Func<ISpecies, Task<bool>> filterFunction, bool invertCondition = false);
        Task OrderByAsync(IComparer<ISearchResultGroup> groupComparer);
        Task OrderByAsync(IComparer<ISpecies> resultComparer);
        Task FormatByAsync(Func<ISpecies, Task<string>> formatterFunction);

        Task<IEnumerable<ISpecies>> GetResultsAsync();
        Task<IEnumerable<string>> GetStringResultsAsync();

    }

}
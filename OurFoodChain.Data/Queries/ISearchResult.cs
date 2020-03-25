using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
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

    public delegate Task<IEnumerable<string>> SpeciesGroupFunction(ISpecies species);
    public delegate Task<bool> SpeciesFilterFunction(ISpecies species);

    public interface ISearchResult :
        IEnumerable<ISearchResultGroup> {

        ISearchResultGroup DefaultGroup { get; }
        IEnumerable<ISearchResultGroup> Groups { get; }
        SearchResultDisplayFormat DisplayFormat { get; set; }
        ITaxonFormatter TaxonFormatter { get; }

        bool HasDefaultOrdering { get; }
        bool HasDefaultGrouping { get; }

        DateTimeOffset Date { get; set; }

        ISearchResultGroup Add(string groupName, ISpecies species);
        ISearchResultGroup Add(string groupName, IEnumerable<ISpecies> species);
        int TotalResults();

        bool ContainsGroup(string groupName);

        Task GroupByAsync(SpeciesGroupFunction groupingFunction);
        Task FilterByAsync(SpeciesFilterFunction filterFunction, bool invertCondition = false);
        Task OrderByAsync(IComparer<ISearchResultGroup> groupComparer);
        Task OrderByAsync(IComparer<ISpecies> resultComparer);
        Task FormatByAsync(ITaxonFormatter formatter);

        Task<IEnumerable<ISpecies>> GetResultsAsync();
        Task<IEnumerable<string>> GetStringResultsAsync();

    }

}
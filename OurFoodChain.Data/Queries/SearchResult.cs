using OurFoodChain.Common.Collections;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    public class SearchResult :
        ISearchResult {

        public const string DefaultGroupName = "";

        public ISearchResultGroup DefaultGroup => groups[DefaultGroupName];
        public IEnumerable<ISearchResultGroup> Groups {
            get {

                List<ISearchResultGroup> groups = new List<ISearchResultGroup>();

                foreach (ISearchResultGroup group in this.groups.Values)
                    groups.Add(group);

                if (groupComparer != null)
                    groups.Sort(groupComparer);

                return groups;

            }
        }
        public SearchResultDisplayFormat DisplayFormat { get; set; } = SearchResultDisplayFormat.None;

        public bool HasDefaultOrdering => hasDefaultOrdering;
        public bool HasDefaultGrouping => hasDefaultGrouping;

        public SearchResult() { }
        public SearchResult(IEnumerable<ISpecies> results) {

            Add(DefaultGroupName, results);

        }

        public void Add(string groupName, ISpecies species) {

            Add(groupName, new ISpecies[] { species });

        }
        public void Add(string groupName, IEnumerable<ISpecies> species) {

            if (!groups.ContainsKey(groupName)) {

                groups.Add(groupName, new SearchResultGroup(groupName, species));

            }
            else
                groups[groupName].Items.AddRange(species);

        }
        public int Count() {

            int count = 0;

            foreach (ISearchResultGroup group in groups.Values)
                count += group.Count();

            return count;

        }

        public bool ContainsGroup(string groupName) {

            return groups.ContainsKey(groupName);

        }

        public async Task GroupByAsync(Func<ISpecies, Task<IEnumerable<string>>> groupingFunction) {

            // Take all species that have already been grouped, assembly them into a single list, and then clear the groupings.
            // Remove any duplicates in the process, since species may have been assigned to multiple groups.

            List<ISpecies> species = new List<ISpecies>();

            foreach (ISearchResultGroup group in groups.Values)
                species.AddRange(group.Items);

            groups.Clear();

            species = species.GroupBy(x => x.Id).Select(x => x.First()).ToList();

            // Assign the species into groups according to the callback.

            foreach (Species s in species)
                foreach (string group in await groupingFunction(s))
                    Add(group, s);

            hasDefaultGrouping = false;

        }
        public async Task FilterByAsync(Func<ISpecies, Task<bool>> filterFunction, bool invertCondition = false) {

            foreach (ISearchResultGroup group in groups.Values) {

                int index = group.Items.Count() - 1;

                while (index >= 0) {

                    bool conditionMet = await filterFunction(group.Items.ElementAt(index));

                    if ((conditionMet && !invertCondition) || (!conditionMet && invertCondition))
                        group.Items.RemoveAt(index);

                    --index;

                }

            }

            // Remove all empty groups.

            foreach (string key in groups.Keys.Where(x => groups[x].Count() <= 0))
                groups.Remove(key);

        }
        public async Task OrderByAsync(IComparer<ISearchResultGroup> groupComparer) {

            // Sorting is performed when we access the search results.

            this.groupComparer = groupComparer;

            await Task.CompletedTask;

        }
        public async Task OrderByAsync(IComparer<ISpecies> resultComparer) {

            foreach (ISearchResultGroup group in groups.Values)
                await group.SortByAsync(resultComparer);

            hasDefaultOrdering = false;

        }
        public async Task FormatByAsync(Func<ISpecies, Task<string>> formatterFunction) {

            foreach (ISearchResultGroup group in groups.Values)
                await group.FormatByAsync(formatterFunction);

        }

        public async Task<IEnumerable<ISpecies>> GetResultsAsync() {

            List<ISpecies> results = new List<ISpecies>();

            foreach (ISearchResultGroup group in Groups)
                results.AddRange(await group.GetResultsAsync());

            return results;

        }
        public async Task<IEnumerable<string>> GetStringResultsAsync() {

            List<string> results = new List<string>();

            foreach (ISearchResultGroup group in Groups)
                results.AddRange(await group.GetStringResultsAsync());

            return results;

        }

        public IEnumerator<ISearchResultGroup> GetEnumerator() {

            return Groups.GetEnumerator();

        }
        IEnumerator IEnumerable.GetEnumerator() {

            return GetEnumerator();

        }

        // Private members

        private readonly SortedDictionary<string, ISearchResultGroup> groups = new SortedDictionary<string, ISearchResultGroup>(new NaturalStringComparer());
        private IComparer<ISearchResultGroup> groupComparer = null;
        private bool hasDefaultGrouping = true;
        private bool hasDefaultOrdering = true;

    }

}
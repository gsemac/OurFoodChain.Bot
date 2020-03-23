using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    public class SearchResultGroup :
        ISearchResultGroup {

        // Public members

        public string Name { get; set; }
        public ICollection<ISpecies> Items { get; } = new List<ISpecies>();

        public SearchResultGroup(string name, IEnumerable<ISpecies> items) {

            Name = name;
            Items = new List<ISpecies>(items);

        }

        public async Task SortByAsync(IComparer<ISpecies> resultComparer) {

            speciesComparers.Add(resultComparer);

            await Task.CompletedTask;

        }
        public async Task FormatByAsync(SpeciesFormatFunction formatterFunction) {

            this.formatter = formatterFunction;

            await Task.CompletedTask;

        }

        public IEnumerable<ISpecies> GetResults() {

            IComparer<ISpecies> firstComparer = speciesComparers
                .FirstOrDefault() ?? Comparer<ISpecies>.Create((lhs, rhs) => lhs.GetShortName().CompareTo(rhs.GetShortName()));

            IOrderedEnumerable<ISpecies> results = Items.OrderBy(species => species, firstComparer);

            foreach (IComparer<ISpecies> comparer in speciesComparers.Skip(1))
                results.ThenBy(species => species, comparer);

            return results;

        }
        public IEnumerable<string> GetStringResults() {

            return GetStringResultsAsync().Result;

        }

        public async Task<IEnumerable<ISpecies>> GetResultsAsync() {

            return await Task.FromResult(GetResults());

        }
        public async Task<IEnumerable<string>> GetStringResultsAsync() {

            List<string> items = new List<string>();

            foreach (ISpecies species in await GetResultsAsync())
                items.Add(await ResultToString(species));

            return items;

        }

        public IEnumerator<ISpecies> GetEnumerator() {

            return Items.GetEnumerator();

        }
        IEnumerator IEnumerable.GetEnumerator() {

            return GetEnumerator();

        }

        // Private members

        private SpeciesFormatFunction formatter = null;
        private List<IComparer<ISpecies>> speciesComparers = new List<IComparer<ISpecies>>();

        private async Task<string> ResultToString(ISpecies species) {

            string result;

            if (formatter != null)
                result = await formatter(species);
            else
                result = species.GetShortName();

            if (species.Status.IsExinct)
                result = string.Format("~~{0}~~", result);

            return result;

        }

    }

}
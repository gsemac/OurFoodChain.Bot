using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using System;
using System.Collections;
using System.Collections.Generic;
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

            (Items as List<ISpecies>).Sort(resultComparer);

            await Task.CompletedTask;

        }
        public async Task FormatByAsync(Func<ISpecies, Task<string>> formatterFunction) {

            this.formatter = formatterFunction;

            await Task.CompletedTask;

        }

        public IEnumerable<ISpecies> GetResults() {

            return Items;

        }
        public IEnumerable<string> GetStringResults() {

            return GetStringResultsAsync().Result;

        }

        public async Task<IEnumerable<ISpecies>> GetResultsAsync() {

            return await Task.FromResult(GetResults());

        }
        public async Task<IEnumerable<string>> GetStringResultsAsync() {

            List<string> items = new List<string>();

            foreach (ISpecies species in this)
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

        private Func<ISpecies, Task<string>> formatter = null;

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
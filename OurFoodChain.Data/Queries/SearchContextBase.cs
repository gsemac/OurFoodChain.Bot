using OurFoodChain.Common;
using OurFoodChain.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    public abstract class SearchContextBase :
         ISearchContext {

        // Public members

        public abstract SQLiteDatabase Database { get; }

        public void RegisterSearchModifier<T>() where T : ISearchModifier, new() {

            if (typeof(T).GetCustomAttributes(typeof(SearchModifierAttribute), true).FirstOrDefault() is SearchModifierAttribute attribute)
                foreach (string name in attribute.Aliases)
                    modifiers.Add(name.ToLowerInvariant(), () => new T());

        }
        public ISearchModifier GetSearchModifier(string modifier) {

            int splitIndex = modifier.IndexOf(':');
            string name = modifier.Substring(0, splitIndex).Trim();
            string value = modifier.Substring(splitIndex + 1, modifier.Length - splitIndex - 1).Trim();
            bool invert = name.Length > 0 ? name[0] == '-' : false;

            if (name.StartsWith("-"))
                name = name.Substring(1, name.Length - 1);

            // Trim outer quotes from the value.

            if (value.Length > 1 && value.First() == '"' && value.Last() == '"')
                value = value.Trim('"');

            ISearchModifier searchModifier = GetSearchModifier(name, value);

            if (searchModifier != null)
                searchModifier.Invert = invert;

            return searchModifier;

        }
        public ISearchModifier GetSearchModifier(string name, string value) {

            ISearchModifier modifier = modifiers.GetOrDefault(name.ToLowerInvariant())?.Invoke();

            if (modifier != null) {

                modifier.Name = name;
                modifier.Value = value;

            }

            return modifier;

        }

        public virtual async Task<ICreator> GetCreatorAsync(ICreator creator) => await Task.FromResult(creator);

        // Protected members

        protected SearchContextBase() {

            // Register default modifiers.

            RegisterSearchModifier<GroupBySearchModifier>();
            RegisterSearchModifier<OrderBySearchModifier>();
            RegisterSearchModifier<ZoneSearchModifier>();
            RegisterSearchModifier<RoleSearchModifier>();
            RegisterSearchModifier<FormatBySearchModifier>();
            RegisterSearchModifier<CreatorSearchModifier>();
            RegisterSearchModifier<StatusSearchModifier>();
            RegisterSearchModifier<TaxonSearchModifier>();
            RegisterSearchModifier<RandomSearchModifier>();
            RegisterSearchModifier<PreySearchModifier>();
            RegisterSearchModifier<PreyNotesSearchModifier>();
            RegisterSearchModifier<PredatorSearchModifier>();
            RegisterSearchModifier<HasSearchModifier>();
            RegisterSearchModifier<AncestorSearchModifier>();
            RegisterSearchModifier<DescendantSearchModifier>();
            RegisterSearchModifier<LimitSearchModifier>();
            RegisterSearchModifier<ArtistSearchModifier>();
            RegisterSearchModifier<GenerationSearchModifier>();

        }

        // Private members

        private readonly IDictionary<string, Func<ISearchModifier>> modifiers = new Dictionary<string, Func<ISearchModifier>>();

    }

}
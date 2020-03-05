using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    [SearchModifier("orderby", "sortby", "sort", "ordering")]
    public class OrderBySearchModifier :
        ISearchModifier {

        // Public members

        public string Name { get; set; }
        public string Value { get; set; }
        public bool Invert { get; set; } = false;

        public async Task ApplyAsync(ISearchContext context, ISearchResult result) {

            switch (ParseOrderBy(Value)) {

                case OrderBy.Newest:
                    await result.OrderByAsync(Comparer<ISpecies>.Create((lhs, rhs) => rhs.CreationDate.CompareTo(lhs.CreationDate)));
                    break;

                case OrderBy.Oldest:
                    await result.OrderByAsync(Comparer<ISpecies>.Create((lhs, rhs) => lhs.CreationDate.CompareTo(rhs.CreationDate)));
                    break;

                case OrderBy.Smallest:
                    await result.OrderByAsync(Comparer<ISpecies>.Create((lhs, rhs) => SpeciesSizeMatch.Find(lhs.Description).MaxSize.ToMeters().CompareTo(SpeciesSizeMatch.Find(rhs.Description).MaxSize.ToMeters())));
                    break;

                case OrderBy.Largest:
                    await result.OrderByAsync(Comparer<ISpecies>.Create((lhs, rhs) => SpeciesSizeMatch.Find(rhs.Description).MaxSize.ToMeters().CompareTo(SpeciesSizeMatch.Find(lhs.Description).MaxSize.ToMeters())));
                    break;

                case OrderBy.Count:
                    await result.OrderByAsync(Comparer<ISearchResultGroup>.Create((lhs, rhs) => lhs.Count().CompareTo(rhs.Count())));
                    break;

                default:
                case OrderBy.Default:
                    await result.OrderByAsync(Comparer<ISpecies>.Create((lhs, rhs) => lhs.ShortName.CompareTo(rhs.ShortName)));
                    break;

            }

        }

        // Private members

        private enum OrderBy {

            Unknown = 0,

            Default,

            Newest,
            Oldest,
            Smallest,
            Largest,
            Count

        }

        private OrderBy ParseOrderBy(string value) {

            switch (value.ToLowerInvariant()) {

                case "smallest":
                    return OrderBy.Smallest;

                case "largest":
                case "biggest":
                case "size":
                    return OrderBy.Largest;

                case "newest":
                case "recent":
                    return OrderBy.Newest;

                case "age":
                case "date":
                case "oldest":
                    return OrderBy.Oldest;

                case "number":
                case "total":
                case "count":
                    return OrderBy.Count;

                default:
                    return OrderBy.Unknown;

            }

        }

    }

}
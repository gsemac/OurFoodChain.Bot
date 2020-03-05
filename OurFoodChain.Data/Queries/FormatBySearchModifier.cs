using OurFoodChain.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    [SearchModifier("n", "name", "format")]
    public class FormatBySearchModifier :
        ISearchModifier {

        // Public members

        public string Name { get; set; }
        public string Value { get; set; }
        public bool Invert { get; set; } = false;

        public async Task ApplyAsync(ISearchContext context, ISearchResult result) {

            switch (ParseFormatBy(Value)) {

                case FormatBy.CommonName:
                    await result.FormatByAsync(async (species) => await Task.FromResult(species.CommonNames.FirstOrDefault()?.ToTitle() ?? species.ShortName));
                    break;

                case FormatBy.FullName:
                    await result.FormatByAsync(async (species) => await Task.FromResult(species.FullName));
                    break;

                case FormatBy.SpeciesOnly:
                    await result.FormatByAsync(async (species) => await Task.FromResult(species.Name.ToLowerInvariant()));
                    break;

                case FormatBy.Gallery:
                    result.DisplayFormat = SearchResultDisplayFormat.Gallery;
                    break;

                case FormatBy.Leaderboard:

                    result.DisplayFormat = SearchResultDisplayFormat.Leaderboard;

                    // If the ordering has not already been modified, order the groups by the number of items in each.

                    if (result.HasDefaultOrdering)
                        await result.OrderByAsync(Comparer<ISearchResultGroup>.Create((lhs, rhs) => lhs.Count().CompareTo(rhs.Count())));

                    break;

                default:
                case FormatBy.ShortName:
                    await result.FormatByAsync(async (species) => await Task.FromResult(species.ShortName));
                    break;

            }

        }

        // Private members

        private enum FormatBy {

            Unknown = 0,

            None,

            FullName,
            ShortName,
            CommonName,
            SpeciesOnly,

            Gallery,
            Leaderboard

        }

        private FormatBy ParseFormatBy(string value) {

            switch (value.ToLowerInvariant()) {

                case "c":
                case "common":
                    return FormatBy.CommonName;

                case "f":
                case "full":
                    return FormatBy.FullName;

                case "s":
                case "short":
                    return FormatBy.ShortName;

                case "sp":
                case "species":
                    return FormatBy.SpeciesOnly;

                case "gallery":
                    return FormatBy.Gallery;

                case "group":
                case "groups":
                case "leaderboard":
                    return FormatBy.Leaderboard;

                default:
                    return FormatBy.Unknown;

            }

        }

    }

}
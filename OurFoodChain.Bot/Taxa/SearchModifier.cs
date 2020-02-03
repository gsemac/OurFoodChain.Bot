using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Taxa {

    public class SearchModifier :
        ISearchModifier {

        // Public members

        public string Name { get; }
        public string Value { get; }
        public bool Subtractive { get; }
        public SearchModifierType Type { get; }

        public static ISearchModifier Parse(string modifier) {

            SearchModifier searchModifier = new SearchModifier(modifier);

            switch (searchModifier.Type) {

                case SearchModifierType.GroupBy:
                    return new GroupingSearchModifier(GetSearchResultGrouping(searchModifier.Value));

                case SearchModifierType.OrderBy:
                    return new OrderingSearchModifier(GetSearchResultOrdering(searchModifier.Value));

                case SearchModifierType.Format:
                    return new DisplayFormatSearchModifier(GetSearchResultDisplayFormat(searchModifier.Value));

                default:
                    return searchModifier;

            }

        }

        // Protected members

        protected SearchModifier(SearchModifierType type, string value) {

            Name = type.ToString().ToLower();
            Value = value;
            Subtractive = false;
            Type = type;

        }
        protected SearchModifier(string modifier) {

            int splitIndex = modifier.IndexOf(':');
            string name = modifier.Substring(0, splitIndex).Trim();
            string value = modifier.Substring(splitIndex + 1, modifier.Length - splitIndex - 1).Trim();
            bool subtractive = name.Length > 0 ? name[0] == '-' : false;

            if (name.StartsWith("-"))
                name = name.Substring(1, name.Length - 1);

            // Trim outer quotes from the value.

            if (value.Length > 1 && value.First() == '"' && value.Last() == '"')
                value = value.Trim('"');

            Name = name.ToLowerInvariant();
            Value = value;
            Subtractive = subtractive;
            Type = GetSearchModifierType(Name);

        }

        // Private members

        private static SearchModifierType GetSearchModifierType(string value) {

            switch (value.ToLowerInvariant()) {

                case "groupby":
                case "group":
                    return SearchModifierType.GroupBy;

                case "orderby":
                case "sortby":
                case "sort":
                case "ordering":
                    return SearchModifierType.OrderBy;

                case "z":
                case "zone":
                    return SearchModifierType.Zone;

                case "r":
                case "role":
                    return SearchModifierType.Role;

                case "n":
                case "name":
                case "format":
                    return SearchModifierType.Format;

                case "owner":
                case "creator":
                    return SearchModifierType.Creator;

                case "status":
                    return SearchModifierType.Status;

                case "s":
                case "species":
                    return SearchModifierType.Species;

                case "g":
                case "genus":
                    return SearchModifierType.Genus;

                case "f":
                case "family":
                    return SearchModifierType.Family;

                case "o":
                case "order":
                    return SearchModifierType.Order;

                case "c":
                case "class":
                    return SearchModifierType.Class;

                case "p":
                case "phylum":
                    return SearchModifierType.Class;

                case "k":
                case "kingdom":
                    return SearchModifierType.Kingdom;

                case "d":
                case "domain":
                    return SearchModifierType.Domain;

                case "t":
                case "taxon":
                    return SearchModifierType.Taxon;

                case "random":
                    return SearchModifierType.Random;

                case "prey":
                case "predates":
                case "eats":
                    return SearchModifierType.Prey;

                case "preynote":
                case "preynotes":
                    return SearchModifierType.PreyNotes;

                case "pred":
                case "predator":
                    return SearchModifierType.Predator;

                case "has":
                    return SearchModifierType.Has;

                case "anc":
                case "ancestor":
                    return SearchModifierType.Ancestor;

                case "evo":
                case "descendant":
                    return SearchModifierType.Descendant;

                case "limit":
                    return SearchModifierType.Limit;

                case "artist":
                    return SearchModifierType.Artist;

                case "gen":
                case "generation":
                    return SearchModifierType.Generation;

                default:
                    return SearchModifierType.Unknown;

            }

        }
        private static SearchResultGrouping GetSearchResultGrouping(string value) {

            switch (value.ToLowerInvariant()) {

                case "z":
                case "zones":
                case "zone":
                    return SearchResultGrouping.Zone;

                case "g":
                case "genus":
                    return SearchResultGrouping.Genus;

                case "f":
                case "family":
                    return SearchResultGrouping.Family;

                case "o":
                case "order":
                    return SearchResultGrouping.Order;

                case "c":
                case "class":
                    return SearchResultGrouping.Class;

                case "p":
                case "phylum":
                    return SearchResultGrouping.Phylum;

                case "k":
                case "kingdom":
                    return SearchResultGrouping.Kingdom;

                case "d":
                case "domain":
                    return SearchResultGrouping.Kingdom;

                case "creator":
                case "owner":
                    return SearchResultGrouping.Creator;

                case "status":
                case "extant":
                case "extinct":
                    return SearchResultGrouping.Status;

                case "role":
                    return SearchResultGrouping.Role;

                case "gen":
                case "generation":
                    return SearchResultGrouping.Generation;

                default:
                    return SearchResultGrouping.Unknown;

            }

        }
        private static SearchResultOrdering GetSearchResultOrdering(string value) {

            switch (value.ToLowerInvariant()) {

                case "smallest":
                    return SearchResultOrdering.Smallest;

                case "largest":
                case "biggest":
                case "size":
                    return SearchResultOrdering.Largest;

                case "newest":
                case "recent":
                    return SearchResultOrdering.Newest;

                case "age":
                case "date":
                case "oldest":
                    return SearchResultOrdering.Oldest;

                case "number":
                case "total":
                case "count":
                    return SearchResultOrdering.Count;

                default:
                    return SearchResultOrdering.Unknown;

            }

        }
        private static SearchResultDisplayFormat GetSearchResultDisplayFormat(string value) {

            switch (value.ToLowerInvariant()) {

                case "c":
                case "common":
                    return SearchResultDisplayFormat.CommonName;

                case "f":
                case "full":
                    return SearchResultDisplayFormat.FullName;

                case "s":
                case "short":
                    return SearchResultDisplayFormat.ShortName;

                case "sp":
                case "species":
                    return SearchResultDisplayFormat.SpeciesOnly;

                case "gallery":
                    return SearchResultDisplayFormat.Gallery;

                case "group":
                case "groups":
                case "leaderboard":
                    return SearchResultDisplayFormat.Leaderboard;

                default:
                    return SearchResultDisplayFormat.Unknown;

            }

        }

    }

    public class GroupingSearchModifier
        : SearchModifier {

        public GroupingSearchModifier(SearchResultGrouping grouping) :
            base(SearchModifierType.GroupBy, grouping.ToString().ToLowerInvariant()) {

            Grouping = grouping;

        }

        public SearchResultGrouping Grouping { get; }

    }

    public class OrderingSearchModifier
        : SearchModifier {

        public OrderingSearchModifier(SearchResultOrdering ordering) :
            base(SearchModifierType.OrderBy, ordering.ToString().ToLowerInvariant()) {

            Ordering = ordering;

        }

        public SearchResultOrdering Ordering { get; }

    }

    public class DisplayFormatSearchModifier
        : SearchModifier {

        public DisplayFormatSearchModifier(SearchResultDisplayFormat displayFormat) :
            base(SearchModifierType.Format, displayFormat.ToString().ToLowerInvariant()) {

            DisplayFormat = displayFormat;

        }

        public SearchResultDisplayFormat DisplayFormat { get; }

    }

}
using OurFoodChain.Common.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Taxa {

    public class SearchResult :
        ISearchResult {

        // Public members

        public class Group :
            IEnumerable<Species> {

            // Public members

            public string Name { get; set; } = DefaultGroupName;
            public SearchResultOrdering OrderBy { get; set; } = SearchResultOrdering.Default;
            public SearchResultDisplayFormat DisplayFormat { get; set; } = SearchResultDisplayFormat.ShortName;
            public List<Species> Items { get; set; } = new List<Species>();

            public string[] ToStringArray() {

                return ToArray().Select(x => SpeciesToString(x)).ToArray();

            }
            public Species[] ToArray() {

                switch (OrderBy) {

                    case SearchResultOrdering.Newest:
                        Items.Sort((lhs, rhs) => rhs.Timestamp.CompareTo(lhs.Timestamp));
                        break;

                    case SearchResultOrdering.Oldest:
                        Items.Sort((lhs, rhs) => lhs.Timestamp.CompareTo(rhs.Timestamp));
                        break;

                    case SearchResultOrdering.Smallest:
                        Items.Sort((lhs, rhs) => SpeciesSizeMatch.Match(lhs.Description).MaxSize.ToMeters().CompareTo(SpeciesSizeMatch.Match(rhs.Description).MaxSize.ToMeters()));
                        break;

                    case SearchResultOrdering.Largest:
                        Items.Sort((lhs, rhs) => SpeciesSizeMatch.Match(rhs.Description).MaxSize.ToMeters().CompareTo(SpeciesSizeMatch.Match(lhs.Description).MaxSize.ToMeters()));
                        break;

                    default:
                    case SearchResultOrdering.Default:
                        Items.Sort((lhs, rhs) => lhs.ShortName.CompareTo(rhs.ShortName));
                        break;

                }

                return Items.ToArray();

            }

            public IEnumerator<Species> GetEnumerator() {
                return Items.GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator() {
                return Items.GetEnumerator();
            }

            // Private members

            private string SpeciesToString(Species species) {

                string str = species.ShortName;

                switch (DisplayFormat) {

                    case SearchResultDisplayFormat.CommonName:

                        if (!string.IsNullOrEmpty(species.CommonName))
                            str = StringUtilities.ToTitleCase(species.CommonName);

                        break;

                    case SearchResultDisplayFormat.FullName:
                        str = species.FullName;
                        break;

                    case SearchResultDisplayFormat.SpeciesOnly:
                        str = species.Name.ToLower();
                        break;

                }

                if (species.IsExtinct)
                    str = string.Format("~~{0}~~", str);

                return str;

            }

        }

        public const string DefaultGroupName = "";

        public void Add(string group, Species species) {

            Add(group, new Species[] { species });

        }
        public void Add(string group, Species[] species) {

            if (!groups.ContainsKey(group)) {

                groups.Add(group, new Group {
                    Name = group,
                    Items = new List<Species>(species)
                });

            }
            else
                groups[group].Items.AddRange(species);

        }
        public int Count() {

            int count = 0;

            foreach (Group group in groups.Values)
                count += group.Items.Count();

            return count;

        }

        public bool HasGroup(string name) {

            return groups.ContainsKey(name);

        }

        public async Task GroupByAsync(Func<Species, Task<string[]>> func) {

            // Take all species that have already been grouped, assembly them into a single list, and then clear the groupings.
            // Remove any duplicates in the process, since species may have been assigned to multiple groups.

            List<Species> species = new List<Species>();

            foreach (Group group in groups.Values)
                species.AddRange(group.Items);

            groups.Clear();

            species = species.GroupBy(x => x.Id).Select(x => x.First()).ToList();

            // Assign the species into groups according to the callback.

            foreach (Species s in species)
                foreach (string group in await func(s))
                    Add(group, s);

        }
        /// <summary>
        /// Removes all results for which the given condition is met.
        /// </summary>
        /// <param name="func">The condition to use when checking each result.</param>
        /// <param name="constructive">If true, removes results for which the condition is not met instead.</param>
        /// <returns></returns>
        public async Task FilterByAsync(Func<Species, Task<bool>> func, bool constructive = false) {

            foreach (Group group in groups.Values) {

                int index = group.Items.Count() - 1;

                while (index >= 0) {

                    bool condition_met = await func(group.Items[index]);

                    if ((condition_met && !constructive) || (!condition_met && constructive))
                        group.Items.RemoveAt(index);

                    --index;

                }

            }

            // Remove all empty groups.

            foreach (string key in groups.Keys.Where(x => groups[x].Count() <= 0).ToArray())
                groups.Remove(key);

        }

        public Species[] ToArray() {

            List<Species> results = new List<Species>();

            foreach (Group group in Groups)
                results.AddRange(group.ToArray());

            return results.ToArray();

        }

        public Group DefaultGroup {
            get {
                return groups[DefaultGroupName];
            }
        }
        public Group[] Groups {
            get {

                List<Group> groups = new List<Group>();

                foreach (Group group in this.groups.Values)
                    groups.Add(group);

                switch (OrderBy) {

                    case SearchResultOrdering.Count:
                        groups.Sort((x, y) => y.Items.Count.CompareTo(x.Items.Count));
                        break;

                }

                return groups.ToArray();

            }
        }

        public SearchResultOrdering OrderBy {
            get {
                return orderBy;
            }
            set {

                foreach (Group group in groups.Values)
                    group.OrderBy = value;

                orderBy = value;

            }
        }
        public SearchResultDisplayFormat DisplayFormat {
            get {
                return displayFormat;
            }
            set {

                foreach (Group group in groups.Values)
                    group.DisplayFormat = value;

                displayFormat = value;

            }
        }

        // Private members

        private SortedDictionary<string, Group> groups = new SortedDictionary<string, Group>(new ArrayUtils.NaturalStringComparer());
        private SearchResultOrdering orderBy = SearchResultOrdering.Default;
        private SearchResultDisplayFormat displayFormat = SearchResultDisplayFormat.ShortName;

    }

}

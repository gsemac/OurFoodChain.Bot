using Discord.Commands;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Taxa {

    public enum DisplayFormat {
        FullName,
        ShortName,
        CommonName,
        SpeciesOnly,
        Gallery,
        Leaderboard
    }

    public enum OrderBy {
        Default,
        Newest,
        Oldest,
        Smallest,
        Largest,
        Count
    }

    public class SearchQueryResult {

        // Public members

        public class Group :
            IEnumerable<Species> {

            // Public members

            public string Name { get; set; } = SearchQuery.DefaultGroupName;
            public OrderBy OrderBy { get; set; } = OrderBy.Default;
            public DisplayFormat DisplayFormat { get; set; } = DisplayFormat.ShortName;
            public List<Species> Items { get; set; } = new List<Species>();

            public string[] ToStringArray() {

                return ToArray().Select(x => SpeciesToString(x)).ToArray();

            }
            public Species[] ToArray() {

                switch (OrderBy) {

                    case OrderBy.Newest:
                        Items.Sort((lhs, rhs) => rhs.Timestamp.CompareTo(lhs.Timestamp));
                        break;

                    case OrderBy.Oldest:
                        Items.Sort((lhs, rhs) => lhs.Timestamp.CompareTo(rhs.Timestamp));
                        break;

                    case OrderBy.Smallest:
                        Items.Sort((lhs, rhs) => SpeciesSizeMatch.Match(lhs.Description).MaxSize.ToMeters().CompareTo(SpeciesSizeMatch.Match(rhs.Description).MaxSize.ToMeters()));
                        break;

                    case OrderBy.Largest:
                        Items.Sort((lhs, rhs) => SpeciesSizeMatch.Match(rhs.Description).MaxSize.ToMeters().CompareTo(SpeciesSizeMatch.Match(lhs.Description).MaxSize.ToMeters()));
                        break;

                    default:
                    case OrderBy.Default:
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

                    case DisplayFormat.CommonName:

                        if (!string.IsNullOrEmpty(species.CommonName))
                            str = StringUtils.ToTitleCase(species.CommonName);

                        break;

                    case DisplayFormat.FullName:
                        str = species.FullName;
                        break;

                    case DisplayFormat.SpeciesOnly:
                        str = species.Name.ToLower();
                        break;

                }

                if (species.IsExtinct)
                    str = string.Format("~~{0}~~", str);

                return str;

            }

        }

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

        }

        public Species[] ToArray() {

            List<Species> results = new List<Species>();

            foreach (Group group in Groups)
                results.AddRange(group.ToArray());

            return results.ToArray();

        }

        public Group DefaultGroup {
            get {
                return groups[SearchQuery.DefaultGroupName];
            }
        }
        public Group[] Groups {
            get {

                List<Group> groups = new List<Group>();

                foreach (Group group in this.groups.Values)
                    groups.Add(group);

                switch (OrderBy) {

                    case OrderBy.Count:
                        groups.Sort((x, y) => y.Items.Count.CompareTo(x.Items.Count));
                        break;

                }

                return groups.ToArray();

            }
        }

        public OrderBy OrderBy {
            get {
                return orderBy;
            }
            set {

                foreach (Group group in groups.Values)
                    group.OrderBy = value;

                orderBy = value;

            }
        }
        public DisplayFormat DisplayFormat {
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
        private OrderBy orderBy = OrderBy.Default;
        private DisplayFormat displayFormat = DisplayFormat.ShortName;

    }

    public class SearchQuery {

        // Public members

        public const string DefaultGroupName = "";

        public SearchQuery(ICommandContext context, string queryString) :
            this(context, ParseQueryString(queryString)) {
        }
        public SearchQuery(ICommandContext context, string[] keywords) {

            this.context = context;

            // Filter out the basic search terms from the modifiers.

            foreach (string i in keywords) {

                string keyword = i.Trim().ToLower();

                if (string.IsNullOrEmpty(keyword))
                    continue;

                if (IsSearchModifier(keyword))
                    searchModifiers.Add(keyword);
                else
                    searchTerms.Add(keyword);

            }

        }

        public async Task<SearchQueryResult> GetResultAsync() {

            // Build up a list of conditions to query for.

            List<string> conditions = new List<string>();

            // Create a condition for each basic search term.

            for (int i = 0; i < searchTerms.Count(); ++i)
                conditions.Add(string.Format("(name LIKE {0} OR description LIKE {0} OR common_name LIKE {0})", string.Format("$term{0}", i)));

            // Build the SQL query.
            string sql_command_str;

            if (conditions.Count > 0)
                sql_command_str = string.Format("SELECT * FROM Species WHERE {0};", string.Join(" AND ", conditions));
            else
                sql_command_str = "SELECT * FROM Species;";

            List<Species> matches = new List<Species>();

            using (SQLiteCommand cmd = new SQLiteCommand(sql_command_str)) {

                // Replace all parameters with their respective terms.

                for (int i = 0; i < searchTerms.Count(); ++i) {
                    string term = "%" + searchTerms[i].Trim() + "%";
                    cmd.Parameters.AddWithValue(string.Format("$term{0}", i), term);
                }

                // Execute the query, and add all matching species to the list.

                using (DataTable rows = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in rows.Rows)
                        matches.Add(await SpeciesUtils.SpeciesFromDataRow(row));

            }

            // Apply any post-match modifiers (e.g. groupings), and return the result.
            SearchQueryResult result = await ApplyPostMatchModifiersAsync(matches);

            // Return the result.
            return result;

        }

        // Private members

        private bool IsSearchModifier(string input) {

            return input.Contains(":");

        }
        private async Task<SearchQueryResult> ApplyPostMatchModifiersAsync(List<Species> matches) {

            SearchQueryResult result = new SearchQueryResult();

            result.Add(DefaultGroupName, matches.ToArray());

            foreach (string modifier in searchModifiers)
                await ApplyPostMatchModifierAsync(result, modifier);

            return result;

        }
        private async Task ApplyPostMatchModifierAsync(SearchQueryResult result, string modifier) {

            int split_index = modifier.IndexOf(':');
            string name = modifier.Substring(0, split_index).Trim();
            string value = modifier.Substring(split_index + 1, modifier.Length - split_index - 1).Trim();
            bool subtract = name.Length > 0 ? name[0] == '-' : false;

            if (name.StartsWith("-"))
                name = name.Substring(1, name.Length - 1);

            // Trim outer quotes from the value.

            if (value.Length > 0 && value.First() == '"' && value.Last() == '"')
                value = value.Trim('"');

            switch (name) {

                case "groupby":
                case "group":

                    // Applies grouping to the species based on the given attribute.

                    switch (value.ToLower()) {

                        case "z":
                        case "zones":
                        case "zone":
                            await result.GroupByAsync(async (x) => {
                                return (await BotUtils.GetZonesFromDb(x.Id)).Select(z => z.GetFullName()).ToArray();
                            });
                            break;

                        case "g":
                        case "genus":
                            await result.GroupByAsync((x) => {
                                return Task.FromResult(new string[] { x.GenusName });
                            });
                            break;

                        case "f":
                        case "family":
                            await result.GroupByAsync(async (x) => {

                                Taxon taxon = (await BotUtils.GetFullTaxaFromDb(x)).Family;

                                return new string[] { taxon is null ? "N/A" : taxon.GetName() };

                            });
                            break;

                        case "o":
                        case "order":
                            await result.GroupByAsync(async (x) => {

                                Taxon taxon = (await BotUtils.GetFullTaxaFromDb(x)).Order;

                                return new string[] { taxon is null ? "N/A" : taxon.GetName() };

                            });
                            break;

                        case "c":
                        case "class":
                            await result.GroupByAsync(async (x) => {

                                Taxon taxon = (await BotUtils.GetFullTaxaFromDb(x)).Class;

                                return new string[] { taxon is null ? "N/A" : taxon.GetName() };

                            });
                            break;

                        case "p":
                        case "phylum":
                            await result.GroupByAsync(async (x) => {

                                Taxon taxon = (await BotUtils.GetFullTaxaFromDb(x)).Phylum;

                                return new string[] { taxon is null ? "N/A" : taxon.GetName() };

                            });
                            break;

                        case "k":
                        case "kingdom":
                            await result.GroupByAsync(async (x) => {

                                Taxon taxon = (await BotUtils.GetFullTaxaFromDb(x)).Kingdom;

                                return new string[] { taxon is null ? "N/A" : taxon.GetName() };

                            });
                            break;

                        case "d":
                        case "domain":
                            await result.GroupByAsync(async (x) => {

                                Taxon taxon = (await BotUtils.GetFullTaxaFromDb(x)).Domain;

                                return new string[] { taxon is null ? "N/A" : taxon.GetName() };

                            });
                            break;

                        case "owner":
                            await result.GroupByAsync(async (x) => {
                                return new string[] { await SpeciesUtils.GetOwnerOrDefaultAsync(x, context) };
                            });
                            break;

                        case "status":
                        case "extant":
                        case "extinct":
                            await result.GroupByAsync((x) => {
                                return Task.FromResult(new string[] { x.IsExtinct ? "extinct" : "extant" });
                            });
                            break;

                        case "role":
                            await result.GroupByAsync(async (x) => {
                                return (await SpeciesUtils.GetRolesAsync(x.Id)).Select(z => z.name).ToArray();
                            });
                            break;

                        case "gen":
                        case "generation":
                            if (OurFoodChainBot.Instance.Config.GenerationsEnabled)
                                await result.GroupByAsync(async (x) => {
                                    return new string[] { (await GenerationUtils.GetGenerationByTimestampAsync(x.Timestamp)).Name };
                                });
                            break;

                    }

                    break;

                case "orderby":
                case "sortby":
                case "sort":
                case "ordering":

                    switch (value.ToLower()) {

                        case "smallest":
                            result.OrderBy = OrderBy.Smallest;
                            break;

                        case "largest":
                        case "biggest":
                        case "size":
                            result.OrderBy = OrderBy.Largest;
                            break;

                        case "newest":
                        case "recent":
                            result.OrderBy = OrderBy.Newest;
                            break;

                        case "age":
                        case "date":
                        case "oldest":
                            result.OrderBy = OrderBy.Oldest;
                            break;

                        case "number":
                        case "total":
                        case "count":
                            result.OrderBy = OrderBy.Count;
                            break;

                    }

                    break;

                case "z":
                case "zone":

                    // Filters out all species that aren't in the given zone(s).

                    long[] zone_list = (await ZoneUtils.GetZonesByZoneListAsync(value)).Zones.Select(x => x.Id).ToArray();

                    await result.FilterByAsync(async (x) => {
                        return !(await BotUtils.GetZonesFromDb(x.Id)).Any(z => zone_list.Contains(z.Id));
                    }, subtract);

                    break;

                case "r":
                case "role":

                    // Filters out all species that don't have the given roles.

                    string[] role_list = value.Split(',').Select(x => x.Trim().ToLower()).ToArray();

                    await result.FilterByAsync(async (x) => {
                        return !(await SpeciesUtils.GetRolesAsync(x.Id)).Any(r => role_list.Contains(r.name.ToLower()));
                    }, subtract);

                    break;

                case "n":
                case "name":
                case "format":

                    // Changes how names are displayed.

                    switch (value) {

                        case "c":
                        case "common":
                            result.DisplayFormat = DisplayFormat.CommonName;
                            break;

                        case "f":
                        case "full":
                            result.DisplayFormat = DisplayFormat.FullName;
                            break;

                        case "s":
                        case "short":
                            result.DisplayFormat = DisplayFormat.ShortName;
                            break;

                        case "sp":
                        case "species":
                            result.DisplayFormat = DisplayFormat.SpeciesOnly;
                            break;

                        case "gallery":
                            result.DisplayFormat = DisplayFormat.Gallery;
                            break;

                        case "group":
                        case "groups":
                        case "leaderboard":

                            result.DisplayFormat = DisplayFormat.Leaderboard;

                            if (result.OrderBy == OrderBy.Default)
                                result.OrderBy = OrderBy.Count;

                            break;

                    }

                    break;

                case "owner":

                    Discord.IUser user = await CommandUtils.GetUserFromUsernameOrMentionAsync(context, value);

                    await result.FilterByAsync(async (x) => {
                        return (user is null) ? ((await SpeciesUtils.GetOwnerOrDefaultAsync(x, context)).ToLower() != value.ToLower()) : (ulong)x.OwnerUserId != user.Id;
                    }, subtract);

                    break;

                case "status":

                    switch (value) {

                        case "lc":
                        case "extant":

                            await result.FilterByAsync((x) => {
                                return Task.FromResult(x.IsExtinct);
                            }, subtract);

                            break;

                        case "ex":
                        case "extinct":

                            await result.FilterByAsync((x) => {
                                return Task.FromResult(!x.IsExtinct);
                            }, subtract);

                            break;

                        case "en":
                        case "endangered":

                            await result.FilterByAsync(async (x) => {
                                return !await BotUtils.IsEndangeredSpeciesAsync(x);
                            }, subtract);

                            break;

                    }

                    break;

                case "s":
                case "species":

                    await result.FilterByAsync((Func<Species, Task<bool>>)((x) => {
                        return Task.FromResult(x.Name.ToLower() != value.ToLower());
                    }), subtract);

                    break;

                case "g":
                case "genus":

                    await result.FilterByAsync((x) => {
                        return Task.FromResult(x.GenusName.ToLower() != value.ToLower());
                    }, subtract);

                    break;

                case "f":
                case "family":

                    await result.FilterByAsync(async (x) => {
                        return !(await BotUtils.GetFullTaxaFromDb(x)).Contains(value, TaxonRank.Family);
                    }, subtract);

                    break;

                case "o":
                case "order":

                    await result.FilterByAsync(async (x) => {
                        return !(await BotUtils.GetFullTaxaFromDb(x)).Contains(value, TaxonRank.Order);
                    }, subtract);

                    break;

                case "c":
                case "class":

                    await result.FilterByAsync(async (x) => {
                        return !(await BotUtils.GetFullTaxaFromDb(x)).Contains(value, TaxonRank.Class);
                    }, subtract);

                    break;

                case "p":
                case "phylum":

                    await result.FilterByAsync(async (x) => {
                        return !(await BotUtils.GetFullTaxaFromDb(x)).Contains(value, TaxonRank.Phylum);
                    }, subtract);

                    break;

                case "k":
                case "kingdom":

                    await result.FilterByAsync(async (x) => {
                        return !(await BotUtils.GetFullTaxaFromDb(x)).Contains(value, TaxonRank.Kingdom);
                    }, subtract);

                    break;

                case "d":
                case "domain":

                    await result.FilterByAsync(async (x) => {
                        return !(await BotUtils.GetFullTaxaFromDb(x)).Contains(value, TaxonRank.Domain);
                    }, subtract);

                    break;

                case "t":
                case "taxon":

                    await result.FilterByAsync(async (x) => {
                        return !(await BotUtils.GetFullTaxaFromDb(x)).Contains(value);
                    }, subtract);

                    break;

                case "random": {

                        if (!int.TryParse(value, out int count))
                            break;

                        if (count <= 0)
                            break;

                        // Generate N random IDs from the results.
                        long[] ids = result.ToArray().OrderBy(i => BotUtils.RandomInteger(int.MaxValue)).Take(count).Select(i => i.Id).ToArray();

                        // Filter all but those results.

                        await result.FilterByAsync((x) => {
                            return Task.FromResult(!ids.Contains(x.Id));
                        }, subtract);

                    }

                    break;

                case "prey":
                case "predates":
                case "eats": {

                        // Filters out all species that do not prey upon the given species.

                        Species[] prey_list = await SpeciesUtils.GetSpeciesAsync(value);
                        Species[] predator_list = prey_list.Count() == 1 ? await SpeciesUtils.GetPredatorsAsync(prey_list[0]) : new Species[] { };

                        await result.FilterByAsync((x) => {
                            return Task.FromResult(!predator_list.Any(i => i.Id == x.Id));
                        }, subtract);

                    }

                    break;

                case "pred":
                case "predator": {

                        // Filters out all species that are not in the prey list of the given species.

                        Species[] predator_list = await SpeciesUtils.GetSpeciesAsync(value);
                        PreyInfo[] prey_list = predator_list.Count() == 1 ? await SpeciesUtils.GetPreyAsync(predator_list[0]) : new PreyInfo[] { };

                        await result.FilterByAsync((x) => {
                            return Task.FromResult(!prey_list.Any(i => i.Prey.Id == x.Id));
                        }, subtract);

                    }

                    break;

                case "preynote":
                case "preynotes": {

                        // Filters out species that don't have the given keyword in the prey notes.

                        await result.FilterByAsync(async (x) => {
                            return !(await SpeciesUtils.GetPreyAsync(x)).Where(n => n.Notes.ToLower().Contains(value.ToLower())).Any();
                        }, subtract);

                        break;

                    }

                case "has": {

                        switch (value) {

                            case "prey":

                                await result.FilterByAsync(async (x) => {
                                    return (await SpeciesUtils.GetPreyAsync(x)).Count() <= 0;
                                }, subtract);

                                break;

                            case "predator":
                            case "predators":

                                await result.FilterByAsync(async (x) => {
                                    return (await SpeciesUtils.GetPredatorsAsync(x)).Count() <= 0;
                                }, subtract);

                                break;

                            case "ancestor":
                            case "ancestors":

                                await result.FilterByAsync(async (x) => {
                                    return await SpeciesUtils.GetAncestorAsync(x) is null;
                                }, subtract);

                                break;

                            case "descendant":
                            case "descendants":
                            case "evo":
                            case "evos":
                            case "evolution":
                            case "evolutions":

                                await result.FilterByAsync(async (x) => {
                                    return (await SpeciesUtils.GetDirectDescendantsAsync(x)).Count() <= 0;
                                }, subtract);

                                break;

                            case "role":
                            case "roles":

                                await result.FilterByAsync(async (x) => {
                                    return (await SpeciesUtils.GetRolesAsync(x)).Count() <= 0;
                                }, subtract);

                                break;

                            case "pic":
                            case "pics":
                            case "picture":
                            case "pictures":
                            case "image":
                            case "images":

                                await result.FilterByAsync(async (x) => {
                                    return (await SpeciesUtils.GetPicturesAsync(x)).Count() <= 0;
                                }, subtract);

                                break;

                            case "size":

                                await result.FilterByAsync((x) => {
                                    return Task.FromResult(SpeciesSizeMatch.Match(x.Description).ToString() == SpeciesSizeMatch.UNKNOWN_SIZE_STRING);
                                }, subtract);

                                break;

                        }

                    }

                    break;

                case "anc":
                case "ancestor": {

                        // Filter all species that don't have the given species as an ancestor.

                        Species species = await SpeciesUtils.GetUniqueSpeciesAsync(value);

                        await result.FilterByAsync(async (x) => {
                            return species is null || !(await SpeciesUtils.GetAncestorIdsAsync(x.Id)).Any(id => id == species.Id);
                        }, subtract);

                    }

                    break;

                case "evo":
                case "descendant": {

                        // Filter all species that don't have the given species as a descendant.

                        Species species = await SpeciesUtils.GetUniqueSpeciesAsync(value);
                        long[] ancestorIds = await SpeciesUtils.GetAncestorIdsAsync(species.Id);

                        await result.FilterByAsync((x) => {
                            return Task.FromResult(ancestorIds.Length <= 0 || !ancestorIds.Any(id => id == x.Id));
                        }, subtract);

                    }

                    break;

                case "limit": {

                        // Filter all species that aren't in the first n results.

                        if (int.TryParse(value, out int limit)) {

                            Species[] searchResults = result.ToArray().Take(limit).ToArray();

                            await result.FilterByAsync(async (x) =>
                                await Task.FromResult(!searchResults.Any(n => n.Id == x.Id)), subtract);

                        }

                    }

                    break;

                case "artist":

                    await result.FilterByAsync(async (x) => {
                        return !(await SpeciesUtils.GetPicturesAsync(x)).Any(n => n.artist.ToLowerInvariant().Equals(value.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase));
                    }, subtract);

                    break;

                // The following are only available when generations are enabled.

                case "gen":
                case "generation":
                    if (OurFoodChainBot.Instance.Config.GenerationsEnabled)
                        await result.FilterByAsync(async (x) => {

                            Generation gen = await GenerationUtils.GetGenerationByTimestampAsync(x.Timestamp);

                            return gen == null || gen.Number.ToString() != value;

                        }, subtract);
                    break;


            }

        }

        private static string[] ParseQueryString(string queryString) {

            List<string> keywords = new List<string>();

            string keyword = "";
            bool in_quotes = false;

            for (int i = 0; i < queryString.Length; ++i) {

                if (queryString[i] == '\"') {

                    in_quotes = !in_quotes;

                    keyword += queryString[i];

                }
                else if (!in_quotes && char.IsWhiteSpace(queryString[i])) {

                    keywords.Add(keyword);
                    keyword = "";

                }
                else
                    keyword += queryString[i];

            }

            if (keyword.Length > 0)
                keywords.Add(keyword);

            return keywords.ToArray();

        }

        private readonly ICommandContext context;
        private List<string> searchTerms = new List<string>();
        private List<string> searchModifiers = new List<string>();

    }

}
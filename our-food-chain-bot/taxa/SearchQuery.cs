using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class SearchQuery {

        public const string DEFAULT_GROUP = "";

        public enum DisplayFormat {
            FullName,
            ShortName,
            CommonName,
            SpeciesOnly,
            Gallery
        }

        public enum OrderBy {
            Default,
            Newest,
            Oldest,
            Smallest,
            Largest,
            Count
        }

        public class FindResult {

            public void Add(string group, Species species) {
                Add(group, new Species[] { species });
            }
            public void Add(string group, Species[] species) {

                if (!_groups.ContainsKey(group))
                    _groups.Add(group, new FindResultGroup {
                        Name = group,
                        Items = new List<Species>(species)
                    });
                else
                    _groups[group].Items.AddRange(species);

            }
            public int Count() {

                int count = 0;

                foreach (FindResultGroup group in _groups.Values)
                    count += group.Items.Count();

                return count;

            }

            public bool HasGroup(string name) {
                return _groups.ContainsKey(name);
            }

            public async Task GroupByAsync(Func<Species, Task<string[]>> func) {

                // Take all species that have already been grouped, assembly them into a single list, and then clear the groupings.
                // Remove any duplicates in the process, since species may have been assigned to multiple groups.

                List<Species> species = new List<Species>();

                foreach (FindResultGroup group in _groups.Values)
                    species.AddRange(group.Items);

                _groups.Clear();

                species = species.GroupBy(x => x.id).Select(x => x.First()).ToList();

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

                foreach (FindResultGroup group in _groups.Values) {

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

                foreach (FindResultGroup group in Groups)
                    results.AddRange(group.Items);

                return results.ToArray();

            }

            public FindResultGroup DefaultGroup {
                get {
                    return _groups[DEFAULT_GROUP];
                }
            }
            public FindResultGroup[] Groups {
                get {

                    List<FindResultGroup> groups = new List<FindResultGroup>();

                    foreach (FindResultGroup group in _groups.Values)
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
                    return _order_by;
                }
                set {

                    foreach (FindResultGroup group in _groups.Values)
                        group.OrderBy = value;

                    _order_by = value;

                }
            }
            public DisplayFormat DisplayFormat {
                get {
                    return _display_format;
                }
                set {

                    foreach (FindResultGroup group in _groups.Values)
                        group.DisplayFormat = value;

                    _display_format = value;

                }
            }

            private SortedDictionary<string, FindResultGroup> _groups = new SortedDictionary<string, FindResultGroup>(new ArrayUtils.NaturalStringComparer());
            private OrderBy _order_by = OrderBy.Default;
            private DisplayFormat _display_format = DisplayFormat.ShortName;

        }

        public class FindResultGroup {

            public string Name { get; set; } = DEFAULT_GROUP;
            public OrderBy OrderBy { get; set; } = OrderBy.Default;
            public DisplayFormat DisplayFormat { get; set; } = DisplayFormat.ShortName;
            public List<Species> Items { get; set; } = new List<Species>();

            public List<string> ToList() {

                switch (OrderBy) {

                    case OrderBy.Newest:
                        Items.Sort((lhs, rhs) => rhs.timestamp.CompareTo(lhs.timestamp));
                        break;

                    case OrderBy.Oldest:
                        Items.Sort((lhs, rhs) => lhs.timestamp.CompareTo(rhs.timestamp));
                        break;

                    case OrderBy.Smallest:
                        Items.Sort((lhs, rhs) => SpeciesSizeMatch.Match(lhs.description).MaxSize.ToMeters().CompareTo(SpeciesSizeMatch.Match(rhs.description).MaxSize.ToMeters()));
                        break;

                    case OrderBy.Largest:
                        Items.Sort((lhs, rhs) => SpeciesSizeMatch.Match(rhs.description).MaxSize.ToMeters().CompareTo(SpeciesSizeMatch.Match(lhs.description).MaxSize.ToMeters()));
                        break;

                    default:
                    case OrderBy.Default:
                        Items.Sort((lhs, rhs) => lhs.GetShortName().CompareTo(rhs.GetShortName()));
                        break;

                }

                return Items.Select(x => _speciesToString(x)).ToList();

            }

            private string _speciesToString(Species species) {

                string str = species.GetShortName();

                switch (DisplayFormat) {

                    case DisplayFormat.CommonName:

                        if (!string.IsNullOrEmpty(species.commonName))
                            str = StringUtils.ToTitleCase(species.commonName);

                        break;

                    case DisplayFormat.FullName:
                        str = species.GetFullName();
                        break;

                    case DisplayFormat.SpeciesOnly:
                        str = species.name.ToLower();
                        break;

                }

                if (species.isExtinct)
                    str = string.Format("~~{0}~~", str);

                return str;

            }

        }

        public SearchQuery(ICommandContext context, string queryString) :
            this(context, _parseQueryString(queryString)) {
        }
        public SearchQuery(ICommandContext context, string[] keywords) {

            _context = context;

            // Filter out the basic search terms from the modifiers.

            foreach (string i in keywords) {

                string keyword = i.Trim().ToLower();

                if (string.IsNullOrEmpty(keyword))
                    continue;

                if (_isModifier(keyword))
                    _modifiers.Add(keyword);
                else
                    _terms.Add(keyword);

            }

        }

        public async Task<FindResult> FindMatchesAsync() {

            // Build up a list of conditions to query for.

            List<string> conditions = new List<string>();

            // Create a condition for each basic search term.

            for (int i = 0; i < _terms.Count(); ++i)
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

                for (int i = 0; i < _terms.Count(); ++i) {
                    string term = "%" + _terms[i].Trim() + "%";
                    cmd.Parameters.AddWithValue(string.Format("$term{0}", i), term);
                }

                // Execute the query, and add all matching species to the list.

                using (DataTable rows = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in rows.Rows)
                        matches.Add(await Species.FromDataRow(row));

            }

            // Apply any post-match modifiers (e.g. groupings), and return the result.
            FindResult result = await _applyPostMatchModifiersAsync(matches);

            // Return the result.
            return result;

        }

        private bool _isModifier(string input) {

            return input.Contains(":");

        }
        private async Task<FindResult> _applyPostMatchModifiersAsync(List<Species> matches) {

            FindResult result = new FindResult();

            result.Add(DEFAULT_GROUP, matches.ToArray());

            foreach (string modifier in _modifiers)
                await _applyPostMatchModifierAsync(result, modifier);

            return result;

        }
        private async Task _applyPostMatchModifierAsync(FindResult result, string modifier) {

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
                                return (await BotUtils.GetZonesFromDb(x.id)).Select(z => z.GetFullName()).ToArray();
                            });
                            break;

                        case "g":
                        case "genus":
                            await result.GroupByAsync((x) => {
                                return Task.FromResult(new string[] { x.genus });
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
                                return new string[] { await x.GetOwnerOrDefault(_context) };
                            });
                            break;

                        case "status":
                        case "extant":
                        case "extinct":
                            await result.GroupByAsync((x) => {
                                return Task.FromResult(new string[] { x.isExtinct ? "extinct" : "extant" });
                            });
                            break;

                        case "role":
                            await result.GroupByAsync(async (x) => {
                                return (await SpeciesUtils.GetRolesAsync(x.id)).Select(z => z.name).ToArray();
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
                        return !(await BotUtils.GetZonesFromDb(x.id)).Any(z => zone_list.Contains(z.Id));
                    }, subtract);

                    break;

                case "r":
                case "role":

                    // Filters out all species that don't have the given roles.

                    string[] role_list = value.Split(',').Select(x => x.Trim().ToLower()).ToArray();

                    await result.FilterByAsync(async (x) => {
                        return !(await SpeciesUtils.GetRolesAsync(x.id)).Any(r => role_list.Contains(r.name.ToLower()));
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

                    }

                    break;

                case "owner":

                    Discord.IUser user = await CommandUtils.GetUserFromUsernameOrMentionAsync(_context, value);

                    await result.FilterByAsync(async (x) => {
                        return (user is null) ? ((await x.GetOwnerOrDefault(_context)).ToLower() != value.ToLower()) : (ulong)x.user_id != user.Id;
                    }, subtract);

                    break;

                case "status":

                    switch (value) {

                        case "lc":
                        case "extant":

                            await result.FilterByAsync((x) => {
                                return Task.FromResult(x.isExtinct);
                            }, subtract);

                            break;

                        case "ex":
                        case "extinct":

                            await result.FilterByAsync((x) => {
                                return Task.FromResult(!x.isExtinct);
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

                    await result.FilterByAsync((x) => {
                        return Task.FromResult(x.name.ToLower() != value.ToLower());
                    }, subtract);

                    break;

                case "g":
                case "genus":

                    await result.FilterByAsync((x) => {
                        return Task.FromResult(x.genus.ToLower() != value.ToLower());
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
                        long[] ids = result.ToArray().OrderBy(i => BotUtils.RandomInteger(int.MaxValue)).Take(count).Select(i => i.id).ToArray();

                        // Filter all but those results.

                        await result.FilterByAsync((x) => {
                            return Task.FromResult(!ids.Contains(x.id));
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
                            return Task.FromResult(!predator_list.Any(i => i.id == x.id));
                        }, subtract);

                    }

                    break;

                case "pred":
                case "predator": {

                        // Filters out all species that are not in the prey list of the given species.

                        Species[] predator_list = await SpeciesUtils.GetSpeciesAsync(value);
                        Species[] prey_list = predator_list.Count() == 1 ? await SpeciesUtils.GetPreyAsync(predator_list[0]) : new Species[] { };

                        await result.FilterByAsync((x) => {
                            return Task.FromResult(!prey_list.Any(i => i.id == x.id));
                        }, subtract);

                    }

                    break;

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

                        }

                    }

                    break;

                case "ancestor": {

                        Species[] species_list = await SpeciesUtils.GetSpeciesAsync(value);

                        await result.FilterByAsync(async (x) => {
                            return species_list.Count() != 1 || !(await SpeciesUtils.GetAncestorIdsAsync(x.id)).Any(id => id == species_list[0].id);
                        }, subtract);

                    }

                    break;

            }

        }

        private static string[] _parseQueryString(string queryString) {

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

        private readonly ICommandContext _context;
        private List<string> _terms = new List<string>();
        private List<string> _modifiers = new List<string>();

    }

}
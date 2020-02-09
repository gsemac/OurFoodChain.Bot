using Discord;
using Discord.Commands;
using OurFoodChain.Data;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Taxa;
using OurFoodChain.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;

namespace OurFoodChain.Bot.Services {

    public class SearchService :
        ISearchService {

        // Public members

        public SearchService(IOfcBotConfiguration config, SQLiteDatabase database) {

            this.config = config;
            this.database = database;

        }

        public async Task<Taxa.SearchResult> GetQueryResultAsync(ICommandContext context, SearchQuery searchQuery) {

            // Build up a list of conditions to query for.

            List<string> conditions = new List<string>();

            // Create a condition for each basic search term.

            for (int i = 0; i < searchQuery.Keywords.Count(); ++i)
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

                for (int i = 0; i < searchQuery.Keywords.Count(); ++i) {

                    string term = "%" + searchQuery.Keywords.ElementAt(i).Trim() + "%";

                    cmd.Parameters.AddWithValue(string.Format("$term{0}", i), term);

                }

                // Execute the query, and add all matching species to the list.

                using (DataTable rows = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in rows.Rows)
                        matches.Add(await SpeciesUtils.SpeciesFromDataRow(row));

            }

            // Apply any post-match modifiers (e.g. groupings), and return the result.

            Taxa.SearchResult result = await ApplyPostMatchModifiersAsync(matches, context, searchQuery);

            // Return the result.
            return result;

        }

        // Private members

        private readonly IOfcBotConfiguration config;
        private readonly SQLiteDatabase database;

        private async Task<Taxa.SearchResult> ApplyPostMatchModifiersAsync(List<Species> matches, ICommandContext context, SearchQuery searchQuery) {

            Taxa.SearchResult result = new Taxa.SearchResult();

            result.Add(Taxa.SearchResult.DefaultGroupName, matches.ToArray());

            foreach (SearchModifier modifier in searchQuery.Modifiers)
                await ApplyPostMatchModifierAsync(result, context, modifier);

            return result;

        }
        private async Task ApplyPostMatchModifierAsync(Taxa.SearchResult result, ICommandContext context, SearchModifier modifier) {

            switch (modifier.Type) {

                case SearchModifierType.GroupBy:

                    switch ((modifier as GroupingSearchModifier).Grouping) {

                        case SearchResultGrouping.Zone:

                            await result.GroupByAsync(async (x) => {
                                return (await BotUtils.GetZonesFromDb(x.Id)).Select(z => z.GetFullName()).ToArray();
                            });
                            break;

                        case SearchResultGrouping.Genus:

                            await result.GroupByAsync((x) => {
                                return Task.FromResult(new string[] { x.GenusName });
                            });
                            break;

                        case SearchResultGrouping.Family:

                            await result.GroupByAsync(async (x) => {

                                Taxon taxon = (await BotUtils.GetFullTaxaFromDb(x)).Family;

                                return new string[] { taxon is null ? "N/A" : taxon.GetName() };

                            });
                            break;

                        case SearchResultGrouping.Order:

                            await result.GroupByAsync(async (x) => {

                                Taxon taxon = (await BotUtils.GetFullTaxaFromDb(x)).Order;

                                return new string[] { taxon is null ? "N/A" : taxon.GetName() };

                            });
                            break;

                        case SearchResultGrouping.Class:

                            await result.GroupByAsync(async (x) => {

                                Taxon taxon = (await BotUtils.GetFullTaxaFromDb(x)).Class;

                                return new string[] { taxon is null ? "N/A" : taxon.GetName() };

                            });
                            break;

                        case SearchResultGrouping.Phylum:

                            await result.GroupByAsync(async (x) => {

                                Taxon taxon = (await BotUtils.GetFullTaxaFromDb(x)).Phylum;

                                return new string[] { taxon is null ? "N/A" : taxon.GetName() };

                            });
                            break;

                        case SearchResultGrouping.Kingdom:

                            await result.GroupByAsync(async (x) => {

                                Taxon taxon = (await BotUtils.GetFullTaxaFromDb(x)).Kingdom;

                                return new string[] { taxon is null ? "N/A" : taxon.GetName() };

                            });
                            break;

                        case SearchResultGrouping.Domain:

                            await result.GroupByAsync(async (x) => {

                                Taxon taxon = (await BotUtils.GetFullTaxaFromDb(x)).Domain;

                                return new string[] { taxon is null ? "N/A" : taxon.GetName() };

                            });
                            break;

                        case SearchResultGrouping.Creator:

                            await result.GroupByAsync(async (x) => {
                                return new string[] { await SpeciesUtils.GetOwnerOrDefaultAsync(x, context) };
                            });
                            break;

                        case SearchResultGrouping.Status:

                            await result.GroupByAsync((x) => {
                                return Task.FromResult(new string[] { x.IsExtinct ? "extinct" : "extant" });
                            });
                            break;

                        case SearchResultGrouping.Role:

                            await result.GroupByAsync(async (x) => {
                                return (await SpeciesUtils.GetRolesAsync(x.Id)).Select(z => z.name).ToArray();
                            });
                            break;

                        case SearchResultGrouping.Generation:

                            if (config.GenerationsEnabled)
                                await result.GroupByAsync(async (x) => {
                                    return new string[] { (await GenerationUtils.GetGenerationByTimestampAsync(x.Timestamp)).Name };
                                });
                            break;

                    }

                    break;

                case SearchModifierType.OrderBy:

                    result.OrderBy = (modifier as OrderingSearchModifier).Ordering;

                    break;

                case SearchModifierType.Zone:

                    // Filters out all species that aren't in the given zone(s).

                    long[] zone_list = (await ZoneUtils.GetZonesByZoneListAsync(modifier.Value)).Zones.Select(x => x.Id).ToArray();

                    await result.FilterByAsync(async (x) => {
                        return !(await BotUtils.GetZonesFromDb(x.Id)).Any(z => zone_list.Contains(z.Id));
                    }, modifier.Subtractive);

                    break;

                case SearchModifierType.Role:

                    // Filters out all species that don't have the given roles.

                    string[] role_list = modifier.Value.Split(',').Select(x => x.Trim().ToLower()).ToArray();

                    await result.FilterByAsync(async (x) => {
                        return !(await SpeciesUtils.GetRolesAsync(x.Id)).Any(r => role_list.Contains(r.name.ToLower()));
                    }, modifier.Subtractive);

                    break;

                case SearchModifierType.Format:

                    // Changes how names are displayed.

                    result.DisplayFormat = (modifier as DisplayFormatSearchModifier).DisplayFormat;

                    if (result.DisplayFormat == SearchResultDisplayFormat.Leaderboard && result.OrderBy == SearchResultOrdering.Default)
                        result.OrderBy = SearchResultOrdering.Count;

                    break;

                case SearchModifierType.Creator:

                    IUser user = await DiscordUtils.GetUserFromUsernameOrMentionAsync(context, modifier.Value);

                    await result.FilterByAsync(async (x) => {
                        return (user is null) ? ((await SpeciesUtils.GetOwnerOrDefaultAsync(x, context)).ToLower() != modifier.Value.ToLower()) : (ulong)x.OwnerUserId != user.Id;
                    }, modifier.Subtractive);

                    break;

                case SearchModifierType.Status:

                    switch (modifier.Value.ToLowerInvariant()) {

                        case "lc":
                        case "extant":

                            await result.FilterByAsync((x) => {
                                return Task.FromResult(x.IsExtinct);
                            }, modifier.Subtractive);

                            break;

                        case "ex":
                        case "extinct":

                            await result.FilterByAsync((x) => {
                                return Task.FromResult(!x.IsExtinct);
                            }, modifier.Subtractive);

                            break;

                        case "en":
                        case "endangered":

                            await result.FilterByAsync(async (x) => {
                                return !await BotUtils.IsEndangeredSpeciesAsync(x);
                            }, modifier.Subtractive);

                            break;

                    }

                    break;

                case SearchModifierType.Species:

                    await result.FilterByAsync((x) => {
                        return Task.FromResult(x.Name.ToLowerInvariant() != modifier.Value.ToLowerInvariant());
                    }, modifier.Subtractive);

                    break;

                case SearchModifierType.Genus:

                    await result.FilterByAsync((x) => {
                        return Task.FromResult(x.GenusName.ToLowerInvariant() != modifier.Value.ToLowerInvariant());
                    }, modifier.Subtractive);

                    break;

                case SearchModifierType.Family:

                    await result.FilterByAsync(async (x) => {
                        return !(await BotUtils.GetFullTaxaFromDb(x)).Contains(modifier.Value, TaxonRank.Family);
                    }, modifier.Subtractive);

                    break;

                case SearchModifierType.Order:

                    await result.FilterByAsync(async (x) => {
                        return !(await BotUtils.GetFullTaxaFromDb(x)).Contains(modifier.Value, TaxonRank.Order);
                    }, modifier.Subtractive);

                    break;

                case SearchModifierType.Class:

                    await result.FilterByAsync(async (x) => {
                        return !(await BotUtils.GetFullTaxaFromDb(x)).Contains(modifier.Value, TaxonRank.Class);
                    }, modifier.Subtractive);

                    break;

                case SearchModifierType.Phylum:

                    await result.FilterByAsync(async (x) => {
                        return !(await BotUtils.GetFullTaxaFromDb(x)).Contains(modifier.Value, TaxonRank.Phylum);
                    }, modifier.Subtractive);

                    break;

                case SearchModifierType.Kingdom:

                    await result.FilterByAsync(async (x) => {
                        return !(await BotUtils.GetFullTaxaFromDb(x)).Contains(modifier.Value, TaxonRank.Kingdom);
                    }, modifier.Subtractive);

                    break;

                case SearchModifierType.Domain:

                    await result.FilterByAsync(async (x) => {
                        return !(await BotUtils.GetFullTaxaFromDb(x)).Contains(modifier.Value, TaxonRank.Domain);
                    }, modifier.Subtractive);

                    break;

                case SearchModifierType.Taxon:

                    await result.FilterByAsync(async (x) => {
                        return !(await BotUtils.GetFullTaxaFromDb(x)).Contains(modifier.Value);
                    }, modifier.Subtractive);

                    break;

                case SearchModifierType.Random: {

                        if (!int.TryParse(modifier.Value, out int count))
                            break;

                        if (count <= 0)
                            break;

                        // Generate N random IDs from the results.
                        long[] ids = result.ToArray().OrderBy(i => BotUtils.RandomInteger(int.MaxValue)).Take(count).Select(i => i.Id).ToArray();

                        // Filter all but those results.

                        await result.FilterByAsync((x) => {
                            return Task.FromResult(!ids.Contains(x.Id));
                        }, modifier.Subtractive);

                    }

                    break;

                case SearchModifierType.Prey: {

                        // Filters out all species that do not prey upon the given species.

                        Species[] prey_list = await SpeciesUtils.GetSpeciesAsync(modifier.Value);
                        Species[] predator_list = prey_list.Count() == 1 ? await SpeciesUtils.GetPredatorsAsync(prey_list[0]) : new Species[] { };

                        await result.FilterByAsync((x) => {
                            return Task.FromResult(!predator_list.Any(i => i.Id == x.Id));
                        }, modifier.Subtractive);

                    }

                    break;

                case SearchModifierType.Predator: {

                        // Filters out all species that are not in the prey list of the given species.

                        Species[] predator_list = await SpeciesUtils.GetSpeciesAsync(modifier.Value);
                        PreyInfo[] prey_list = predator_list.Count() == 1 ? await SpeciesUtils.GetPreyAsync(predator_list[0]) : new PreyInfo[] { };

                        await result.FilterByAsync((x) => {
                            return Task.FromResult(!prey_list.Any(i => i.Prey.Id == x.Id));
                        }, modifier.Subtractive);

                    }

                    break;

                case SearchModifierType.PreyNotes: {

                        // Filters out species that don't have the given keyword in the prey notes.

                        await result.FilterByAsync(async (x) => {
                            return !(await SpeciesUtils.GetPreyAsync(x)).Where(n => n.Notes.ToLower().Contains(modifier.Value.ToLowerInvariant())).Any();
                        }, modifier.Subtractive);

                        break;

                    }

                case SearchModifierType.Has: {

                        switch (modifier.Value.ToLowerInvariant()) {

                            case "prey":

                                await result.FilterByAsync(async (x) => {
                                    return (await SpeciesUtils.GetPreyAsync(x)).Count() <= 0;
                                }, modifier.Subtractive);

                                break;

                            case "predator":
                            case "predators":

                                await result.FilterByAsync(async (x) => {
                                    return (await SpeciesUtils.GetPredatorsAsync(x)).Count() <= 0;
                                }, modifier.Subtractive);

                                break;

                            case "ancestor":
                            case "ancestors":

                                await result.FilterByAsync(async (x) => {
                                    return await SpeciesUtils.GetAncestorAsync(x) is null;
                                }, modifier.Subtractive);

                                break;

                            case "descendant":
                            case "descendants":
                            case "evo":
                            case "evos":
                            case "evolution":
                            case "evolutions":

                                await result.FilterByAsync(async (x) => {
                                    return (await SpeciesUtils.GetDirectDescendantsAsync(x)).Count() <= 0;
                                }, modifier.Subtractive);

                                break;

                            case "role":
                            case "roles":

                                await result.FilterByAsync(async (x) => {
                                    return (await SpeciesUtils.GetRolesAsync(x)).Count() <= 0;
                                }, modifier.Subtractive);

                                break;

                            case "pic":
                            case "pics":
                            case "picture":
                            case "pictures":
                            case "image":
                            case "images":

                                await result.FilterByAsync(async (x) => {
                                    return (await database.GetAllPicturesAsync(new SpeciesAdapter(x))).Count() <= 0;
                                }, modifier.Subtractive);

                                break;

                            case "size":

                                await result.FilterByAsync((x) => {
                                    return Task.FromResult(SpeciesSizeMatch.Match(x.Description).ToString() == SpeciesSizeMatch.UNKNOWN_SIZE_STRING);
                                }, modifier.Subtractive);

                                break;

                        }

                    }

                    break;

                case SearchModifierType.Ancestor: {

                        // Filter all species that don't have the given species as an ancestor.

                        Species species = await SpeciesUtils.GetUniqueSpeciesAsync(modifier.Value);

                        await result.FilterByAsync(async (x) => {
                            return species is null || !(await SpeciesUtils.GetAncestorIdsAsync(x.Id)).Any(id => id == species.Id);
                        }, modifier.Subtractive);

                    }

                    break;

                case SearchModifierType.Descendant: {

                        // Filter all species that don't have the given species as a descendant.

                        Species species = await SpeciesUtils.GetUniqueSpeciesAsync(modifier.Value);
                        long[] ancestorIds = await SpeciesUtils.GetAncestorIdsAsync(species.Id);

                        await result.FilterByAsync((x) => {
                            return Task.FromResult(ancestorIds.Length <= 0 || !ancestorIds.Any(id => id == x.Id));
                        }, modifier.Subtractive);

                    }

                    break;

                case SearchModifierType.Limit: {

                        // Filter all species that aren't in the first n results.

                        if (int.TryParse(modifier.Value, out int limit)) {

                            Species[] searchResults = result.ToArray().Take(limit).ToArray();

                            await result.FilterByAsync(async (x) =>
                                await Task.FromResult(!searchResults.Any(n => n.Id == x.Id)), modifier.Subtractive);

                        }

                    }

                    break;

                case SearchModifierType.Artist:

                    await result.FilterByAsync(async (x) => {
                        return !(await database.GetAllPicturesAsync(new SpeciesAdapter(x))).Any(n => n.Artist.ToString().Equals(modifier.Value, StringComparison.OrdinalIgnoreCase));
                    }, modifier.Subtractive);

                    break;

                // The following are only available when generations are enabled.

                case SearchModifierType.Generation:
                    if (config.GenerationsEnabled)
                        await result.FilterByAsync(async (x) => {

                            Generation gen = await GenerationUtils.GetGenerationByTimestampAsync(x.Timestamp);

                            return gen == null || gen.Number.ToString() != modifier.Value;

                        }, modifier.Subtractive);
                    break;


            }

        }

    }

}
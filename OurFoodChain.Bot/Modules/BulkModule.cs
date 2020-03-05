using Discord.Commands;
using OurFoodChain.Adapters;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Zones;
using OurFoodChain.Data;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Data.Queries;
using OurFoodChain.Discord.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot.Modules {

    public class BulkModule :
        ModuleBase {

        public SQLiteDatabase Db { get; set; }

        [Command("bulk"), RequirePrivilege(PrivilegeLevel.ServerModerator), DifficultyLevel(DifficultyLevel.Advanced)]
        public async Task Bulk([Remainder]string operationString) {

            // Instantiate the bulk operation and get the query results.

            Taxa.IBulkOperation bulkOperation = new Taxa.BulkOperation(operationString);
            ISearchResult queryResult = await Db.GetSearchResultsAsync(new SearchContext(Context, Db), bulkOperation.Query);

            if (queryResult.Count() <= 0) {

                await DiscordUtilities.ReplyInfoAsync(Context.Channel, "No species matching this query could be found.");

            }
            else {

                // Perform the requested operation.

                switch (bulkOperation.OperationName) {

                    case "addto": {

                            // Move the species into the given zone.

                            if (bulkOperation.Arguments.Count() != 1)
                                throw new Exception(string.Format("This operation requires {0} argument(s), but was given {1}.", 1, bulkOperation.Arguments.Count()));

                            string zoneName = bulkOperation.Arguments.First();
                            IZone zone = await Db.GetZoneAsync(zoneName);

                            if (await BotUtils.ReplyValidateZoneAsync(Context, zone, zoneName)) {

                                PaginatedMessageBuilder message = new PaginatedMessageBuilder {
                                    Message = string.Format("**{0}** species will be added to **{1}**. Is this OK?", queryResult.Count(), zone.GetFullName()),
                                    Restricted = true,
                                    Callback = async args => {

                                        await DiscordUtilities.ReplyInfoAsync(Context.Channel, "Performing the requested operation.");

                                        foreach (Species species in queryResult.ToArray()) {

                                            //await SpeciesUtils.RemoveZonesAsync(species, (await SpeciesUtils.GetZonesAsync(species)).Select(z => z.Zone));
                                            await Db.AddZonesAsync(new SpeciesAdapter(species), new IZone[] { zone });

                                        }

                                        await DiscordUtilities.ReplySuccessAsync(Context.Channel, "Operation completed successfully.");

                                    }
                                };

                                message.AddReaction(PaginatedMessageReaction.Yes);

                                await DiscordUtils.SendMessageAsync(Context, message.Build());

                            }

                        }
                        break;

                    default:

                        await DiscordUtilities.ReplyErrorAsync(Context.Channel, string.Format("Unknown operation \"{0}\".", bulkOperation.OperationName));

                        break;

                }

            }

        }

    }

}
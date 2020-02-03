using Discord.Commands;
using OurFoodChain.Bot.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot.Modules {

    public class BulkModule :
        ModuleBase {

        public Services.ISearchService SearchService { get; set; }

        [Command("bulk"), RequirePrivilege(PrivilegeLevel.ServerModerator), DifficultyLevel(DifficultyLevel.Advanced)]
        public async Task Bulk([Remainder]string operationString) {

            // Instantiate the bulk operation and get the query results.

            Taxa.IBulkOperation bulkOperation = new Taxa.BulkOperation(operationString);
            Taxa.SearchResult queryResult = await SearchService.GetQueryResultAsync(Context, bulkOperation.Query);

            if (queryResult.Count() <= 0) {

                await Discord.DiscordUtilities.ReplyInfoAsync(Context.Channel, "No species matching this query could be found.");

            }
            else {

                // Perform the requested operation.

                switch (bulkOperation.OperationName) {

                    case "addto": {

                            // Move the species into the given zone.

                            if (bulkOperation.Arguments.Count() != 1)
                                throw new Exception(string.Format("This operation requires {0} argument(s), but was given {1}.", 1, bulkOperation.Arguments.Count()));

                            string zoneName = bulkOperation.Arguments.First();
                            Zone zone = await ZoneUtils.GetZoneAsync(zoneName);

                            if (await BotUtils.ReplyValidateZoneAsync(Context, zone, zoneName)) {

                                PaginatedMessageBuilder message = new PaginatedMessageBuilder {
                                    Message = string.Format("**{0}** species will be added to **{1}**. Is this OK?", queryResult.Count(), zone.FullName),
                                    Restricted = true,
                                    Callback = async args => {

                                        await Discord.DiscordUtilities.ReplyInfoAsync(Context.Channel, "Performing the requested operation.");

                                        foreach (Species species in queryResult.ToArray()) {

                                            //await SpeciesUtils.RemoveZonesAsync(species, (await SpeciesUtils.GetZonesAsync(species)).Select(z => z.Zone));
                                            await SpeciesUtils.AddZonesAsync(species, new Zone[] { zone });

                                        }

                                        await Discord.DiscordUtilities.ReplySuccessAsync(Context.Channel, "Operation completed successfully.");

                                    }
                                };

                                message.AddReaction(PaginatedMessageReaction.Yes);

                                await DiscordUtils.SendMessageAsync(Context, message.Build());

                            }

                        }
                        break;

                    default:

                        await Discord.DiscordUtilities.ReplyErrorAsync(Context.Channel, string.Format("Unknown operation \"{0}\".", bulkOperation.OperationName));

                        break;

                }

            }

        }

    }

}
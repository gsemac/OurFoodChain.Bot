using Discord.Commands;
using OurFoodChain.Bot;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Data;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Discord.Messaging;
using OurFoodChain.Discord.Services;
using OurFoodChain.Discord.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class OfcModuleBase :
        ModuleBase {

        // Public members

        public IDatabaseService DatabaseService { get; set; }
        public IPaginatedMessageService PaginatedMessageService { get; set; }
        public IOfcBotConfiguration Config { get; set; }

        public SQLiteDatabase Db => GetDatabaseAsync().Result;
        public SearchContext SearchContext => new SearchContext(Context, Db);

        public async Task<SQLiteDatabase> GetDatabaseAsync() => await DatabaseService.GetDatabaseAsync(Context.Guild.Id);

        public async Task ReplyInfoAsync(string message) => await DiscordUtilities.ReplyInfoAsync(Context.Channel, message);
        public async Task ReplyWarningAsync(string message) => await DiscordUtilities.ReplyWarningAsync(Context.Channel, message);
        public async Task ReplyErrorAsync(string message) => await DiscordUtilities.ReplyErrorAsync(Context.Channel, message);
        public async Task ReplySuccessAsync(string message) => await DiscordUtilities.ReplySuccessAsync(Context.Channel, message);

        public async Task ReplyAsync(IPaginatedMessage message) => await PaginatedMessageService.SendMessageAsync(Context, message);
        public async Task ReplyAndWaitAsync(IPaginatedMessage message) => await PaginatedMessageService.SendMessageAndWaitAsync(Context, message);
        public async Task ReplyAsync(IEmbed message) => await ReplyAsync("", false, message.ToDiscordEmbed());

        public async Task<ISpecies> GetSpeciesOrReplyAsync(string genusName, string speciesName) {

            ISpecies species = null;
            IEnumerable<ISpecies> matchingSpecies = await Db.GetSpeciesAsync(genusName, speciesName);

            if (matchingSpecies.Count() <= 0) {

                // The species could not be found.

                species = await ReplySpeciesSuggestionAync(genusName, speciesName);

            }
            else if (matchingSpecies.Count() > 1) {

                // Multiple species were found.

                await ReplyMatchingSpeciesAsync(matchingSpecies);

            }
            else {

                species = matchingSpecies.First();

            }

            return species;

        }

        public async Task<ISpecies> ReplySpeciesSuggestionAync(string genusName, string speciesName) {

            int minimumDistance = int.MaxValue;
            ISpecies suggestion = null;

            foreach (ISpecies species in await Db.GetSpeciesAsync()) {

                int dist = StringUtilities.GetLevenshteinDistance(speciesName, species.Name);

                if (dist < minimumDistance) {

                    minimumDistance = dist;

                    suggestion = species;

                }

            }

            return await ReplyNoSuchSpeciesExistsAsync(suggestion);

        }
        public async Task<ISpecies> ReplyNoSuchSpeciesExistsAsync(ISpecies suggestion) {

            StringBuilder sb = new StringBuilder();

            sb.Append("No such species exists.");

            if (suggestion != null)
                sb.Append(string.Format(" Did you mean **{0}**?", suggestion));

            IPaginatedMessage message = new Discord.Messaging.PaginatedMessage(sb.ToString()) {
                Restricted = true
            };

            if (suggestion != null) {

                bool confirmed = false;

                message.AddReaction(PaginatedMessageReactionType.Yes, async (args) => {

                    confirmed = true;

                    await Task.CompletedTask;

                });

                await ReplyAndWaitAsync(message);

                if (!confirmed)
                    suggestion = null;

            }
            else
                await ReplyAsync(message);

            return suggestion;

        }
        public async Task ReplyMatchingSpeciesAsync(IEnumerable<ISpecies> speciesList) {

            Embed embed = new Embed();

            List<string> lines = new List<string>();

            embed.Title = string.Format("Matching species ({0})", speciesList.Count());

            foreach (Species sp in speciesList)
                lines.Add(sp.FullName);

            embed.Description = (string.Join(Environment.NewLine, lines));

            await ReplyAsync(embed);

        }

    }

}
using Discord.Commands;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Common;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Data;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Discord.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public class PreyModule :
        OfcModuleBase {

        // Public members

        [Command("+prey", RunMode = RunMode.Async), Alias("setprey", "seteats", "setpredates"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddPrey(string speciesName, string preySpeciesName) {

            if (IsSpeciesList(preySpeciesName)) {

                // The user has specified multiple prey items in a list.

                await ReplyAddPreyAsync(string.Empty, speciesName, SplitSpeciesList(preySpeciesName), string.Empty);

            }
            else {

                // The user has provided a single prey item.

                await AddPrey(string.Empty, speciesName, string.Empty, preySpeciesName);

            }

        }
        [Command("+prey", RunMode = RunMode.Async), Alias("setprey", "seteats", "setpredates"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddPrey(string arg0, string arg1, string arg2) {

            // We have the following cases:
            // <genusName> <speciesName> <preySpeciesName>
            // <speciesName> <preyGenusName> <preySpeciesName>
            // <speciesName> <preySpecieName> <Notes>

            // If the user provided a prey list, it's easier to determine what they meant-- Check for that first.

            if (IsSpeciesList(arg1)) {

                // <speciesName> <preyList> <notes>

                await ReplyAddPreyAsync(string.Empty, arg0, SplitSpeciesList(arg1), arg2);

            }
            else if (IsSpeciesList(arg2)) {

                // <genusName> <speciesName> <preyList>

                await ReplyAddPreyAsync(arg0, arg1, SplitSpeciesList(arg2), string.Empty);

            }
            else {

                ISpeciesAmbiguityResolverResult resolverResult = await ReplyResolveAmbiguityAsync(arg0, arg1, arg2, options: AmbiguityResolverOptions.AllowExtra);

                if (resolverResult.Success)
                    await ReplyAddPreyAsync(resolverResult.First.First(), new ISpecies[] { resolverResult.Second.First() }, resolverResult.Extra);

            }

        }
        [Command("+prey", RunMode = RunMode.Async), Alias("setprey", "seteats", "setpredates"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddPrey(string arg0, string arg1, string arg2, string arg3) {

            ISpeciesAmbiguityResolverResult resolverResult = await ReplyResolveAmbiguityAsync(arg0, arg1, arg2, arg3, options: AmbiguityResolverOptions.AllowExtra);

            if (resolverResult.Success)
                await ReplyAddPreyAsync(resolverResult.First.First(), new ISpecies[] { resolverResult.Second.First() }, resolverResult.Extra);

        }
        [Command("+prey", RunMode = RunMode.Async), Alias("setprey", "seteats", "setpredates"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddPrey(string genusName, string speciesName, string preyGenusName, string preySpeciesName, string notes) {

            ISpecies predatorSpecies = await GetSpeciesOrReplyAsync(genusName, speciesName);
            ISpecies preySpecies = predatorSpecies.IsValid() ? await GetSpeciesOrReplyAsync(preyGenusName, preySpeciesName) : null;

            if (predatorSpecies.IsValid() && preySpecies.IsValid())
                await ReplyAddPreyAsync(predatorSpecies, new ISpecies[] { preySpecies }, notes);

        }

        [Command("-prey", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task RemovePrey(string speciesName, string preySpeciesName) {

            await RemovePrey(string.Empty, speciesName, string.Empty, preySpeciesName);

        }
        [Command("-prey", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task RemovePrey(string arg0, string arg1, string arg2) {

            // We have the following possibilities:
            // <genusName> <speciesName> <preySpeciesName>
            // <speciesName> <preyGenusName> <preySpeciesName>

            ISpeciesAmbiguityResolverResult result = await ReplyResolveAmbiguityAsync(arg0, arg1, arg2);

            if (result.Success)
                await ReplyRemovePreyAsync(result.First.First(), result.Second.First());

        }
        [Command("-prey", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task RemovePrey(string genusName, string speciesName, string preyGenusName, string preySpeciesName) {

            ISpecies predatorSpecies = await GetSpeciesOrReplyAsync(genusName, speciesName);
            ISpecies preySpecies = predatorSpecies.IsValid() ? await GetSpeciesOrReplyAsync(preyGenusName, preySpeciesName) : null;

            if (predatorSpecies.IsValid() && preySpecies.IsValid())
                await ReplyRemovePreyAsync(predatorSpecies, preySpecies);

        }

        [Command("predates", RunMode = RunMode.Async), Alias("eats", "pred", "predators")]
        public async Task Predates(string genusName, string speciesName = "") {

            // If the species parameter was not provided, assume the user only provided the species.

            if (string.IsNullOrEmpty(speciesName)) {

                speciesName = genusName;
                genusName = string.Empty;

            }

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid()) {

                IEnumerable<IPredationInfo> predatorSpecies = (await Db.GetPredatorsAsync(species))
                    .Where(info => !info.Species.IsExtinct())
                    .OrderBy(info => info.Species.GetShortName());

                if (predatorSpecies.Count() > 0) {

                    Discord.Messaging.IEmbed embed = new Discord.Messaging.Embed();

                    List<string> lines = new List<string>();

                    foreach (IPredationInfo info in predatorSpecies) {

                        string lineText = TaxonFormatter.GetString(info.Species);

                        if (!string.IsNullOrEmpty(info.Notes))
                            lineText += string.Format(" ({0})", info.Notes.ToLowerInvariant());

                        lines.Add(lineText);

                    }

                    embed.Title = string.Format("Predators of {0} ({1})", species.GetShortName(), lines.Count());
                    embed.Description = string.Join(Environment.NewLine, lines);

                    await ReplyAsync(embed);

                }
                else {

                    await ReplyInfoAsync(string.Format("**{0}** has no extant natural predators.", species.GetShortName()));

                }

            }

        }

        [Command("prey", RunMode = RunMode.Async)]
        public async Task Prey(string genusName, string speciesName = "") {

            // If no species argument was provided, assume the user omitted the genus.

            if (string.IsNullOrEmpty(speciesName)) {

                speciesName = genusName;
                genusName = string.Empty;

            }

            // Get the specified species.

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid()) {

                // Get the preyed-upon species.

                IEnumerable<IPredationInfo> preySpecies = (await Db.GetPreyAsync(species))
                    .OrderBy(info => info.Species.GetShortName());

                if (preySpecies.Count() > 0) {

                    List<string> lines = new List<string>();

                    foreach (IPredationInfo preyInfo in preySpecies) {

                        string line = TaxonFormatter.GetString(preyInfo.Species);

                        if (!string.IsNullOrEmpty(preyInfo.Notes))
                            line += (string.Format(" ({0})", preyInfo.Notes.ToLowerInvariant()));

                        lines.Add(line);

                    }

                    string title = string.Format("Species preyed upon by {0} ({1})", species.GetShortName(), preySpecies.Count());

                    IEnumerable<Discord.Messaging.IEmbed> pages = EmbedUtilities.CreateEmbedPages(string.Empty, lines, columnsPerPage: 2, options: EmbedPaginationOptions.AddPageNumbers);

                    Discord.Messaging.IPaginatedMessage message = new Discord.Messaging.PaginatedMessage(pages);

                    message.SetTitle(title);

                    await ReplyAsync(message);

                }
                else {

                    await ReplyInfoAsync(string.Format("**{0}** does not prey upon any other species.", species.GetShortName()));

                }

            }

        }

        // Private members

        private bool IsSpeciesList(string input) {

            return SplitSpeciesList(input).Count() > 1;

        }
        private IEnumerable<string> SplitSpeciesList(string input) {

            return input.Split(',', '/', '\\');

        }

        private async Task ReplyAddPreyAsync(string genusName, string speciesName, IEnumerable<string> preySpeciesNames, string notes) {

            ISpecies predator = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (predator.IsValid()) {

                List<ISpecies> preyList = new List<ISpecies>();
                List<string> failedPrey = new List<string>();

                foreach (string preySpeciesName in preySpeciesNames) {

                    ISpecies prey = await Db.GetUniqueSpeciesAsync(preySpeciesName);

                    if (prey is null)
                        failedPrey.Add(preySpeciesName);
                    else
                        preyList.Add(prey);

                }

                if (failedPrey.Count() > 0)
                    await ReplyWarningAsync(string.Format("The following species could not be determined: {0}.",
                       StringUtilities.ConjunctiveJoin(failedPrey.Select(x => string.Format("**{0}**", StringUtilities.ToTitleCase(x))))));

                if (preyList.Count() > 0)
                    await ReplyAddPreyAsync(predator, preyList.ToArray(), notes);

            }

        }
        private async Task ReplyAddPreyAsync(ISpecies predatorSpecies, IEnumerable<ISpecies> preySpecies, string notes) {

            IEnumerable<IPredationInfo> currentPrey = await Db.GetPreyAsync(predatorSpecies);
            IEnumerable<IPredationInfo> existingPrey =
                currentPrey.Where(info => preySpecies.Any(sp => sp.Id == info.Species.Id && (notes ?? "").Equals(info.Notes ?? "", StringComparison.OrdinalIgnoreCase))).ToArray();

            // Prey should be added to the database even if it already exists in order to update the prey notes.

            foreach (ISpecies prey in preySpecies)
                await Db.AddPreyAsync(predatorSpecies, prey, notes);

            if (existingPrey.Any()) {

                string existingPreyStr = StringUtilities.ConjunctiveJoin(existingPrey.Select(info => info.Species.GetShortName().ToBold()));

                await ReplyWarningAsync($"{predatorSpecies.GetShortName().ToBold()} already preys upon {existingPreyStr}.");

            }

            preySpecies = preySpecies.Where(sp => !existingPrey.Any(info => info.Species.Id == sp.Id)).ToArray();

            if (preySpecies.Count() > 0) {

                string preySpeciesStr = StringUtilities.ConjunctiveJoin(preySpecies.Select(x => x.GetShortName().ToBold()));

                await ReplySuccessAsync($"{predatorSpecies.GetShortName().ToBold()} now preys upon {preySpeciesStr}.");

            }

        }

        private async Task ReplyRemovePreyAsync(ISpecies predatorSpecies, ISpecies preySpecies) {

            if (predatorSpecies.IsValid() && preySpecies.IsValid()) {

                IEnumerable<IPredationInfo> existingPrey = await Db.GetPreyAsync(predatorSpecies);

                if (existingPrey.Any(info => info.Species.Id == preySpecies.Id)) {

                    await Db.RemovePreyAsync(predatorSpecies, preySpecies);

                    await ReplySuccessAsync(string.Format("**{0}** no longer preys upon **{1}**.",
                        predatorSpecies.GetShortName(),
                        preySpecies.GetShortName()));

                }
                else {

                    await ReplyWarningAsync(string.Format("**{0}** does not prey upon **{1}**.",
                       predatorSpecies.GetShortName(),
                       preySpecies.GetShortName()));

                }

            }

        }

    }
}
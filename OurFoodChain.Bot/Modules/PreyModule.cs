using Discord;
using Discord.Commands;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Data;
using OurFoodChain.Data.Extensions;
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

        [Command("+prey"), Alias("setprey", "seteats", "setpredates"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
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
        [Command("+prey"), Alias("setprey", "seteats", "setpredates"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddPrey(string arg0, string arg1, string arg2) {

            // We have the following possibilities, which we will check for in-order:
            // <genusName> <speciesName> <preySpeciesName>
            // <speciesName> <preyGenusName> <preySpeciesName>
            // <speciesName> <preySpecieName> <Notes>

            // If the user provided a prey list, it's easier to determine what they meant-- Check for that first.

            if (IsSpeciesList(arg1)) {

                // <speciesName> <preyList> <notes>

                await ReplyAddPreyAsync(string.Empty, arg0, SplitSpeciesList(arg1), arg2);

            }
            else {

                ISpecies species = null, preySpecies = null;
                string notes = string.Empty;

                // <genusName> <speciesName> <preyList>

                species = await Db.GetUniqueSpeciesAsync(arg0, arg1);

                if (species.IsValid() && IsSpeciesList(arg2)) {

                    await ReplyAddPreyAsync(arg0, arg1, SplitSpeciesList(arg2), string.Empty);

                }
                else {

                    // <genusName> <speciesName> <preySpeciesName>

                    preySpecies = species is null ? null : await Db.GetUniqueSpeciesAsync(arg2);
                    notes = string.Empty;

                    if (species is null || preySpecies is null) {

                        // <speciesName> <preyGenusName> <preySpeciesName>

                        species = await Db.GetUniqueSpeciesAsync(arg0);
                        preySpecies = species is null ? null : await Db.GetUniqueSpeciesAsync(arg1, arg2);
                        notes = string.Empty;

                    }

                    if (species is null || preySpecies is null) {

                        // <speciesName> <preySpeciesName> <Notes>

                        species = await Db.GetUniqueSpeciesAsync(arg0);
                        preySpecies = species is null ? null : await Db.GetUniqueSpeciesAsync(arg1);
                        notes = arg2;

                    }

                    if (species is null)
                        await ReplyErrorAsync("The given species does not exist.");
                    else if (preySpecies is null)
                        await ReplyErrorAsync("The given prey species does not exist.");
                    else
                        await ReplyAddPreyAsync(species, new ISpecies[] { preySpecies }, notes);

                }

            }

        }
        [Command("+prey"), Alias("setprey", "seteats", "setpredates"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddPrey(string genusName, string speciesName, string preyGenusName, string preySpeciesName, string notes = "") {

            ISpecies predatorSpecies = await GetSpeciesOrReplyAsync(genusName, speciesName);
            ISpecies preySpecies = predatorSpecies.IsValid() ? await GetSpeciesOrReplyAsync(genusName, speciesName) : null;

            if (predatorSpecies.IsValid() && preySpecies.IsValid())
                await ReplyAddPreyAsync(predatorSpecies, new ISpecies[] { preySpecies }, notes);

        }

        [Command("-prey"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task RemovePrey(string speciesName, string preySpeciesName) {

            await RemovePrey(string.Empty, speciesName, string.Empty, preySpeciesName);

        }
        [Command("-prey"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task RemovePrey(string arg0, string arg1, string arg2) {

            // We have the following possibilities:
            // <genusName> <speciesName> <preySpeciesName>
            // <speciesName> <preyGenusName> <preySpeciesName>

            ISpeciesAmbiguityResolverResult result = await ReplyResolveAmbiguityAsync(arg0, arg1, arg2);

            if (result.Success)
                await ReplyRemovePreyAsync(result.First, result.Second);

        }
        [Command("-prey"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task RemovePrey(string genusName, string speciesName, string preyGenusName, string preySpeciesName) {

            ISpecies predatorSpecies = await GetSpeciesOrReplyAsync(genusName, speciesName);
            ISpecies preySpecies = predatorSpecies.IsValid() ? await GetSpeciesOrReplyAsync(genusName, speciesName) : null;

            if (predatorSpecies.IsValid() && preySpecies.IsValid())
                await ReplyRemovePreyAsync(predatorSpecies, preySpecies);

        }

        [Command("predates"), Alias("eats", "pred", "predators")]
        public async Task Predates(string genusName, string speciesName = "") {

            // If the species parameter was not provided, assume the user only provided the species.

            if (string.IsNullOrEmpty(speciesName)) {

                speciesName = genusName;
                genusName = string.Empty;

            }

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid()) {

                IEnumerable<ISpecies> predatorSpecies = (await Db.GetPredatorsAsync(species)).Where(s => !s.IsExtinct());

                if (predatorSpecies.Count() > 0) {

                    Discord.Messaging.IEmbed embed = new Discord.Messaging.Embed();

                    List<string> lines = new List<string>();

                    foreach(ISpecies sp in predatorSpecies) {

                        // ...

                    }

                    foreach (DataRow row in rows) {

                        ISpecies species = await BotUtils.GetSpeciesFromDb(row.Field<long>("species_id"));
                        string notes = row.Field<string>("notes");

                        string line_text = s.ShortName;

                        if (!string.IsNullOrEmpty(notes))
                            line_text += string.Format(" ({0})", notes.ToLower());

                        lines.Add(s.IsExtinct ? string.Format("~~{0}~~", line_text) : line_text);

                    }

                    lines.Sort();

                    embed.WithTitle(string.Format("Predators of {0} ({1})", sp.ShortName, lines.Count()));
                    embed.WithDescription(string.Join(Environment.NewLine, lines));

                    await ReplyAsync("", false, embed.Build());

                }
                else {

                    await ReplyInfoAsync(string.Format("**{0}** has no extant natural predators.", species.GetShortName()));

                }

            }

        }

        [Command("prey")]
        public async Task Prey(string genusName, string speciesName = "") {

            // If no species argument was provided, assume the user omitted the genus.
            if (string.IsNullOrEmpty(speciesName)) {
                speciesName = genusName;
                genusName = string.Empty;
            }

            // Get the specified species.

            Species sp = await BotUtils.ReplyFindSpeciesAsync(Context, genusName, speciesName);

            if (sp is null)
                return;

            // Get the preyed-upon species.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Predates WHERE species_id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", sp.Id);

                IEnumerable<DataRow> rows = await Db.GetRowsAsync(cmd);

                if (rows.Count() <= 0)
                    await BotUtils.ReplyAsync_Info(Context, string.Format("**{0}** does not prey upon any other species.", sp.ShortName));
                else {

                    List<Tuple<Species, string>> prey_list = new List<Tuple<Species, string>>();

                    foreach (DataRow row in rows) {

                        prey_list.Add(new Tuple<Species, string>(
                            await BotUtils.GetSpeciesFromDb(row.Field<long>("eats_id")),
                            row.Field<string>("notes")));

                    }

                    prey_list.Sort((lhs, rhs) => lhs.Item1.ShortName.CompareTo(rhs.Item1.ShortName));

                    List<string> lines = new List<string>();

                    foreach (Tuple<Species, string> prey in prey_list) {

                        string line = prey.Item1.IsExtinct ? BotUtils.Strikeout(prey.Item1.ShortName) : prey.Item1.ShortName;

                        if (!string.IsNullOrEmpty(prey.Item2))
                            line += (string.Format(" ({0})", prey.Item2.ToLower()));

                        lines.Add(line);

                    }

                    PaginatedMessageBuilder embed = new PaginatedMessageBuilder();

                    embed.AddPages(EmbedUtils.LinesToEmbedPages(lines));

                    embed.SetTitle(string.Format("Species preyed upon by {0} ({1})", sp.ShortName, prey_list.Count()));
                    embed.AddPageNumbers();

                    await DiscordUtils.SendMessageAsync(Context, embed.Build());

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
                       StringUtilities.ConjunctiveJoin(", ", failedPrey.Select(x => string.Format("**{0}**", StringUtilities.ToTitleCase(x))).ToArray())));

                if (preyList.Count() > 0)
                    await ReplyAddPreyAsync(predator, preyList.ToArray(), notes);

            }

        }
        private async Task ReplyAddPreyAsync(ISpecies species, IEnumerable<ISpecies> preySpecies, string notes) {

            foreach (Species prey in preySpecies) {

                using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO Predates(species_id, eats_id, notes) VALUES($species_id, $eats_id, $notes)")) {

                    cmd.Parameters.AddWithValue("$species_id", species.Id);
                    cmd.Parameters.AddWithValue("$eats_id", prey.Id);
                    cmd.Parameters.AddWithValue("$notes", notes);

                    await Db.ExecuteNonQueryAsync(cmd);

                }

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** now preys upon {1}.",
                species.ShortName,
                StringUtilities.ConjunctiveJoin(", ", preySpecies.Select(x => string.Format("**{0}**", x.ShortName)).ToArray())
                ));

        }

        private async Task ReplyRemovePreyAsync(ISpecies predatorSpecies, ISpecies preySpecies) {

            if (predatorSpecies is null || preySpecies is null)
                return;

            // Remove the relationship.

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeOrOwnershipAsync(Context, BotConfiguration, PrivilegeLevel.ServerModerator, predatorSpecies))
                return;

            PreyInfo[] existing_prey = await SpeciesUtils.GetPreyAsync(predatorSpecies);

            if (existing_prey.Any(x => x.Prey.Id == preySpecies.Id)) {

                using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Predates WHERE species_id = $species_id AND eats_id = $eats_id")) {

                    cmd.Parameters.AddWithValue("$species_id", predatorSpecies.Id);
                    cmd.Parameters.AddWithValue("$eats_id", preySpecies.Id);

                    await Db.ExecuteNonQueryAsync(cmd);

                }

                await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** no longer preys upon **{1}**.",
                    predatorSpecies.ShortName,
                    preySpecies.ShortName));

            }
            else {

                await BotUtils.ReplyAsync_Warning(Context, string.Format("**{0}** does not prey upon **{1}**.",
                   predatorSpecies.ShortName,
                   preySpecies.ShortName));

            }

        }

    }
}
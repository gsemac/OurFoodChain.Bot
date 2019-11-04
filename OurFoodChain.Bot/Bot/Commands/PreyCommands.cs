using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {
    public class PreyCommands :
        ModuleBase {

        // Public methods

        [Command("+prey"), Alias("setprey", "seteats", "setpredates")]
        public async Task AddPrey(string speciesName, string preySpeciesName) {

            if (_isSpeciesList(preySpeciesName))
                // The user has specified multiple prey items in a list.
                await _addPrey(string.Empty, speciesName, _splitSpeciesList(preySpeciesName), string.Empty);
            else
                // The user has provided a single prey item.
                await AddPrey(string.Empty, speciesName, string.Empty, preySpeciesName);
        }
        [Command("+prey"), Alias("setprey", "seteats", "setpredates")]
        public async Task AddPrey(string arg0, string arg1, string arg2) {

            // We have the following possibilities, which we will check for in-order:
            // <genusName> <speciesName> <preySpeciesName>
            // <speciesName> <preyGenusName> <preySpeciesName>
            // <speciesName> <preySpecieName> <Notes>

            // If the user provided a prey list, it's easier to determine what they meant-- Check for that first.

            if (_isSpeciesList(arg1))
                await _addPrey(string.Empty, arg0, _splitSpeciesList(arg1), arg2);
            else if (_isSpeciesList(arg2))
                await _addPrey(arg0, arg1, _splitSpeciesList(arg2), string.Empty);
            else {

                Species species = null, preySpecies = null;
                string notes = string.Empty;

                // <genusName> <speciesName> <preySpeciesName>

                species = await SpeciesUtils.GetUniqueSpeciesAsync(arg0, arg1);
                preySpecies = species is null ? null : await SpeciesUtils.GetUniqueSpeciesAsync(arg2);
                notes = string.Empty;

                if (species is null || preySpecies is null) {

                    // <speciesName> <preyGenusName> <preySpeciesName>

                    species = await SpeciesUtils.GetUniqueSpeciesAsync(arg0);
                    preySpecies = species is null ? null : await SpeciesUtils.GetUniqueSpeciesAsync(arg1, arg2);
                    notes = string.Empty;

                }

                if (species is null || preySpecies is null) {

                    // <speciesName> <preySpecieName> <Notes>

                    species = await SpeciesUtils.GetUniqueSpeciesAsync(arg0);
                    preySpecies = species is null ? null : await SpeciesUtils.GetUniqueSpeciesAsync(arg1);
                    notes = arg2;

                }

                if (species is null)
                    await BotUtils.ReplyAsync_Error(Context, "The given species does not exist.");
                else if (preySpecies is null)
                    await BotUtils.ReplyAsync_Error(Context, "The given prey species does not exist.");
                else
                    await _addPrey(species, new Species[] { preySpecies }, notes);

            }

        }
        [Command("+prey"), Alias("setprey", "seteats", "setpredates")]
        public async Task AddPrey(string genusName, string speciesName, string preyGenusName, string preySpeciesName, string notes = "") {

            Species[] species_list = await SpeciesUtils.GetSpeciesAsync(genusName, speciesName);
            Species[] prey_list = await SpeciesUtils.GetSpeciesAsync(preyGenusName, preySpeciesName);

            if (species_list.Count() <= 0)
                await BotUtils.ReplyAsync_SpeciesSuggestions(Context, genusName, speciesName);
            else if (prey_list.Count() <= 0)
                await BotUtils.ReplyAsync_SpeciesSuggestions(Context, preyGenusName, preySpeciesName);
            else if (!await BotUtils.ReplyValidateSpeciesAsync(Context, species_list) || !await BotUtils.ReplyValidateSpeciesAsync(Context, prey_list))
                return;
            else
                await _addPrey(species_list[0], prey_list, notes);

        }

        [Command("-prey")]
        public async Task RemovePrey(string speciesName, string preySpeciesName) {
            await RemovePrey(string.Empty, speciesName, string.Empty, preySpeciesName);
        }
        [Command("-prey")]
        public async Task RemovePrey(string arg0, string arg1, string arg2) {

            // We have the following possibilities:
            // <genusName> <speciesName> <preySpeciesName>
            // <speciesName> <preyGenusName> <preySpeciesName>

            Resolve3ArgumentFindSpeciesAmbiguityResult result = await BotUtils.ReplyResolve3ArgumentSpeciesQueryAmbiguityAsync(Context, arg0, arg1, arg2);

            if (result.Case != Resolve3ArgumentFindSpeciesAmbiguityCase.Unknown)
                await _removePrey(result.Species1, result.Species2);

        }
        [Command("-prey")]
        public async Task RemovePrey(string genusName, string speciesName, string preyGenusName, string preySpeciesName) {

            Species predator = await BotUtils.ReplyFindSpeciesAsync(Context, genusName, speciesName);
            Species prey = predator is null ? null : await BotUtils.ReplyFindSpeciesAsync(Context, preyGenusName, preySpeciesName);

            await _removePrey(predator, prey);

        }

        [Command("predates"), Alias("eats", "pred", "predators")]
        public async Task Predates(string genus, string species = "") {

            // If the species parameter was not provided, assume the user only provided the species.
            if (string.IsNullOrEmpty(species)) {
                species = genus;
                genus = string.Empty;
            }

            Species sp = await BotUtils.ReplyFindSpeciesAsync(Context, genus, species);

            if (sp is null)
                return;

            EmbedBuilder embed = new EmbedBuilder();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Predates WHERE eats_id=$eats_id AND species_id NOT IN (SELECT species_id FROM Extinctions);")) {

                cmd.Parameters.AddWithValue("$eats_id", sp.Id);

                using (DataTable rows = await Database.GetRowsAsync(cmd)) {

                    if (rows.Rows.Count <= 0)
                        await BotUtils.ReplyAsync_Info(Context, string.Format("**{0}** has no extant natural predators.", sp.ShortName));
                    else {

                        List<string> lines = new List<string>();

                        foreach (DataRow row in rows.Rows) {

                            Species s = await BotUtils.GetSpeciesFromDb(row.Field<long>("species_id"));
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

                }

            }

        }

        [Command("prey")]
        public async Task Prey(string genus, string species = "") {

            // If no species argument was provided, assume the user omitted the genus.
            if (string.IsNullOrEmpty(species)) {
                species = genus;
                genus = string.Empty;
            }

            // Get the specified species.

            Species sp = await BotUtils.ReplyFindSpeciesAsync(Context, genus, species);

            if (sp is null)
                return;

            // Get the preyed-upon species.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Predates WHERE species_id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", sp.Id);

                using (DataTable rows = await Database.GetRowsAsync(cmd)) {

                    if (rows.Rows.Count <= 0)
                        await BotUtils.ReplyAsync_Info(Context, string.Format("**{0}** does not prey upon any other species.", sp.ShortName));
                    else {

                        List<Tuple<Species, string>> prey_list = new List<Tuple<Species, string>>();

                        foreach (DataRow row in rows.Rows) {

                            prey_list.Add(new Tuple<Species, string>(
                                await BotUtils.GetSpeciesFromDb(row.Field<long>("eats_id")),
                                row.Field<string>("notes")));

                        }

                        prey_list.Sort((lhs, rhs) => lhs.Item1.ShortName.CompareTo(rhs.Item1.ShortName));

                        StringBuilder description = new StringBuilder();

                        foreach (Tuple<Species, string> prey in prey_list) {

                            description.Append(prey.Item1.IsExtinct ? BotUtils.Strikeout(prey.Item1.ShortName) : prey.Item1.ShortName);

                            if (!string.IsNullOrEmpty(prey.Item2))
                                description.Append(string.Format(" ({0})", prey.Item2.ToLower()));

                            description.AppendLine();

                        }

                        EmbedBuilder embed = new EmbedBuilder();

                        embed.WithTitle(string.Format("Species preyed upon by {0} ({1})", sp.ShortName, prey_list.Count()));
                        embed.WithDescription(description.ToString());

                        await ReplyAsync("", false, embed.Build());

                    }

                }

            }

        }

        // Private methods

        private bool _isSpeciesList(string input) {
            return _splitSpeciesList(input).Count() > 1;
        }
        private string[] _splitSpeciesList(string input) {
            return input.Split(',', '/', '\\');
        }

        private async Task _addPrey(string genusName, string speciesName, string[] preySpeciesNames, string notes) {

            Species predator = await BotUtils.ReplyFindSpeciesAsync(Context, genusName, speciesName);

            if (predator != null) {

                List<Species> prey_list = new List<Species>();
                List<string> failed_prey = new List<string>();

                foreach (string prey_species_name in preySpeciesNames) {

                    Species prey = await SpeciesUtils.GetUniqueSpeciesAsync(prey_species_name);

                    if (prey is null)
                        failed_prey.Add(prey_species_name);
                    else
                        prey_list.Add(prey);

                }

                if (failed_prey.Count() > 0)
                    await BotUtils.ReplyAsync_Warning(Context, string.Format("The following species could not be determined: {0}.",
                       StringUtils.ConjunctiveJoin(", ", failed_prey.Select(x => string.Format("**{0}**", StringUtils.ToTitleCase(x))).ToArray())));

                if (prey_list.Count() > 0)
                    await _addPrey(predator, prey_list.ToArray(), notes);

            }

        }
        private async Task _addPrey(Species species, Species[] preySpecies, string notes) {

            // Ensure that the user has necessary privileges.
            if (!await BotUtils.ReplyHasPrivilegeOrOwnershipAsync(Context, PrivilegeLevel.ServerModerator, species))
                return;

            foreach (Species prey in preySpecies) {

                using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO Predates(species_id, eats_id, notes) VALUES($species_id, $eats_id, $notes)")) {

                    cmd.Parameters.AddWithValue("$species_id", species.Id);
                    cmd.Parameters.AddWithValue("$eats_id", prey.Id);
                    cmd.Parameters.AddWithValue("$notes", notes);

                    await Database.ExecuteNonQuery(cmd);

                }

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** now preys upon {1}.",
                species.ShortName,
                StringUtils.ConjunctiveJoin(", ", preySpecies.Select(x => string.Format("**{0}**", x.ShortName)).ToArray())
                ));

        }

        private async Task _removePrey(Species predatorSpecies, Species preySpecies) {

            if (predatorSpecies is null || preySpecies is null)
                return;

            // Remove the relationship.

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeOrOwnershipAsync(Context, PrivilegeLevel.ServerModerator, predatorSpecies))
                return;

            PreyInfo[] existing_prey = await SpeciesUtils.GetPreyAsync(predatorSpecies);

            if (existing_prey.Any(x => x.Prey.Id == preySpecies.Id)) {

                using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Predates WHERE species_id = $species_id AND eats_id = $eats_id")) {

                    cmd.Parameters.AddWithValue("$species_id", predatorSpecies.Id);
                    cmd.Parameters.AddWithValue("$eats_id", preySpecies.Id);

                    await Database.ExecuteNonQuery(cmd);

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
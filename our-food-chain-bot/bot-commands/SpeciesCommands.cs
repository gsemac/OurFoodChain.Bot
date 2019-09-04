using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Commands {

    public static class SpeciesCommandsUtils {

        public static async Task ReplySpeciesInfoAsync(ICommandContext context, Species species) {

            EmbedBuilder embed = new EmbedBuilder();
            StringBuilder description_builder = new StringBuilder();

            string embed_title = species.GetFullName();
            Color embed_color = Color.Blue;

            CommonName[] common_names = await SpeciesUtils.GetCommonNamesAsync(species);

            if (common_names.Count() > 0)
                embed_title += string.Format(" ({0})", string.Join(", ", (object[])common_names));

            embed.AddField("Owner", await species.GetOwnerOrDefault(context), inline: true);

            // Group zones according to the ones that have the same notes.

            List<string> zones_value_builder = new List<string>();

            SpeciesZone[] zone_list = await SpeciesUtils.GetZonesAsync(species);

            zone_list.GroupBy(x => string.IsNullOrEmpty(x.Notes) ? "" : x.Notes)
                .OrderBy(x => x.Key)
                .ToList()
                .ForEach(x => {

                    // Create an array of zone names, and sort them according to name.
                    List<string> zones_array = x.Select(y => y.Zone.GetShortName()).ToList();
                    zones_array.Sort((lhs, rhs) => new ArrayUtils.NaturalStringComparer().Compare(lhs, rhs));

                    if (string.IsNullOrEmpty(x.Key))
                        zones_value_builder.Add(StringUtils.CollapseAlphanumericList(string.Join(", ", zones_array), ", "));
                    else
                        zones_value_builder.Add(string.Format("{0} ({1})", StringUtils.CollapseAlphanumericList(string.Join(", ", zones_array), ", "), x.Key.ToLower()));

                });

            if (zone_list.Count() > 0) {

                embed_color = zone_list
                    .GroupBy(x => ZoneUtils.GetZoneColor(x.Zone.Type))
                    .OrderBy(x => x.Count())
                    .Last()
                    .Key;

            }

            string zones_value = string.Join("; ", zones_value_builder);

            embed.AddField("Zone(s)", string.IsNullOrEmpty(zones_value) ? "None" : zones_value, inline: true);

            // Check if the species is extinct.
            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Extinctions WHERE species_id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", species.id);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null)) {

                    embed_title = "[EXTINCT] " + embed_title;
                    embed_color = Color.Red;

                    string reason = row.Field<string>("reason");
                    long timestamp = (long)row.Field<decimal>("timestamp");

                    if (!string.IsNullOrEmpty(reason))
                        description_builder.AppendLine(string.Format("**Extinct ({0}):** _{1}_\n", BotUtils.TimestampToLongDateString(timestamp), reason));

                }

            }

            description_builder.Append(species.GetDescriptionOrDefault());

            embed.WithTitle(embed_title);
            embed.WithThumbnailUrl(species.pics);
            embed.WithColor(embed_color);

            // If the description puts us over the character limit, we'll paginate.

            if (embed.Length + description_builder.Length > CommandUtils.MAX_EMBED_LENGTH) {

                List<EmbedBuilder> pages = new List<EmbedBuilder>();

                int chunk_size = (description_builder.Length - ((embed.Length + description_builder.Length) - CommandUtils.MAX_EMBED_LENGTH)) - 3;
                int written_size = 0;
                string desc = description_builder.ToString();

                while (written_size < desc.Length) {

                    EmbedBuilder page = new EmbedBuilder();

                    page.WithTitle(embed.Title);
                    page.WithThumbnailUrl(embed.ThumbnailUrl);
                    page.WithFields(embed.Fields);
                    page.WithDescription(desc.Substring(written_size, Math.Min(chunk_size, desc.Length - written_size)) + (written_size + chunk_size < desc.Length ? "..." : ""));

                    written_size += chunk_size;

                    pages.Add(page);

                }

                PaginatedEmbedBuilder builder = new PaginatedEmbedBuilder(pages);
                builder.AddPageNumbers();
                builder.SetColor(embed_color);

                await CommandUtils.ReplyAsync_SendPaginatedMessage(context, builder.Build());

            }
            else {

                embed.WithDescription(description_builder.ToString());

                await context.Channel.SendMessageAsync("", false, embed.Build());

            }

        }

    }

    public class SpeciesCommands :
        ModuleBase {

        // Public members

        [Command("info"), Alias("i")]
        public async Task GetInfo(string genus, string species) {
            await SpeciesInfo(genus, species);
        }

        [Command("species"), Alias("sp", "s")]
        public async Task SpeciesInfo() {
            await ListSpecies();
        }
        [Command("species"), Alias("sp", "s")]
        public async Task SpeciesInfo(string species) {
            await SpeciesInfo("", species);
        }
        [Command("species"), Alias("sp", "s")]
        public async Task SpeciesInfo(string genus, string species) {

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species,
                async (BotUtils.ConfirmSuggestionArgs args) => await SpeciesInfo(args.Suggestion));

            if (sp is null)
                return;

            await SpeciesCommandsUtils.ReplySpeciesInfoAsync(Context, sp);

        }

        [Command("listspecies"), Alias("specieslist", "listsp", "splist")]
        public async Task ListSpecies() {

            // Get all species.

            List<Species> species = new List<Species>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species;"))
            using (DataTable table = await Database.GetRowsAsync(cmd))
                foreach (DataRow row in table.Rows)
                    species.Add(await Species.FromDataRow(row));

            // If there are no species, state so.

            if (species.Count <= 0) {

                await BotUtils.ReplyAsync_Info(Context, "No species have been added yet.");

                return;

            }

            // Create embed pages.

            species.Sort((lhs, rhs) => lhs.GetShortName().CompareTo(rhs.GetShortName()));

            List<EmbedBuilder> pages = EmbedUtils.SpeciesListToEmbedPages(species, fieldName: string.Format("All species ({0}):", species.Count()));

            // Send the result.

            CommandUtils.PaginatedMessage reply = new CommandUtils.PaginatedMessage();

            foreach (EmbedBuilder page in pages)
                reply.pages.Add(page.Build());

            await CommandUtils.ReplyAsync_SendPaginatedMessage(Context, reply);

        }
        [Command("listspecies"), Alias("specieslist", "listsp", "splist")]
        public async Task ListSpecies(string taxonName) {

            // Get the taxon.

            Taxon taxon = await BotUtils.GetTaxonFromDb(taxonName);

            if (taxon is null) {

                await BotUtils.ReplyAsync_Error(Context, "No such taxon exists.");

                return;

            }

            // Get all species under that taxon.

            List<Species> species = new List<Species>();
            species.AddRange(await BotUtils.GetSpeciesInTaxonFromDb(taxon));

            species.Sort((lhs, rhs) => lhs.GetFullName().CompareTo(rhs.GetFullName()));

            // We might get a lot of species, which may not fit in one embed.
            // We'll need to use a paginated embed to reliably display the full list.

            // Create embed pages.

            List<EmbedBuilder> pages = EmbedUtils.SpeciesListToEmbedPages(species, fieldName: string.Format("Species in this {0} ({1}):", taxon.GetTypeName(), species.Count()));

            if (pages.Count <= 0)
                pages.Add(new EmbedBuilder());

            // Add description to the first page.

            StringBuilder description_builder = new StringBuilder();
            description_builder.AppendLine(taxon.GetDescriptionOrDefault());

            if (species.Count() <= 0) {

                description_builder.AppendLine();
                description_builder.AppendLine(string.Format("This {0} contains no species.", Taxon.GetRankName(taxon.type)));

            }

            // Add title to all pages.

            foreach (EmbedBuilder page in pages) {

                page.WithTitle(string.IsNullOrEmpty(taxon.CommonName) ? taxon.GetName() : string.Format("{0} ({1})", taxon.GetName(), taxon.GetCommonName()));
                page.WithDescription(description_builder.ToString());
                page.WithThumbnailUrl(taxon.pics);

            }

            // Send the result.

            CommandUtils.PaginatedMessage reply = new CommandUtils.PaginatedMessage();

            foreach (EmbedBuilder page in pages)
                reply.pages.Add(page.Build());

            await CommandUtils.ReplyAsync_SendPaginatedMessage(Context, reply);

        }

        [Command("setspecies"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetSpecies(string species, string newName) {
            await SetSpecies("", species, newName);
        }
        [Command("setspecies"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetSpecies(string genus, string species, string newName) {

            // Get the specified species.

            Species sp = await BotUtils.ReplyFindSpeciesAsync(Context, genus, species);

            if (sp is null)
                return;

            // Update the species.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET name=$name WHERE id=$species_id;")) {

                cmd.Parameters.AddWithValue("$name", newName.ToLower());
                cmd.Parameters.AddWithValue("$species_id", sp.id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** has been successfully renamed to **{1}**.", sp.GetShortName(), BotUtils.GenerateSpeciesName(sp.genus, newName)));

        }

        [Command("addspecies"), Alias("addsp"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddSpecies(string genus, string species, string zone = "", string description = "") {

            // Check if the species already exists before attempting to add it.

            if ((await BotUtils.GetSpeciesFromDb(genus, species)).Count() > 0) {
                await BotUtils.ReplyAsync_Warning(Context, string.Format("The species \"{0}\" already exists.", BotUtils.GenerateSpeciesName(genus, species)));
                return;
            }

            await BotUtils.AddGenusToDb(genus);

            Taxon genus_info = await BotUtils.GetGenusFromDb(genus);

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO Species(name, description, genus_id, owner, timestamp, user_id) VALUES($name, $description, $genus_id, $owner, $timestamp, $user_id);")) {

                cmd.Parameters.AddWithValue("$name", species.ToLower());
                cmd.Parameters.AddWithValue("$description", description);
                cmd.Parameters.AddWithValue("$genus_id", genus_info.id);
                cmd.Parameters.AddWithValue("$owner", Context.User.Username);
                cmd.Parameters.AddWithValue("$user_id", Context.User.Id);
                cmd.Parameters.AddWithValue("$timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                await Database.ExecuteNonQuery(cmd);

            }

            Species[] sp_list = await BotUtils.GetSpeciesFromDb(genus, species);
            Species sp = sp_list.Count() > 0 ? sp_list[0] : null;
            long species_id = sp == null ? -1 : sp.id;

            if (species_id < 0) {
                await BotUtils.ReplyAsync_Error(Context, "Failed to add species (invalid Species ID).");
                return;
            }

            // Add to all given zones.
            await _plusZone(sp, zone, string.Empty, onlyShowErrors: true);

            // Add the user to the trophy scanner queue in case their species earned them any new trophies.

            if (OurFoodChainBot.Instance.Config.TrophiesEnabled)
                await Global.TrophyScanner.AddToQueueAsync(Context, Context.User.Id);

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully created new species, **{0}**.", BotUtils.GenerateSpeciesName(genus, species)));

        }

        [Command("setzone"), Alias("setzones"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetZone(string genus, string species, string zone = "") {

            // If the zone argument is empty, assume the user omitted the genus.

            if (string.IsNullOrEmpty(zone)) {
                zone = species;
                species = genus;
                genus = string.Empty;
            }

            // Get the specified species.

            Species sp = await BotUtils.ReplyFindSpeciesAsync(Context, genus, species);

            if (sp is null)
                return;

            // Delete existing zone information for the species.

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM SpeciesZones WHERE species_id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", sp.id);

                await Database.ExecuteNonQuery(cmd);

            }

            // Add new zone information for the species.
            await _plusZone(sp, zone, string.Empty, onlyShowErrors: false);

        }

        [Command("+zone"), Alias("+zones"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task PlusZone(string arg0, string arg1, string arg2) {

            // Possible cases:
            // 1. <species> <zone> <notes>
            // 2. <genus> <species> <zone>

            // If a species exists with the given genus/species, assume the user intended case (2).

            Species[] species_list = await SpeciesUtils.GetSpeciesAsync(arg0, arg1);

            if (species_list.Count() == 1) {

                // If there is a unqiue species match, proceed with the assumption of case (2).

                await _plusZone(species_list[0], zoneList: arg2, notes: string.Empty, onlyShowErrors: false);

            }
            else if (species_list.Count() > 1) {

                // If there are species matches but no unique result, show the user.
                await BotUtils.ReplyValidateSpeciesAsync(Context, species_list);

            }
            else if (species_list.Count() <= 0) {

                // If there were no matches, assume the user intended case (1).

                species_list = await SpeciesUtils.GetSpeciesAsync(string.Empty, arg0);

                if (await BotUtils.ReplyValidateSpeciesAsync(Context, species_list))
                    await _plusZone(species_list[0], zoneList: arg1, notes: arg2, onlyShowErrors: false);

            }

        }
        [Command("+zone"), Alias("+zones"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task PlusZone(string species, string zoneList) {
            await PlusZone(string.Empty, species, zoneList, string.Empty);
        }
        [Command("+zone"), Alias("+zones"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task PlusZone(string genus, string species, string zoneList, string notes) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeAsync(Context, PrivilegeLevel.ServerModerator))
                return;

            Species sp = await BotUtils.ReplyFindSpeciesAsync(Context, genus, species);

            if (!(sp is null))
                await _plusZone(sp, zoneList: zoneList, notes: notes, onlyShowErrors: false);

        }

        [Command("-zone"), Alias("-zones"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task MinusZone(string species, string zone) {
            await MinusZone("", species, zone);
        }
        [Command("-zone"), Alias("-zones"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task MinusZone(string genus, string species, string zoneList) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeAsync(Context, PrivilegeLevel.ServerModerator))
                return;

            // Get the specified species.

            Species sp = await BotUtils.ReplyFindSpeciesAsync(Context, genus, species);

            if (sp is null)
                return;

            // Get the zones that the species currently resides in.
            // These will be used to show warning messages (e.g., doesn't exist in the given zone).

            long[] current_zone_ids = (await BotUtils.GetZonesFromDb(sp.id)).Select(x => x.Id).ToArray();

            // Get the zones from user input.
            ZoneListResult zones = await ZoneUtils.GetZonesByZoneListAsync(zoneList);

            // Remove the zones from the species.
            await SpeciesUtils.RemoveZonesAsync(sp, zones.Zones);

            if (zones.Invalid.Count() > 0) {

                // Show a warning if the user provided any invalid zones.

                await BotUtils.ReplyAsync_Warning(Context, string.Format("{0} {1} not exist.",
                    StringUtils.ConjunctiveJoin(", ", zones.Invalid.Select(x => string.Format("**{0}**", ZoneUtils.FormatZoneName(x))).ToArray()),
                    zones.Invalid.Count() == 1 ? "does" : "do"));

            }

            if (zones.Zones.Any(x => !current_zone_ids.Contains(x.Id))) {

                // Show a warning if the species wasn't in one or more of the zones provided.

                await BotUtils.ReplyAsync_Warning(Context, string.Format("**{0}** is already absent from {1}.",
                    sp.GetShortName(),
                    StringUtils.ConjunctiveJoin(", ", zones.Zones.Where(x => !current_zone_ids.Contains(x.Id)).Select(x => string.Format("**{0}**", x.GetFullName())).ToArray())));

            }

            if (zones.Zones.Any(x => current_zone_ids.Contains(x.Id))) {

                // Show a confirmation of all valid zones.

                await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** no longer inhabits {1}.",
                    sp.GetShortName(),
                    StringUtils.DisjunctiveJoin(", ", zones.Zones.Where(x => current_zone_ids.Contains(x.Id)).Select(x => string.Format("**{0}**", x.GetFullName())).ToArray())));

            }

        }

        [Command("setowner"), Alias("setown", "claim"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetOwner(string species, IUser user) {

            await SetOwner("", species, user);

        }
        [Command("setowner"), Alias("setown", "claim"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetOwner(string genus, string species, IUser user) {

            if (user is null)
                user = Context.User;

            string owner = user.Username;

            Species sp = await BotUtils.ReplyFindSpeciesAsync(Context, genus, species);

            if (sp is null)
                return;

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET owner = $owner, user_id = $user_id WHERE id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", sp.id);
                cmd.Parameters.AddWithValue("$owner", owner);
                cmd.Parameters.AddWithValue("$user_id", user.Id);

                await Database.ExecuteNonQuery(cmd);

            }

            // Add the new owner to the trophy scanner queue in case their species earned them any new trophies.

            if (OurFoodChainBot.Instance.Config.TrophiesEnabled)
                await Global.TrophyScanner.AddToQueueAsync(Context, user.Id);

            await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** is now owned by **{1}**.", sp.GetShortName(), owner));

        }

        // Private members

        public async Task _plusZone(Species species, string zoneList, string notes, bool onlyShowErrors = false) {

            // Get the zones from user input.
            ZoneListResult zones = await ZoneUtils.GetZonesByZoneListAsync(zoneList);

            // Add the zones to the species.
            await SpeciesUtils.AddZonesAsync(species, zones.Zones, notes);

            if (zones.Invalid.Count() > 0) {

                // Show a warning if the user provided any invalid zones.

                await BotUtils.ReplyAsync_Warning(Context, string.Format("{0} {1} not exist.",
                    StringUtils.ConjunctiveJoin(", ", zones.Invalid.Select(x => string.Format("**{0}**", ZoneUtils.FormatZoneName(x))).ToArray()),
                    zones.Invalid.Count() == 1 ? "does" : "do"));

            }

            if (zones.Zones.Count() > 0 && !onlyShowErrors) {

                // Show a confirmation of all valid zones.

                await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** now inhabits {1}.",
                      species.GetShortName(),
                      StringUtils.ConjunctiveJoin(", ", zones.Zones.Select(x => string.Format("**{0}**", x.GetFullName())).ToArray())));

            }

        }

    }

}
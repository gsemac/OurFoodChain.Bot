using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class Commands :
        ModuleBase {

        private const int MAX_EMBED_LENGTH = 2048;
        private static bool _running_backup = false;

        [Command("info"), Alias("i")]
        public async Task GetInfo(string name) {

            // Prioritize species first.

            Species[] species = await BotUtils.GetSpeciesFromDb("", name);

            if (species.Count() > 0) {

                if (await BotUtils.ReplyAsync_ValidateSpecies(Context, species))
                    await GetSpecies(species[0]);

            }
            else {

                // Otherwise, show other taxon.

                Taxon[] taxa = await BotUtils.GetTaxaFromDb(name);

                if (taxa.Count() <= 0)
                    // This command was traditionally used with species, so show the user species suggestions in the event of no matches.
                    await BotUtils.ReplyAsync_SpeciesSuggestions(Context, "", name, async (BotUtils.ConfirmSuggestionArgs args) => await GetInfo(args.Suggestion));
                else if (await BotUtils.ReplyAsync_ValidateTaxa(Context, taxa))
                    await BotUtils.Command_ShowTaxon(Context, taxa[0].type, name);

            }

        }
        [Command("info"), Alias("i")]
        public async Task GetInfo(string genus, string species) {
            await GetSpecies(genus, species);
        }


        [Command("species"), Alias("sp", "s")]
        public async Task GetSpecies() {
            await ListSpecies();
        }
        [Command("species"), Alias("sp", "s")]
        public async Task GetSpecies(string species) {
            await GetSpecies("", species);
        }
        [Command("species"), Alias("sp", "s")]
        public async Task GetSpecies(string genus, string species) {

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species,
                async (BotUtils.ConfirmSuggestionArgs args) => await GetSpecies(args.Suggestion));

            if (sp is null)
                return;

            await GetSpecies(sp);

        }
        public async Task GetSpecies(Species sp) {

            EmbedBuilder embed = new EmbedBuilder();
            StringBuilder description_builder = new StringBuilder();

            string embed_title = sp.GetFullName();
            Color embed_color = Color.Blue;

            CommonName[] common_names = await SpeciesUtils.GetCommonNamesAsync(sp);

            if (common_names.Count() > 0)
                embed_title += string.Format(" ({0})", string.Join(", ", (object[])common_names));

            embed.AddField("Owner", await sp.GetOwnerOrDefault(Context), inline: true);

            // Group zones according to the ones that have the same notes.

            List<string> zones_value_builder = new List<string>();

            SpeciesZone[] zone_list = await SpeciesUtils.GetZonesAsync(sp);
            zone_list.GroupBy(x => string.IsNullOrEmpty(x.Notes) ? "" : x.Notes)
                .OrderBy(x => x.Key)
                .ToList()
                .ForEach(x => {

                    if (x.Any(y => y.Zone.type == ZoneType.Terrestrial))
                        embed_color = Color.DarkGreen;

                    // Create an array of zone names, and sort them according to name.
                    List<string> zones_array = x.Select(y => y.Zone.GetShortName()).ToList();
                    zones_array.Sort((lhs, rhs) => new ArrayUtils.NaturalStringComparer().Compare(lhs, rhs));

                    if (string.IsNullOrEmpty(x.Key))
                        zones_value_builder.Add(StringUtils.CollapseAlphanumericList(string.Join(", ", zones_array), ", "));
                    else
                        zones_value_builder.Add(string.Format("{0} ({1})", StringUtils.CollapseAlphanumericList(string.Join(", ", zones_array), ", "), x.Key.ToLower()));

                });

            string zones_value = string.Join("; ", zones_value_builder);

            embed.AddField("Zone(s)", string.IsNullOrEmpty(zones_value) ? "None" : zones_value, inline: true);

            // Check if the species is extinct.
            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Extinctions WHERE species_id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", sp.id);

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

            description_builder.Append(sp.GetDescriptionOrDefault());

            embed.WithTitle(embed_title);
            embed.WithThumbnailUrl(sp.pics);
            embed.WithColor(embed_color);

            // If the description puts us over the character limit, we'll paginate.

            if (embed.Length + description_builder.Length > MAX_EMBED_LENGTH) {

                List<EmbedBuilder> pages = new List<EmbedBuilder>();

                int chunk_size = (description_builder.Length - ((embed.Length + description_builder.Length) - MAX_EMBED_LENGTH)) - 3;
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

                await CommandUtils.ReplyAsync_SendPaginatedMessage(Context, builder.Build());

            }
            else {

                embed.WithDescription(description_builder.ToString());

                await ReplyAsync("", false, embed.Build());

            }

        }

        [Command("setspecies")]
        public async Task SetSpecies(string species, string newName) {
            await SetSpecies("", species, newName);
        }
        [Command("setspecies")]
        public async Task SetSpecies(string genus, string species, string newName) {

            // Get the specified species.

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeOrOwnershipAsync(Context, PrivilegeLevel.ServerModerator, sp))
                return;

            // Update the species.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET name=$name WHERE id=$species_id;")) {

                cmd.Parameters.AddWithValue("$name", newName.ToLower());
                cmd.Parameters.AddWithValue("$species_id", sp.id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** has been successfully renamed to **{1}**.", sp.GetShortName(), BotUtils.GenerateSpeciesName(sp.genus, newName)));

        }

        [Command("addspecies"), Alias("addsp")]
        public async Task AddSpecies(string genus, string species, string zone = "", string description = "") {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeAsync(Context, PrivilegeLevel.ServerModerator))
                return;

            // Check if the species already exists before attempting to add it.

            if ((await BotUtils.GetSpeciesFromDb(genus, species)).Count() > 0) {
                await BotUtils.ReplyAsync_Warning(Context, string.Format("The species \"{0}\" already exists.", BotUtils.GenerateSpeciesName(genus, species)));
                return;
            }

            await BotUtils.AddGenusToDb(genus);

            Genus genus_info = await BotUtils.GetGenusFromDb(genus);

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
            await PlusZone(sp, zone, string.Empty, onlyShowErrors: true);

            // Add the user to the trophy scanner queue in case their species earned them any new trophies.

            if (OurFoodChainBot.Instance.Config.TrophiesEnabled)
                await Global.TrophyScanner.AddToQueueAsync(Context, Context.User.Id);

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully created new species, **{0}**.", BotUtils.GenerateSpeciesName(genus, species)));

        }

        [Command("addzone"), Alias("addz")]
        public async Task AddZone(string name, string type = "", string description = "") {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeAsync(Context, PrivilegeLevel.ServerModerator))
                return;

            // Allow the user to specify zones with numbers (e.g., "1") or single letters (e.g., "A").
            // Otherwise, the name is taken as-is.
            name = ZoneUtils.FormatZoneName(name).ToLower();

            // If an invalid type was provided, assume the user meant it as a description instead.
            // i.e., "addzone <name> <description>"
            if (type.ToLower() != "aquatic" && type.ToLower() != "terrestrial") {

                description = type;
                type = "";

            }

            type = type.ToLower();

            // Attempt to determine the type of the zone automatically if it was not provided.

            if (string.IsNullOrEmpty(type)) {

                if (Regex.Match(name, @"\d+$").Success)
                    type = "aquatic";
                else if (Regex.Match(name, "[a-z]+$").Success)
                    type = "terrestrial";
                else
                    type = "unknown";

            }

            // Don't attempt to create the zone if it already exists.

            Zone zone = await ZoneUtils.GetZoneAsync(name);

            if (!(zone is null)) {
                await BotUtils.ReplyAsync_Warning(Context, string.Format("A zone named \"{0}\" already exists.", StringUtils.ToTitleCase(zone.name)));
                return;
            }

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO Zones(name, type, description) VALUES($name, $type, $description);")) {

                cmd.Parameters.AddWithValue("$name", name.ToLower());
                cmd.Parameters.AddWithValue("$type", type.ToLower());
                cmd.Parameters.AddWithValue("$description", description);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully created new {0} zone, **{1}**.",
                type.ToLower(),
                ZoneUtils.FormatZoneName(name))
                );

        }

        [Command("zone"), Alias("z", "zones")]
        public async Task Zone(string name = "") {

            if (string.IsNullOrEmpty(name) || name == "aquatic" || name == "terrestrial") {

                // If no zone was provided, list all zones.

                // Get the zones from the datbase.
                // If the user provided a zone type, get all zones of that type. Otherwise, get all zones.

                string cmd_str = "SELECT * FROM Zones;";

                if (!string.IsNullOrEmpty(name))
                    cmd_str = string.Format("SELECT * FROM Zones WHERE type=\"{0}\";", name);

                List<Zone> zone_list = new List<Zone>();

                using (SQLiteConnection conn = await Database.GetConnectionAsync())
                using (SQLiteCommand cmd = new SQLiteCommand(cmd_str))
                using (DataTable rows = await Database.GetRowsAsync(conn, cmd))
                    foreach (DataRow row in rows.Rows)
                        zone_list.Add(OurFoodChain.Zone.FromDataRow(row));

                zone_list.Sort((lhs, rhs) => new ArrayUtils.NaturalStringComparer().Compare(lhs.name, rhs.name));

                if (zone_list.Count() > 0) {

                    // Create a line describing each zone.

                    List<string> lines = new List<string>();

                    // We need to make sure that even if the "short" description is actually long, we can show n zones per page.

                    string embed_title = StringUtils.ToTitleCase(string.Format("{0} zones ({1})", name, zone_list.Count()));
                    string embed_description = string.Format("For detailed zone information, use `{0}zone <zone>` (e.g. `{0}zone 1`).\n\n", OurFoodChainBot.Instance.Config.Prefix);
                    int zones_per_page = 20;
                    int max_line_length = (EmbedUtils.MAX_EMBED_LENGTH - embed_title.Length - embed_description.Length) / zones_per_page;

                    foreach (Zone zone in zone_list) {

                        string line = string.Format("{1} **{0}**\t-\t{2}", StringUtils.ToTitleCase(zone.name), zone.type == ZoneType.Aquatic ? "🌊" : "🌳", zone.GetShortDescription());

                        if (line.Length > max_line_length)
                            line = line.Substring(0, max_line_length - 3) + "...";

                        lines.Add(line);

                    }

                    // Build paginated message.

                    PaginatedEmbedBuilder embed = new PaginatedEmbedBuilder(EmbedUtils.LinesToEmbedPages(lines, 20));
                    embed.AddPageNumbers();

                    if (string.IsNullOrEmpty(name))
                        name = "all";
                    else if (name == "aquatic")
                        embed.SetColor(Color.Blue);
                    else if (name == "terrestrial")
                        embed.SetColor(Color.DarkGreen);

                    embed.SetTitle(embed_title);
                    embed.PrependDescription(embed_description);

                    await CommandUtils.ReplyAsync_SendPaginatedMessage(Context, embed.Build());

                }
                else {

                    await BotUtils.ReplyAsync_Info(Context, "No zones have been added yet.");

                }

                return;

            }
            else {

                Zone zone = await ZoneUtils.GetZoneAsync(name);

                if (!await BotUtils.ReplyAsync_ValidateZone(Context, zone))
                    return;

                List<Embed> pages = new List<Embed>();

                string title = string.Format("{0} {1}", zone.type == ZoneType.Aquatic ? "🌊" : "🌳", StringUtils.ToTitleCase(zone.name));
                string description = zone.GetDescriptionOrDefault();
                Color color = Color.Blue;

                switch (zone.type) {
                    case ZoneType.Aquatic:
                        color = Color.Blue;
                        break;
                    case ZoneType.Terrestrial:
                        color = Color.DarkGreen;
                        break;
                }

                // Get all species living in this zone.

                List<Species> species_list = new List<Species>(await BotUtils.GetSpeciesFromDbByZone(zone));

                species_list.Sort((lhs, rhs) => lhs.GetShortName().CompareTo(rhs.GetShortName()));

                // Starting building a paginated message.
                // The message will have a paginated species list, and a toggle button to display the species sorted by role.

                List<EmbedBuilder> embed_pages = EmbedUtils.SpeciesListToEmbedPages(species_list, fieldName: (string.Format("Extant species in this zone ({0}):", species_list.Count())));
                PaginatedEmbedBuilder paginated = new PaginatedEmbedBuilder(embed_pages);

                if (embed_pages.Count() <= 0)
                    embed_pages.Add(new EmbedBuilder());

                // Add title, decription, etc., to all pages.

                paginated.SetTitle(title);
                paginated.SetDescription(description);
                paginated.SetThumbnailUrl(zone.pics);
                paginated.SetColor(color);

                // This page will have species organized by role.
                // Only bother with the role page if species actually exist in this zone.

                if (species_list.Count() > 0) {

                    EmbedBuilder role_page = new EmbedBuilder();

                    role_page.WithTitle(title);
                    role_page.WithDescription(description);
                    //role_page.WithThumbnailUrl(zone.pics);
                    role_page.WithColor(color);

                    Dictionary<string, List<Species>> roles_map = new Dictionary<string, List<Species>>();

                    foreach (Species sp in species_list) {

                        Role[] roles_list = await SpeciesUtils.GetRolesAsync(sp);

                        if (roles_list.Count() <= 0) {

                            if (!roles_map.ContainsKey("no role"))
                                roles_map["no role"] = new List<Species>();

                            roles_map["no role"].Add(sp);

                            continue;

                        }

                        foreach (Role role in roles_list) {

                            if (!roles_map.ContainsKey(role.name))
                                roles_map[role.name] = new List<Species>();

                            roles_map[role.name].Add(sp);

                        }

                    }

                    // Sort the list of species belonging to each role.

                    foreach (List<Species> i in roles_map.Values)
                        i.Sort((lhs, rhs) => lhs.GetShortName().CompareTo(rhs.GetShortName()));

                    // Create a sorted list of keys so that the roles are in order.

                    List<string> sorted_keys = new List<string>(roles_map.Keys);
                    sorted_keys.Sort();

                    foreach (string i in sorted_keys) {

                        StringBuilder lines = new StringBuilder();

                        foreach (Species j in roles_map[i])
                            lines.AppendLine(j.GetShortName());

                        role_page.AddField(string.Format("{0}s ({1})", StringUtils.ToTitleCase(i), roles_map[i].Count()), lines.ToString(), inline: true);

                    }

                    // Add the page to the builder.

                    paginated.AddReaction("🇷");
                    paginated.SetCallback(async (CommandUtils.PaginatedMessageCallbackArgs args) => {

                        if (args.reaction != "🇷")
                            return;

                        args.paginatedMessage.paginationEnabled = !args.on;

                        if (args.on)
                            await args.discordMessage.ModifyAsync(msg => msg.Embed = role_page.Build());
                        else
                            await args.discordMessage.ModifyAsync(msg => msg.Embed = args.paginatedMessage.pages[args.paginatedMessage.index]);

                    });

                }

                await CommandUtils.ReplyAsync_SendPaginatedMessage(Context, paginated.Build());

            }

        }

        [Command("+extinct"), Alias("setextinct")]
        public async Task SetExtinct(string species) {
            await SetExtinct("", species, "");
        }
        [Command("+extinct"), Alias("setextinct")]
        public async Task SetExtinct(string arg0, string arg1) {

            // We either have a genus/species, or a species/description.

            Species[] species_list = await SpeciesUtils.GetSpeciesAsync(arg0, arg1);

            if (species_list.Count() > 0)
                // If such a species does exist, assume we have a genus/species.
                await SetExtinct(arg0, arg1, string.Empty);
            else
                // If no such species exists, assume we have a species/description.
                await SetExtinct(string.Empty, arg0, arg1);

        }
        [Command("+extinct"), Alias("setextinct")]
        public async Task SetExtinct(string genus, string species, string reason) {

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            await SetExtinct(sp, reason);

        }
        private async Task SetExtinct(Species species, string reason) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeOrOwnershipAsync(Context, PrivilegeLevel.ServerModerator, species))
                return;

            await SpeciesUtils.SetExtinctionInfoAsync(species, new ExtinctionInfo {
                IsExtinct = true,
                Reason = reason,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });

            await BotUtils.ReplyAsync_Success(Context, string.Format(
                species.isExtinct ?
                "Updated extinction details for **{0}**." :
                "The last **{0}** has perished, and the species is now extinct.",
                species.GetShortName()));

        }
        [Command("-extinct"), Alias("setextant", "unextinct")]
        public async Task MinusExtinct(string species) {
            await MinusExtinct("", species);
        }
        [Command("-extinct"), Alias("setextant", "unextinct")]
        public async Task MinusExtinct(string genus, string species) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeAsync(Context, PrivilegeLevel.ServerModerator))
                return;

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            // If the species is not extinct, don't do anything.

            if (!sp.isExtinct) {

                await BotUtils.ReplyAsync_Warning(Context, string.Format("**{0}** is not extinct.", sp.GetShortName()));

                return;

            }

            // Delete the extinction from the database.

            await SpeciesUtils.SetExtinctionInfoAsync(sp, new ExtinctionInfo { IsExtinct = false });

            await BotUtils.ReplyAsync_Success(Context, string.Format("A population of **{0}** has been discovered! The species is no longer considered extinct.", sp.GetShortName()));

        }
        [Command("extinct")]
        public async Task Extinct() {

            List<Species> sp_list = new List<Species>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id IN (SELECT species_id FROM Extinctions);"))
            using (DataTable rows = await Database.GetRowsAsync(cmd))
                foreach (DataRow row in rows.Rows)
                    sp_list.Add(await Species.FromDataRow(row));

            sp_list.Sort((lhs, rhs) => lhs.GetShortName().CompareTo(rhs.GetShortName()));

            PaginatedEmbedBuilder embed = new PaginatedEmbedBuilder();
            embed.AddPages(EmbedUtils.SpeciesListToEmbedPages(sp_list, fieldName: string.Format("Extinct species ({0})", sp_list.Count()), flags: EmbedPagesFlag.None));

            await CommandUtils.ReplyAsync_SendPaginatedMessage(Context, embed.Build(), "There are currently no extinct species.");

        }

        [Command("setancestor")]
        public async Task SetAncestor(string species, string ancestorSpecies) {
            await SetAncestor(string.Empty, species, string.Empty, ancestorSpecies);
        }
        [Command("setancestor")]
        public async Task SetAncestor(string genus, string species, string ancestorSpecies) {
            await SetAncestor(genus, species, genus, ancestorSpecies);
        }
        [Command("setancestor")]
        public async Task SetAncestor(string genus, string species, string ancestorGenus, string ancestorSpecies) {

            // Get the descendant and ancestor species.

            Species[] descendant_list = await BotUtils.GetSpeciesFromDb(genus, species);
            Species[] ancestor_list = await BotUtils.GetSpeciesFromDb(ancestorGenus, ancestorSpecies);

            if (descendant_list.Count() > 1)
                await BotUtils.ReplyAsync_Error(Context, string.Format("The child species \"{0}\" is too vague (there are multiple matches). Try including the genus.", species));
            else if (ancestor_list.Count() > 1)
                await BotUtils.ReplyAsync_Error(Context, string.Format("The ancestor species \"{0}\" is too vague (there are multiple matches). Try including the genus.", ancestorSpecies));
            else if (descendant_list.Count() == 0)
                await BotUtils.ReplyAsync_Error(Context, "The child species does not exist.");
            else if (ancestor_list.Count() == 0)
                await BotUtils.ReplyAsync_Error(Context, "The parent species does not exist.");
            else if (descendant_list[0].id == ancestor_list[0].id)
                await BotUtils.ReplyAsync_Error(Context, "A species cannot be its own ancestor.");
            else {

                Species descendant = descendant_list[0];
                Species ancestor = ancestor_list[0];

                // Ensure that the user has necessary privileges to use this command.
                if (!await BotUtils.ReplyHasPrivilegeOrOwnershipAsync(Context, PrivilegeLevel.ServerModerator, descendant))
                    return;

                // Check if an ancestor has already been set for this species. If so, update the ancestor, but we'll show a different message later notifying the user of the change.

                Species existing_ancestor_sp = null;

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT ancestor_id FROM Ancestors WHERE species_id=$species_id;")) {

                    cmd.Parameters.AddWithValue("$species_id", descendant.id);

                    DataRow row = await Database.GetRowAsync(cmd);

                    if (!(row is null)) {

                        long ancestor_id = row.Field<long>("ancestor_id");

                        existing_ancestor_sp = await BotUtils.GetSpeciesFromDb(ancestor_id);

                    }

                }

                // If the ancestor has already been set to the species specified, quit.

                if (!(existing_ancestor_sp is null) && existing_ancestor_sp.id == ancestor.id) {

                    await BotUtils.ReplyAsync_Warning(Context, string.Format("**{0}** has already been set as the ancestor of **{1}**.", ancestor.GetShortName(), descendant.GetShortName()));

                    return;

                }

                // Insert the new relationship into the database.

                using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO Ancestors(species_id, ancestor_id) VALUES($species_id, $ancestor_id);")) {

                    cmd.Parameters.AddWithValue("$species_id", descendant.id);
                    cmd.Parameters.AddWithValue("$ancestor_id", ancestor.id);

                    await Database.ExecuteNonQuery(cmd);

                }

                if (existing_ancestor_sp is null)
                    await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** has been set as the ancestor of **{1}**.", ancestor.GetShortName(), descendant.GetShortName()));
                else
                    await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** has replaced **{1}** as the ancestor of **{2}**.", ancestor.GetShortName(), existing_ancestor_sp.GetShortName(), descendant.GetShortName()));

            }

        }

        [Command("ancestry"), Alias("lineage", "ancestors", "anc")]
        public async Task Lineage(string species) {
            await Lineage("", species);
        }
        [Command("ancestry"), Alias("lineage", "ancestors", "anc")]
        public async Task Lineage(string genus, string species) {

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            List<string> entries = new List<string>();

            entries.Add(string.Format("{0} - {1}", sp.GetTimeStampAsDateString(), sp.GetShortName()));

            long species_id = sp.id;

            while (true) {

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT ancestor_id FROM Ancestors WHERE species_id=$species_id;")) {

                    cmd.Parameters.AddWithValue("$species_id", species_id);

                    DataRow row = await Database.GetRowAsync(cmd);

                    if (row is null)
                        break;

                    species_id = row.Field<long>("ancestor_id");

                    Species ancestor = await BotUtils.GetSpeciesFromDb(species_id);

                    entries.Add(string.Format("{0} - {1}", ancestor.GetTimeStampAsDateString(), ancestor.GetShortName()));

                }

            }

            entries.Reverse();

            await ReplyAsync(string.Format("```{0}```", string.Join(Environment.NewLine, entries)));

        }
        [Command("ancestry2"), Alias("lineage2", "anc2")]
        public async Task Lineage2(string species) {
            await Lineage2("", species);
        }
        [Command("ancestry2"), Alias("lineage2", "anc2")]
        public async Task Lineage2(string genus, string species) {

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            string image = await BotUtils.GenerateEvolutionTreeImage(sp);

            await Context.Channel.SendFileAsync(image);

        }

        [Command("evolution"), Alias("evo")]
        public async Task Evolution(string species) {
            await Evolution("", species);
        }
        [Command("evolution"), Alias("evo")]
        public async Task Evolution(string genus, string species) {

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            string image = await BotUtils.GenerateEvolutionTreeImage(sp, descendantsOnly: true);

            await Context.Channel.SendFileAsync(image);

        }

        [Command("+zone"), Alias("+zones")]
        public async Task PlusZone(string arg0, string arg1, string arg2) {

            // Possible cases:
            // 1. <species> <zone> <notes>
            // 2. <genus> <species> <zone>

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeAsync(Context, PrivilegeLevel.ServerModerator))
                return;

            // If a species exists with the given genus/species, assume the user intended case (2).

            Species[] species_list = await SpeciesUtils.GetSpeciesAsync(arg0, arg1);

            if (species_list.Count() == 1) {

                // If there is a unqiue species match, proceed with the assumption of case (2).

                await PlusZone(species_list[0], zoneList: arg2, notes: string.Empty, onlyShowErrors: false);

            }
            else if (species_list.Count() > 1) {

                // If there are species matches but no unique result, show the user.
                await BotUtils.ReplyAsync_ValidateSpecies(Context, species_list);

            }
            else if (species_list.Count() <= 0) {

                // If there were no matches, assume the user intended case (1).

                species_list = await SpeciesUtils.GetSpeciesAsync(string.Empty, arg0);

                if (await BotUtils.ReplyAsync_ValidateSpecies(Context, species_list))
                    await PlusZone(species_list[0], zoneList: arg1, notes: arg2, onlyShowErrors: false);

            }

        }
        [Command("+zone"), Alias("+zones")]
        public async Task PlusZone(string species, string zoneList) {
            await PlusZone(string.Empty, species, zoneList, string.Empty);
        }
        [Command("+zone"), Alias("+zones")]
        public async Task PlusZone(string genus, string species, string zoneList, string notes) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeAsync(Context, PrivilegeLevel.ServerModerator))
                return;

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (!(sp is null))
                await PlusZone(sp, zoneList: zoneList, notes: notes, onlyShowErrors: false);

        }
        public async Task PlusZone(Species species, string zoneList, string notes, bool onlyShowErrors = false) {

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
        [Command("-zone"), Alias("-zones")]
        public async Task MinusZone(string species, string zone) {
            await MinusZone("", species, zone);
        }
        [Command("-zone"), Alias("-zones")]
        public async Task MinusZone(string genus, string species, string zoneList) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeAsync(Context, PrivilegeLevel.ServerModerator))
                return;

            // Get the specified species.

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            // Get the zones that the species currently resides in.
            // These will be used to show warning messages (e.g., doesn't exist in the given zone).

            long[] current_zone_ids = (await BotUtils.GetZonesFromDb(sp.id)).Select(x => x.id).ToArray();

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

            if (zones.Zones.Any(x => !current_zone_ids.Contains(x.id))) {

                // Show a warning if the species wasn't in one or more of the zones provided.

                await BotUtils.ReplyAsync_Warning(Context, string.Format("**{0}** is already absent from {1}.",
                    sp.GetShortName(),
                    StringUtils.ConjunctiveJoin(", ", zones.Zones.Where(x => !current_zone_ids.Contains(x.id)).Select(x => string.Format("**{0}**", x.GetFullName())).ToArray())));

            }

            if (zones.Zones.Any(x => current_zone_ids.Contains(x.id))) {

                // Show a confirmation of all valid zones.

                await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** no longer inhabits {1}.",
                    sp.GetShortName(),
                    StringUtils.DisjunctiveJoin(", ", zones.Zones.Where(x => current_zone_ids.Contains(x.id)).Select(x => string.Format("**{0}**", x.GetFullName())).ToArray())));

            }

        }

        [Command("migration"), Alias("spread")]
        public async Task Migration(string speciesName) {
            await Migration("", speciesName);
        }
        [Command("migration"), Alias("spread")]
        public async Task Migration(string genusName, string speciesName) {

            Species species = await BotUtils.ReplyAsync_FindSpecies(Context, genusName, speciesName);

            if (species is null)
                return;

            // Group zones changes that happened closely together (12 hours).

            SpeciesZone[] zones = (await SpeciesUtils.GetZonesAsync(species)).OrderBy(x => x.Timestamp).ToArray();
            List<List<SpeciesZone>> zone_groups = new List<List<SpeciesZone>>();

            long last_timestamp = zones.Count() > 0 ? zones.First().Timestamp : 0;

            foreach (SpeciesZone zone in zones) {

                if (zone_groups.Count() <= 0)
                    zone_groups.Add(new List<SpeciesZone>());

                if (zone_groups.Last().Count() <= 0 || Math.Abs(zone_groups.Last().Last().Timestamp - zone.Timestamp) < 60 * 60 * 12)
                    zone_groups.Last().Add(zone);
                else {

                    last_timestamp = zone.Timestamp;
                    zone_groups.Add(new List<SpeciesZone> { zone });

                }


            }

            StringBuilder result = new StringBuilder();

            for (int i = 0; i < zone_groups.Count(); ++i) {

                if (zone_groups[i].Count() <= 0)
                    continue;

                long ts = i == 0 ? species.timestamp : zone_groups[i].First().Timestamp;

                if (ts <= 0)
                    ts = species.timestamp;

                result.Append(string.Format("{0} - ", BotUtils.GetTimeStampAsDateString(ts)));
                result.Append(i == 0 ? "Started in " : "Spread to ");
                result.Append(zone_groups[i].Count() == 1 ? "Zone " : "Zones ");
                result.Append(StringUtils.ConjunctiveJoin(", ", zone_groups[i].Select(x => x.Zone.ShortName)));

                result.AppendLine();

            }


            await ReplyAsync(string.Format("```{0}```", result.ToString()));

        }

        [Command("setzone"), Alias("setzones")]
        public async Task SetZone(string genus, string species, string zone = "") {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeAsync(Context, PrivilegeLevel.ServerModerator))
                return;

            // If the zone argument is empty, assume the user omitted the genus.

            if (string.IsNullOrEmpty(zone)) {
                zone = species;
                species = genus;
                genus = string.Empty;
            }

            // Get the specified species.

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            // Delete existing zone information for the species.

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM SpeciesZones WHERE species_id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", sp.id);

                await Database.ExecuteNonQuery(cmd);

            }

            // Add new zone information for the species.
            await PlusZone(sp, zone, string.Empty, onlyShowErrors: false);

        }

        [Command("setzonepic")]
        public async Task SetZonePic(string zone, string imageUrl) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeAsync(Context, PrivilegeLevel.ServerModerator))
                return;

            // Make sure that the given zone exists.

            Zone z = await ZoneUtils.GetZoneAsync(zone);

            if (!await BotUtils.ReplyAsync_ValidateZone(Context, z))
                return;

            // Make sure the image URL is valid.

            if (!await BotUtils.ReplyIsImageUrlValidAsync(Context, imageUrl))
                return;

            // Update the zone.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Zones SET pics=$pics WHERE id=$id;")) {

                cmd.Parameters.AddWithValue("$pics", imageUrl);
                cmd.Parameters.AddWithValue("$id", z.id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully updated the picture for **{0}**.", z.GetFullName()));

        }

        [Command("setzonedesc"), Alias("setzdesc")]
        public async Task SetZoneDescription(string zoneName, string description) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeAsync(Context, PrivilegeLevel.ServerModerator))
                return;

            // Get the zone from the database.

            Zone zone = await ZoneUtils.GetZoneAsync(zoneName);

            if (!await BotUtils.ReplyAsync_ValidateZone(Context, zone))
                return;

            // Update the description for the zone.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Zones SET description=$description WHERE id=$id;")) {

                cmd.Parameters.AddWithValue("$description", description);
                cmd.Parameters.AddWithValue("$id", zone.id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully updated the description for **{0}**.", zone.GetFullName()));

        }

        [Command("setowner"), Alias("setown", "claim")]
        public async Task SetOwner(string species, IUser user) {

            await SetOwner("", species, user);

        }
        [Command("setowner"), Alias("setown", "claim")]
        public async Task SetOwner(string genus, string species, IUser user) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeAsync(Context, PrivilegeLevel.ServerModerator))
                return;

            if (user is null)
                user = Context.User;

            string owner = user.Username;

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

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

        [Command("size"), Alias("sz")]
        public async Task Size(string species) {
            await Size("", species);
        }
        [Command("size"), Alias("sz")]
        public async Task Size(string genusOrSpecies, string speciesOrUnits) {

            // This command can be used in a number of ways:
            // <genus> <species>    -> returns size for that species
            // <species> <units>    -> returns size for that species, using the given units

            Species species = null;
            LengthUnit units = LengthUnit.Unknown;

            // Attempt to get the specified species, assuming the user passed in <genus> <species>.

            Species[] species_array = await BotUtils.GetSpeciesFromDb(genusOrSpecies, speciesOrUnits);

            if (species_array.Count() > 1)
                await BotUtils.ReplyAsync_ValidateSpecies(Context, species_array);
            else if (species_array.Count() == 1)
                species = species_array[0];
            else if (species_array.Count() <= 0) {

                // If we didn't get any species by treating the arguments as <genus> <species>, attempt to get the species by <species> only.         
                species = await BotUtils.ReplyAsync_FindSpecies(Context, "", genusOrSpecies);

                // If this still fails, there's nothing left to do.

                if (species is null)
                    return;

                // Assume the second argument was the desired units.
                // Make sure the units given are valid.

                units = new Length(0.0, speciesOrUnits).Units;

                if (units == LengthUnit.Unknown) {

                    await BotUtils.ReplyAsync_Error(Context, string.Format("Invalid units (\"{0}\").", speciesOrUnits));

                    return;

                }

            }

            if (!(species is null))
                await Size(species, units);

        }
        public async Task Size(Species species, string units) {

            LengthUnit length_units = new Length(0.0, units).Units;

            if (length_units == LengthUnit.Unknown)
                await BotUtils.ReplyAsync_Error(Context, string.Format("Invalid units (\"{0}\").", units));
            else
                await Size(species, length_units);

        }
        public async Task Size(Species species, LengthUnit units) {

            // Attempt to get the size of the species.

            SpeciesSizeMatch match = SpeciesSizeMatch.Match(species.description);

            // Output the result.

            EmbedBuilder embed = new EmbedBuilder();
            embed.Title = string.Format("Size of {0}", species.GetFullName());
            embed.WithDescription(units == LengthUnit.Unknown ? match.ToString() : match.ToString(units));
            embed.WithFooter("Size is determined from species description, and may not be accurate.");

            await ReplyAsync("", false, embed.Build());

        }
        [Command("size"), Alias("sz")]
        public async Task Size(string genus, string species, string units) {

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (!(species is null))
                await Size(sp, units);

        }

        [Command("addedby"), Alias("ownedby", "own", "owned")]
        public async Task AddedBy() {
            await AddedBy(Context.User);
        }
        [Command("addedby"), Alias("ownedby", "own", "owned")]
        public async Task AddedBy(IUser user) {

            if (user is null)
                user = Context.User;

            // Get all species belonging to this user.

            List<Species> species_list = new List<Species>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE owner = $owner OR user_id = $user_id;")) {

                cmd.Parameters.AddWithValue("$owner", user.Username);
                cmd.Parameters.AddWithValue("$user_id", user.Id);

                using (DataTable rows = await Database.GetRowsAsync(cmd)) {

                    foreach (DataRow row in rows.Rows)
                        species_list.Add(await Species.FromDataRow(row));

                    species_list.Sort((lhs, rhs) => lhs.GetShortName().CompareTo(rhs.GetShortName()));

                }

            }

            // Display the species belonging to this user.

            await _displaySpeciesAddedBy(user.Username, user.GetAvatarUrl(size: 32), species_list);

        }
        [Command("addedby"), Alias("ownedby", "own", "owned")]
        public async Task AddedBy(string owner) {

            // If we get this overload, then the requested user does not currently exist in the guild.

            // Get all species belonging to this user.

            // First, see if we can find a user ID belong to this user in the database. 
            // This allows us to find all species they have made even if their username had changed at some point.

            List<Species> species_list = new List<Species>();
            long user_id = 0;

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT owner, user_id FROM Species WHERE owner = $owner COLLATE NOCASE AND user_id IS NOT NULL LIMIT 1;")) {

                cmd.Parameters.AddWithValue("$owner", owner);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null)) {

                    owner = row.Field<string>("owner");
                    user_id = row.Field<long>("user_id");

                }

            }

            // Generate a list of species belonging to this username or user ID.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE owner = $owner COLLATE NOCASE OR user_id = $user_id;")) {

                cmd.Parameters.AddWithValue("$owner", owner);
                cmd.Parameters.AddWithValue("$user_id", user_id);

                using (DataTable rows = await Database.GetRowsAsync(cmd)) {

                    foreach (DataRow row in rows.Rows) {

                        Species sp = await Species.FromDataRow(row);
                        owner = sp.owner;

                        species_list.Add(sp);

                    }

                    species_list.Sort((lhs, rhs) => lhs.GetShortName().CompareTo(rhs.GetShortName()));

                }

            }

            // If no species were found, then no such user exists.

            if (species_list.Count() <= 0) {

                await BotUtils.ReplyAsync_Error(Context, "No such user exists.");

                return;

            }

            // Display the species belonging to this user.

            await _displaySpeciesAddedBy(owner, string.Empty, species_list);

        }
        private async Task _displaySpeciesAddedBy(string username, string thumbnailUrl, List<Species> speciesList) {

            if (speciesList.Count() <= 0) {

                await BotUtils.ReplyAsync_Info(Context, string.Format("**{0}** has not submitted any species yet.", username));

            }
            else {

                PaginatedEmbedBuilder embed = new PaginatedEmbedBuilder(EmbedUtils.SpeciesListToEmbedPages(speciesList,
                    fieldName: string.Format("Species owned by {0} ({1})", username, speciesList.Count())));

                embed.SetThumbnailUrl(thumbnailUrl);

                await CommandUtils.ReplyAsync_SendPaginatedMessage(Context, embed.Build());

            }

        }

        [Command("search")]
        public async Task Search([Remainder]string queryString) {

            // Create and execute the search query.

            SearchQuery query = new SearchQuery(Context, queryString);
            SearchQuery.FindResult result = await query.FindMatchesAsync();

            // Build the embed.

            if (result.Count() <= 0) {

                await BotUtils.ReplyAsync_Info(Context, "No species matching this query could be found.");

            }
            else if (result.Count() == 1) {

                // If there's only one result, just show that species.
                await GetSpecies(result.ToArray()[0]);

            }
            else {

                PaginatedEmbedBuilder embed;

                if (result.HasGroup(SearchQuery.DEFAULT_GROUP)) {

                    // If there's only one group, just list the species without creating separate fields.
                    embed = new PaginatedEmbedBuilder(EmbedUtils.ListToEmbedPages(result.DefaultGroup.ToList(), fieldName: string.Format("Search results ({0})", result.Count())));

                }
                else {

                    embed = new PaginatedEmbedBuilder();
                    embed.AddPages(EmbedUtils.SearchQueryResultToEmbedPages(result));

                }

                embed.SetFooter("");
                embed.AddPageNumbers();

                await CommandUtils.ReplyAsync_SendPaginatedMessage(Context, embed.Build());

            }


        }

        [Command("roll")]
        public async Task Roll() {
            await Roll(6);
        }
        [Command("roll")]
        public async Task Roll(int max) {

            if (max < 1)
                await BotUtils.ReplyAsync_Error(Context, "Value must be greater than or equal 1.");

            else
                await Roll(1, max);

        }
        [Command("roll")]
        public async Task Roll(int min, int max) {

            if (min < 0 || max < 0)
                await BotUtils.ReplyAsync_Error(Context, "Values must be greater than 1.");
            if (min > max + 1)
                await BotUtils.ReplyAsync_Error(Context, "Minimum value must be less than or equal to the maximum value.");
            else {

                int result = BotUtils.RandomInteger(min, max + 1);

                await ReplyAsync(result.ToString());

            }

        }

        [Command("backup", RunMode = RunMode.Async)]
        public async Task Backup() {

            if (!await BotUtils.ReplyHasPrivilegeAsync(Context, PrivilegeLevel.BotAdmin))
                return;

            if (_running_backup) {

                await BotUtils.ReplyAsync_Error(Context, "A backup is already in progress. Please wait until it has completed.");

            }
            else {

                _running_backup = true;

                if (System.IO.File.Exists(Database.GetFilePath()))
                    try {

                        await BotUtils.ReplyAsync_Info(Context, "Uploading database backup. The backup will be posted in this channel when it is complete.");

                        await Context.Channel.SendFileAsync(Database.GetFilePath(), string.Format(string.Format("`Database backup {0}`", DateTime.UtcNow.ToString())));

                    }
                    catch (Exception) {
                        await BotUtils.ReplyAsync_Error(Context, "Database file cannot be accessed.");
                    }
                else
                    await BotUtils.ReplyAsync_Error(Context, "Database file does not exist at the specified path.");

                _running_backup = false;

            }

        }

        [Command("taxonomy"), Alias("taxon")]
        public async Task Taxonomy(string species) {
            await Taxonomy("", species);
        }
        [Command("taxonomy"), Alias("taxon")]
        public async Task Taxonomy(string genus, string species) {

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle(string.Format("Taxonomy of {0}", sp.GetShortName()));
            embed.WithThumbnailUrl(sp.pics);

            TaxonSet set = await BotUtils.GetFullTaxaFromDb(sp);

            string unknown = "Unknown";
            string genus_name = set.Genus is null ? unknown : set.Genus.GetName();
            string family_name = set.Family is null ? unknown : set.Family.GetName();
            string order_name = set.Order is null ? unknown : set.Order.GetName();
            string class_name = set.Class is null ? unknown : set.Class.GetName();
            string phylum_name = set.Phylum is null ? unknown : set.Phylum.GetName();
            string kingdom_name = set.Kingdom is null ? unknown : set.Kingdom.GetName();
            string domain_name = set.Domain is null ? unknown : set.Domain.GetName();

            embed.AddField("Domain", domain_name, inline: true);
            embed.AddField("Kingdom", kingdom_name, inline: true);
            embed.AddField("Phylum", phylum_name, inline: true);
            embed.AddField("Class", class_name, inline: true);
            embed.AddField("Order", order_name, inline: true);
            embed.AddField("Family", family_name, inline: true);
            embed.AddField("Genus", genus_name, inline: true);
            embed.AddField("Species", StringUtils.ToTitleCase(sp.name), inline: true);

            await ReplyAsync("", false, embed.Build());

        }

        [Command("recent")]
        public async Task Recent() {
            await Recent("48h");
        }
        [Command("recent")]
        public async Task Recent(string timespan) {

            TimeAmount time_amount = TimeAmount.Parse(timespan);

            if (time_amount != null) {

                long start_ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - time_amount.ToUnixTimeSeconds();

                // Get all species created recently.

                List<Species> new_species = new List<Species>();

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE timestamp >= $start_ts;")) {

                    cmd.Parameters.AddWithValue("$start_ts", start_ts);

                    using (DataTable table = await Database.GetRowsAsync(cmd))
                        foreach (DataRow row in table.Rows)
                            new_species.Add(await Species.FromDataRow(row));

                }

                new_species.Sort();

                // Get all extinctions that occurred recently.

                List<Species> extinct_species = new List<Species>();

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Extinctions WHERE timestamp >= $start_ts;")) {

                    cmd.Parameters.AddWithValue("$start_ts", start_ts);

                    using (DataTable table = await Database.GetRowsAsync(cmd))
                        foreach (DataRow row in table.Rows)
                            extinct_species.Add(await BotUtils.GetSpeciesFromDb(row.Field<long>("species_id")));

                }

                extinct_species.Sort();

                // Build embed.

                PaginatedEmbedBuilder embed = new PaginatedEmbedBuilder();
                List<EmbedBuilder> pages = new List<EmbedBuilder>();
                List<string> field_lines = new List<string>();

                if (new_species.Count() > 0) {

                    foreach (Species sp in new_species)
                        field_lines.Add(sp.GetFullName());

                    EmbedUtils.AddLongFieldToEmbedPages(pages, field_lines, fieldName: string.Format("New species ({0})", new_species.Count()));

                    field_lines.Clear();

                }

                if (extinct_species.Count() > 0) {

                    foreach (Species sp in extinct_species)
                        field_lines.Add(sp.GetFullName());

                    EmbedUtils.AddLongFieldToEmbedPages(pages, field_lines, fieldName: string.Format("Extinctions ({0})", extinct_species.Count()));

                    field_lines.Clear();

                }

                embed.AddPages(pages);

                embed.SetTitle(string.Format("Recent events ({0})", time_amount.ToString()));
                embed.SetFooter(string.Empty); // remove page numbers added automatically
                embed.AddPageNumbers();

                await CommandUtils.ReplyAsync_SendPaginatedMessage(Context, embed.Build());

            }
            else
                await BotUtils.ReplyAsync_Error(Context, "Invalid timespan provided.");

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

        [Command("random"), Alias("rand")]
        public async Task Random() {

            // Get a random species from the database.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id NOT IN (SELECT species_id FROM Extinctions) ORDER BY RANDOM() LIMIT 1;")) {

                DataRow row = await Database.GetRowAsync(cmd);

                if (row is null)
                    await BotUtils.ReplyAsync_Info(Context, "There are currently no extant species.");
                else
                    await GetSpecies(await Species.FromDataRow(row));

            }

        }
        [Command("random"), Alias("rand")]
        public async Task Random(string taxonName) {

            // Get the taxon.

            Taxon taxon = await BotUtils.GetTaxonFromDb(taxonName);

            if (taxon is null) {

                await BotUtils.ReplyAsync_Error(Context, "No such taxon exists.");

                return;

            }

            // Get all species under that taxon.

            List<Species> species = new List<Species>();
            species.AddRange(await BotUtils.GetSpeciesInTaxonFromDb(taxon));
            species.RemoveAll(x => x.isExtinct);

            if (species.Count() <= 0)
                await BotUtils.ReplyAsync_Info(Context, string.Format("{0} **{1}** does not contain any extant species.", StringUtils.ToTitleCase(taxon.GetTypeName()), taxon.GetName()));
            else
                await GetSpecies(species[BotUtils.RandomInteger(species.Count())]);

        }

        [Command("profile")]
        public async Task Profile() {
            await Profile(Context.User);
        }
        [Command("profile")]
        public async Task Profile(IUser user) {

            long species_count = 0;

            // Get the total number of species.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) FROM Species;"))
                species_count = await Database.GetScalar<long>(cmd);

            // Get this user's species count, first timestamp, and last timestamp.

            long user_species_count = 0;
            long timestamp_min = 0;
            long timestamp_max = 0;
            long timestamp_diff_days = 0;

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) AS count, MIN(timestamp) AS timestamp_min, MAX(timestamp) AS timestamp_max FROM Species WHERE owner=$owner OR user_id=$user_id;")) {

                cmd.Parameters.AddWithValue("$owner", user.Username);
                cmd.Parameters.AddWithValue("$user_id", user.Id);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null) && !row.IsNull("timestamp_min")) {

                    user_species_count = row.Field<long>("count");
                    timestamp_min = row.Field<long>("timestamp_min");
                    timestamp_max = row.Field<long>("timestamp_max");

                    timestamp_diff_days = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timestamp_min) / 60 / 60 / 24;

                }

            }

            // Get the user rankings according to the number of submitted species.

            int user_rank = 1;

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT owner, COUNT(id) AS count FROM Species GROUP BY user_id ORDER BY count DESC;"))
            using (DataTable table = await Database.GetRowsAsync(cmd)) {

                foreach (DataRow row in table.Rows) {

                    long count = row.Field<long>("count");

                    if (count > user_species_count)
                        user_rank += 1;
                    else
                        break;

                }

            }

            // Get the user's most active genus.

            string favorite_genus = "N/A";
            long genus_count = 0;

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT genus_id, COUNT(genus_id) AS count FROM Species WHERE owner=$owner OR user_id=$user_id GROUP BY genus_id ORDER BY count DESC LIMIT 1;")) {

                cmd.Parameters.AddWithValue("$owner", user.Username);
                cmd.Parameters.AddWithValue("$user_id", user.Id);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null)) {

                    long genus_id = row.Field<long>("genus_id");
                    genus_count = row.Field<long>("count");

                    Genus genus = await BotUtils.GetGenusFromDb(genus_id);

                    favorite_genus = genus.name;

                }

            }

            // Get the user's rarest trophy.

            string rarest_trophy = "N/A";

            Trophies.UnlockedTrophyInfo[] unlocked = await Global.TrophyRegistry.GetUnlockedTrophiesAsync(user.Id);

            if (unlocked.Count() > 0) {

                Array.Sort(unlocked, (lhs, rhs) => lhs.timesUnlocked.CompareTo(rhs.timesUnlocked));

                Trophies.Trophy trophy = await Global.TrophyRegistry.GetTrophyByIdentifierAsync(unlocked[0].identifier);

                rarest_trophy = trophy.GetName();

            }

            // Put together the user's profile.

            EmbedBuilder embed = new EmbedBuilder();

            embed.WithTitle(string.Format("{0}'s profile", user.Username));
            embed.WithThumbnailUrl(user.GetAvatarUrl(size: 64));

            if (user_species_count > 0) {

                embed.WithDescription(string.Format("{1} made their first species on **{2}**.{0}Since then, they have submitted **{3:0.0}** species per day.{0}{0}Their submissions make up **{4:0.0}%** of all species.",
                    Environment.NewLine,
                    user.Username,
                    BotUtils.TimestampToLongDateString(timestamp_min),
                    timestamp_diff_days == 0 ? user_species_count : (double)user_species_count / timestamp_diff_days,
                    ((double)user_species_count / species_count) * 100.0));
                embed.AddField("Species", string.Format("{0} (Rank **#{1}**)", user_species_count, user_rank), inline: true);
                embed.AddField("Favorite genus", string.Format("{0} ({1} spp.)", StringUtils.ToTitleCase(favorite_genus), genus_count), inline: true);
                embed.AddField("Trophies", string.Format("{0} ({1:0.0}%)",
                    (await Global.TrophyRegistry.GetUnlockedTrophiesAsync(user.Id)).Count(),
                    await Global.TrophyRegistry.GetUserCompletionRateAsync(user.Id)
                    ), inline: true);
                embed.AddField("Rarest trophy", rarest_trophy, inline: true);

            }
            else
                embed.WithDescription(string.Format("{0} has not submitted any species.", user.Username));

            await ReplyAsync("", false, embed.Build());

        }
        [Command("leaderboard")]
        public async Task Leaderboard() {

            List<string> lines = new List<string>();
            long place = 1;
            long last_count = -1;

            // Get the users and their species counts, ordered by species count.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT owner, user_id, COUNT(id) AS count FROM Species GROUP BY user_id ORDER BY count DESC;")) {

                using (DataTable table = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in table.Rows) {

                        // Get information about the user.

                        ulong user_id = row.IsNull("user_id") ? 0 : (ulong)row.Field<long>("user_id");
                        IUser user = Context.Guild is null ? null : await Context.Guild.GetUserAsync(user_id);
                        long count = row.Field<long>("count");
                        string icon = "";

                        if (last_count != -1 && count < last_count)
                            ++place;

                        last_count = count;

                        switch (place) {

                            case 1:
                                icon = "👑";
                                break;

                            case 2:
                                icon = "🥈";
                                break;

                            case 3:
                                icon = "🥉";
                                break;

                            default:
                                icon = "➖";
                                break;

                        }


                        lines.Add(string.Format("**`{0}`**{1}`{2}` {3}",
                            string.Format("{0}.", place.ToString("000")),
                            icon,
                            count.ToString("000"),
                            string.Format(place <= 3 ? "**{0}**" : "{0}", user is null ? row.Field<string>("owner") : user.Username)
                           ));

                    }

            }

            // Create the embed.

            PaginatedEmbedBuilder embed = new PaginatedEmbedBuilder();
            embed.AddPages(EmbedUtils.LinesToEmbedPages(lines));
            embed.SetTitle(string.Format("🏆 Leaderboard ({0})", lines.Count));
            embed.SetColor(255, 204, 77);
            embed.AddPageNumbers();

            // Send the embed.
            await CommandUtils.ReplyAsync_SendPaginatedMessage(Context, embed.Build());

        }

        [Command("+fav"), Alias("addfav")]
        public async Task AddFav(string species) {

            await AddFav("", species);

        }
        [Command("+fav"), Alias("addfav")]
        public async Task AddFav(string genus, string species) {

            // Get the requested species.

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            // Add this species to the user's favorites list.

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Favorites(user_id, species_id) VALUES($user_id, $species_id);")) {

                cmd.Parameters.AddWithValue("$user_id", Context.User.Id);
                cmd.Parameters.AddWithValue("$species_id", sp.id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully added **{0}** to **{1}**'s favorites list.", sp.GetShortName(), Context.User.Username));

        }
        [Command("-fav")]
        public async Task MinusFav(string species) {

            await MinusFav("", species);

        }
        [Command("-fav")]
        public async Task MinusFav(string genus, string species) {

            // Get the requested species.

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            // Remove this species from the user's favorites list.

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Favorites WHERE user_id = $user_id AND species_id = $species_id;")) {

                cmd.Parameters.AddWithValue("$user_id", Context.User.Id);
                cmd.Parameters.AddWithValue("$species_id", sp.id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully removed **{0}** from **{1}**'s favorites list.", sp.GetShortName(), Context.User.Username));

        }
        [Command("favs"), Alias("fav", "favorites", "favourites")]
        public async Task Favs() {

            // Get all species fav'd by this user.

            List<string> lines = new List<string>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id IN (SELECT species_id FROM Favorites WHERE user_id = $user_id);")) {

                cmd.Parameters.AddWithValue("$user_id", Context.User.Id);

                using (DataTable rows = await Database.GetRowsAsync(cmd)) {

                    foreach (DataRow row in rows.Rows) {

                        Species sp = await Species.FromDataRow(row);
                        long fav_count = 0;

                        // Get the number of times this species has been favorited.

                        using (SQLiteCommand cmd2 = new SQLiteCommand("SELECT COUNT(*) FROM Favorites WHERE species_id = $species_id;")) {

                            cmd2.Parameters.AddWithValue("$species_id", sp.id);

                            fav_count = await Database.GetScalar<long>(cmd2);

                        }

                        lines.Add(sp.GetShortName() + (fav_count > 1 ? string.Format(" ({0} favs)", fav_count) : ""));

                    }

                    lines.Sort();

                }

            }

            // Display the species list.

            if (lines.Count() <= 0) {

                await BotUtils.ReplyAsync_Info(Context, string.Format("**{0}** has not favorited any species.", Context.User.Username));

            }
            else {

                PaginatedEmbedBuilder embed = new PaginatedEmbedBuilder(EmbedUtils.LinesToEmbedPages(lines));
                embed.SetTitle(string.Format("⭐ Species favorited by {0} ({1})", Context.User.Username, lines.Count()));
                embed.SetThumbnailUrl(Context.User.GetAvatarUrl(size: 32));
                embed.AddPageNumbers();

                await CommandUtils.ReplyAsync_SendPaginatedMessage(Context, embed.Build());

            }

        }

    }

}
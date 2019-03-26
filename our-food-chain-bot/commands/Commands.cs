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

                Taxon taxon = await BotUtils.GetTaxonFromDb(name);

                if (taxon is null)
                    // For now, call the regular "GetSpecies" command so this command still shows species suggestions.
                    await GetSpecies(name);
                else
                    await BotUtils.Command_ShowTaxon(Context, taxon.type, name);

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

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            await GetSpecies(sp);

        }
        public async Task GetSpecies(Species sp) {

            EmbedBuilder embed = new EmbedBuilder();
            StringBuilder description_builder = new StringBuilder();

            string embed_title = sp.GetFullName();
            Color embed_color = Color.Blue;

            if (!string.IsNullOrEmpty(sp.commonName))
                embed_title += string.Format(" ({0})", StringUtils.ToTitleCase(sp.commonName));

            embed.AddField("Owner", await sp.GetOwnerOrDefault(Context), inline: true);

            List<string> zone_names = new List<string>();

            foreach (Zone zone in await BotUtils.GetZonesFromDb(sp.id)) {

                if (zone.type == ZoneType.Terrestrial)
                    embed_color = Color.DarkGreen;

                zone_names.Add(zone.GetShortName());

            }

            zone_names.Sort((lhs, rhs) => new ArrayUtils.NaturalStringComparer().Compare(lhs, rhs));

            string zones_value = string.Join(", ", zone_names);

            embed.AddField("Zone(s)", string.IsNullOrEmpty(zones_value) ? "None" : zones_value, inline: true);

            // Check if the species is extinct.
            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Extinctions WHERE species_id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", sp.id);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null)) {

                    embed_title = "[EXTINCT] " + embed_title;
                    embed_color = Color.Red;

                    string reason = row.Field<string>("reason");
                    string ts = BotUtils.GetTimeStampAsDateString((long)row.Field<decimal>("timestamp"));

                    if (!string.IsNullOrEmpty(reason))
                        description_builder.AppendLine(string.Format("**Extinct ({0}):** _{1}_\n", ts, reason));

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
            if (!await BotUtils.ReplyAsync_CheckPrivilegeOrOwnership(Context, (IGuildUser)Context.User, PrivilegeLevel.ServerModerator, sp))
                return;

            // Update the species.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET name=$name WHERE id=$species_id;")) {

                cmd.Parameters.AddWithValue("$name", newName.ToLower());
                cmd.Parameters.AddWithValue("$species_id", sp.id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** has been successfully renamed to **{1}**.", sp.GetShortName(), BotUtils.GenerateSpeciesName(sp.genus, newName)));

        }

        [Command("setpic"), Alias("setspeciespic", "setspic")]
        public async Task SetPic(string species, string imageUrl) {
            await SetPic("", species, imageUrl);
        }
        [Command("setpic"), Alias("setspeciespic", "setspic")]
        public async Task SetPic(string genus, string species, string imageUrl) {

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyAsync_CheckPrivilegeOrOwnership(Context, (IGuildUser)Context.User, PrivilegeLevel.ServerModerator, sp))
                return;

            if (!await BotUtils.ReplyAsync_ValidateImageUrl(Context, imageUrl))
                return;

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET pics=$url WHERE id=$species_id;")) {

                cmd.Parameters.AddWithValue("$url", imageUrl);
                cmd.Parameters.AddWithValue("$species_id", sp.id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully set the picture for **{0}**.", sp.GetShortName()));

        }

        [Command("+pic")]
        public async Task PlusPic(string species, string imageUrl) {
            await PlusPic("", species, imageUrl, "");
        }
        [Command("+pic")]
        public async Task PlusPic(string genus, string species, string imageUrl) {
            await PlusPic(genus, species, imageUrl, "");
        }
        [Command("+pic")]
        public async Task PlusPic(string genus, string species, string imageUrl, string description) {

            // Get the species.

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            // Validate the image URL.

            if (!await BotUtils.ReplyAsync_ValidateImageUrl(Context, imageUrl))
                return;

            // If the species doesn't have a picture yet, use this as the picture for that species.

            if (string.IsNullOrEmpty(sp.pics)) {

                using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET pics=$url WHERE id=$species_id;")) {

                    cmd.Parameters.AddWithValue("$url", imageUrl);
                    cmd.Parameters.AddWithValue("$species_id", sp.id);

                    await Database.ExecuteNonQuery(cmd);

                }

            }

            // Create a gallery for the species if it doesn't already exist.

            string gallery_name = "species" + sp.id.ToString();

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Gallery(name) VALUES($name);")) {

                cmd.Parameters.AddWithValue("$name", gallery_name);

                await Database.ExecuteNonQuery(cmd);

            }

            // Get the gallery for the species.

            Gallery gallery = await BotUtils.GetGalleryFromDb(gallery_name);

            if (gallery is null) {

                await BotUtils.ReplyAsync_Error(Context, string.Format("Could not create a picture gallery for **{0}**.", sp.GetShortName()));

                return;

            }

            // Add the new picture to the gallery.

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO Picture(url, gallery_id, artist, description) VALUES($url, $gallery_id, $artist, $description);")) {

                cmd.Parameters.AddWithValue("$url", imageUrl);
                cmd.Parameters.AddWithValue("$gallery_id", gallery.id);
                cmd.Parameters.AddWithValue("$artist", Context.User.Username);
                cmd.Parameters.AddWithValue("$description", description);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully added new picture for **{0}**.", sp.GetShortName()));

        }

        [Command("addspecies"), Alias("addsp")]
        public async Task AddSpecies(string genus, string species, string zone = "", string description = "") {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyAsync_CheckPrivilege(Context, (IGuildUser)Context.User, PrivilegeLevel.ServerModerator))
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
            await BotUtils.ReplyAsync_AddZonesToSpecies(Context, sp, zone, showErrorsOnly: true);

            // Add the user to the trophy scanner queue in case their species earned them any new trophies.
            await trophies.TrophyScanner.AddToQueueAsync(Context, Context.User.Id);

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully created new species, **{0}**.", BotUtils.GenerateSpeciesName(genus, species)));

        }

        [Command("appenddescription"), Alias("appenddesc")]
        public async Task AppendDescription(string species, string description) {

            await AppendDescription("", species, description);

        }
        [Command("appenddescription"), Alias("appenddesc")]
        public async Task AppendDescription(string genus, string species, string description) {

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyAsync_CheckPrivilegeOrOwnership(Context, (IGuildUser)Context.User, PrivilegeLevel.ServerModerator, sp))
                return;

            // Ensure the decription is of a reasonable size.

            const int MAX_DESCRIPTION_LENGTH = 10000;

            if (sp.description.Length + description.Length > MAX_DESCRIPTION_LENGTH) {

                await BotUtils.ReplyAsync_Error(Context, string.Format("The description length exceeds the maximum allowed length ({0} characters).", MAX_DESCRIPTION_LENGTH));

                return;

            }

            // Append text to the existing description.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET description=$description WHERE id=$id;")) {

                cmd.Parameters.AddWithValue("$id", sp.id);
                cmd.Parameters.AddWithValue("$description", sp.description + description);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully updated the description for **{0}**.", sp.GetShortName()));

        }

        [Command("addzone"), Alias("addz")]
        public async Task AddZone(string name, string type = "", string description = "") {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyAsync_CheckPrivilege(Context, (IGuildUser)Context.User, PrivilegeLevel.ServerModerator))
                return;

            // Allow the user to specify zones with numbers (e.g., "1") or single letters (e.g., "A").
            // Otherwise, the name is taken as-is.
            name = OurFoodChain.Zone.GetFullName(name).ToLower();

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

            Zone zone = await BotUtils.GetZoneFromDb(name);

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
                OurFoodChain.Zone.GetFullName(name))
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

                    foreach (Zone zone in zone_list)
                        lines.Add(string.Format("{1} **{0}** - {2}", StringUtils.ToTitleCase(zone.name), zone.type == ZoneType.Aquatic ? "🌊" : "🌳", zone.GetShortDescription()));

                    // Build paginated message.

                    PaginatedEmbedBuilder embed = new PaginatedEmbedBuilder(EmbedUtils.LinesToEmbedPages(lines, 20));
                    embed.AddPageNumbers();

                    if (string.IsNullOrEmpty(name))
                        name = "all";
                    else if (name == "aquatic")
                        embed.SetColor(Color.Blue);
                    else if (name == "terrestrial")
                        embed.SetColor(Color.DarkGreen);

                    embed.SetTitle(StringUtils.ToTitleCase(string.Format("{0} zones ({1})", name, zone_list.Count())));
                    embed.PrependDescription(string.Format("For detailed zone information, use `{0}zone <zone>` (e.g. `{0}zone 1`).\n\n", OurFoodChainBot.GetInstance().GetConfig().prefix));

                    await CommandUtils.ReplyAsync_SendPaginatedMessage(Context, embed.Build());

                }
                else {

                    await BotUtils.ReplyAsync_Info(Context, "No zones have been added yet.");

                }

                return;

            }
            else {

                Zone zone = await BotUtils.GetZoneFromDb(name);

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

                        Role[] roles_list = await BotUtils.GetRolesFromDbBySpecies(sp);

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
        public async Task SetExtinct(string genus, string species) {
            await SetExtinct(genus, species, "");
        }
        [Command("+extinct"), Alias("setextinct")]
        public async Task SetExtinct(string genus, string species, string reason) {

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyAsync_CheckPrivilegeOrOwnership(Context, (IGuildUser)Context.User, PrivilegeLevel.ServerModerator, sp))
                return;

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO Extinctions(species_id, reason, timestamp) VALUES($species_id, $reason, $timestamp);")) {

                cmd.Parameters.AddWithValue("$species_id", sp.id);
                cmd.Parameters.AddWithValue("$reason", reason);
                cmd.Parameters.AddWithValue("$timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("The last **{0}** has perished, and the species is now extinct.", sp.GetShortName()));

        }
        [Command("-extinct"), Alias("setextant", "unextinct")]
        public async Task MinusExtinct(string species) {
            await MinusExtinct("", species);
        }
        [Command("-extinct"), Alias("setextant", "unextinct")]
        public async Task MinusExtinct(string genus, string species) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyAsync_CheckPrivilege(Context, (IGuildUser)Context.User, PrivilegeLevel.ServerModerator))
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

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Extinctions WHERE species_id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", sp.id);

                await Database.ExecuteNonQuery(cmd);

            }

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

            StringBuilder description = new StringBuilder();

            foreach (Species sp in sp_list)
                description.AppendLine(sp.GetShortName());

            EmbedBuilder embed = new EmbedBuilder();

            embed.WithTitle(string.Format("Extinct species ({0})", sp_list.Count()));
            embed.WithDescription(description.ToString());

            await ReplyAsync("", false, embed.Build());

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
                if (!await BotUtils.ReplyAsync_CheckPrivilegeOrOwnership(Context, (IGuildUser)Context.User, PrivilegeLevel.ServerModerator, descendant))
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
        public async Task PlusZone(string species, string zone) {
            await PlusZone("", species, zone);
        }
        [Command("+zone"), Alias("+zones")]
        public async Task PlusZone(string genus, string species, string zone) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyAsync_CheckPrivilege(Context, (IGuildUser)Context.User, PrivilegeLevel.ServerModerator))
                return;

            // Get the specified species.

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            // Add new zone information for the species.
            await BotUtils.ReplyAsync_AddZonesToSpecies(Context, sp, zone, showErrorsOnly: false);

        }
        [Command("-zone"), Alias("-zones")]
        public async Task MinusZone(string species, string zone) {
            await MinusZone("", species, zone);
        }
        [Command("-zone"), Alias("-zones")]
        public async Task MinusZone(string genus, string species, string zone) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyAsync_CheckPrivilege(Context, (IGuildUser)Context.User, PrivilegeLevel.ServerModerator))
                return;

            // Get the specified species.

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            // Get the zones that the species currently resides in.
            // These will be used to show warning messages (e.g., doesn't exist in the given zone).

            long[] current_zone_ids = (await BotUtils.GetZonesFromDb(sp.id)).Select(x => x.id).ToArray();

            // Remove the zone information for the species.
            // #todo This can be done in a single query.

            List<string> valid_zones_list = new List<string>(); // List of all zones the species was successfully removed from
            List<string> invalid_zones_list = new List<string>(); // List of all zones given that don't exist
            List<string> not_in_zones_list = new List<string>(); // List of all zones given that the species did not exist in

            foreach (string zoneName in OurFoodChain.Zone.ParseZoneList(zone)) {

                Zone zone_info = await BotUtils.GetZoneFromDb(zoneName);

                // If the given zone does not exist, skip it. We'll show a warning later.

                if (zone_info is null) {

                    invalid_zones_list.Add(string.Format("**{0}**", OurFoodChain.Zone.GetFullName(zoneName)));

                    continue;

                }

                // If the species was never in the given zone, skip it. We'll show a warning later.

                if (!current_zone_ids.Contains(zone_info.id)) {

                    not_in_zones_list.Add(string.Format("**{0}**", OurFoodChain.Zone.GetFullName(zoneName)));

                    continue;

                }

                // The zone exists and the species resides within it, so remove the species from this zone.

                valid_zones_list.Add(string.Format("**{0}**", StringUtils.ToTitleCase(zone_info.name)));

                using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM SpeciesZones WHERE species_id=$species_id AND zone_id=$zone_id;")) {

                    cmd.Parameters.AddWithValue("$species_id", sp.id);
                    cmd.Parameters.AddWithValue("$zone_id", (zone_info.id));

                    await Database.ExecuteNonQuery(cmd);

                }

            }

            if (invalid_zones_list.Count() > 0)
                await BotUtils.ReplyAsync_Warning(Context, string.Format("{0} {1} not exist.", StringUtils.ConjunctiveJoin(", ", invalid_zones_list),
                    invalid_zones_list.Count() == 1 ? "does" : "do"));

            if (not_in_zones_list.Count() > 0)
                await BotUtils.ReplyAsync_Warning(Context, string.Format("**{0}** is already absent from {1}.", sp.GetShortName(), StringUtils.ConjunctiveJoin(", ", not_in_zones_list)));

            if (valid_zones_list.Count() > 0)
                await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** no longer inhabits {1}.", sp.GetShortName(), StringUtils.DisjunctiveJoin(", ", valid_zones_list)));

        }

        [Command("setzone"), Alias("setzones")]
        public async Task SetZone(string genus, string species, string zone = "") {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyAsync_CheckPrivilege(Context, (IGuildUser)Context.User, PrivilegeLevel.ServerModerator))
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
            await BotUtils.ReplyAsync_AddZonesToSpecies(Context, sp, zone, showErrorsOnly: false);

        }

        [Command("setzonepic")]
        public async Task SetZonePic(string zone, string imageUrl) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyAsync_CheckPrivilege(Context, (IGuildUser)Context.User, PrivilegeLevel.ServerModerator))
                return;

            // Make sure that the given zone exists.

            Zone z = await BotUtils.GetZoneFromDb(zone);

            if (!await BotUtils.ReplyAsync_ValidateZone(Context, z))
                return;

            // Make sure the image URL is valid.

            if (!await BotUtils.ReplyAsync_ValidateImageUrl(Context, imageUrl))
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
            if (!await BotUtils.ReplyAsync_CheckPrivilege(Context, (IGuildUser)Context.User, PrivilegeLevel.ServerModerator))
                return;

            // Get the zone from the database.

            Zone zone = await BotUtils.GetZoneFromDb(zoneName);

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

        [Command("setcommonname"), Alias("setcommon")]
        public async Task SetCommonName(string genus, string species, string commonName = "") {

            // If the "commonName" argument was omitted, assume the user omitted the genus.
            // e.g. setcommon <species> <commonName>

            if (string.IsNullOrEmpty(commonName)) {
                commonName = species;
                species = genus;
                genus = string.Empty;
            }

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyAsync_CheckPrivilegeOrOwnership(Context, (IGuildUser)Context.User, PrivilegeLevel.ServerModerator, sp))
                return;

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET common_name = $common_name WHERE id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", sp.id);
                cmd.Parameters.AddWithValue("$common_name", commonName.ToLower());

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** is now commonly known as the **{1}**.", sp.GetShortName(), StringUtils.ToTitleCase(commonName)));

        }

        [Command("setowner"), Alias("setown", "claim")]
        public async Task SetOwner(string species, IUser user) {

            await SetOwner("", species, user);

        }
        [Command("setowner"), Alias("setown", "claim")]
        public async Task SetOwner(string genus, string species, IUser user) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyAsync_CheckPrivilege(Context, (IGuildUser)Context.User, PrivilegeLevel.ServerModerator))
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
            await trophies.TrophyScanner.AddToQueueAsync(Context, user.Id);

            await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** is now owned by **{1}**.", sp.GetShortName(), owner));

        }

        [Command("+prey"), Alias("setprey", "seteats", "setpredates")]
        public async Task SetPredates(string species, string eatsSpecies, string notes = "") {
            await SetPredates("", species, "", eatsSpecies, notes);
        }
        [Command("+prey"), Alias("setprey", "seteats", "setpredates")]
        public async Task SetPredates(string genus, string species, string eatsGenus, string eatsSpecies, string notes = "") {

            Species[] predator_list = await BotUtils.GetSpeciesFromDb(genus, species);
            Species[] eaten_list = await BotUtils.GetSpeciesFromDb(eatsGenus, eatsSpecies);

            if (predator_list.Count() <= 0)
                await BotUtils.ReplyAsync_Error(Context, "The predator species does not exist.");
            else if (eaten_list.Count() <= 0)
                await BotUtils.ReplyAsync_Error(Context, "The victim species does not exist.");
            else if (!await BotUtils.ReplyAsync_ValidateSpecies(Context, predator_list) || !await BotUtils.ReplyAsync_ValidateSpecies(Context, eaten_list))
                return;
            else {

                // Ensure that the user has necessary privileges to use this command.
                if (!await BotUtils.ReplyAsync_CheckPrivilegeOrOwnership(Context, (IGuildUser)Context.User, PrivilegeLevel.ServerModerator, predator_list[0]))
                    return;

                using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO Predates(species_id, eats_id, notes) VALUES($species_id, $eats_id, $notes);")) {

                    cmd.Parameters.AddWithValue("$species_id", predator_list[0].id);
                    cmd.Parameters.AddWithValue("$eats_id", eaten_list[0].id);
                    cmd.Parameters.AddWithValue("$notes", notes);

                    await Database.ExecuteNonQuery(cmd);

                }

                await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** now preys upon **{1}**.", predator_list[0].GetShortName(), eaten_list[0].GetShortName()));

            }

        }
        [Command("-prey")]
        public async Task RemovePrey(string species, string eatsSpecies) {
            await RemovePrey("", species, "", eatsSpecies);
        }
        [Command("-prey")]
        public async Task RemovePrey(string genus, string species, string eatsGenus, string eatsSpecies) {

            // Get the predator and prey species.

            Species predator = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);
            Species prey = await BotUtils.ReplyAsync_FindSpecies(Context, eatsGenus, eatsSpecies);

            if (predator is null || prey is null)
                return;

            // Remove the relationship.

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyAsync_CheckPrivilegeOrOwnership(Context, (IGuildUser)Context.User, PrivilegeLevel.ServerModerator, predator))
                return;

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Predates WHERE species_id=$species_id AND eats_id=$eats_id;")) {

                cmd.Parameters.AddWithValue("$species_id", predator.id);
                cmd.Parameters.AddWithValue("$eats_id", prey.id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** no longer preys upon **{1}**.", predator.GetShortName(), prey.GetShortName()));

        }

        [Command("predates"), Alias("eats", "pred", "predators")]
        public async Task Predates(string genus, string species = "") {

            // If the species parameter was not provided, assume the user only provided the species.
            if (string.IsNullOrEmpty(species)) {
                species = genus;
                genus = string.Empty;
            }

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            EmbedBuilder embed = new EmbedBuilder();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Predates WHERE eats_id=$eats_id AND species_id NOT IN (SELECT species_id FROM Extinctions);")) {

                cmd.Parameters.AddWithValue("$eats_id", sp.id);

                using (DataTable rows = await Database.GetRowsAsync(cmd)) {

                    if (rows.Rows.Count <= 0)
                        await BotUtils.ReplyAsync_Info(Context, string.Format("**{0}** has no extant natural predators.", sp.GetShortName()));
                    else {

                        List<string> lines = new List<string>();

                        foreach (DataRow row in rows.Rows) {

                            Species s = await BotUtils.GetSpeciesFromDb(row.Field<long>("species_id"));
                            string notes = row.Field<string>("notes");

                            string line_text = s.GetShortName();

                            if (!string.IsNullOrEmpty(notes))
                                line_text += string.Format(" ({0})", notes);

                            lines.Add(s.isExtinct ? string.Format("~~{0}~~", line_text) : line_text);

                        }

                        lines.Sort();

                        embed.WithTitle(string.Format("Predators of {0} ({1})", sp.GetShortName(), lines.Count()));
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

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            // Get the preyed-upon species.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Predates WHERE species_id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", sp.id);

                using (DataTable rows = await Database.GetRowsAsync(cmd)) {

                    if (rows.Rows.Count <= 0)
                        await BotUtils.ReplyAsync_Info(Context, string.Format("**{0}** does not prey upon any other species.", sp.GetShortName()));
                    else {

                        List<Tuple<Species, string>> prey_list = new List<Tuple<Species, string>>();

                        foreach (DataRow row in rows.Rows) {

                            prey_list.Add(new Tuple<Species, string>(
                                await BotUtils.GetSpeciesFromDb(row.Field<long>("eats_id")),
                                row.Field<string>("notes")));

                        }

                        prey_list.Sort((lhs, rhs) => lhs.Item1.GetShortName().CompareTo(rhs.Item1.GetShortName()));

                        StringBuilder description = new StringBuilder();

                        foreach (Tuple<Species, string> prey in prey_list) {

                            description.Append(prey.Item1.isExtinct ? BotUtils.Strikeout(prey.Item1.GetShortName()) : prey.Item1.GetShortName());

                            if (!string.IsNullOrEmpty(prey.Item2))
                                description.Append(string.Format(" ({0})", prey.Item2.ToLower()));

                            description.AppendLine();

                        }

                        EmbedBuilder embed = new EmbedBuilder();

                        embed.WithTitle(string.Format("Species preyed upon by {0} ({1})", sp.GetShortName(), prey_list.Count()));
                        embed.WithDescription(description.ToString());

                        await ReplyAsync("", false, embed.Build());

                    }

                }

            }

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
        public async Task Search(params string[] terms) {

            // Create and execute the search query.

            SearchQuery query = new SearchQuery(Context, terms);
            SearchQuery.FindResult result = await query.FindMatchesAsync();

            // Build the embed.

            if (result.Count() <= 0) {

                await BotUtils.ReplyAsync_Info(Context, "No species matching this query could be found.");

            }
            else {

                PaginatedEmbedBuilder embed;

                if (result.groups.ContainsKey(SearchQuery.DEFAULT_GROUP)) {

                    // If there's only one group, just list the species without creating separate fields.
                    embed = new PaginatedEmbedBuilder(EmbedUtils.ListToEmbedPages(result.groups[SearchQuery.DEFAULT_GROUP].ToList(), fieldName: string.Format("Search results ({0})", result.Count())));

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

        private static Random _random_generator = new Random();
        [Command("roll")]
        public async Task Roll(int min = 0, int max = 0) {

            if (min == 0 && max == 0) {
                min = 1;
                max = 6;
            }
            else if (max == 0) {
                max = min;
                min = 0;
            }

            // [min, max)
            await ReplyAsync(_random_generator.Next(min, max + 1).ToString());

        }

        [Command("backup")]
        public async Task Backup() {

            if (!await BotUtils.ReplyAsync_CheckPrivilege(Context, (IGuildUser)Context.User, PrivilegeLevel.BotAdmin))
                return;

            if (System.IO.File.Exists(Database.GetFilePath()))
                try {
                    await Context.Channel.SendFileAsync(Database.GetFilePath(), string.Format(string.Format("Database backup {0}", DateTime.UtcNow.ToString())));
                }
                catch (Exception) {
                    await BotUtils.ReplyAsync_Error(Context, "Database file cannot be accessed.");
                }
            else
                await BotUtils.ReplyAsync_Error(Context, "Database file does not exist at the specified path.");

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

            double hours = 48;
            long start_ts = DateTimeOffset.UtcNow.AddHours(-hours).ToUnixTimeSeconds();

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

            embed.SetTitle(string.Format("Recent events ({0} hours)", hours));
            embed.SetFooter(string.Empty); // remove page numbers added automatically
            embed.AddPageNumbers();

            await CommandUtils.ReplyAsync_SendPaginatedMessage(Context, embed.Build());

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
                description_builder.AppendLine(string.Format("This {0} contains no species.", Taxon.TypeToName(taxon.type)));

            }

            // Add title to all pages.

            foreach (EmbedBuilder page in pages) {

                page.WithTitle(string.IsNullOrEmpty(taxon.common_name) ? taxon.GetName() : string.Format("{0} ({1})", taxon.GetName(), taxon.GetCommonName()));
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
                await GetSpecies(species[_random_generator.Next(species.Count())]);

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

            // Get the user's trophy count.

            long trophy_count = (await trophies.TrophyRegistry.GetTrophiesAsync()).Count;
            long user_trophy_count = 0;

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT user_id, COUNT(trophy_name) AS trophy_count FROM Trophies WHERE user_id=$user_id GROUP BY user_id;")) {

                cmd.Parameters.AddWithValue("$user_id", user.Id);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null))
                    user_trophy_count = row.Field<long>("trophy_count");

            }

            // Get the user's rarest trophy.

            string rarest_trophy = "N/A";

            trophies.UnlockedTrophyInfo[] unlocked = await trophies.TrophyRegistry.GetUnlockedTrophiesAsync(user.Id);

            if (unlocked.Count() > 0) {

                Array.Sort(unlocked, (lhs, rhs) => lhs.timesUnlocked.CompareTo(rhs.timesUnlocked));

                trophies.Trophy trophy = await trophies.TrophyRegistry.GetTrophyByIdentifierAsync(unlocked[0].identifier);

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
                    BotUtils.GetTimeStampAsDateString(timestamp_min, "MMMM dd, yyyy"),
                    timestamp_diff_days == 0 ? user_species_count : (double)user_species_count / timestamp_diff_days,
                    ((double)user_species_count / species_count) * 100.0));
                embed.AddField("Species", string.Format("{0} (Rank **#{1}**)", user_species_count, user_rank), inline: true);
                embed.AddField("Favorite genus", string.Format("{0} ({1} spp.)", StringUtils.ToTitleCase(favorite_genus), genus_count), inline: true);
                embed.AddField("Trophies", string.Format("{0} ({1:0.0}%)", user_trophy_count, ((double)user_trophy_count / trophy_count) * 100.0), inline: true);
                embed.AddField("Rarest trophy", rarest_trophy, inline: true);

            }
            else
                embed.WithDescription(string.Format("{0} has not submitted any species.", user.Username));

            await ReplyAsync("", false, embed.Build());

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
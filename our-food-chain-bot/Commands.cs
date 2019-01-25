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

        [Command("genus"), Alias("g", "genera")]
        public async Task Genus(string name = "") {

            using (SQLiteConnection conn = await Database.GetConnectionAsync()) {

                EmbedBuilder embed = new EmbedBuilder();
                StringBuilder builder = new StringBuilder();

                // If no genus name was provided, list all genera.

                if (string.IsNullOrEmpty(name)) {

                    List<Genus> genera = new List<Genus>();

                    using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Genus ORDER BY name ASC;"))
                    using (DataTable rows = await Database.GetRowsAsync(conn, cmd))
                        foreach (DataRow row in rows.Rows)
                            genera.Add(OurFoodChain.Genus.FromDataRow(row));

                    foreach (Genus genus_info in genera) {

                        long count = 0;

                        using (SQLiteCommand cmd = new SQLiteCommand("SELECT count(*) FROM Species WHERE genus_id=$genus_id;")) {

                            cmd.Parameters.AddWithValue("$genus_id", genus_info.id);

                            count = (await Database.GetRowAsync(cmd)).Field<long>("count(*)");

                        }

                        // Empty genera will not be listed.
                        // Since genera cannot be manually added at the moment, this will only occur when all species within it have been moved or deleted.

                        if (count > 0)
                            builder.AppendLine(string.Format("{0} ({1})",
                                StringUtils.ToTitleCase(genus_info.name),
                                count
                                ));

                    }

                    embed.WithTitle(string.Format("All genera ({0})", genera.Count()));
                    embed.WithDescription(builder.ToString());

                    await ReplyAsync("", false, embed.Build());

                    return;

                }

                embed.WithTitle(StringUtils.ToTitleCase(name));

                // Get information about the genus.

                long genus_id = -1;
                string genus_name = name;

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Genus WHERE name=$name;")) {

                    cmd.Parameters.AddWithValue("$name", name.ToLower());

                    DataRow row = await Database.GetRowAsync(conn, cmd);

                    if (!(row is null)) {

                        Genus genus_info = OurFoodChain.Genus.FromDataRow(row);

                        string description = genus_info.description;
                        genus_id = genus_info.id;
                        genus_name = genus_info.name;

                        embed.WithThumbnailUrl(genus_info.pics);

                        if (string.IsNullOrEmpty(description))
                            description = BotUtils.DEFAULT_GENUS_DESCRIPTION;

                        builder.AppendLine(description);

                    }
                    else {

                        await ReplyAsync("No such genus exists.");

                        return;

                    }

                }

                // Get information about the species.

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE genus_id=$genus_id;")) {

                    cmd.Parameters.AddWithValue("$genus_id", genus_id);

                    using (DataTable rows = await Database.GetRowsAsync(conn, cmd)) {

                        List<Species> sp_list = new List<Species>();

                        foreach (DataRow row in rows.Rows)
                            sp_list.Add(await Species.FromDataRow(row));

                        sp_list.Sort((lhs, rhs) => lhs.GetShortName().CompareTo(rhs.GetShortName()));

                        builder.AppendLine();
                        builder.AppendLine(string.Format("**Species in this genus ({0}):**", sp_list.Count()));

                        foreach (Species sp in sp_list)
                            if (sp.isExtinct)
                                builder.AppendLine(string.Format("~~{0}~~", sp.GetShortName()));
                            else
                                builder.AppendLine(sp.GetShortName());

                        embed.WithDescription(builder.ToString());

                        await ReplyAsync("", false, embed.Build());

                    }

                }

            }

        }
        [Command("addgenus")]
        public async Task AddGenus(string genus, string description = "") {

            // Make sure that the genus doesn't already exist.

            if (!(await BotUtils.GetGenusFromDb(genus) is null)) {

                await BotUtils.ReplyAsync_Warning(Context, string.Format("The genus **{0}** already exists.", StringUtils.ToTitleCase(genus)));

                return;

            }

            Genus genus_info = new Genus();
            genus_info.name = genus;
            genus_info.description = description;

            await BotUtils.AddGenusToDb(genus_info);

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully created new genus, **{0}**.", StringUtils.ToTitleCase(genus)));

        }
        [Command("setgenus")]
        public async Task SetGenus(string genus, string species, string newGenus = "") {

            // If there is no argument for "newGenus", assume the user omitted the original genus.
            // e.g.: setgenus <species> <newGenus>

            if (string.IsNullOrEmpty(newGenus)) {
                newGenus = species;
                species = genus;
                genus = string.Empty;
            }

            // Get the specified species.

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            // Get the specified genus.

            Genus genus_info = await BotUtils.GetGenusFromDb(newGenus);

            if (!await BotUtils.ReplyAsync_ValidateGenus(Context, genus_info))
                return;

            // Update the species.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET genus_id=$genus_id WHERE id=$species_id;")) {

                cmd.Parameters.AddWithValue("$genus_id", genus_info.id);
                cmd.Parameters.AddWithValue("$species_id", sp.id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** has successfully been assigned to the genus **{1}**.", sp.GetShortName(), StringUtils.ToTitleCase(genus_info.name)));

        }
        [Command("setgenuspic"), Alias("setgpic")]
        public async Task SetGenusPic(string genus, string picUrl) {

            Genus genus_info = await BotUtils.GetGenusFromDb(genus);

            if (!await BotUtils.ReplyAsync_ValidateGenus(Context, genus_info))
                return;

            if (!await BotUtils.ReplyAsync_ValidateImageUrl(Context, picUrl))
                return;

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Genus SET pics=$url WHERE id=$genus_id;")) {

                cmd.Parameters.AddWithValue("$url", picUrl);
                cmd.Parameters.AddWithValue("$genus_id", genus_info.id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully added a picture for **{0}**.", StringUtils.ToTitleCase(genus_info.name)));

        }

        [Command("info"), Alias("i", "species", "sp", "s")]
        public async Task Info(string genus, string species = "") {

            // If the user does not provide a genus + species, query by species only.
            if (string.IsNullOrEmpty(species)) {

                species = genus;
                genus = "";

            }

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            EmbedBuilder embed = new EmbedBuilder();
            StringBuilder description_builder = new StringBuilder();

            string embed_title = sp.GetFullName();
            Color embed_color = Color.Blue;

            if (!string.IsNullOrEmpty(sp.commonName))
                embed_title += string.Format(" ({0})", StringUtils.ToTitleCase(sp.commonName));

            embed.AddField("Owner", string.IsNullOrEmpty(sp.owner) ? "?" : sp.owner, inline: true);

            List<string> zone_names = new List<string>();

            foreach (Zone zone in await BotUtils.GetZonesFromDb(sp.id)) {

                if (zone.type == ZoneType.Terrestrial)
                    embed_color = Color.DarkGreen;

                zone_names.Add(zone.GetShortName());

            }

            embed.WithColor(embed_color);

            zone_names.Sort((lhs, rhs) => new ArrayUtils.NaturalStringComparer().Compare(lhs, rhs));

            string zones_value = string.Join(", ", zone_names);

            embed.AddField("Zone(s)", string.IsNullOrEmpty(zones_value) ? "None" : zones_value, inline: true);

            // Check if the species is extinct.
            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Extinctions WHERE species_id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", sp.id);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null)) {

                    embed_title = "[EXTINCT] " + embed_title;
                    embed.WithColor(Color.Red);

                    string reason = row.Field<string>("reason");
                    string ts = BotUtils.GetTimeStampAsDateString((long)row.Field<decimal>("timestamp"));

                    if (!string.IsNullOrEmpty(reason))
                        description_builder.AppendLine(string.Format("**{0}: {1}**\n", ts, reason));

                }

            }

            description_builder.Append(sp.GetDescriptionOrDefault());

            embed.WithTitle(embed_title);
            embed.WithDescription(description_builder.ToString());
            embed.WithThumbnailUrl(sp.pics);

            await ReplyAsync("", false, embed.Build());

        }
        [Command("setspecies")]
        public async Task SetSpecies(string genus, string species, string newName = "") {

            // If no "newName" argument is provided, assume that the user omitted the genus.
            // i.e. "setspecies <old> <new>"

            if (string.IsNullOrEmpty(newName)) {
                newName = genus;
                species = genus;
                genus = string.Empty;
            }

            // Get the specified species.

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            // Update the species.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET name=$name WHERE id=$species_id;")) {

                cmd.Parameters.AddWithValue("$name", newName.ToLower());
                cmd.Parameters.AddWithValue("$species_id", sp.id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** has been successfully renamed to **{1}**.", sp.GetShortName(), BotUtils.GenerateSpeciesName(genus, newName)));

        }

        [Command("setpic"), Alias("setspeciespic", "setspic")]
        public async Task SetPic(string genus, string species, string imageUrl = "") {

            // If no argument was provided for the image URL, assume the user only provided the species and URL.

            if (string.IsNullOrEmpty(imageUrl)) {
                imageUrl = species;
                species = genus;
                genus = string.Empty;
            }

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
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
        [Command("gallery"), Alias("pic", "pics")]
        public async Task Gallery(string genus, string species = "") {

            // If no argument was provided for the "species" parameter, assume the user omitted the genus.

            if (string.IsNullOrEmpty(species)) {
                species = genus;
                genus = string.Empty;
            }

            // Get the species.

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            List<Picture> pictures = new List<Picture>();

            // If the species has a picture assigned to it, add that as the first picture.

            if (!string.IsNullOrEmpty(sp.pics))
                pictures.Add(new Picture(sp.pics));

            // Check the database for additional pictures to add to the gallery.
            // We'll do this by generating a default gallery name for the species, and then checking that gallery.

            string gallery_name = "species" + sp.id.ToString();

            Gallery gallery = await BotUtils.GetGalleryFromDb(gallery_name);

            if (!(gallery is null))
                foreach (Picture p in await BotUtils.GetPicsFromDb(gallery))
                    if (p.url != sp.pics)
                        pictures.Add(p);

            // If there were no images for this query, show a message and quit.

            if (pictures.Count() <= 0) {

                await BotUtils.ReplyAsync_Info(Context, string.Format("**{0}** does not have any pictures.", sp.GetShortName()));

                return;

            }

            // Display a paginated image gallery.

            CommandUtils.PaginatedMessage message = new CommandUtils.PaginatedMessage();
            int index = 1;

            foreach (Picture p in pictures) {

                EmbedBuilder embed = new EmbedBuilder();

                string title = string.Format("Pictures of {0} ({1} of {2})", sp.GetShortName(), index, pictures.Count());
                string footer = string.Format("\"{0}\" by {2} — {1}", p.GetName(), p.GetDescriptionOrDefault(), p.GetArtist());

                embed.WithTitle(title);
                embed.WithImageUrl(p.url);
                embed.WithFooter(footer);

                message.pages.Add(embed.Build());

                ++index;

            }

            await CommandUtils.ReplyAsync_SendPaginatedMessage(Context, message);

        }
        [Command("+pic")]
        public async Task PlusPic(string species, string imageUrl) {
            await PlusPic("", species, imageUrl, "");
        }
        [Command("+pic")]
        public async Task PlusPic(string species, string imageUrl, string description) {
            await PlusPic("", species, imageUrl, description);
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

            // Check if the species already exists before attempting to add it.

            if ((await BotUtils.GetSpeciesFromDb(genus, species)).Count() > 0) {
                await BotUtils.ReplyAsync_Warning(Context, string.Format("The species \"{0}\" already exists.", BotUtils.GenerateSpeciesName(genus, species)));
                return;
            }

            await BotUtils.AddGenusToDb(genus);

            Genus genus_info = await BotUtils.GetGenusFromDb(genus);

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO Species(name, description, genus_id, owner, timestamp) VALUES($name, $description, $genus_id, $owner, $timestamp);")) {

                cmd.Parameters.AddWithValue("$name", species.ToLower());
                cmd.Parameters.AddWithValue("$description", description);
                cmd.Parameters.AddWithValue("$genus_id", genus_info.id);
                cmd.Parameters.AddWithValue("$owner", Context.User.Username);
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

        [Command("setdescription"), Alias("setdesc", "setspeciesdesc", "setsdesc")]
        public async Task SetDescription(string genus, string species = "", string description = "") {

            // If the "species" argument was not provided, assume the user omitted the genus.

            if (string.IsNullOrEmpty(species)) {
                species = genus;
                genus = string.Empty;
            }

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            if (string.IsNullOrEmpty(description)) {

                TwoPartCommandWaitParams p = new TwoPartCommandWaitParams();
                p.type = TwoPartCommandWaitParamsType.Description;
                p.args = new string[] { genus, species };
                p.timestamp = DateTime.Now;

                BotUtils.TWO_PART_COMMAND_WAIT_PARAMS[Context.User.Id] = p;

                await ReplyAsync(string.Format("Enter a description for **{0}**.", sp.GetShortName()));

            }
            else {

                await BotUtils.UpdateSpeciesDescription(genus, species, description);

                await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully updated the description for **{0}**.", sp.GetShortName()));

            }

        }

        [Command("addzone"), Alias("addz")]
        public async Task AddZone(string name, string type = "", string description = "") {

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

            // If no zone was provided, list all zones.

            if (string.IsNullOrEmpty(name) || name == "aquatic" || name == "terrestrial") {

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

                EmbedBuilder embed = new EmbedBuilder();

                embed.WithFooter(string.Format("For detailed information, use \"{0}zone <zone>\", e.g.: \"{0}zone 1\"",
                    OurFoodChainBot.GetInstance().GetConfig().prefix));

                StringBuilder sb = new StringBuilder();

                foreach (Zone zone_info in zone_list) {

                    string description = zone_info.description;

                    if (string.IsNullOrEmpty(description))
                        description = BotUtils.DEFAULT_ZONE_DESCRIPTION;

                    sb.Append(string.Format("{1} **{0}**: ", StringUtils.ToTitleCase(zone_info.name), zone_info.type == ZoneType.Aquatic ? "🌊" : "🌳"));
                    sb.Append(OurFoodChain.Zone.GetShortDescription(description));

                    sb.AppendLine();

                }

                if (string.IsNullOrEmpty(name))
                    name = "all";
                else if (name == "aquatic")
                    embed.WithColor(Color.Blue);
                else if (name == "terrestrial")
                    embed.WithColor(Color.DarkGreen);

                embed.WithTitle(StringUtils.ToTitleCase(string.Format("{0} zones", name)));
                embed.WithDescription(sb.ToString());

                await ReplyAsync("", false, embed.Build());

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

                // Page #1 will contain a simple list of organisms.

                EmbedBuilder embed1 = new EmbedBuilder();

                embed1.WithTitle(title);
                embed1.WithDescription(description);
                embed1.WithColor(color);

                // Get all species living in this zone.

                List<Species> species_list = new List<Species>(await BotUtils.GetSpeciesFromDbByZone(zone));

                species_list.Sort((lhs, rhs) => lhs.GetShortName().CompareTo(rhs.GetShortName()));

                if (species_list.Count() > 0) {

                    StringBuilder lines = new StringBuilder();

                    foreach (Species sp in species_list)
                        lines.AppendLine(sp.GetShortName());

                    embed1.AddField(string.Format("Extant species in this zone ({0}):", species_list.Count()), lines.ToString());

                }

                pages.Add(embed1.Build());

                // Page 2 will contain the organisms organized by role.

                EmbedBuilder embed2 = new EmbedBuilder();

                embed2.WithTitle(title);
                embed2.WithDescription(description);
                embed2.WithColor(color);

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

                    embed2.AddField(string.Format("{0}s ({1})", StringUtils.ToTitleCase(i), roles_map[i].Count()), lines.ToString(), inline: true);

                }

                pages.Add(embed2.Build());

                // 

                IUserMessage message = await ReplyAsync("", false, pages[0]);

                // Only bother with pagination if the zone actually contains species.

                if (species_list.Count() > 0) {

                    CommandUtils.PaginatedMessage paginated = new CommandUtils.PaginatedMessage {
                        pages = pages
                    };

                    paginated.emojiToggle = "🇷";
                    await message.AddReactionAsync(new Emoji("🇷"));

                    CommandUtils.PAGINATED_MESSAGES.Add(message.Id, paginated);

                }

            }

        }

        [Command("+extinct"), Alias("setextinct")]
        public async Task SetExtinct(string species, string reason = "") {
            await SetExtinct("", species, reason);
        }
        [Command("+extinct"), Alias("setextinct")]
        public async Task SetExtinct(string genus, string species, string reason = "") {

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO Extinctions(species_id, reason, timestamp) VALUES($species_id, $reason, $timestamp);")) {

                cmd.Parameters.AddWithValue("$species_id", sp.id);
                cmd.Parameters.AddWithValue("$reason", reason);
                cmd.Parameters.AddWithValue("$timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("The last **{0}** has perished, and the species is now extinct.", sp.GetShortName()));

        }
        [Command("-extinct"), Alias("setextant")]
        public async Task MinusExtinct(string species, string reason = "") {
            await MinusExtinct("", species, reason);
        }
        [Command("-extinct"), Alias("setextant")]
        public async Task MinusExtinct(string genus, string species, string reason = "") {

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

        [Command("map")]
        public async Task Map() {

            string footer = "Click the Z reaction to toggle zone labels.";

            EmbedBuilder page1 = new EmbedBuilder {
                ImageUrl = "https://cdn.discordapp.com/attachments/526503466001104926/533536364910608395/post-earthquake-map-blank.png"
            };
            page1.WithFooter(footer);

            EmbedBuilder page2 = new EmbedBuilder {
                ImageUrl = "https://cdn.discordapp.com/attachments/526503466001104926/533536393121759234/post-earthquake-map.png"
            };
            page2.WithFooter(footer);

            IUserMessage message = await ReplyAsync("", false, page1.Build());

            CommandUtils.PaginatedMessage paginated = new CommandUtils.PaginatedMessage {
                pages = { page1.Build(), page2.Build() }
            };

            paginated.emojiToggle = "🇿";
            await message.AddReactionAsync(new Emoji("🇿"));

            CommandUtils.PAGINATED_MESSAGES.Add(message.Id, paginated);

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

        [Command("ancestry"), Alias("lineage", "ancestors")]
        public async Task Lineage(string species) {
            await Lineage("", species);
        }
        [Command("ancestry"), Alias("lineage", "ancestors")]
        public async Task Lineage(string genus, string species) {

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            List<string> entries = new List<string>();

            entries.Add(string.Format("**{0} - {1}**", sp.GetTimeStampAsDateString(), sp.GetShortName()));

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

            await ReplyAsync(string.Join(Environment.NewLine, entries));

        }
        [Command("ancestry2"), Alias("lineage2")]
        public async Task Lineage2(string species) {
            await Lineage2("", species);
        }
        [Command("ancestry2"), Alias("lineage2")]
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
        public async Task PlusZone(string genus, string species, string zone = "") {

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

            // Add new zone information for the species.
            await BotUtils.ReplyAsync_AddZonesToSpecies(Context, sp, zone, showErrorsOnly: false);

        }
        [Command("-zone"), Alias("-zones")]
        public async Task MinusZone(string genus, string species, string zone = "") {

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

            // Remove the zone information for the species.
            // #todo This can be done in a single query.

            List<string> removed_from_zones_list = new List<string>();

            foreach (string zoneName in OurFoodChain.Zone.ParseZoneList(zone)) {

                Zone zone_info = await BotUtils.GetZoneFromDb(zoneName);

                // If the given zone does not exist, silently skip it.
                if (zone_info is null)
                    continue;

                removed_from_zones_list.Add(StringUtils.ToTitleCase(zone_info.name));

                using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM SpeciesZones WHERE species_id=$species_id AND zone_id=$zone_id;")) {

                    cmd.Parameters.AddWithValue("$species_id", sp.id);
                    cmd.Parameters.AddWithValue("$zone_id", (zone_info.id));

                    await Database.ExecuteNonQuery(cmd);

                }

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** no longer inhabits **{1}**.", sp.GetShortName(), StringUtils.DisjunctiveJoin(", ", removed_from_zones_list)));

        }

        [Command("setzone"), Alias("setzones")]
        public async Task SetZone(string genus, string species, string zone = "") {

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

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET common_name = $common_name WHERE id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", sp.id);
                cmd.Parameters.AddWithValue("$common_name", commonName.ToLower());

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** is now commonly known as the **{1}**.", sp.GetShortName(), StringUtils.ToTitleCase(commonName)));

        }

        [Command("setowner"), Alias("setown", "claim")]
        public async Task SetOwner(string species, IUser user = null) {

            await SetOwner("", species, user);

        }
        [Command("setowner"), Alias("setown", "claim")]
        public async Task SetOwner(string genus, string species, IUser user = null) {

            if (user is null)
                user = Context.User;

            string owner = user.Username;

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET owner = $owner WHERE id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", sp.id);
                cmd.Parameters.AddWithValue("$owner", owner);

                await Database.ExecuteNonQuery(cmd);

            }

            // Add the new owner to the trophy scanner queue in case their species earned them any new trophies.
            await trophies.TrophyScanner.AddToQueueAsync(Context, user.Id);

            await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** is now owned by **{1}**.", sp.GetShortName(), owner));

        }

        #region "help"

        private class CommandInfo {
            public string name = "";
            public string description = "";
            public string category = "uncategorized";
            public string[] aliases;
            public string[] examples;
        }

        [Command("help"), Alias("h")]
        public async Task Help(string command = "") {



            // Load all command info files.

            List<CommandInfo> command_info = new List<CommandInfo>();
            string[] fnames = System.IO.Directory.GetFiles("help", "*.json", System.IO.SearchOption.TopDirectoryOnly);

            foreach (string fname in fnames)
                command_info.Add(JsonConvert.DeserializeObject<CommandInfo>(System.IO.File.ReadAllText(fname)));

            if (!string.IsNullOrEmpty(command)) {

                // Find the requested command.

                command = command.ToLower();

                CommandInfo info = null;

                foreach (CommandInfo c in command_info)
                    if (c.name == command || c.aliases.Contains(command)) {
                        info = c;
                        break;
                    }

                if (info is null)
                    await ReplyAsync("The given command does not exist, or is not yet documented.");
                else {

                    EmbedBuilder builder = new EmbedBuilder();

                    builder.WithTitle(string.Format("Help: {0}", info.name));

                    builder.AddField("Description", info.description);

                    if (info.aliases.Count() > 0)
                        builder.AddField("Aliases", string.Join(", ", info.aliases));

                    if (info.examples.Count() > 0) {

                        for (int i = 0; i < info.examples.Count(); ++i)
                            info.examples[i] = OurFoodChainBot.GetInstance().GetConfig().prefix + info.examples[i];

                        builder.AddField("Example(s)", string.Join(Environment.NewLine, info.examples));

                    }

                    await ReplyAsync("", false, builder.Build());

                }

            }
            else {

                // Sort commands alphabetically.
                command_info.Sort((lhs, rhs) => lhs.name.CompareTo(rhs.name));

                SortedDictionary<string, List<CommandInfo>> commands_lists = new SortedDictionary<string, List<CommandInfo>>();

                foreach (CommandInfo c in command_info) {

                    if (!commands_lists.ContainsKey(c.category))
                        commands_lists[c.category] = new List<CommandInfo>();

                    commands_lists[c.category].Add(c);

                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("Commands list");
                builder.WithFooter(string.Format("Want to know more about a command? Use \"{0}help <command>\", e.g.: \"{0}help setpic\"",
                    OurFoodChainBot.GetInstance().GetConfig().prefix));

                foreach (string cat in commands_lists.Keys) {

                    List<string> command_str_list = new List<string>();

                    foreach (CommandInfo c in commands_lists[cat])
                        command_str_list.Add(string.Format("`{0}`", c.name));

                    builder.AddField(StringUtils.ToTitleCase(cat), string.Join("  ", command_str_list));

                }

                await ReplyAsync("", false, builder.Build());

            }

        }

        #endregion

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
        public async Task RemovePrey(string genus, string species, string eatsGenus, string eatsSpecies) {

            // Get the predator and prey species.

            Species predator = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);
            Species prey = await BotUtils.ReplyAsync_FindSpecies(Context, eatsGenus, eatsSpecies);

            if (predator is null || prey is null)
                return;

            // Remove the relationship.

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Predates WHERE species_id=$species_id AND eats_id=$eats_id;")) {

                cmd.Parameters.AddWithValue("$species_id", predator.id);
                cmd.Parameters.AddWithValue("$eats_id", prey.id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** no longer preys upon **{1}**.", predator.GetShortName(), prey.GetShortName()));

        }

        [Command("predates"), Alias("eats")]
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
                                description.Append(string.Format(" ({0})", prey.Item2));

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

        [Command("setgenusdescription"), Alias("setgenusdesc", "setgdesc")]
        public async Task SetGenusDescription(string genus, string description) {

            Genus genus_info = await BotUtils.GetGenusFromDb(genus);

            if (!await BotUtils.ReplyAsync_ValidateGenus(Context, genus_info))
                return;

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Genus SET description=$description WHERE id=$genus_id;")) {

                cmd.Parameters.AddWithValue("$description", description);
                cmd.Parameters.AddWithValue("$genus_id", genus_info.id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully updated description for genus **{0}**.", StringUtils.ToTitleCase(genus_info.name)));

        }


        [Command("family"), Alias("f", "families")]
        public async Task Family(string name = "") {
            await BotUtils.Command_ShowTaxon(Context, TaxonType.Family, name);
        }
        [Command("addfamily")]
        public async Task AddFamily(string name, string description = "") {
            await BotUtils.Command_AddTaxon(Context, TaxonType.Family, name, description);
        }
        [Command("setfamily")]
        public async Task SetFamily(string child, string parent) {
            await BotUtils.Command_SetTaxon(Context, TaxonType.Family, child, parent);
        }
        [Command("setfamilydesc"), Alias("setfamilydescription", "setfdesc")]
        public async Task SetFamilyDesc(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Family, name, description);
        }
        [Command("setfamilycommonname"), Alias("setfamilycommon", "setfcommon")]
        public async Task SetFamilyCommon(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, TaxonType.Family, name, commonName);
        }

        [Command("order"), Alias("o", "orders")]
        public async Task Order(string name = "") {
            await BotUtils.Command_ShowTaxon(Context, TaxonType.Order, name);
        }
        [Command("addorder")]
        public async Task AddOrder(string name, string description = "") {
            await BotUtils.Command_AddTaxon(Context, TaxonType.Order, name, description);
        }
        [Command("setorder")]
        public async Task SetOrder(string child, string parent) {
            await BotUtils.Command_SetTaxon(Context, TaxonType.Order, child, parent);
        }
        [Command("setorderdesc"), Alias("setorderdescription", "setodesc")]
        public async Task SetOrderDescription(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Order, name, description);
        }
        [Command("setordercommonname"), Alias("setordercommon", "setocommon")]
        public async Task SetOrderCommon(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, TaxonType.Order, name, commonName);
        }

        [Command("class"), Alias("c", "classes")]
        public async Task Class(string name = "") {
            await BotUtils.Command_ShowTaxon(Context, TaxonType.Class, name);
        }
        [Command("addclass")]
        public async Task AddClass(string name, string description = "") {
            await BotUtils.Command_AddTaxon(Context, TaxonType.Class, name, description);
        }
        [Command("setclass")]
        public async Task SetClass(string child, string parent) {
            await BotUtils.Command_SetTaxon(Context, TaxonType.Class, child, parent);
        }
        [Command("setclassdesc"), Alias("setclassdescription", "setcdesc")]
        public async Task SetClassDescription(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Class, name, description);
        }
        [Command("setclasscommonname"), Alias("setclasscommon", "setccommon")]
        public async Task SetClassCommon(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, TaxonType.Class, name, commonName);
        }

        [Command("phylum"), Alias("p", "phyla")]
        public async Task Phylum(string name = "") {
            await BotUtils.Command_ShowTaxon(Context, TaxonType.Phylum, name);
        }
        [Command("addphylum")]
        public async Task AddPhylum(string name, string description = "") {
            await BotUtils.Command_AddTaxon(Context, TaxonType.Phylum, name, description);
        }
        [Command("setphylum")]
        public async Task SetPhylum(string child, string parent) {
            await BotUtils.Command_SetTaxon(Context, TaxonType.Phylum, child, parent);
        }
        [Command("setphylumdesc"), Alias("setphylumdescription", "setpdesc")]
        public async Task SetPhylumDescription(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Phylum, name, description);
        }
        [Command("setphylumcommonname"), Alias("setphylumcommon", "setpcommon")]
        public async Task SetPhylumCommon(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, TaxonType.Phylum, name, commonName);
        }

        [Command("kingdom"), Alias("k", "kingdoms")]
        public async Task Kingdom(string name = "") {
            await BotUtils.Command_ShowTaxon(Context, TaxonType.Kingdom, name);
        }
        [Command("addkingdom")]
        public async Task AddKingdom(string name, string description = "") {
            await BotUtils.Command_AddTaxon(Context, TaxonType.Kingdom, name, description);
        }
        [Command("setkingdom")]
        public async Task SetKingdom(string child, string parent) {
            await BotUtils.Command_SetTaxon(Context, TaxonType.Kingdom, child, parent);
        }
        [Command("setkingdomdesc"), Alias("setkingdomdescription", "setkdesc")]
        public async Task SetKingdomDescription(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Kingdom, name, description);
        }
        [Command("setkingdomcommonname"), Alias("setkingdomcommon", "setkcommon")]
        public async Task SetKingdomCommon(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, TaxonType.Kingdom, name, commonName);
        }

        [Command("domain"), Alias("d", "domains")]
        public async Task Domain(string name = "") {
            await BotUtils.Command_ShowTaxon(Context, TaxonType.Domain, name);
        }
        [Command("adddomain")]
        public async Task AddDomain(string name, string description = "") {
            await BotUtils.Command_AddTaxon(Context, TaxonType.Domain, name, description);
        }
        [Command("setdomain")]
        public async Task SetDomain(string child, string parent) {
            await BotUtils.Command_SetTaxon(Context, TaxonType.Domain, child, parent);
        }
        [Command("setdomaindesc"), Alias("setdomaindescription", "setddesc")]
        public async Task SetDomainDescription(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Domain, name, description);
        }
        [Command("setdomaincommonname"), Alias("setdomaincommon", "setdcommon")]
        public async Task SetDomainCommon(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, TaxonType.Domain, name, commonName);
        }

        [Command("addedby"), Alias("ownedby")]
        public async Task AddedBy(IUser user = null) {

            if (user is null)
                user = Context.User;

            string username = user.Username;

            // List all species owned by the given user.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE owner=$owner;")) {

                cmd.Parameters.AddWithValue("$owner", username);

                using (DataTable rows = await Database.GetRowsAsync(cmd)) {

                    EmbedBuilder embed = new EmbedBuilder();
                    List<Species> species_list = new List<Species>();

                    foreach (DataRow row in rows.Rows)
                        species_list.Add(await Species.FromDataRow(row));

                    species_list.Sort((lhs, rhs) => lhs.GetShortName().CompareTo(rhs.GetShortName()));

                    StringBuilder description = new StringBuilder();

                    foreach (Species sp in species_list)
                        description.AppendLine(sp.isExtinct ? BotUtils.Strikeout(sp.GetShortName()) : sp.GetShortName());

                    embed.WithTitle(string.Format("Species owned by {0}", username));
                    embed.WithDescription(description.ToString());
                    embed.WithThumbnailUrl(user.GetAvatarUrl(size: 32));

                    await ReplyAsync("", false, embed.Build());

                }

            }

        }

        [Command("search")]
        public async Task Search(params string[] terms) {

            if (terms.Count() <= 0) {

                await BotUtils.ReplyAsync_Error(Context, "Too few search terms have been provided.");

                return;

            }

            List<Species> list = new List<Species>();

            List<string> term_query_builder = new List<string>();

            for (int i = 0; i < terms.Count(); ++i)
                term_query_builder.Add(string.Format("(name LIKE {0} OR description LIKE {0} OR common_name LIKE {0})", string.Format("$term{0}", i)));

            string query_str = string.Format("SELECT * FROM Species WHERE {0};", string.Join(" AND ", term_query_builder));

            using (SQLiteCommand cmd = new SQLiteCommand(query_str)) {

                // Add all terms to the query.

                for (int i = 0; i < terms.Count(); ++i) {

                    string term = "%" + terms[i].Trim() + "%";

                    cmd.Parameters.AddWithValue(string.Format("$term{0}", i), term);

                }

                using (DataTable rows = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in rows.Rows)
                        list.Add(await Species.FromDataRow(row));

            }

            SortedSet<string> names_list = new SortedSet<string>();

            foreach (Species sp in list)
                if (sp.isExtinct)
                    names_list.Add(BotUtils.Strikeout(sp.GetShortName()));
                else
                    names_list.Add(sp.GetShortName());

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle("Search results");
            embed.WithDescription(string.Join(Environment.NewLine, names_list));

            await ReplyAsync("", false, embed.Build());

        }

        [Command("addrole")]
        public async Task AddRole(string name, string description = "") {

            Role role = new Role {
                name = name,
                description = description
            };

            await BotUtils.AddRoleToDb(role);

            await BotUtils.ReplyAsync_Success(Context, string.Format("Succesfully created the new role **{0}**.", name));

        }

        [Command("+role"), Alias("setrole")]
        public async Task SetRole(string species, string role) {
            await SetRole("", species, role, "");
        }
        [Command("+role"), Alias("setrole")]
        public async Task SetRole(string genus, string species, string role, string notes = "") {

            // Get the species.

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            // Get the role.

            Role role_info = await BotUtils.GetRoleFromDb(role);

            if (!await BotUtils.ReplyAsync_ValidateRole(Context, role_info))
                return;

            // Update the species.

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO SpeciesRoles(species_id, role_id, notes) VALUES($species_id, $role_id, $notes);")) {

                cmd.Parameters.AddWithValue("$species_id", sp.id);
                cmd.Parameters.AddWithValue("$role_id", role_info.id);
                cmd.Parameters.AddWithValue("$notes", notes);

                await Database.ExecuteNonQuery(cmd);

                await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** has successfully been assigned the role of **{1}**.", sp.GetShortName(), StringUtils.ToTitleCase(role_info.name)));

            }

        }

        [Command("-role"), Alias("unsetrole")]
        public async Task RemoveRole(string species, string role) {
            await RemoveRole("", species, role);
        }
        [Command("-role"), Alias("unsetrole")]
        public async Task RemoveRole(string genus, string species, string role) {

            // Get the species.

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            // Get the role.

            Role role_info = await BotUtils.GetRoleFromDb(role);

            if (!await BotUtils.ReplyAsync_ValidateRole(Context, role_info))
                return;

            // Update the species.

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM SpeciesRoles WHERE species_id=$species_id AND role_id=$role_id;")) {

                cmd.Parameters.AddWithValue("$species_id", sp.id);
                cmd.Parameters.AddWithValue("$role_id", role_info.id);

                await Database.ExecuteNonQuery(cmd);

                await BotUtils.ReplyAsync_Success(Context, string.Format("Role **{0}** has successfully been unassigned from **{1}**.", StringUtils.ToTitleCase(role_info.name), sp.GetShortName()));

            }

        }

        [Command("roles"), Alias("role")]
        public async Task Roles(string nameOrGenus = "", string species = "") {

            // If both arguments were left empty, just list all roles.

            if (string.IsNullOrEmpty(nameOrGenus) && string.IsNullOrEmpty(species)) {

                EmbedBuilder embed = new EmbedBuilder();

                Role[] roles_list = await BotUtils.GetRolesFromDb();

                embed.WithTitle(string.Format("All roles ({0})", roles_list.Count()));

                foreach (Role role in roles_list) {

                    long count = 0;

                    using (SQLiteCommand cmd = new SQLiteCommand("SELECT count(*) FROM SpeciesRoles WHERE role_id=$role_id;")) {

                        cmd.Parameters.AddWithValue("$role_id", role.id);

                        count = (await Database.GetRowAsync(cmd)).Field<long>("count(*)");

                    }

                    string title = string.Format("{0} ({1})",
                        StringUtils.ToTitleCase(role.name),
                        count);

                    embed.AddField(title, role.GetDescriptionOrDefault());

                }

                await ReplyAsync("", false, embed.Build());

                return;

            }

            // If only the first argument was provided, show the role with that name.

            if (!string.IsNullOrEmpty(nameOrGenus) && string.IsNullOrEmpty(species)) {

                Role role = await BotUtils.GetRoleFromDb(nameOrGenus);

                if (!await BotUtils.ReplyAsync_ValidateRole(Context, role))
                    return;

                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle(string.Format("Role: {0}", StringUtils.ToTitleCase(role.name)));
                embed.WithDescription(role.GetDescriptionOrDefault());

                // List species with this role.

                List<Species> species_list = new List<Species>(await BotUtils.GetSpeciesFromDbByRole(role));

                species_list.Sort((lhs, rhs) => lhs.GetShortName().CompareTo(rhs.GetShortName()));

                if (species_list.Count() > 0) {

                    StringBuilder lines = new StringBuilder();

                    foreach (Species sp in species_list)
                        lines.AppendLine(sp.GetShortName());

                    embed.WithDescription(string.Format("{2}\n\n**Species with this role ({1}):**\n{0}", lines.ToString(), species_list.Count(), role.GetDescriptionOrDefault()));

                }

                await ReplyAsync("", false, embed.Build());

            }

            // If two arguments were provided, take them as a genus and species.
            // We will display the roles assigned to that species.

            if (!string.IsNullOrEmpty(nameOrGenus) && !string.IsNullOrEmpty(species)) {

                // Get the species.

                Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, nameOrGenus, species);

                if (sp is null)
                    return;

                // Get the role(s) assigned to this species.

                Role[] roles = await BotUtils.GetRolesFromDbBySpecies(sp);

                if (roles.Count() <= 0) {
                    await BotUtils.ReplyAsync_Info(Context, string.Format("**{0}** has not been assigned any roles.", sp.GetShortName()));
                    return;
                }

                // Display the role(s) to the user.

                StringBuilder lines = new StringBuilder();

                foreach (Role i in roles) {

                    lines.Append(StringUtils.ToTitleCase(i.name));

                    if (!string.IsNullOrEmpty(i.notes))
                        lines.Append(string.Format(" ({0})", i.notes));

                    lines.AppendLine();

                }

                EmbedBuilder embed = new EmbedBuilder();

                embed.WithTitle(string.Format("{0}'s role(s) ({1})", sp.GetShortName(), roles.Count()));
                embed.WithDescription(lines.ToString());

                await ReplyAsync("", false, embed.Build());

            }

        }

        [Command("setroledescription"), Alias("setroledesc")]
        public async Task SetRoleDescription(string name, string description) {

            Role role = await BotUtils.GetRoleFromDb(name);

            if (!await BotUtils.ReplyAsync_ValidateRole(Context, role))
                return;

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Roles SET description=$description WHERE name=$name;")) {

                cmd.Parameters.AddWithValue("$name", role.name);
                cmd.Parameters.AddWithValue("$description", description);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully updated description for role **{0}**.", StringUtils.ToTitleCase(role.name)));

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

        [Command("taxonomy")]
        public async Task Taxonomy(string species) {
            await Taxonomy("", species);
        }
        [Command("taxonomy")]
        public async Task Taxonomy(string genus, string species) {

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle(string.Format("Taxonomy of {0}", sp.GetShortName()));

            Taxon genus_info = await BotUtils.GetTaxonFromDb(sp.genusId, TaxonType.Genus);
            Taxon family_info = await BotUtils.GetTaxonFromDb(genus_info.parent_id, TaxonType.Family);
            Taxon order_info = await BotUtils.GetTaxonFromDb(family_info.parent_id, TaxonType.Order);
            Taxon class_info = await BotUtils.GetTaxonFromDb(order_info.parent_id, TaxonType.Class);
            Taxon phylum_info = await BotUtils.GetTaxonFromDb(class_info.parent_id, TaxonType.Phylum);
            Taxon kingdom_info = await BotUtils.GetTaxonFromDb(phylum_info.parent_id, TaxonType.Kingdom);
            Taxon domain_info = await BotUtils.GetTaxonFromDb(kingdom_info.parent_id, TaxonType.Domain);

            string unknown = "Unknown";
            string genus_name = genus_info is null ? unknown : genus_info.GetName();
            string family_name = family_info is null ? unknown : family_info.GetName();
            string order_name = order_info is null ? unknown : order_info.GetName();
            string class_name = class_info is null ? unknown : class_info.GetName();
            string phylum_name = phylum_info is null ? unknown : phylum_info.GetName();
            string kingdom_name = kingdom_info is null ? unknown : kingdom_info.GetName();
            string domain_name = domain_info is null ? unknown : domain_info.GetName();

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

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle(string.Format("Recent events ({0} hours)", hours));

            StringBuilder description = new StringBuilder();

            if (new_species.Count() > 0) {

                foreach (Species sp in new_species)
                    description.AppendLine(sp.GetFullName());

                embed.AddField(string.Format("New species ({0})", new_species.Count()), description.ToString(), inline: true);

                description.Clear();

            }

            if (extinct_species.Count() > 0) {

                foreach (Species sp in extinct_species)
                    description.AppendLine(sp.GetFullName());

                embed.AddField(string.Format("Extinctions ({0})", extinct_species.Count()), description.ToString(), inline: true);

                description.Clear();

            }

            await ReplyAsync("", false, embed.Build());

        }

    }

}
using Discord;
using Discord.Commands;
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

                        string description = row.Field<string>("description");
                        genus_id = row.Field<long>("id");
                        genus_name = row.Field<string>("name");

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

                        builder.AppendLine();
                        builder.AppendLine("**Species in this genus:**");

                        foreach (DataRow row in rows.Rows)
                            builder.AppendLine(BotUtils.GenerateSpeciesName(genus_name, row.Field<string>("name")));

                        embed.WithDescription(builder.ToString());

                        await ReplyAsync("", false, embed.Build());

                    }

                }

            }

        }

        [Command("info"), Alias("i", "species", "sp")]
        public async Task Info(string genus, string species) {

            Species[] sp_list = await BotUtils.GetSpeciesFromDb(genus, species);

            if (sp_list.Count() <= 0)
                await ReplyAsync("No such species exists.");
            else if (sp_list.Count() > 1) {

                StringBuilder builder = new StringBuilder();
                builder.AppendLine("**Species is ambiguous:**");

                foreach (Species sp in sp_list)
                    builder.AppendLine(BotUtils.GenerateSpeciesName(sp));

                await ReplyAsync(builder.ToString());

            }
            else {

                EmbedBuilder embed = new EmbedBuilder();

                StringBuilder builder = new StringBuilder();
                Species sp = sp_list[0];

                if (!string.IsNullOrEmpty(sp.commonName))
                    builder.AppendLine(string.Format("**AKA:** {0}", StringUtils.ToTitleCase(sp.commonName)));

                builder.AppendLine(string.Format("**Owner:** {0}", sp.owner));
                builder.Append("**Zone(s):** ");

                List<string> zone_names = new List<string>();
                Color embed_color = Color.Blue;

                foreach (Zone zone in await BotUtils.GetZonesFromDb(sp.id)) {

                    if (zone.type == ZoneType.Terrestrial)
                        embed_color = Color.DarkGreen;

                    zone_names.Add(zone.name);

                }

                embed.WithColor(embed_color);

                builder.AppendLine(string.Join(", ", zone_names));

                string description = sp.description;

                if (string.IsNullOrEmpty(description))
                    description = BotUtils.DEFAULT_SPECIES_DESCRIPTION;

                string title = string.Format("{0} {1}", StringUtils.ToTitleCase(sp.genus), sp.name);

                builder.AppendLine("**Description:**");
                builder.AppendLine(description);

                // Check if the species is extinct.
                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Extinctions WHERE species_id=$species_id;")) {

                    cmd.Parameters.AddWithValue("$species_id", sp.id);

                    DataRow row = await Database.GetRowAsync(cmd);

                    if (!(row is null)) {

                        title += " (EXTINCT)";
                        embed.WithColor(Color.Red);

                        builder.AppendLine(string.Format("**{0}**", row.Field<string>("reason")));

                    }

                }

                embed.WithTitle(title);
                embed.WithDescription(builder.ToString());
                embed.WithImageUrl(sp.pics);

                await ReplyAsync("", false, embed.Build());


            }

        }

        [Command("setpic")]
        public async Task SetPic(string genus, string species, string imageUrl) {

            Species[] sp_list = await BotUtils.GetSpeciesFromDb(genus, species);

            if (sp_list.Count() <= 0)
                await ReplyAsync("No such species exists.");
            else if (!Regex.Match(imageUrl, "^https?:").Success)
                await ReplyAsync("Please provide a valid image URL.");
            else {

                Species sp = sp_list[0];

                using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET pics=$url WHERE id=$species_id;")) {

                    cmd.Parameters.AddWithValue("$url", imageUrl);
                    cmd.Parameters.AddWithValue("$species_id", sp.id);

                    await Database.ExecuteNonQuery(cmd);

                }

                await ReplyAsync("Image added successfully.");

            }

        }

        [Command("addspecies"), Alias("addsp")]
        public async Task AddSpecies(string genus, string species, string zone, string description = "") {

            string[] zones = zone.Split(',', '/');
            species = species.ToLower();

            await BotUtils.AddGenusToDb(genus);

            Genus genus_info = await BotUtils.GetGenusFromDb(genus);

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO Species(name, description, phylum_id, genus_id, owner, timestamp) VALUES($name, $description, $phylum_id, $genus_id, $owner, $timestamp);")) {

                cmd.Parameters.AddWithValue("$name", species);
                cmd.Parameters.AddWithValue("$description", description);
                cmd.Parameters.AddWithValue("$phylum_id", 0);
                cmd.Parameters.AddWithValue("$genus_id", genus_info.id);
                cmd.Parameters.AddWithValue("$owner", Context.User.Username);
                cmd.Parameters.AddWithValue("$timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                await Database.ExecuteNonQuery(cmd);

            }

            long species_id = await BotUtils.GetSpeciesIdFromDb(genus_info.id, species);

            // Add to all given zones.

            foreach (string zoneName in zones) {

                Zone zone_info = await BotUtils.GetZoneFromDb(zoneName);

                if (zone_info is null || zone_info.id == -1) {

                    await ReplyAsync("The given Zone does not exist.");

                    return;

                }

                using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO SpeciesZones(species_id, zone_id) VALUES($species_id, $zone_id);")) {

                    cmd.Parameters.AddWithValue("$species_id", species_id);
                    cmd.Parameters.AddWithValue("$zone_id", zone_info.id);

                    await Database.ExecuteNonQuery(cmd);

                }

            }

            await ReplyAsync("Species added successfully.");

        }

        [Command("setdescription"), Alias("setdesc")]
        public async Task SetDescription(string genus, string species, string description = "") {

            Species[] sp_list = await BotUtils.GetSpeciesFromDb(genus, species);

            if (sp_list.Count() <= 0)
                await ReplyAsync("No such species exists.");
            else {

                if (string.IsNullOrEmpty(description)) {

                    TwoPartCommandWaitParams p = new TwoPartCommandWaitParams();
                    p.type = TwoPartCommandWaitParamsType.Description;
                    p.args = new string[] { genus, species };
                    p.timestamp = DateTime.Now;

                    BotUtils.TWO_PART_COMMAND_WAIT_PARAMS[Context.User.Id] = p;

                    await ReplyAsync(string.Format("Enter a description for {0}.", BotUtils.GenerateSpeciesName(genus, species)));

                }
                else {

                    await BotUtils.UpdateSpeciesDescription(genus, species, description);

                    await ReplyAsync("Description added successfully.");

                }

            }

        }

        [Command("addzone"), Alias("addz")]
        public async Task AddZone(string name, string type = "", string description = "") {

            // Allow the user to specify zones with numbers (e.g., "1") or single letters (e.g., "A").
            // Otherwise, the name is taken as-is.
            name = OurFoodChain.Zone.GetFullName(name);

            name = name.ToLower();

            // If an invalid type was provided, use it as the description instead.
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

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO Zones(name, type, description) VALUES($name, $type, $description);")) {

                cmd.Parameters.AddWithValue("$name", name.ToLower());
                cmd.Parameters.AddWithValue("$type", type.ToLower());
                cmd.Parameters.AddWithValue("$description", description);

                await Database.ExecuteNonQuery(cmd);

            }

            await ReplyAsync("Zone added successfully.");

        }

        [Command("zone"), Alias("z", "zones")]
        public async Task Zone(string name = "") {

            EmbedBuilder embed = new EmbedBuilder();

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

                foreach (Zone zone_info in zone_list) {

                    string description = zone_info.description;

                    if (string.IsNullOrEmpty(description))
                        description = BotUtils.DEFAULT_ZONE_DESCRIPTION;

                    embed.AddField(string.Format("**{0}** ({1})",
                        StringUtils.ToTitleCase(zone_info.name), zone_info.type.ToString()),
                        OurFoodChain.Zone.GetShortDescription(description)
                        );

                }

                if (string.IsNullOrEmpty(name))
                    name = "all";
                else if (name == "aquatic")
                    embed.WithColor(Color.Blue);
                else if (name == "terrestrial")
                    embed.WithColor(Color.DarkGreen);

                embed.WithTitle(StringUtils.ToTitleCase(string.Format("{0} zones", name)));

                await ReplyAsync("", false, embed.Build());

                return;

            }

            Zone zone = await BotUtils.GetZoneFromDb(name);

            if (zone is null)
                await ReplyAsync("No such zone exists.");
            else {

                string description = zone.description;

                if (string.IsNullOrEmpty(description))
                    description = BotUtils.DEFAULT_ZONE_DESCRIPTION;

                embed.WithTitle(string.Format("{0} ({1})", StringUtils.ToTitleCase(zone.name), zone.type.ToString()));
                embed.WithDescription(description);

                switch (zone.type) {
                    case ZoneType.Aquatic:
                        embed.WithColor(Color.Blue);
                        break;
                    case ZoneType.Terrestrial:
                        embed.WithColor(Color.DarkGreen);
                        break;
                }

                // Get all species living in this zone.

                List<string> species_name_list = new List<string>();

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * from Species WHERE id IN (SELECT species_id FROM SpeciesZones WHERE zone_id=$zone_id) AND id NOT IN (SELECT species_id FROM Extinctions);")) {

                    cmd.Parameters.AddWithValue("$zone_id", zone.id);

                    DataTable rows = await Database.GetRowsAsync(cmd);

                    foreach (DataRow row in rows.Rows)
                        species_name_list.Add((await Species.FromDataRow(row)).GetShortName());

                }

                species_name_list.Sort();

                if (species_name_list.Count() > 0)
                    embed.AddField(string.Format("Extant species in this zone ({0}):", species_name_list.Count()), string.Join(Environment.NewLine, species_name_list));


                await ReplyAsync("", false, embed.Build());

            }

        }

        [Command("setextinct")]
        public async Task SetExtinct(string genus, string species, string reason = "") {

            Species[] sp_list = await BotUtils.GetSpeciesFromDb(genus, species);

            if (sp_list.Count() <= 0)
                await ReplyAsync("No such species exists.");
            else {

                using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO Extinctions(species_id, reason, timestamp) VALUES($species_id, $reason, $timestamp);")) {

                    cmd.Parameters.AddWithValue("$species_id", sp_list[0].id);
                    cmd.Parameters.AddWithValue("$reason", reason);
                    cmd.Parameters.AddWithValue("$timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                    await Database.ExecuteNonQuery(cmd);

                    await ReplyAsync("The species is now extinct.");

                }

            }

        }

        [Command("map")]
        public async Task Map() {

            EmbedBuilder page1 = new EmbedBuilder {
                ImageUrl = "https://cdn.discordapp.com/attachments/526503466001104926/526549551444787211/image0.jpg"
            };

            EmbedBuilder page2 = new EmbedBuilder {
                ImageUrl = "https://cdn.discordapp.com/attachments/526503466001104926/526549561238487040/image0.jpg"
            };

            IUserMessage message = await ReplyAsync("", false, page1.Build());
            await message.AddReactionAsync(new Emoji("🇿"));

            CommandUtils.PaginatedMessage paginated = new CommandUtils.PaginatedMessage {
                pages = new Embed[] { page1.Build(), page2.Build() }
            };

            CommandUtils.PAGINATED_MESSAGES.Add(message.Id, paginated);

        }

        [Command("setancestor")]
        public async Task SetAncestor(string genus, string species, string ancestorGenus, string ancestorSpecies = "") {

            // If the ancestor species was left blank, assume the same genus as current species.
            if (string.IsNullOrEmpty(ancestorSpecies)) {

                ancestorSpecies = ancestorGenus;
                ancestorGenus = genus;

            }

            Species[] descendant_list = await BotUtils.GetSpeciesFromDb(genus, species);
            Species[] ancestor_list = await BotUtils.GetSpeciesFromDb(ancestorGenus, ancestorSpecies);

            if (descendant_list.Count() == 0)
                await ReplyAsync("The child species does not exist.");
            else if (ancestor_list.Count() == 0)
                await ReplyAsync("The parent species does not exist.");
            else if (descendant_list[0].id == ancestor_list[0].id)
                await ReplyAsync("A species cannot be its own ancestor.");
            else {

                using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO Ancestors(species_id, ancestor_id) VALUES($species_id, $ancestor_id);")) {

                    cmd.Parameters.AddWithValue("$species_id", descendant_list[0].id);
                    cmd.Parameters.AddWithValue("$ancestor_id", ancestor_list[0].id);

                    await Database.ExecuteNonQuery(cmd);

                }

                await ReplyAsync("Ancestor updated successfully.");

            }

        }

        [Command("lineage"), Alias("ancestry", "ancestors")]
        public async Task Lineage(string genus, string species) {

            Species[] species_list = await BotUtils.GetSpeciesFromDb(genus, species);

            if (species_list.Count() <= 0)
                await ReplyAsync("No such species exists.");
            else {

                List<string> entries = new List<string>();

                entries.Add(string.Format("**{0} - {1}**", species_list[0].GetTimeStampAsDateString(), species_list[0].GetShortName()));

                long species_id = species_list[0].id;

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

        }

        [Command("setzone"), Alias("setzones")]
        public async Task SetZone(string genus, string species, string zone) {

            string[] zones = zone.Split(',', '/');

            Species[] sp_list = await BotUtils.GetSpeciesFromDb(genus, species);

            if (sp_list.Count() <= 0)
                await ReplyAsync("No such species exists.");
            else {

                // Remove existing zone information for this species.
                using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM SpeciesZones WHERE species_id=$species_id;")) {

                    cmd.Parameters.AddWithValue("$species_id", sp_list[0].id);

                    await Database.ExecuteNonQuery(cmd);

                }

                // Add new zone information for this species.
                foreach (string zoneName in zones) {

                    string name = zoneName.Trim();

                    using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO SpeciesZones(species_id, zone_id) VALUES($species_id, $zone_id);")) {

                        cmd.Parameters.AddWithValue("$species_id", sp_list[0].id);
                        cmd.Parameters.AddWithValue("$zone_id", (await BotUtils.GetZoneFromDb(name)).id);

                        await Database.ExecuteNonQuery(cmd);

                    }

                }

                await ReplyAsync("Zone(s) added successfully.");

            }

        }

        [Command("setcommonname"), Alias("setcommon")]
        public async Task SetCommonName(string genus, string species, string commonName) {

            Species[] sp_list = await BotUtils.GetSpeciesFromDb(genus, species);

            if (sp_list.Count() <= 0)
                await ReplyAsync("No such species exists.");
            else {

                using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET common_name = $common_name WHERE id=$species_id;")) {

                    cmd.Parameters.AddWithValue("$species_id", sp_list[0].id);
                    cmd.Parameters.AddWithValue("$common_name", commonName);

                    await Database.ExecuteNonQuery(cmd);

                }

                await ReplyAsync("Common name added successfully.");

            }

        }

        [Command("setowner"), Alias("setown", "claim")]
        public async Task SetOwner(string genus, string species, string owner = "") {

            if (string.IsNullOrEmpty(owner))
                owner = Context.User.Username;

            Species[] sp_list = await BotUtils.GetSpeciesFromDb(genus, species);

            if (sp_list.Count() <= 0)
                await ReplyAsync("No such species exists.");
            else {

                using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET owner = $owner WHERE id=$species_id;")) {

                    cmd.Parameters.AddWithValue("$species_id", sp_list[0].id);
                    cmd.Parameters.AddWithValue("$owner", owner);

                    await Database.ExecuteNonQuery(cmd);

                }

                await ReplyAsync("Owner added successfully.");

            }

        }

        [Command("help"), Alias("h")]
        public async Task Help(string command = "") {

            EmbedBuilder builder = new EmbedBuilder();

            if (string.IsNullOrEmpty(command)) {

                builder.WithTitle("Commands list");
                builder.WithFooter("For more information, use \"help <command>\".");

                builder.AddField("Info", "`genus` `info` `zone` `map` `lineage` `help` `predates` `prey`");
                builder.AddField("Updates", "`addsp` `addzone` `setpic` `setdesc` `setextinct` `setowner` `setancestor` `setcommonname` `setprey` `setgenusdesc`");

            }
            else {

                builder.WithTitle(string.Format("Help: {0}", command));
                string description = "No description available";
                string aliases = "-";
                string example = "-";

                switch (command) {

                    case "genus":
                    case "g":
                    case "genera":
                        description = "Lists all species under the given genus. If no genus is provided, lists all genera.";
                        aliases = "genus, g, genera";
                        example = "?genus helix";
                        break;

                    case "info":
                    case "i":
                        description = "Shows information about the given species.";
                        aliases = "info, i";
                        example = "?info H. quattuorus";
                        break;

                    case "zone":
                    case "z":
                    case "zones":
                        description = "Shows information about the given zone. If no zone is provided, lists all zones.";
                        aliases = "zone, zones, z";
                        example = "?zone 1\n?zones aquatic\n?zones terrestrial";
                        break;

                    case "map":
                        description = "Displays the map.";
                        aliases = "map";
                        example = "?map";
                        break;

                    case "lineage":
                    case "ancestry":
                    case "ancestors":
                        description = "Lists ancestors of the given species.";
                        aliases = "lineage, ancestry, ancestors";
                        example = "?lineage H. quattuorus";
                        break;

                    case "help":
                    case "h":
                        description = "Displays help information.";
                        aliases = "help, h";
                        example = "?help";
                        break;

                    case "addsp":
                    case "addspecies":
                        description = "Adds a new species to the database.";
                        aliases = "addsp, addspecies";
                        example = "?addsp helix quattuorus \"zone 12\" \"my description\"\n?addsp helix quattuorus 12";
                        break;

                    case "addzone":
                    case "addz":
                        description = "Adds a new zone to the database. Numeric zones are automatically categorized as aquatic, and alphabetic zones are categorized as terrestrial.";
                        aliases = "addz, addzone";
                        example = "?addzone 25\n?addzone 25 aquatic\n?addzone 25 terrestrial \"my description\"";
                        break;

                    case "setpic":
                        description = "Sets the picture for the given species.";
                        aliases = "setpic";
                        example = "?setpic H. quattuorus https://website.com/image.jpg";
                        break;

                    case "setdesc":
                    case "setdescription":
                        description = "Sets the description for the given species. Leave description blank to provide it in a separate message.";
                        aliases = "setdesc, setdescription";
                        example = "?setdesc H. quattuorus \"my description\"\n?setdesc H. quattuorus";
                        break;

                    case "setextinct":
                        description = "Marks the given species as extinct.";
                        aliases = "setextinct";
                        example = "?setextinct H. quattuorus \"died of starvation\"\n?setextinct H. quattuorus";
                        break;

                    case "setown":
                    case "claim":
                    case "setowner":
                        description = "Sets the owner of the given species.";
                        aliases = "setowner, setown, claim";
                        example = "?claim H. quattuorus\n?setowner H. quattuorus \"my name\"";
                        break;

                    case "setancestor":
                        description = "Sets the ancestor of the given species (i.e., the species it evolved from).";
                        aliases = "setancestor";
                        example = "?setancestor H. quattuorus H. ancientous";
                        break;

                    case "setcommon":
                    case "setcommonname":
                        description = "Sets the common name for the given species.";
                        aliases = "setcommonname, setcommon";
                        example = "?setcommonname H. quattuorus \"swirly star\"";
                        break;

                    case "setpredates":
                    case "seteats":
                    case "setprey":
                        description = "Sets a species eaten by another species. Successive calls are additive, and do not replace existing relationships.";
                        aliases = "setprey, seteats, setpredates";
                        example = "?setprey P. filterarious H. quattuorus\n?setprey P. filterarious H. quattuorus \"babies only\"";
                        break;

                    case "prey":
                        description = "Lists the species prayed upon by the given species.";
                        aliases = "prey";
                        example = "?prey P. filterarious";
                        break;

                    case "eats":
                    case "predates":
                        description = "Lists the species that pray upon the given species.";
                        aliases = "predates, eats";
                        example = "?predates H. quattuorus";
                        break;

                    case "setgenusdescription":
                    case "setgenusdesc":
                    case "setgdesc":
                        description = "Sets the description for the given genus.";
                        aliases = "setgenusdescription, setgenusdesc, setgdesc";
                        example = "?setgdesc helix \"they have swirly shells\"";
                        break;

                    default:
                        await ReplyAsync("No such command exists.");
                        return;

                }

                builder.AddField("Description", description);
                builder.AddField("Aliases", aliases);
                builder.AddField("Example(s)", example);

            }

            await ReplyAsync("", false, builder.Build());

        }

        [Command("setprey"), Alias("seteats", "setpredates")]
        public async Task SetPredates(string genus, string species, string eatsGenus, string eatsSpecies, string notes = "") {

            Species[] predator_list = await BotUtils.GetSpeciesFromDb(genus, species);
            Species[] eaten_list = await BotUtils.GetSpeciesFromDb(eatsGenus, eatsSpecies);

            if (predator_list.Count() == 0)
                await ReplyAsync("The predator species does not exist.");
            else if (eaten_list.Count() == 0)
                await ReplyAsync("The victim species does not exist.");
            else {

                using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO Predates(species_id, eats_id, notes) VALUES($species_id, $eats_id, $notes);")) {

                    cmd.Parameters.AddWithValue("$species_id", predator_list[0].id);
                    cmd.Parameters.AddWithValue("$eats_id", eaten_list[0].id);
                    cmd.Parameters.AddWithValue("$notes", notes);

                    await Database.ExecuteNonQuery(cmd);

                }

                await ReplyAsync("Predation updated successfully.");

            }

        }

        [Command("predates"), Alias("eats")]
        public async Task Predates(string genus, string species) {

            Species[] sp_list = await BotUtils.GetSpeciesFromDb(genus, species);

            if (sp_list.Count() == 0)
                await ReplyAsync("No such species exists.");
            else {

                EmbedBuilder embed = new EmbedBuilder();

                embed.WithTitle(string.Format("Predators of {0}", sp_list[0].GetShortName()));

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Predates WHERE eats_id=$eats_id;")) {

                    cmd.Parameters.AddWithValue("$eats_id", sp_list[0].id);

                    DataTable rows = await Database.GetRowsAsync(cmd);

                    if (rows.Rows.Count <= 0)
                        await ReplyAsync("This species has no natural predators.");
                    else {

                        StringBuilder builder = new StringBuilder();

                        foreach (DataRow row in rows.Rows) {

                            Species sp = await BotUtils.GetSpeciesFromDb(row.Field<long>("species_id"));
                            string notes = row.Field<string>("notes");

                            builder.Append(sp.GetShortName());

                            if (!string.IsNullOrEmpty(notes))
                                builder.Append(string.Format(" ({0})", notes));

                            builder.AppendLine();

                        }

                        embed.WithDescription(builder.ToString());

                        await ReplyAsync("", false, embed.Build());

                    }

                }

            }

        }

        [Command("prey")]
        public async Task Prey(string genus, string species) {

            Species[] sp_list = await BotUtils.GetSpeciesFromDb(genus, species);

            if (sp_list.Count() == 0)
                await ReplyAsync("No such species exists.");
            else {

                EmbedBuilder embed = new EmbedBuilder();

                embed.WithTitle(string.Format("Species preyed upon by {0}", sp_list[0].GetShortName()));

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Predates WHERE species_id=$species_id;")) {

                    cmd.Parameters.AddWithValue("$species_id", sp_list[0].id);

                    DataTable rows = await Database.GetRowsAsync(cmd);

                    if (rows.Rows.Count <= 0)
                        await ReplyAsync("This species does not prey upon any other species.");
                    else {

                        StringBuilder builder = new StringBuilder();

                        foreach (DataRow row in rows.Rows) {

                            Species sp = await BotUtils.GetSpeciesFromDb(row.Field<long>("eats_id"));
                            string notes = row.Field<string>("notes");

                            builder.Append(sp.GetShortName());

                            if (!string.IsNullOrEmpty(notes))
                                builder.Append(string.Format(" ({0})", notes));

                            builder.AppendLine();

                        }

                        embed.WithDescription(builder.ToString());

                        await ReplyAsync("", false, embed.Build());

                    }

                }

            }

        }

        [Command("setgenusdescription"), Alias("setgenusdesc", "setgdesc")]
        public async Task SetGenusDescription(string genus, string description) {

            Genus genus_info = await BotUtils.GetGenusFromDb(genus);

            if (genus_info is null || genus_info.id == -1) {

                await ReplyAsync("No such genus exists");

                return;

            }
            else {

                using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Genus SET description=$description WHERE id=$genus_id;")) {

                    cmd.Parameters.AddWithValue("$description", description);
                    cmd.Parameters.AddWithValue("$genus_id", genus_info.id);

                    await Database.ExecuteNonQuery(cmd);

                }

                await ReplyAsync("Description added successfully.");
            }

        }

        [Command("setphylum")]
        public async Task SetPhylum(string genus, string species, string phylum) {

            // Get the specified species.

            Species[] sp_list = await BotUtils.GetSpeciesFromDb(genus, species);

            if (sp_list.Count() <= 0) {

                await ReplyAsync("No such species exists.");

                return;

            }

            // Create the phylum if it doesn't already exist.

            phylum = phylum.ToLower();

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Phylum(name) VALUES($phylum);")) {

                cmd.Parameters.AddWithValue("$phylum", phylum);

                await Database.ExecuteNonQuery(cmd);

            }

            // Get the ID of the phylum.

            long phylum_id = -1;

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT id FROM Phylum WHERE name=$name;")) {

                cmd.Parameters.AddWithValue("$name", phylum);

                phylum_id = (await Database.GetRowAsync(cmd)).Field<long>("id");

            }

            // Update the species.

            Species sp = sp_list[0];

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET phylum_id=$phylum_id WHERE id=$species_id;")) {

                cmd.Parameters.AddWithValue("$phylum_id", phylum_id);
                cmd.Parameters.AddWithValue("$species_id", sp.id);

                await Database.ExecuteNonQuery(cmd);

            }

            await ReplyAsync("Phylum set successfully.");

        }

    }

}
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class ZoneCommands :
        ModuleBase {

        [Command("addzone"), Alias("addz"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddZone(string name, string type = "", string description = "") {

            if (string.IsNullOrEmpty(name)) {

                await BotUtils.ReplyAsync_Error(Context, "Zone name cannot be empty.");

            }
            else {

                // Allow the user to specify zones with numbers (e.g., "1") or single letters (e.g., "A").
                // Otherwise, the name is taken as-is.
                name = ZoneUtils.FormatZoneName(name).ToLower();

                // If an invalid type was provided, assume the user meant it as a description instead.
                // i.e., "addzone <name> <description>"

                ZoneType zone_type = await ZoneUtils.GetZoneTypeAsync(type);

                if (zone_type is null || zone_type.Id == ZoneType.NullZoneTypeId) {

                    description = type;

                    // Attempt to determine the zone type automatically if one wasn't provided.
                    // Currently, this is only possible if users are using the default zone types (i.e. "aquatic" and "terrestrial").

                    zone_type = await ZoneUtils.GetDefaultZoneTypeAsync(name);

                }

                if (await ZoneUtils.GetZoneAsync(name) != null) {

                    // Don't attempt to create the zone if it already exists.

                    await BotUtils.ReplyAsync_Warning(Context, string.Format("A zone named \"{0}\" already exists.", ZoneUtils.FormatZoneName(name)));

                }
                else {

                    await ZoneUtils.AddZoneAsync(new Zone {
                        Name = name,
                        Description = description,
                        ZoneTypeId = zone_type.Id
                    });

                    await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully created new {0} zone, **{1}**.",
                        zone_type.Name.ToLower(),
                        ZoneUtils.FormatZoneName(name)));

                }

            }

        }

        [Command("zone"), Alias("z", "zones")]
        public async Task Zone(string arg0 = "") {

            ZoneType zone_type = await ZoneUtils.GetZoneTypeAsync(arg0);

            if (string.IsNullOrEmpty(arg0) || (zone_type != null && zone_type.Id != ZoneType.NullZoneTypeId)) {

                // Display all zones, or, if the user passed in a valid zone type, all zones of that type.

                Zone[] zones = await ZoneUtils.GetZonesAsync(zone_type);

                if (zones.Count() > 0) {

                    // Create a line describing each zone.

                    List<string> lines = new List<string>();

                    // We need to make sure that even if the "short" description is actually long, we can show n zones per page.

                    string embed_title = StringUtils.ToTitleCase(string.Format("{0} zones ({1})", arg0, zones.Count()));
                    string embed_description = string.Format("For detailed zone information, use `{0}zone <zone>` (e.g. `{0}zone 1`).\n\n", OurFoodChainBot.Instance.Config.Prefix);
                    int zones_per_page = 20;
                    int max_line_length = (EmbedUtils.MAX_EMBED_LENGTH - embed_title.Length - embed_description.Length) / zones_per_page;

                    foreach (Zone zone in zones) {

                        ZoneType type = await ZoneUtils.GetZoneTypeAsync(zone.ZoneTypeId);

                        string line = string.Format("{1} **{0}**\t-\t{2}", StringUtils.ToTitleCase(zone.Name), (type is null ? new ZoneType() : type).Icon, zone.GetShortDescription());

                        if (line.Length > max_line_length)
                            line = line.Substring(0, max_line_length - 3) + "...";

                        lines.Add(line);

                    }

                    // Build paginated message.

                    PaginatedEmbedBuilder embed = new PaginatedEmbedBuilder(EmbedUtils.LinesToEmbedPages(lines, 20));
                    embed.AddPageNumbers();

                    if (zone_type is null || zone_type.Id == ZoneType.NullZoneTypeId)
                        arg0 = "all";
                    else
                        embed.SetColor(DiscordUtils.ConvertColor(zone_type.Color));

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

                Zone zone = await ZoneUtils.GetZoneAsync(arg0);

                if (await BotUtils.ReplyAsync_ValidateZone(Context, zone)) {

                    List<Embed> pages = new List<Embed>();

                    ZoneType type = await ZoneUtils.GetZoneTypeAsync(zone.ZoneTypeId) ?? new ZoneType();
                    string title = string.Format("{0} {1}", type.Icon, zone.FullName);
                    string description = zone.GetDescriptionOrDefault();
                    Color color = DiscordUtils.ConvertColor(type.Color);

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
                    paginated.SetThumbnailUrl(zone.Pics);
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

        }

        [Command("setzonepic"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetZonePic(string zone, string imageUrl) {

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
                cmd.Parameters.AddWithValue("$id", z.Id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully updated the picture for **{0}**.", z.GetFullName()));

        }

        [Command("setzonedesc"), Alias("setzdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetZoneDescription(string zoneName, string description) {

            // Get the zone from the database.

            Zone zone = await ZoneUtils.GetZoneAsync(zoneName);

            if (!await BotUtils.ReplyAsync_ValidateZone(Context, zone))
                return;

            // Update the description for the zone.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Zones SET description=$description WHERE id=$id;")) {

                cmd.Parameters.AddWithValue("$description", description);
                cmd.Parameters.AddWithValue("$id", zone.Id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully updated the description for **{0}**.", zone.GetFullName()));

        }

        [Command("addzonetype"), RequirePrivilege(PrivilegeLevel.ServerModerator), DifficultyLevel(DifficultyLevel.Advanced)]
        public async Task AddZoneType(params string[] args) {

            if (args.Count() > 0) {

                string name = args[0];
                string icon = ZoneType.DefaultIcon;
                string color = ZoneType.DefaultColorHex;
                string description = "";

                if (await ZoneUtils.GetZoneTypeAsync(name) != null) {

                    // If a zone type with this name already exists, do not create a new one.
                    await BotUtils.ReplyAsync_Error(Context, string.Format("The zone type \"{0}\" already exists.", name));

                }
                else {

                    // Read the rest of the arguments.

                    for (int i = 1; i < args.Count(); ++i) {

                        if (DiscordUtils.StringIsEmoji(args[i]))
                            icon = args[i];
                        else if (args[i].StartsWith("#"))
                            color = args[i];
                        else
                            description = args[i];

                    }

                    ZoneType type = new ZoneType {
                        Name = name,
                        Icon = icon,
                        Description = description
                    };

                    if (!type.SetColor(color))
                        await BotUtils.ReplyAsync_Error(Context, string.Format("Unable to parse given color code ({0}).", color));
                    else {

                        // Add the zone type to the database.

                        await ZoneUtils.AddZoneTypeAsync(type);

                        await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully created new zone type **{0}**.", type.Name));

                    }

                }

            }
            else
                await BotUtils.ReplyAsync_Error(Context, "You must specify a name for the zone type.");

        }

    }

}
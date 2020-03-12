using Discord.Commands;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Common.Zones;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Discord.Utilities;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot.Modules {

    public class ZoneModule :
        OfcModuleBase {

        [Command("addzone"), Alias("addz"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddZone(string zoneName, string arg1 = "", string arg2 = "") {

            // Cases:
            // 1. <zoneName>
            // 2. <zoneName> <zoneTypeName>
            // 3. <zoneName> <zoneTypeName> <description>
            // 4. <zoneName> <description>

            string zoneTypeName = arg1;
            string description = arg2;

            if (string.IsNullOrEmpty(zoneName)) {

                // The user must specify a non-empty zone name.

                await ReplyErrorAsync("Zone name cannot be empty.");

            }
            else {

                // Allow the user to specify zones with numbers (e.g. "1") or single letters (e.g. "A").
                // Otherwise, the name is taken as-is.

                zoneName = ZoneUtilities.GetFullName(zoneName).ToLowerInvariant();

                IZoneType zoneType = await Db.GetZoneTypeAsync(arg1);

                if (!zoneType.IsValid()) {

                    // If an invalid type was provided, assume the user meant it as a description instead (4).

                    description = zoneTypeName;

                    // Attempt to determine the zone type automatically based on the zome name.
                    // This is currently only possible for default zones ("aquatic" or "terrestrial").

                    zoneType = await Db.GetDefaultZoneTypeAsync(zoneName);

                }

                if (await Db.GetZoneAsync(zoneName) != null) {

                    // Don't attempt to create the zone if it already exists.

                    await ReplyWarningAsync($"A zone named {ZoneUtilities.GetFullName(zoneName).ToBold()} already exists.");

                }
                else {

                    // Add the new zone.

                    await Db.AddZoneAsync(new Zone {
                        Name = zoneName,
                        Description = description,
                        TypeId = zoneType.Id
                    });

                    await ReplySuccessAsync($"Successfully created new {zoneType.Name.ToLowerInvariant()} zone, {ZoneUtilities.GetFullName(zoneName).ToBold()}.");

                }

            }

        }

        [Command("zone"), Alias("z", "zones")]
        public async Task Zone(string arg0 = "") {

            IZoneType zone_type = await Db.GetZoneTypeAsync(arg0);

            if (string.IsNullOrEmpty(arg0) || zone_type.IsValid()) {

                // Display all zones, or, if the user passed in a valid zone type, all zones of that type.

                IEnumerable<IZone> zones = await Db.GetZonesAsync(zone_type);

                if (zones.Count() > 0) {

                    // We need to make sure that even if the "short" description is actually long, we can show n zones per page.

                    string embedTitle = StringUtilities.ToTitleCase(string.Format("{0} zones ({1})", string.IsNullOrEmpty(arg0) ? "All" : arg0, zones.Count()));
                    string embedDescription = string.Format("For detailed zone information, use `{0}zone <zone>` (e.g. `{0}zone {1}`).\n\n",
                        Config.Prefix,
                        zones.First().GetShortName().Contains(" ") ? string.Format("\"{0}\"", zones.First().GetShortName().ToLowerInvariant()) : zones.First().GetShortName().ToLowerInvariant());

                    // Build paginated message.

                    IEnumerable<Discord.Messaging.IEmbed> pages = await BotUtils.ZonesToEmbedPagesAsync(embedTitle.Length + embedDescription.Length, zones, Db);

                    if (zone_type.IsValid())
                        foreach (Discord.Messaging.IEmbed page in pages)
                            page.Color = zone_type.Color;

                    await ReplyAsync(new Discord.Messaging.PaginatedMessage(pages));

                }
                else {

                    await BotUtils.ReplyAsync_Info(Context, "No zones have been added yet.");

                }

                return;

            }
            else {

                IZone zone = await Db.GetZoneAsync(arg0);

                if (await BotUtils.ReplyValidateZoneAsync(Context, zone)) {

                    List<Discord.Messaging.IEmbed> pages = new List<Discord.Messaging.IEmbed>();

                    IZoneType type = await Db.GetZoneTypeAsync(zone.TypeId) ?? new ZoneType();
                    string title = string.Format("{0} {1}", type.Icon, zone.GetFullName());
                    string description = zone.GetDescriptionOrDefault();
                    System.Drawing.Color color = type.Color;

                    // Get all species living in this zone.

                    List<ISpecies> species_list = new List<ISpecies>(await Db.GetSpeciesAsync(zone));

                    species_list.Sort((lhs, rhs) => lhs.GetShortName().CompareTo(rhs.GetShortName()));

                    // Starting building a paginated message.
                    // The message will have a paginated species list, and a toggle button to display the species sorted by role.

                    List<Discord.Messaging.IEmbed> embed_pages =
                        new List<Discord.Messaging.IEmbed>(EmbedUtilities.CreateEmbedPages(string.Format("Extant species in this zone ({0}):", species_list.Count()), species_list, options: EmbedPaginationOptions.AddPageNumbers));

                    if (embed_pages.Count() <= 0)
                        embed_pages.Add(new Discord.Messaging.Embed());

                    // Add title, decription, etc., to all pages.

                    foreach (Discord.Messaging.IEmbed page in pages) {

                        page.Title = title;
                        page.Description = description;
                        page.ThumbnailUrl = zone.Pictures.FirstOrDefault()?.Url;
                        page.Color = color;

                    }

                    // This page will have species organized by role.
                    // Only bother with the role page if species actually exist in this zone.

                    if (species_list.Count() > 0) {

                        Discord.Messaging.IEmbed role_page = new Discord.Messaging.Embed();

                        role_page.Title = title;
                        role_page.Description = description;
                        //role_page.WithThumbnailUrl(zone.pics);
                        role_page.Color = color;

                        Dictionary<string, List<ISpecies>> roles_map = new Dictionary<string, List<ISpecies>>();

                        foreach (ISpecies sp in species_list) {

                            IEnumerable<Common.Roles.IRole> roles_list = await Db.GetRolesAsync(sp);

                            if (roles_list.Count() <= 0) {

                                if (!roles_map.ContainsKey("no role"))
                                    roles_map["no role"] = new List<ISpecies>();

                                roles_map["no role"].Add(sp);

                                continue;

                            }

                            foreach (Common.Roles.IRole role in roles_list) {

                                if (!roles_map.ContainsKey(role.GetName()))
                                    roles_map[role.GetName()] = new List<ISpecies>();

                                roles_map[role.GetName()].Add(sp);

                            }

                        }

                        // Sort the list of species belonging to each role.

                        foreach (List<ISpecies> i in roles_map.Values)
                            i.Sort((lhs, rhs) => lhs.GetShortName().CompareTo(rhs.GetShortName()));

                        // Create a sorted list of keys so that the roles are in order.

                        List<string> sorted_keys = new List<string>(roles_map.Keys);
                        sorted_keys.Sort();

                        foreach (string i in sorted_keys) {

                            StringBuilder lines = new StringBuilder();

                            foreach (Species j in roles_map[i])
                                lines.AppendLine(j.GetShortName());

                            role_page.AddField(string.Format("{0}s ({1})", StringUtilities.ToTitleCase(i), roles_map[i].Count()), lines.ToString(), inline: true);

                        }

                        // Add the page to the builder.

                        Discord.Messaging.IPaginatedMessage message = new Discord.Messaging.PaginatedMessage(pages);

                        message.AddReaction("🇷", async (args) => {

                            if (args.Emoji != "🇷")
                                return;

                            args.Message.PaginationEnabled = !args.ReactionAdded;

                            if (args.ReactionAdded)
                                args.Message.CurrentPage = new Discord.Messaging.Message() { Embed = role_page };
                            else
                                args.Message.CurrentPage = null;

                        });

                        await ReplyAsync(message);

                    }

                }

            }

        }

        [Command("setzonepic"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetZonePic(string zone, string imageUrl) {

            // Make sure that the given zone exists.

            IZone z = await Db.GetZoneAsync(zone);

            if (!await BotUtils.ReplyValidateZoneAsync(Context, z))
                return;

            // Make sure the image URL is valid.

            if (!await BotUtils.ReplyIsImageUrlValidAsync(Context, imageUrl))
                return;

            // Update the zone.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Zones SET pics=$pics WHERE id=$id;")) {

                cmd.Parameters.AddWithValue("$pics", imageUrl);
                cmd.Parameters.AddWithValue("$id", z.Id);

                await Db.ExecuteNonQueryAsync(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully updated the picture for **{0}**.", z.GetFullName()));

        }

        [Command("setzonedesc"), Alias("setzdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetZoneDescription(string zoneName, string description) {

            // Get the zone from the database.

            IZone zone = await Db.GetZoneAsync(zoneName);

            if (!await BotUtils.ReplyValidateZoneAsync(Context, zone))
                return;

            // Update the description for the zone.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Zones SET description=$description WHERE id=$id;")) {

                cmd.Parameters.AddWithValue("$description", description);
                cmd.Parameters.AddWithValue("$id", zone.Id);

                await Db.ExecuteNonQueryAsync(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully updated the description for **{0}**.", zone.GetFullName()));

        }

        [Command("zonetype"), DifficultyLevel(DifficultyLevel.Advanced)]
        public async Task GetZoneType(string arg0) {

            // If the given argument is a zone type, display information for that type.
            // If the given argument is a zone name, display information for the type corresponding to that zone.

            IZoneType type = await Db.GetZoneTypeAsync(arg0);

            if (!type.IsValid()) {

                // If no zone type exists with this name, attempt to get the type of the zone with this name.

                IZone zone = await Db.GetZoneAsync(arg0);

                if (zone != null)
                    type = await Db.GetZoneTypeAsync(zone.TypeId);

            }

            if (type.IsValid()) {

                // We got a valid zone type, so show information about the zone type.

                IEnumerable<IZone> zones = await Db.GetZonesAsync(type);

                string embedTitle = string.Format("{0} {1} Zones ({2})", type.Icon, type.Name, zones.Count());
                string embedDescription = type.Description + "\n\n";
                System.Drawing.Color embedColor = type.Color;

                IEnumerable<Discord.Messaging.IEmbed> pages = await BotUtils.ZonesToEmbedPagesAsync(embedTitle.Length + embedDescription.Length, zones, Db, showIcon: false);

                EmbedUtilities.AddPageNumbers(pages);

                await ReplyAsync(new Discord.Messaging.PaginatedMessage(pages));

            }
            else
                await BotUtils.ReplyAsync_Error(Context, "No such zone type exists.");

        }

        [Command("addzonetype"), RequirePrivilege(PrivilegeLevel.ServerModerator), DifficultyLevel(DifficultyLevel.Advanced)]
        public async Task AddZoneType(params string[] args) {

            if (args.Count() <= 0) {
                await BotUtils.ReplyAsync_Error(Context, "You must specify a name for the zone type.");
            }
            else if (args.Count() > 4) {
                await BotUtils.ReplyAsync_Error(Context, "Too many arguments have been provided.");
            }
            else {

                string name = args[0];
                string icon = ZoneTypeBase.DefaultIcon;
                System.Drawing.Color color = ZoneTypeBase.DefaultColor;
                string description = "";

                if (await Db.GetZoneTypeAsync(name) != null) {

                    // If a zone type with this name already exists, do not create a new one.
                    await BotUtils.ReplyAsync_Warning(Context, string.Format("A zone type named \"{0}\" already exists.", name));

                }
                else {

                    // Read the rest of the arguments.

                    for (int i = 1; i < args.Count(); ++i) {

                        if (Bot.DiscordUtils.IsEmoji(args[i]))
                            icon = args[i];
                        else if (StringUtilities.TryParseColor(args[i], out System.Drawing.Color result))
                            color = result;
                        else if (string.IsNullOrEmpty(description))
                            description = args[i];
                        else
                            await BotUtils.ReplyAsync_Warning(Context, string.Format("Invalid argument provided: {0}", args[i]));

                    }

                    ZoneType type = new ZoneType {
                        Name = name,
                        Icon = icon,
                        Description = description,
                        Color = color
                    };

                    // Add the zone type to the database.

                    await Db.AddZoneTypeAsync(type);

                    await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully created new zone type **{0}**.", type.Name));

                }

            }


        }

        [Command("setzonetype"), DifficultyLevel(DifficultyLevel.Advanced)]
        public async Task SetZoneType(string zoneName, string zoneType) {

            IZone zone = await Db.GetZoneAsync(zoneName);
            IZoneType type = await Db.GetZoneTypeAsync(zoneType);

            if (await BotUtils.ReplyValidateZoneAsync(Context, zone) && await BotUtils.ReplyValidateZoneTypeAsync(Context, type)) {

                zone.TypeId = type.Id;

                await Db.UpdateZoneAsync(zone);

                await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully set the type of {0}**{1}** to **{2}**.",
                    zone.GetFullName().StartsWith("Zone") ? string.Empty : "zone ",
                    zone.GetFullName(),
                    type.Name));

            }

        }

        [Command("setparentzone"), DifficultyLevel(DifficultyLevel.Advanced)]
        public async Task SetParentZone(string zoneName, string parentZoneName) {

            IZone zone = await Db.GetZoneAsync(zoneName);
            IZone parent = await Db.GetZoneAsync(parentZoneName);

            if (await BotUtils.ReplyValidateZoneAsync(Context, zone) && await BotUtils.ReplyValidateZoneAsync(Context, parent)) {

                if (zone.Id == parent.Id) {

                    await BotUtils.ReplyAsync_Error(Context, "A zone cannot be its own parent.");

                }
                else if (parent.ParentId == zone.Id) {

                    await BotUtils.ReplyAsync_Error(Context, "A zone cannot have its child as its parent.");

                }
                else if (zone.ParentId == parent.Id) {

                    await BotUtils.ReplyAsync_Warning(Context, string.Format("The parent zone of **{0}** is already **{1}**.",
                        zone.GetFullName(),
                        parent.GetFullName()));

                }
                else {

                    zone.ParentId = parent.Id;

                    await Db.UpdateZoneAsync(zone);

                    await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully set the parent zone of **{0}** to **{1}**.",
                        zone.GetFullName(),
                        parent.GetFullName()));

                }

            }

        }

    }

}
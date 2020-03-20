using Discord.Commands;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Common.Zones;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Discord.Messaging;
using OurFoodChain.Discord.Utilities;
using OurFoodChain.Extensions;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot.Modules {

    public class ZoneModule :
        OfcModuleBase {

        // Public members

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
        public async Task GetZone(string arg0) {

            // Show either the given zone, or all zones of the given type.

            IZone zone = await Db.GetZoneAsync(arg0);

            if (zone.IsValid()) {

                // The user passed in a valid zone, so show information about that zone.

                await ShowZoneAsync(zone);

            }
            else {

                // The user passed in an invalid zone, so check if it's a valid zone type.

                IZoneType zoneType = await Db.GetZoneTypeAsync(arg0);

                if (zoneType.IsValid()) {

                    // The user passed in a valid zone type, so show all zones with that type.

                    IEnumerable<IZone> zones = await Db.GetZonesAsync(zoneType);

                    await ShowZonesAsync(zones, zoneType);

                }
                else
                    await this.ReplyValidateZoneAsync(zone, arg0);

            }

        }
        [Command("zone"), Alias("z", "zones")]
        public async Task GetZones() {

            await ShowZonesAsync(await Db.GetZonesAsync(), null);

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

        [Command("SetZoneName"), Alias("SetZName"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetZoneName(string oldZoneName, string newZoneName) {

            IZone zone = await this.GetZoneOrReplyAsync(oldZoneName);

            if (zone.IsValid()) {

                oldZoneName = zone.GetFullName();
                newZoneName = ZoneUtilities.GetFullName(newZoneName);

                if ((await Db.GetZoneAsync(newZoneName)).IsValid()) {

                    // The given name cannot be the name of an existing zone.

                    await ReplyErrorAsync($"There is already a zone named {newZoneName.ToBold()}.");

                }
                else {

                    if (oldZoneName.Equals(newZoneName, System.StringComparison.OrdinalIgnoreCase)) {

                        await ReplyWarningAsync($"{zone.GetFullName().ToBold()} already has this name.");

                    }
                    else {

                        zone.Name = newZoneName;

                        await Db.UpdateZoneAsync(zone);

                        await ReplySuccessAsync($"{oldZoneName.ToBold()} was successfully renamed to {newZoneName.ToBold()}.");

                    }

                }

            }

        }

        [Command("+ZoneAlias"), Alias("AddZoneAlias", "+ZAlias", "AddZAlias"), RequirePrivilege(PrivilegeLevel.ServerModerator), DifficultyLevel(DifficultyLevel.Advanced)]
        public async Task AddZoneAlias(string zoneName, string alias) {

            IZone zone = await this.GetZoneOrReplyAsync(zoneName);

            if (zone.IsValid()) {

                alias = ZoneUtilities.GetFullName(alias);

                if (zone.GetFullName().Equals(alias, StringComparison.OrdinalIgnoreCase) || zone.Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase)) {

                    // The zone already has the given name/alias.

                    await ReplyWarningAsync($"{zone.GetFullName().ToBold()} already has this name.");

                }
                else if ((await Db.GetZoneAsync(alias)).IsValid()) {

                    // Another zone already has the given name/alias.

                    await ReplyErrorAsync($"There is already a zone named {alias.ToBold()}.");

                }
                else {

                    zone.Aliases.Add(alias);

                    await Db.UpdateZoneAsync(zone);

                    await ReplySuccessAsync($"{alias.ToBold()} was successfully added as an alias for {zone.GetFullName().ToBold()}.");

                }

            }

        }

        [Command("+ZoneField"), Alias("AddZoneField", "+ZField", "AddZField"), RequirePrivilege(PrivilegeLevel.ServerModerator), DifficultyLevel(DifficultyLevel.Advanced)]
        public async Task AddZoneField(string zoneName, string fieldName, string fieldValue) {

            IZone zone = await this.GetZoneOrReplyAsync(zoneName);

            if (zone.IsValid()) {

                IZoneField existingField = zone.Fields
                    .Where(field => field.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();

                if (existingField != null) {

                    existingField.Value = fieldValue;

                    await Db.UpdateZoneAsync(zone);

                    await ReplySuccessAsync($"The value for field {fieldName.ToTitle().ToBold()} was successfully updated.");

                }
                else {

                    zone.Fields.Add(new ZoneField(fieldName, fieldValue));

                    await Db.UpdateZoneAsync(zone);

                    await ReplySuccessAsync($"Field {fieldName.ToTitle().ToBold()} was successfully added to {zone.GetFullName()}.");

                }

            }

        }
        [Command("-ZoneField"), Alias("-ZField"), RequirePrivilege(PrivilegeLevel.ServerModerator), DifficultyLevel(DifficultyLevel.Advanced)]
        public async Task RemoveZoneField(string zoneName, string fieldName) {

            IZone zone = await this.GetZoneOrReplyAsync(zoneName);

            if (zone.IsValid()) {

                IZoneField existingField = zone.Fields
                    .Where(field => field.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();

                if (existingField != null) {

                    zone.Fields.Remove(existingField);

                    await Db.UpdateZoneAsync(zone);

                    await ReplySuccessAsync($"The field {fieldName.ToTitle().ToBold()} was successfully removed from {zone.GetFullName()}.");

                }
                else {

                    await ReplyWarningAsync($"No field named {fieldName.ToTitle()} exists.");

                }

            }

        }

        // Private members

        private async Task ShowZonesAsync(IEnumerable<IZone> zones, IZoneType type) {

            if (zones.Count() > 0) {

                // We need to make sure that even if the "short" description is actually long, we can show n zones per page.

                string embedTitle = string.Format("{0} zones ({1})", type.IsValid() ? type.Name : "All", zones.Count()).ToTitle();
                string embedDescription = string.Format("For detailed zone information, use `{0}zone <zone>` (e.g. `{0}zone {1}`).\n\n",
                    Config.Prefix,
                    zones.First().GetShortName().Contains(" ") ? string.Format("\"{0}\"", zones.First().GetShortName().ToLowerInvariant()) : zones.First().GetShortName().ToLowerInvariant());

                // Build paginated message.

                IEnumerable<IEmbed> pages = await BotUtils.ZonesToEmbedPagesAsync(embedTitle.Length + embedDescription.Length, zones, Db);

                foreach (IEmbed page in pages)
                    page.Description = embedDescription + page.Description;

                IPaginatedMessage message = new PaginatedMessage(pages);

                message.SetTitle(embedTitle);

                if (type.IsValid())
                    message.SetColor(type.Color);

                await ReplyAsync(message);

            }
            else {

                await ReplyInfoAsync("No zones have been added yet.");

            }

        }
        private async Task ShowZoneAsync(IZone zone) {

            if (await this.ReplyValidateZoneAsync(zone)) {

                // Get all species living in this zone.

                List<ISpecies> speciesList = new List<ISpecies>(await Db.GetSpeciesAsync(zone));

                speciesList.Sort((lhs, rhs) => lhs.GetShortName().CompareTo(rhs.GetShortName()));

                // Starting building a paginated message.
                // The message will have a paginated species list, and a toggle button to display the species sorted by role.

                string description = zone.GetDescriptionOrDefault();

                if (!speciesList.Any())
                    description += "\n\nThis zone does not contain any species.";

                List<IEmbed> embedPages = new List<IEmbed>();

                if (zone.Fields.Any()) {

                    embedPages.Add(new Embed());

                    foreach (IZoneField field in zone.Fields)
                        embedPages.Last().AddField(field.Name.ToTitle(), field.Value, true);

                    embedPages.Last().Description = description;

                }

                embedPages.AddRange(EmbedUtilities.CreateEmbedPages(string.Format("Extant species in this zone ({0}):", speciesList.Count()), speciesList));

                // Add title, decription, etc., to all pages.

                if (!embedPages.Any())
                    embedPages.Add(new Embed());

                IZoneType type = await Db.GetZoneTypeAsync(zone.TypeId) ?? new ZoneType();
                string aliases = zone.Aliases.Any() ? string.Format("({0})", string.Join(", ", zone.Aliases.Select(alias => alias.ToTitle()))) : string.Empty;
                string title = string.Format("{0} {1} {2}", type.Icon, zone.GetFullName(), aliases).Trim();

                System.Drawing.Color color = type.Color;

                foreach (IEmbed page in embedPages) {

                    page.Title = title;
                    page.ThumbnailUrl = zone.Pictures.FirstOrDefault()?.Url;
                    page.Color = color;

                    // Add the zone description to all pages if the zone doesn't have any fields (because the info page will be missing).

                    if (!zone.Fields.Any())
                        page.Description = description;

                }

                IPaginatedMessage message = new PaginatedMessage(embedPages);

                message.AddPageNumbers();

                // This page will have species organized by role.
                // Only bother with the role page if species actually exist in this zone.

                if (speciesList.Count() > 0) {

                    IEmbed rolesPage = new Embed {
                        Title = title,
                        ThumbnailUrl = zone.GetPictureUrl(),
                        Color = color
                    };

                    Dictionary<string, List<ISpecies>> rolesMap = new Dictionary<string, List<ISpecies>>();

                    foreach (ISpecies species in speciesList) {

                        IEnumerable<Common.Roles.IRole> roles_list = await Db.GetRolesAsync(species);

                        if (roles_list.Count() <= 0) {

                            if (!rolesMap.ContainsKey("no role"))
                                rolesMap["no role"] = new List<ISpecies>();

                            rolesMap["no role"].Add(species);

                            continue;

                        }

                        foreach (Common.Roles.IRole role in roles_list) {

                            if (!rolesMap.ContainsKey(role.GetName()))
                                rolesMap[role.GetName()] = new List<ISpecies>();

                            rolesMap[role.GetName()].Add(species);

                        }

                    }

                    // Sort the list of species belonging to each role.

                    foreach (List<ISpecies> i in rolesMap.Values)
                        i.Sort((lhs, rhs) => lhs.GetShortName().CompareTo(rhs.GetShortName()));

                    // Create a sorted list of keys so that the roles are in order.

                    List<string> sorted_keys = new List<string>(rolesMap.Keys);
                    sorted_keys.Sort();

                    foreach (string i in sorted_keys) {

                        StringBuilder lines = new StringBuilder();

                        foreach (Species j in rolesMap[i])
                            lines.AppendLine(j.GetShortName());

                        rolesPage.AddField(string.Format("{0}s ({1})", StringUtilities.ToTitleCase(i), rolesMap[i].Count()), lines.ToString(), inline: true);

                    }

                    // Add the page to the builder.

                    message.AddReaction("🇷", async (args) => {

                        if (args.Emoji != "🇷")
                            return;

                        args.Message.PaginationEnabled = !args.ReactionAdded;

                        if (args.ReactionAdded)
                            args.Message.CurrentPage = new Message() { Embed = rolesPage };
                        else
                            args.Message.CurrentPage = null;

                    });

                }

                await ReplyAsync(message);

            }

        }

    }

}
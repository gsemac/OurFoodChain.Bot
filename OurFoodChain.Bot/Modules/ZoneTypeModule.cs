using Discord.Commands;
using OurFoodChain.Bot;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Common.Zones;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Discord.Messaging;
using OurFoodChain.Discord.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Modules {

    public class ZoneTypeModule :
        OfcModuleBase {

        [Command("zonetype"), DifficultyLevel(DifficultyLevel.Advanced)]
        public async Task GetZoneType(string arg0 = "") {

            if (!string.IsNullOrEmpty(arg0)) {

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

                    string embedTitle = string.Format("{0} {1} Zones ({2})", type.Icon, type.Name, zones.Count()).ToTitle();
                    string embedDescription = type.Description + "\n\n";

                    IEnumerable<IEmbed> pages = await BotUtils.ZonesToEmbedPagesAsync(embedTitle.Length + embedDescription.Length, zones, Db, showIcon: false);

                    foreach (IEmbed page in pages)
                        page.Description = embedDescription + page.Description;

                    IPaginatedMessage message = new PaginatedMessage(pages);

                    message.SetTitle(embedTitle);
                    message.SetColor(type.Color);

                    await ReplyAsync(message);

                }
                else {

                    await ReplyErrorAsync("No such zone type exists.");

                }

            }
            else {

                await GetZoneTypes();

            }

        }
        [Command("zonetypes"), DifficultyLevel(DifficultyLevel.Advanced)]
        public async Task GetZoneTypes() {

            IEnumerable<IZoneType> zoneTypes = await Db.GetZoneTypesAsync();

            if (zoneTypes.Any()) {

                List<string> lines = new List<string>();

                foreach (IZoneType zoneType in zoneTypes) {

                    StringBuilder lineBuilder = new StringBuilder();

                    lineBuilder.Append($"{zoneType.Icon} {zoneType.Name.ToTitle().ToBold()}");

                    if (!string.IsNullOrWhiteSpace(zoneType.Description)) {

                        lineBuilder.Append("\t—\t");
                        lineBuilder.Append(zoneType.Description.GetFirstSentence());

                    }

                    lines.Add(lineBuilder.ToString().Truncate(40));

                }

                string description = $"For detailed type information, use `{Config.Prefix}zonetype <type>` (e.g. `{Config.Prefix}zonetype {zoneTypes.First().Name.ToLowerInvariant()}`).\n\n";

                IPaginatedMessage message = new PaginatedMessage();

                message.AddLines($"All Zone Types ({zoneTypes.Count()})", lines, columnsPerPage: 1, options: EmbedPaginationOptions.AddPageNumbers);

                foreach (IEmbed embed in message.Select(page => page.Embed))
                    embed.Description = description + embed.Description;

                await ReplyAsync(message);

            }
            else {

                await ReplyInfoAsync("No zone types have been added.");

            }

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

    }

}
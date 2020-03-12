using Discord;
using Discord.Commands;
using OurFoodChain.Bot;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Generations;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Common.Zones;
using OurFoodChain.Data;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Discord.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OurFoodChain {

    class BotUtils {

        public const string DEFAULT_GENUS_DESCRIPTION = "No description provided.";
        public const string DEFAULT_ZONE_DESCRIPTION = "No description provided.";
        public const string DEFAULT_DESCRIPTION = "No description provided.";

        public static string Strikeout(string str) {

            return string.Format("~~{0}~~", str);

        }

        public static async Task<bool> ReplyAsync_ValidateRole(ICommandContext context, Common.Roles.IRole role) {

            if (role is null || role.Id <= 0) {

                await context.Channel.SendMessageAsync("No such role exists.");

                return false;

            }

            return true;

        }
        public static async Task<bool> ReplyValidateZoneAsync(ICommandContext context, Common.Zones.IZone zone, string zoneName = "") {

            if (!zone.IsValid()) {

                string message = "No such zone exists.";

                if (!string.IsNullOrEmpty(zoneName))
                    message = $"Zone {zoneName.ToTitle().ToBold()} does not exist.";

                await DiscordUtilities.ReplyErrorAsync(context.Channel, message);

                return false;

            }

            return true;

        }
        public static async Task<bool> ReplyValidateZoneTypeAsync(ICommandContext context, IZoneType zoneType) {

            if (!zoneType.IsValid()) {

                await context.Channel.SendMessageAsync("No such zone type exists.");

                return false;

            }

            return true;

        }
        public static async Task<bool> ReplyIsImageUrlValidAsync(ICommandContext context, string imageUrl) {

            if (!StringUtilities.IsImageUrl(imageUrl)) {

                await ReplyAsync_Error(context, "The image URL is invalid.");

                return false;

            }

            return true;

        }
        public static async Task<bool> ReplyAsync_ValidatePeriod(ICommandContext context, Period period) {

            if (period is null || period.id <= 0) {

                await context.Channel.SendMessageAsync("No such period exists.");

                return false;

            }

            return true;

        }

        public static async Task ReplyAsync_Warning(ICommandContext context, string text) {

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription(string.Format("⚠️ {0}", text));
            embed.WithColor(Color.Orange);

            await context.Channel.SendMessageAsync("", false, embed.Build());

        }
        public static async Task ReplyAsync_Error(ICommandContext context, string text) {

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription(string.Format("❌ {0}", text));
            embed.WithColor(Color.Red);

            await context.Channel.SendMessageAsync("", false, embed.Build());

        }
        public static async Task ReplyAsync_Success(ICommandContext context, string text) {

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription(string.Format("✅ {0}", text));
            embed.WithColor(Color.Green);

            await context.Channel.SendMessageAsync("", false, embed.Build());

        }
        public static async Task ReplyAsync_Info(ICommandContext context, string text) {

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription(text);
            embed.WithColor(Color.LightGrey);

            await context.Channel.SendMessageAsync("", false, embed.Build());

        }

        public static async Task<IEnumerable<Discord.Messaging.IEmbed>> ZonesToEmbedPagesAsync(int existingLength, IEnumerable<IZone> zones, SQLiteDatabase database, bool showIcon = true) {

            List<string> lines = new List<string>();
            int zones_per_page = 20;
            int max_line_length = Math.Min(showIcon ? 78 : 80, (DiscordUtils.MaxEmbedLength - existingLength) / zones_per_page);

            foreach (IZone zone in zones) {

                IZoneType type = await database.GetZoneTypeAsync(zone.TypeId);

                string line = string.Format("{1} **{0}**\t-\t{2}", StringUtilities.ToTitleCase(zone.Name), showIcon ? (type is null ? new ZoneType() : type).Icon : "", zone.Description.GetFirstSentence());

                if (line.Length > max_line_length)
                    line = line.Substring(0, max_line_length - 3) + "...";

                lines.Add(line);

            }

            return EmbedUtilities.CreateEmbedPages(string.Empty, lines, itemsPerPage: 20, options: EmbedPaginationOptions.AddPageNumbers);

        }

        public static async Task<string> TimestampToDateStringAsync(long timestamp, IOfcBotContext context, TimestampToDateStringFormat format = TimestampToDateStringFormat.Default) {

            if (context.Configuration.GenerationsEnabled) {

                IGeneration gen = await context.Database.GetGenerationByDateAsync(DateUtilities.GetDateFromTimestamp(timestamp));

                return gen is null ? "Gen ???" : gen.Name;

            }

            return DateUtils.TimestampToDateString(timestamp, format);

        }

    }

}
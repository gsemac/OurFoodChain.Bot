using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Common.Zones;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Discord.Utilities;
using System.Threading.Tasks;

namespace OurFoodChain.Extensions {

    public static class OfcModuleBaseZoneExtensions {

        // Public members

        public static async Task<IZone> GetZoneOrReplyAsync(this OfcModuleBase moduleBase, string zoneName) {

            IZone zone = await moduleBase.Db.GetZoneAsync(zoneName);

            await moduleBase.ReplyValidateZoneAsync(zone, zoneName);

            return zone;

        }
        public static async Task<bool> ReplyValidateZoneAsync(this OfcModuleBase moduleBase, IZone zone, string zoneName = "") {

            if (!zone.IsValid()) {

                string message = "No such zone exists.";

                if (!string.IsNullOrEmpty(zoneName)) {

                    zoneName = ZoneUtilities.GetFullName(zoneName);

                    if (zoneName.StartsWith("zone", System.StringComparison.OrdinalIgnoreCase))
                        message = $"{zoneName.ToTitle().ToBold()} does not exist.";
                    else
                        message = $"Zone {zoneName.ToTitle().ToBold()} does not exist.";

                }

                await DiscordUtilities.ReplyErrorAsync(moduleBase.Context.Channel, message);

                return false;

            }

            return true;

        }

    }

}

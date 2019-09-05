using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public static class DiscordUtils {

        public static int MAX_FIELD_LENGTH = 1024;

        public static async Task<IMessage[]> DownloadAllMessagesAsync(IMessageChannel channel, int limit = 0) {

            List<IMessage> messages = new List<IMessage>();

            if (channel is null)
                return messages.ToArray();

            IEnumerable<IMessage> next_messages = await channel.GetMessagesAsync().FlattenAsync();

            while (next_messages.Count() > 0 && (limit <= 0 || messages.Count() < limit)) {

                if (next_messages.Count() > 0)
                    messages.AddRange(next_messages);

                if (messages.Count() > 0)
                    next_messages = await channel.GetMessagesAsync(messages.Last(), Direction.Before).FlattenAsync();

            }

            messages.Reverse();

            if (limit <= 0)
                return messages.ToArray();
            else
                return messages.Take(limit).ToArray();

        }

        public static Color ConvertColor(System.Drawing.Color color) {
            return new Color(color.R, color.G, color.B);
        }

    }

}

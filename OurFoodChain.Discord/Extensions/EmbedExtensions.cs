using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord.Extensions {

    public static class EmbedExtensions {

        public static Embed ToDiscordEmbed(this Messaging.IEmbed embed) {

            EmbedBuilder embedBuilder = new EmbedBuilder();

            embedBuilder.WithTitle(embed.Title);
            embedBuilder.WithThumbnailUrl(embed.ThumbnailUrl);
            embedBuilder.WithDescription(embed.Description);
            embedBuilder.WithFooter(embed.Footer);
            embedBuilder.WithColor(embed.Color.ToDiscordColor());

            foreach (Messaging.IEmbedField field in embed.Fields)
                embedBuilder.AddField(field.Name, field.Value, field.Inline);

            return embedBuilder.Build();

        }

    }

}
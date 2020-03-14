using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord.Extensions {

    public static class EmbedExtensions {

        public static Embed ToDiscordEmbed(this Messaging.IEmbed embed) {

            EmbedBuilder embedBuilder = new EmbedBuilder();

            embedBuilder.WithTitle(embed.Title);
            embedBuilder.WithImageUrl(embed.ImageUrl);
            embedBuilder.WithThumbnailUrl(embed.ThumbnailUrl);
            embedBuilder.WithDescription(embed.Description);
            embedBuilder.WithFooter(embed.Footer);

            if (embed.Color.HasValue)
                embedBuilder.WithColor(embed.Color.Value.ToDiscordColor());

            foreach (Messaging.IEmbedField field in embed.Fields)
                embedBuilder.AddField(field.Name, field.Value, field.Inline);

            return embedBuilder.Build();

        }

    }

}
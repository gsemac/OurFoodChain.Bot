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
            embedBuilder.WithUrl(embed.Url);

            if (embed.Color.HasValue)
                embedBuilder.WithColor(embed.Color.Value.ToDiscordColor());

            foreach (Messaging.IEmbedField field in embed.Fields)
                embedBuilder.AddField(field.Name, field.Value, field.Inline);

            return embedBuilder.Build();

        }

        public static void SetColor(this Messaging.IEmbed embed, System.Drawing.Color color) {

            embed.Color = color;

        }
        public static void SetColor(this Messaging.IEmbed embed, Color color) {

            embed.Color = color.ToSystemDrawingColor();

        }

        public static void AddField(this Messaging.IEmbed embed, string name, object value, bool inline = false) {

            embed.AddField(new Messaging.EmbedField(name, value) { Inline = inline });

        }
        public static void InsertField(this Messaging.IEmbed embed, int index, string name, object value, bool inline = false) {

            embed.InsertField(index, new Messaging.EmbedField(name, value) { Inline = inline });

        }

    }

}
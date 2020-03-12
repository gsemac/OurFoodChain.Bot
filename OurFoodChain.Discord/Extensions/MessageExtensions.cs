using Discord;
using OurFoodChain.Discord.Messaging;
using OurFoodChain.Discord.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OurFoodChain.Discord.Extensions {

    public static class MessageExtensions {

        public static void SetTitle(this IPaginatedMessage message, string title) {

            foreach (Messaging.IMessage page in message.Where(p => p.Embed != null))
                page.Embed.Title = title;

        }
        public static void SetColor(this IPaginatedMessage message, System.Drawing.Color color) {

            foreach (Messaging.IMessage page in message.Where(p => p.Embed != null))
                page.Embed.Color = color;

        }
        public static void SetColor(this IPaginatedMessage message, Color color) {

            foreach (Messaging.IMessage page in message.Where(p => p.Embed != null))
                page.Embed.Color = color.ToSystemDrawingColor();

        }
        public static void SetColor(this IPaginatedMessage message, int red, int green, int blue) {

            foreach (Messaging.IMessage page in message.Where(p => p.Embed != null))
                page.Embed.Color = System.Drawing.Color.FromArgb(red, green, blue);

        }
        public static void SetDescription(this IPaginatedMessage message, string description) {

            foreach (Messaging.IMessage page in message.Where(p => p.Embed != null))
                page.Embed.Description = description;

        }
        public static void SetFooter(this IPaginatedMessage message, string footer) {

            foreach (Messaging.IMessage page in message.Where(p => p.Embed != null))
                page.Embed.Footer = footer;

        }
        public static void SetThumbnailUrl(this IPaginatedMessage message, string thumbnailUrl) {

            foreach (Messaging.IMessage page in message.Where(p => p.Embed != null))
                page.Embed.ThumbnailUrl = thumbnailUrl;

        }

        public static void AddPage(this IPaginatedMessage message, string text) {

            message.AddPage(new Message(text));

        }
        public static void AddPage(this IPaginatedMessage message, Messaging.IEmbed embed) {

            message.AddPage(new Message() { Embed = embed });

        }

        public static void AddPages(this IPaginatedMessage message, IEnumerable<Messaging.IEmbed> pages) {

            foreach (Messaging.IEmbed embed in pages)
                message.AddPage(new Message() { Embed = embed });

        }

        public static void AddFields(this IPaginatedMessage message, IEnumerable<IEmbedField> fields, int itemsPerPage = 10) {

            List<Messaging.IEmbed> pages = new List<Messaging.IEmbed>();

            foreach (IEmbedField field in fields) {

                if (pages.Count() <= 0 || pages.Last().Fields.Count() >= itemsPerPage)
                    pages.Add(new Messaging.Embed());

                pages.Last().AddField(field);

            }

            message.AddPages(pages);

        }
        public static void AddLines(this IPaginatedMessage message, IEnumerable<string> listItems) {

            message.AddLines(string.Empty, listItems);

        }
        public static void AddLines(this IPaginatedMessage message, string listTitle, IEnumerable<string> listItems, int itemsPerPage = EmbedUtilities.DefaultItemsPerPage, int columnsPerPage = EmbedUtilities.DefaultColumnsPerPage, EmbedPaginationOptions options = EmbedPaginationOptions.None) {

            message.AddPages(EmbedUtilities.CreateEmbedPages(listTitle, listItems, itemsPerPage, columnsPerPage, options));

        }

        public static void AddPageNumbers(this IPaginatedMessage message) {

            EmbedUtilities.AddPageNumbers(message.Select(page => page.Embed).Where(embed => embed != null));

        }

    }

}
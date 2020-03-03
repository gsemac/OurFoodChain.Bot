﻿using Discord;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    [Flags]
    public enum EmbedPagesFlag {
        None = 0,
        CrossOutExtinctSpecies = 1,
        Default = CrossOutExtinctSpecies,
    }

    public class EmbedUtils {

        public static List<List<string>> ListToColumns(List<string> items, int itemsPerColumn) {

            List<List<string>> columns = new List<List<string>>();

            foreach (string item in items) {

                if (columns.Count <= 0 || columns.Last().Count >= itemsPerColumn)
                    columns.Add(new List<string>());

                columns.Last().Add(item);

            }

            return columns;

        }
        public static List<EmbedBuilder> ListToEmbedPages(List<string> items, int itemsPerPage = 40, string fieldName = "\u200B") {

            List<List<string>> columns = ListToColumns(items, itemsPerPage / 2);
            List<EmbedBuilder> pages = new List<EmbedBuilder>();

            EmbedBuilder current_page = new EmbedBuilder();
            bool is_first_field = true;

            foreach (List<string> column in columns) {

                StringBuilder builder = new StringBuilder();

                foreach (string item in column)
                    builder.AppendLine(item);

                if (is_first_field) {

                    current_page.AddField(fieldName, builder.ToString(), inline: true);

                    is_first_field = false;

                }
                else {

                    current_page.AddField("\u200B", builder.ToString(), inline: true);

                    pages.Add(current_page);

                    current_page = new EmbedBuilder();
                    is_first_field = true;

                }

            }

            if (current_page.Fields.Count > 0)
                pages.Add(current_page);

            // Add page numbers.

            for (int i = 0; i < pages.Count; ++i)
                pages[i].WithFooter(string.Format("Page {0} of {1}", i + 1, pages.Count));

            return pages;

        }
        public static List<EmbedBuilder> FieldsToEmbedPages(List<EmbedFieldBuilder> items, int itemsPerPage = 10) {

            List<EmbedBuilder> pages = new List<EmbedBuilder>();

            foreach (EmbedFieldBuilder field in items) {

                if (pages.Count() <= 0 || pages.Last().Fields.Count() >= itemsPerPage)
                    pages.Add(new EmbedBuilder());

                pages.Last().Fields.Add(field);

            }

            return pages;

        }
        public static List<EmbedBuilder> SearchQueryResultToEmbedPages(Taxa.SearchResult result, int itemsPerField = 10) {

            List<EmbedBuilder> pages = new List<EmbedBuilder>();
            int fields_per_page = 6;

            foreach (Taxa.SearchResult.Group group in result.Groups) {

                List<string> items = group.ToStringArray().ToList();

                List<List<string>> columns = ListToColumns(items, itemsPerField);
                int column_index = 1;

                foreach (List<string> column in columns) {

                    // Create the field for this column.

                    string title = group.Name.Length > 25 ? group.Name.Substring(0, 22) + "..." : group.Name;

                    EmbedFieldBuilder field = new EmbedFieldBuilder {
                        Name = column_index == 1 ? string.Format("{0} ({1})", StringUtilities.ToTitleCase(title), items.Count()) : string.Format("...", StringUtilities.ToTitleCase(title)),
                        Value = string.Join(Environment.NewLine, column),
                        IsInline = true
                    };

                    ++column_index;

                    // Add the field to the embed, creating a new page if needed.

                    int field_length = field.Name.ToString().Length + field.Value.ToString().Length;

                    if (pages.Count() <= 0 || pages.Last().Fields.Count() >= fields_per_page || pages.Last().Length + field_length > Bot.DiscordUtils.MaxEmbedLength)
                        pages.Add(new EmbedBuilder());

                    pages.Last().Fields.Add(field);

                }

            }

            return pages;

        }
        public static List<EmbedBuilder> LinesToEmbedPages(List<string> items, int linesPerPage = 20) {

            List<EmbedBuilder> pages = new List<EmbedBuilder>();

            int line_index = 0;

            do {

                StringBuilder sb = new StringBuilder();

                for (int i = line_index; i < items.Count() && (i < line_index + linesPerPage); ++i)
                    sb.AppendLine(items[i]);

                line_index += linesPerPage;

                pages.Add(new EmbedBuilder());
                pages.Last().WithDescription(sb.ToString());

            } while (line_index < items.Count());

            return pages;

        }

        public static void AddLongFieldToEmbedPages(List<EmbedBuilder> pages, List<string> items, int itemsPerPage = 40, string fieldName = "\u200B") {

            // If no pages have been added yet, we can just generate embed pages directly.
            // This also applies if there's no room to add more fields to the last page.

            if (pages.Count <= 0 || pages.Last().Fields.Count() >= 2)
                pages.AddRange(ListToEmbedPages(items, fieldName: fieldName));

            else {

                // Otherwise, we'll start with the second field, and then continue with additional pages from thereon.

                List<List<string>> columns = ListToColumns(items, itemsPerPage / 2);

                if (columns.Count() <= 0)
                    return;

                pages.Last().AddField(fieldName, string.Join(Environment.NewLine, columns[0]), inline: true);

                pages.AddRange(ListToEmbedPages(items.Skip(itemsPerPage / 2).ToList(), fieldName: fieldName));

            }

        }

        public static List<List<ISpecies>> SpeciesListToColumns(IEnumerable<ISpecies> items, int speciesPerColumn) {

            List<List<ISpecies>> columns = new List<List<ISpecies>>();

            foreach (ISpecies sp in items) {

                if (columns.Count <= 0 || columns.Last().Count >= speciesPerColumn)
                    columns.Add(new List<ISpecies>());

                columns.Last().Add(sp);

            }

            return columns;

        }
        public static List<EmbedBuilder> SpeciesListToEmbedPages(IEnumerable<ISpecies> items, int speciesPerPage = 40, string fieldName = "\u200B", EmbedPagesFlag flags = EmbedPagesFlag.Default) {

            List<List<ISpecies>> columns = SpeciesListToColumns(items, speciesPerPage / 2);
            List<EmbedBuilder> pages = new List<EmbedBuilder>();

            EmbedBuilder current_page = new EmbedBuilder();
            bool is_first_field = true;

            foreach (List<ISpecies> column in columns) {

                StringBuilder builder = new StringBuilder();

                foreach (ISpecies sp in column)
                    if (flags.HasFlag(EmbedPagesFlag.CrossOutExtinctSpecies) && sp.Status.IsExinct)
                        builder.AppendLine(string.Format("~~{0}~~", sp.ShortName));
                    else
                        builder.AppendLine(sp.ShortName);

                if (is_first_field) {

                    current_page.AddField(fieldName, builder.ToString(), inline: true);

                    is_first_field = false;

                }
                else {

                    current_page.AddField("\u200B", builder.ToString(), inline: true);

                    pages.Add(current_page);

                    current_page = new EmbedBuilder();
                    is_first_field = true;

                }

            }

            if (current_page.Fields.Count > 0)
                pages.Add(current_page);

            // Add page numbers.

            for (int i = 0; i < pages.Count; ++i)
                pages[i].WithFooter(string.Format("Page {0} of {1}", i + 1, pages.Count));

            return pages;

        }

    }

}

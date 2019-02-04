using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class EmbedUtils {

        public static List<List<Species>> SpeciesListToColumns(List<Species> items, int speciesPerColumn) {

            List<List<Species>> columns = new List<List<Species>>();

            foreach (Species sp in items) {

                if (columns.Count <= 0 || columns.Last().Count >= speciesPerColumn)
                    columns.Add(new List<Species>());

                columns.Last().Add(sp);

            }

            return columns;

        }
        public static List<EmbedBuilder> SpeciesListToEmbedPages(List<Species> items, int speciesPerPage = 40, string fieldName = "\u200B") {

            List<List<Species>> columns = SpeciesListToColumns(items, speciesPerPage / 2);
            List<EmbedBuilder> pages = new List<EmbedBuilder>();

            EmbedBuilder current_page = new EmbedBuilder();
            bool is_first_field = true;

            foreach (List<Species> column in columns) {

                StringBuilder builder = new StringBuilder();

                foreach (Species sp in column)
                    if (sp.isExtinct)
                        builder.AppendLine(string.Format("~~{0}~~", sp.GetShortName()));
                    else
                        builder.AppendLine(sp.GetShortName());

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

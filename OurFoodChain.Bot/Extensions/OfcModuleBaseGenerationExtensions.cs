using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Discord.Messaging;
using OurFoodChain.Discord.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OurFoodChain.Extensions {

    public static class OfcModuleBaseGenerationExtensions {

        public static async Task ReplyRecentEventsAsync(this OfcModuleBase moduleBase, DateTimeOffset start, DateTimeOffset end) {

            IPaginatedMessage message = await moduleBase.BuildRecentEventsMessageAsync(start, end);

            await moduleBase.ReplyAsync(message);

        }

        public static async Task<IPaginatedMessage> BuildRecentEventsMessageAsync(this OfcModuleBase moduleBase, DateTimeOffset start, DateTimeOffset end) {

            IEnumerable<ISpecies> newSpecies = (await moduleBase.Db.GetSpeciesAsync(start, end)).OrderBy(species => moduleBase.TaxonFormatter.GetString(species, false));
            IEnumerable<ISpecies> extinctSpecies = (await moduleBase.Db.GetExtinctSpeciesAsync(start, end)).OrderBy(species => moduleBase.TaxonFormatter.GetString(species, false));

            List<IEmbed> pages = new List<IEmbed>();

            if (newSpecies.Count() > 0)
                EmbedUtilities.AppendEmbedPages(pages, EmbedUtilities.CreateEmbedPages($"New species ({newSpecies.Count()})", newSpecies.Select(species => moduleBase.TaxonFormatter.GetString(species))));

            if (newSpecies.Count() > 0)
                EmbedUtilities.AppendEmbedPages(pages, EmbedUtilities.CreateEmbedPages($"Extinctions ({extinctSpecies.Count()})", extinctSpecies.Select(species => moduleBase.TaxonFormatter.GetString(species, false))));

            EmbedUtilities.AddPageNumbers(pages);

            if (pages.Count() <= 0)
                pages.Add(new Embed() { Description = "No events" });

            foreach (IEmbed page in pages)
                page.Title = $"Recent events ({DateUtilities.GetTimeSpanString(end - start)})";

            IPaginatedMessage message = new PaginatedMessage(pages);

            return message;

        }

    }

}
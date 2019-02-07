using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class PaginatedEmbedBuilder {

        public PaginatedEmbedBuilder(List<EmbedBuilder> pages) {

            _pages.AddRange(pages);
        }

        public void SetTitle(string title) {

            if (_pages.Count <= 0)
                _pages.Add(new EmbedBuilder());

            foreach (EmbedBuilder page in _pages)
                page.WithTitle(title);

        }
        public void SetDescription(string description) {

            if (_pages.Count <= 0)
                _pages.Add(new EmbedBuilder());

            foreach (EmbedBuilder page in _pages)
                page.WithDescription(description);

        }
        public void AppendFooter(string footer) {

            if (_pages.Count <= 0)
                _pages.Add(new EmbedBuilder());

            foreach (EmbedBuilder page in _pages)
                page.WithFooter(page.Footer is null ? footer : page.Footer.Text + footer);

        }

        public CommandUtils.PaginatedMessage Build() {

            CommandUtils.PaginatedMessage message = new CommandUtils.PaginatedMessage();

            foreach (EmbedBuilder page in _pages)
                message.pages.Add(page.Build());

            return message;

        }

        private List<EmbedBuilder> _pages = new List<EmbedBuilder>();


    }

}

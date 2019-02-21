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
        public void SetThumbnailUrl(string thumbnailUrl) {

            if (_pages.Count <= 0)
                _pages.Add(new EmbedBuilder());

            foreach (EmbedBuilder page in _pages)
                page.WithThumbnailUrl(thumbnailUrl);

        }
        public void SetFooter(string footer) {

            if (_pages.Count <= 0)
                _pages.Add(new EmbedBuilder());

            foreach (EmbedBuilder page in _pages)
                page.WithFooter(footer);

        }
        public void AppendFooter(string footer) {

            if (_pages.Count <= 0)
                _pages.Add(new EmbedBuilder());

            foreach (EmbedBuilder page in _pages)
                page.WithFooter(page.Footer is null ? footer : page.Footer.Text + footer);

        }
        public void SetColor(Color color) {

            foreach (EmbedBuilder page in _pages)
                page.WithColor(color);

        }

        public void SetCallback(Action<CommandUtils.PaginatedMessageCallbackArgs> callback) {

            _callback = callback;

        }

        public void AddReaction(string reaction) {

            _reactions.Add(reaction);

        }

        public CommandUtils.PaginatedMessage Build() {

            CommandUtils.PaginatedMessage message = new CommandUtils.PaginatedMessage();

            foreach (EmbedBuilder page in _pages)
                message.pages.Add(page.Build());

            message.callback = _callback;

            // In the future, all reactions should be added.
            if (_reactions.Count() > 0)
                message.emojiToggle = _reactions[0];

            return message;

        }

        private List<EmbedBuilder> _pages = new List<EmbedBuilder>();
        private Action<CommandUtils.PaginatedMessageCallbackArgs> _callback;
        private List<string> _reactions = new List<string>();

    }

}

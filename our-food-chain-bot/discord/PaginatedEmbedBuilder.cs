using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class PaginatedEmbedBuilder {

        public string Message { get; set; }
        public string Title {
            get {
                return _title;
            }
            set {
                _title = value;
            }
        }
        public string Description {
            get {
                return _description;
            }
            set {
                _description = value;
            }
        }
        public int Length {
            get {
                return _pages.Count > 0 ? _pages[0].Length : 0;
            }
        }
        public Color Color {
            get {
                return _color;
            }
            set {
                _color = value;
            }
        }

        public PaginatedEmbedBuilder() { }
        public PaginatedEmbedBuilder(List<EmbedBuilder> pages) {

            _pages.AddRange(pages);
        }

        public void SetTitle(string title) {

            if (_pages.Count <= 0)
                _pages.Add(new EmbedBuilder());

            foreach (EmbedBuilder page in _pages)
                page.WithTitle(title);

        }
        public void PrependDescription(string description) {

            if (_pages.Count <= 0)
                _pages.Add(new EmbedBuilder());

            foreach (EmbedBuilder page in _pages)
                page.WithDescription(string.IsNullOrEmpty(page.Description) ? description : description + page.Description);

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
        public void SetColor(byte r, byte g, byte b) {

            foreach (EmbedBuilder page in _pages)
                page.WithColor(new Color(r, g, b));

        }

        public void AddPages(IEnumerable<EmbedBuilder> pages) {

            if (!string.IsNullOrEmpty(Title))
                pages.ToList().ForEach(x => {
                    x.Title = string.IsNullOrEmpty(x.Title) ? Title : x.Title;
                });

            if (!string.IsNullOrEmpty(Description))
                pages.ToList().ForEach(x => {
                    if (!x.Description.StartsWith(Description))
                        x.Description = Description + x.Description;
                });

            if (Color != Color.DarkGrey)
                pages.ToList().ForEach(x => {
                    x.Color = Color;
                });

            _pages.AddRange(pages);

        }
        public void AddPageNumbers() {

            int num = 1;

            foreach (EmbedBuilder page in _pages) {

                string num_string = string.Format("Page {0} of {1}", num, _pages.Count());

                page.WithFooter(page.Footer is null || string.IsNullOrEmpty(page.Footer.Text) ? num_string : num_string + " — " + page.Footer.Text);

                ++num;

            }

        }

        public void SetCallback(Action<CommandUtils.PaginatedMessageCallbackArgs> callback) {

            _callback = callback;

        }

        public void AddReaction(string reaction) {

            _reactions.Add(reaction);

        }

        public CommandUtils.PaginatedMessage Build() {

            CommandUtils.PaginatedMessage message = new CommandUtils.PaginatedMessage();

            message.message = Message;

            foreach (EmbedBuilder page in _pages)
                message.pages.Add(page.Build());

            message.callback = _callback;

            // In the future, all reactions should be added.
            if (_reactions.Count() > 0)
                message.emojiToggle = _reactions[0];

            return message;

        }

        private string _title = "";
        private string _description = "";
        private Color _color = Color.DarkGrey;

        private List<EmbedBuilder> _pages = new List<EmbedBuilder>();
        private Action<CommandUtils.PaginatedMessageCallbackArgs> _callback;
        private List<string> _reactions = new List<string>();

    }

}

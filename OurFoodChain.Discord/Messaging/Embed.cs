using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace OurFoodChain.Discord.Messaging {

    public class Embed :
        IEmbed {

        // Public members

        public string Title { get; set; }
        public string Url { get; set; }
        public string ImageUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Description { get; set; }
        public string Footer { get; set; }
        public Color? Color { get; set; } 

        public IEnumerable<IEmbedField> Fields => fields;

        public int Length {
            get {

                int length = (int)(
                    Title?.Length +
                    Url?.Length +
                    ImageUrl?.Length +
                    ThumbnailUrl?.Length +
                    Description?.Length + 
                    Footer?.Length
                    ?? 0);

                length += Fields.Sum(f => f.Length);

                return length;

            }
        }

        public void AddField(string name, object value, bool inline = false) {

            fields.Add(new EmbedField(name, value) { Inline = inline });

        }
        public void AddField(IEmbedField field) {

            fields.Add(field);

        }

        // Private members

        private readonly List<IEmbedField> fields = new List<IEmbedField>();

    }

}
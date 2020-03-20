﻿using System;
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

                int length = 0;

                length += Title?.Length ?? 0;
                length += Url?.Length ?? 0;
                length += ImageUrl?.Length ?? 0;
                length += ThumbnailUrl?.Length ?? 0;
                length += Description?.Length ?? 0;
                length += Footer?.Length ?? 0;

                length += Fields.Sum(f => f.Length);

                return length;

            }
        }

        public void InsertField(int index, IEmbedField field) {

            fields.Insert(index, field);

        }
        public void AddField(IEmbedField field) {

            fields.Add(field);

        }

        // Private members

        private readonly List<IEmbedField> fields = new List<IEmbedField>();

    }

}
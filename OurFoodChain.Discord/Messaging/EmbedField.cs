using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord.Messaging {

    public class EmbedField :
        IEmbedField {

        public const string EmptyName = "\u200B";

        public string Name { get; set; }
        public string Value { get; set; }
        public bool Inline { get; set; } = false;

        public int Length => (int)(Name?.Length + Value?.Length);

        public EmbedField(string name, object value) {

            this.Name = name;
            this.Value = value.ToString();

        }

    }

}
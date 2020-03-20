using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace OurFoodChain.Discord.Messaging {

    public interface IEmbed {

        string Title { get; set; }
        string Url { get; set; }
        string ImageUrl { get; set; }
        string ThumbnailUrl { get; set; }
        string Description { get; set; }
        string Footer { get; set; }
        Color? Color { get; set; }
        IEnumerable<IEmbedField> Fields { get; }

        int Length { get; }

        void InsertField(int index, IEmbedField field);
        void AddField(IEmbedField field);

    }

}
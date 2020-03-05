using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord.Messaging {

    public interface IEmbedField {

        string Name { get; set; }
        string Value { get; set; }
        bool Inline { get; set; }

        int Length { get; }

    }

}
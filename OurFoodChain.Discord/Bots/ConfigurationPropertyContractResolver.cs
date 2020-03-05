using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord.Bots {

    public class ConfigurationPropertyContractResolver :
        DefaultContractResolver {

        protected override string ResolvePropertyName(string propertyName) {

            // Property names should be case-insensitive, and underscores should be ignored.

            propertyName = propertyName.Replace("_", "").ToLowerInvariant();

            return base.ResolvePropertyName(propertyName);

        }

    }

}
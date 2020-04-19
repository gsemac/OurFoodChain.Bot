using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Configuration {

    public static class Configuration {

        public static T FromFile<T>(string filePath) {

            return Parse<T>(System.IO.File.ReadAllText(filePath));

        }
        public static T Parse<T>(string json) {

            return JsonConvert.DeserializeObject<T>(json);

        }

    }

}
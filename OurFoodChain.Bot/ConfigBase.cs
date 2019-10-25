using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class ConfigBase<T> {

        public static T FromFile(string filePath) {
            return FromJson(System.IO.File.ReadAllText(filePath));
        }
        public static T FromJson(string json) {
            return JsonConvert.DeserializeObject<T>(json);
        }

    }

}
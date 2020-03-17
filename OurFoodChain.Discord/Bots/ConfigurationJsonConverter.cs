using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OurFoodChain.Discord.Bots {

    public class ConfigurationJsonConverter :
        JsonConverter {

        public override bool CanConvert(Type objectType) {

            return objectType.IsClass;

        }
        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {

            // Underscores and casing should be ignored when mapping keys to properties.

            object instance = objectType.GetConstructor(Type.EmptyTypes).Invoke(null);
            PropertyInfo[] properties = objectType.GetProperties();

            JObject jObject = JObject.Load(reader);

            foreach (JProperty jProperty in jObject.Properties()) {

                string propertyName = jProperty.Name.Replace("_", string.Empty);
                PropertyInfo propertyInfo = properties.FirstOrDefault(property => property.CanWrite && string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase));

                if (propertyInfo != null)
                    propertyInfo.SetValue(instance, jProperty.Value.ToObject(propertyInfo.PropertyType, serializer));

            }

            return instance;

        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {

            throw new NotImplementedException();

        }

    }

}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public abstract class ConfigBase<T> where T : ConfigBase<T> {

        // Public members

        public static T FromFile(string filePath) {
            return FromJson(System.IO.File.ReadAllText(filePath));
        }
        public static T FromJson(string json) {
            return JsonConvert.DeserializeObject<T>(json);
        }
        public void Save(string filePath) {

            System.IO.File.WriteAllText(filePath, JsonConvert.SerializeObject(GetDerived(), Formatting.Indented));

        }

        public PropertyT GetProperty<PropertyT>(string name, PropertyT defaultValue) {

            PropertyInfo property = GetPropertyByJsonPropertyAttribute(name);

            if (property != null)
                return (PropertyT)Convert.ChangeType(property.GetValue(GetDerived()), typeof(PropertyT));
            else
                return defaultValue;

        }
        public bool SetProperty(string name, string value) {

            PropertyInfo property = GetPropertyByJsonPropertyAttribute(name);

            if (property != null)
                property.SetValue(this, Convert.ChangeType(value, property.PropertyType), null);
            else
                return false;

            return true;

        }

        // Private members

        private T GetDerived() {

            return (T)this;

        }

        private PropertyInfo GetPropertyByJsonPropertyAttribute(string jsonPropertyName) {

            return typeof(T).GetProperties().FirstOrDefault(x => {

                return Attribute.GetCustomAttribute(x, typeof(JsonPropertyAttribute)) is JsonPropertyAttribute attribute && string.Equals(attribute.PropertyName, jsonPropertyName, StringComparison.OrdinalIgnoreCase);

            });

        }

    }

}
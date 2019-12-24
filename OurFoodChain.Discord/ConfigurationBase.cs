using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OurFoodChain.Discord {

    public abstract class ConfigurationBase :
        IConfiguration {

        // Public members

        public PropertyT GetProperty<PropertyT>(string name) {

            PropertyInfo property = GetPropertyByJsonPropertyAttribute(name);

            return (PropertyT)Convert.ChangeType(property.GetValue(this), typeof(PropertyT));

        }
        public PropertyT GetProperty<PropertyT>(string name, PropertyT defaultValue) {

            PropertyInfo property = GetPropertyByJsonPropertyAttribute(name);

            if (property != null)
                return (PropertyT)Convert.ChangeType(property.GetValue(this), typeof(PropertyT));
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

        public void Save(string filePath) {

            System.IO.File.WriteAllText(filePath, JsonConvert.SerializeObject(this, Formatting.Indented));

        }

        public static T Open<T>(string filePath) {

            return Parse<T>(System.IO.File.ReadAllText(filePath));

        }
        public static T Parse<T>(string json) {

            return JsonConvert.DeserializeObject<T>(json);

        }

        // Private members

        private PropertyInfo GetPropertyByJsonPropertyAttribute(string jsonPropertyName) {

            return GetType().GetProperties().FirstOrDefault(p => {

                JsonPropertyAttribute attribute = Attribute.GetCustomAttribute(p, typeof(JsonPropertyAttribute)) as JsonPropertyAttribute;

                return attribute != null && string.Equals(attribute.PropertyName, jsonPropertyName, StringComparison.OrdinalIgnoreCase);

            });

        }

    }

}
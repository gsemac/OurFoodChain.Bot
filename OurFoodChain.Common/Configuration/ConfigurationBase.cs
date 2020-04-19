using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OurFoodChain.Common.Configuration {

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

            if (property != null) {

                object convertedValue;

                if (property.PropertyType.IsEnum)
                    convertedValue = Enum.Parse(property.PropertyType, value, true);
                else
                    convertedValue = Convert.ChangeType(value, property.PropertyType);

                property.SetValue(this, convertedValue, null);

            }
            else
                return false;

            return true;

        }

        public void Save(string filePath) {

            System.IO.File.WriteAllText(filePath, JsonConvert.SerializeObject(this, new JsonSerializerSettings() {
                ContractResolver = new DefaultContractResolver() {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
                Formatting = Formatting.Indented
            }));

        }

        // Private members

        private PropertyInfo GetPropertyByJsonPropertyAttribute(string jsonPropertyName) {

            // Underscores and casing are ignored when resolving properties.

            jsonPropertyName = jsonPropertyName.Replace("_", string.Empty);

            return GetType().GetProperties().FirstOrDefault(property => {

                JsonPropertyAttribute attribute = Attribute.GetCustomAttribute(property, typeof(JsonPropertyAttribute)) as JsonPropertyAttribute;

                string attributePropertyName = attribute?.PropertyName;
                string propertyName = property.Name;

                return string.Equals(attributePropertyName, jsonPropertyName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(propertyName, jsonPropertyName, StringComparison.OrdinalIgnoreCase);

            });

        }

    }

}
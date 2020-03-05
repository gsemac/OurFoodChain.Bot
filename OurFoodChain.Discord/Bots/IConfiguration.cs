using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord.Bots {

    public interface IConfiguration {

        void Save(string filePath);

        PropertyT GetProperty<PropertyT>(string name);
        PropertyT GetProperty<PropertyT>(string name, PropertyT defaultValue);
        bool SetProperty(string name, string value);

    }

}
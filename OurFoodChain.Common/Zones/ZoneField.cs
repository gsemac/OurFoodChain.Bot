using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Zones {

    public class ZoneField :
        IZoneField {

        public string Name { get; set; }
        public string Value { get; set; }

        public ZoneField(string name, string value) {

            this.Name = name;
            this.Value = value;

        }

    }

}
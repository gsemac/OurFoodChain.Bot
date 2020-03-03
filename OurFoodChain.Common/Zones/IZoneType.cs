using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace OurFoodChain.Common.Zones {

    public interface IZoneType {

        long? Id { get; set; }

        string Icon { get; set; }
        string Name { get; set; }
        string Description { get; set; }

        Color Color { get; set; }

    }

}
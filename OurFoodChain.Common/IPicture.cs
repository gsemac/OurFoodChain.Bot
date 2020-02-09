using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common {

    public interface IPicture {

        long? Id { get; set; }
        long? GalleryId { get; set; }

        string Url { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        ICreator Artist { get; set; }
        string Caption { get; set; }

    }

}
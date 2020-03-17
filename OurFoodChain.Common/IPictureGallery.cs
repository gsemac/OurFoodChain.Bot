using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common {

    public interface IPictureGallery :
        IEnumerable<IPicture> {

        long? Id { get; set; }
        string Name { get; set; }
        ICollection<IPicture> Pictures { get; }

    }

}
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common {

    public interface ICreator {

        ulong UserId { get; set; }
        string Name { get; set; }

    }

}
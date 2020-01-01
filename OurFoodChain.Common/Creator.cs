using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common {

    public class Creator :
        ICreator {

        public ulong UserId { get; set; }
        public string Name { get; set; }

    }

}
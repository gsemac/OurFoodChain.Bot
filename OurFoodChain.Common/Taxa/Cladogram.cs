using OurFoodChain.Common.Collections;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Taxa {

    public class Cladogram :
        ICladogram {

        public CladogramNode Root { get; }

        public Cladogram(CladogramNode root) {

            this.Root = root;

        }

    }

}
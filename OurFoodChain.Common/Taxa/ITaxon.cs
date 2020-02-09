using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Taxa {

    public interface ITaxon {

        long? Id { get; set; }
        long? ParentId { get; set; }
        ITaxonRank Rank { get; }
        string Name { get; set; }

    }

}
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Taxa {

    public interface ITaxon {

        int? Id { get; set; }
        int? ParentId { get; set; }
        ITaxonRank Rank { get; }
        string Name { get; set; }

    }

}
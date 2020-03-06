using OurFoodChain.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Taxa {

    public abstract class TaxonBase :
        ITaxon {

        // Public members

        public virtual long? Id { get; set; }
        public virtual long? ParentId { get; set; }
        public virtual ITaxonRank Rank { get; protected set; }
        public virtual string Name { get; set; }
        public ICollection<string> CommonNames { get; } = new List<string>();
        public virtual string Description { get; set; }
        public ICollection<IPicture> Pictures { get; } = new List<IPicture>();

        public override string ToString() {

            return Name.ToTitle();

        }

    }

}
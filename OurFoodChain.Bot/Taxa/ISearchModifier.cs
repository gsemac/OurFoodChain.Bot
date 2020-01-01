using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Taxa {

    public enum SearchResultDisplayFormat {

        Unknown = 0,

        None,

        FullName,
        ShortName,
        CommonName,
        SpeciesOnly,
        Gallery,
        Leaderboard

    }

    public enum SearchResultOrdering {

        Unknown = 0,

        Default,

        Newest,
        Oldest,
        Smallest,
        Largest,
        Count

    }

    public enum SearchResultGrouping {

        Unknown = 0,

        Zone,
        Genus,
        Family,
        Order,
        Class,
        Phylum,
        Kingdom,
        Domain,
        Creator,
        Status,
        Role,
        Generation

    }

    public enum SearchModifierType {

        Unknown = 0,

        /// <summary>
        /// Groups search results on a given attribute.
        /// </summary>
        GroupBy,
        OrderBy,
        Zone,
        Role,
        Format,
        Creator,
        Status,
        Species,
        Genus,
        Family,
        Order,
        Class,
        Phylum,
        Kingdom,
        Domain,
        Taxon,
        Random,
        Prey,
        PreyNotes,
        Predator,
        Has,
        Ancestor,
        Descendant,
        Limit,
        Artist,
        Generation

    }

    public interface ISearchModifier {

        string Name { get; }
        string Value { get; }
        bool Subtractive { get; }
        SearchModifierType Type { get; }

    }

}
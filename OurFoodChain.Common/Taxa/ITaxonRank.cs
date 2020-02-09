using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Taxa {

    // https://en.wikipedia.org/wiki/Taxonomic_rank#All_ranks

    public enum TaxonRankType {

        None = 0,

        Domain,
        Kingdom,
        Phylum,
        Class,
        Division,
        Legion,
        Cohort,
        Order,
        Section,
        Family,
        Tribe,
        Genus,
        Species,
        Morph,

        Custom

    }

    public enum TaxonPrefix {

        None = 0,

        Hyper,
        Giga,
        Magn,
        Grand,
        Miro,
        Super,
        Sub,
        Infra,
        Parv,
        Micro,
        Nano,
        Hypo,
        Min,
        SubOrder,
        InfraOrder

    }

    public interface ITaxonRank :
          IComparable<ITaxonRank> {

        TaxonRankType Type { get; }
        string Name { get; }
        string PluralName { get; }
        TaxonPrefix Prefix { get; }
        string Suffix { get; }

    }

}
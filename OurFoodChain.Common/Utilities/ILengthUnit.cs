using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Utilities {

    public enum LengthUnitType {
        Unknown,
        Metric,
        Imperial
    }

    public interface ILengthUnit {

        string Name { get; }
        string Abbreviation { get; }
        double MeterConversionFactor { get; }
        LengthUnitType Type { get; }

        ILengthUnit ToNearestUnits(LengthUnitType type, params string[] allowedUnits);

    }

}
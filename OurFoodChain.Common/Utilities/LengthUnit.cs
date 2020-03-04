using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Utilities {

    public class LengthUnit :
        ILengthUnit {

        public string Name { get; }
        public string Abbreviation { get; }
        public double MeterConversionFactor { get; }
        public LengthUnitType Type { get; } = LengthUnitType.Unknown;

        public LengthUnit(string name, string abbreviation, double meterConversionFactor) :
            this(name, abbreviation, meterConversionFactor, LengthUnitType.Unknown) {
        }
        public LengthUnit(string name, string abbreviation, double meterConversionFactor, LengthUnitType type) {

            this.Name = name;
            this.Abbreviation = abbreviation;
            this.MeterConversionFactor = meterConversionFactor;
            this.Type = type;

        }

        public override string ToString() {

            return Abbreviation;

        }

    }

}
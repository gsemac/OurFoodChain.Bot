using OurFoodChain.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OurFoodChain.Common.Utilities {

    public enum LengthMeasurementFormat {
        Value,
        ValueAndUnits
    }

    public class LengthMeasurement {

        // Public members

        public double Value { get; set; } = 0.0;
        public ILengthUnit Units { get; set; }

        public LengthMeasurement() : this(0.0, "") { }
        public LengthMeasurement(double value, string units) :
            this(value, LengthUnit.Parse(units)) {
        }
        public LengthMeasurement(double value, ILengthUnit units) {

            this.Value = value;
            this.Units = units;

        }

        public double ToMeters() {

            return Value * Units.MeterConversionFactor;

        }
        public LengthMeasurement ToNearestUnits(LengthUnitType type, params string[] allowedUnits) {

            if (Units.Type == type)
                return new LengthMeasurement(Value, Units);

            // Find the unit with the closest conversion factor to ours.

            ILengthUnit unit = Units.ToNearestUnits(type, allowedUnits);

            return ConvertTo(unit);

        }

        public LengthMeasurement ConvertTo(ILengthUnit units) {

            if (units.Name.Equals(Units.Name, StringComparison.OrdinalIgnoreCase))
                return new LengthMeasurement(Value, Units);

            return new LengthMeasurement(ToMeters() / units.MeterConversionFactor, units);

        }
        public LengthMeasurement ConvertTo(string units) {

            if (units.Equals(Units.Name, StringComparison.OrdinalIgnoreCase))
                return new LengthMeasurement(Value, Units);

            return ConvertTo(LengthUnit.Parse(units));

        }

        public override string ToString() {

            return ToString(LengthMeasurementFormat.ValueAndUnits);

        }
        public string ToString(LengthMeasurementFormat format) {

            switch (format) {

                case LengthMeasurementFormat.Value:
                    return FormatValue();

                default:
                    return string.Format("{0} {1}", FormatValue(), FormatUnits());

            }

        }

        // Private members

        private string FormatValue() {

            if (Value < 0.1) {

                string formatted = Value.ToString("0.0e+0", System.Globalization.CultureInfo.InvariantCulture);
                Match match = Regex.Match(formatted, @"([\d.]+)e([+\-])([\d.]+)");

                return string.Format("{0} × 10{1}{2}",
                   match.Groups[1].Value,
                   match.Groups[2].Value == "-" ? "⁻" : "⁺",
                   ConvertDigitsToExponents(match.Groups[3].Value));

            }
            else
                return Value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

        }
        private string FormatUnits() {

            return Units.Abbreviation;

        }
        private string ConvertDigitsToExponents(string digits) {

            return Regex.Replace(digits, @"\d", m => {

                switch (m.Value) {

                    case "0":
                        return "⁰";
                    case "1":
                        return "¹";
                    case "2":
                        return "²";
                    case "3":
                        return "³";
                    case "4":
                        return "⁴";
                    case "5":
                        return "⁵";
                    case "6":
                        return "⁶";
                    case "7":
                        return "⁷";
                    case "8":
                        return "⁸";
                    case "9":
                        return "⁹";
                    case ".":
                        return "˙";
                    default:
                        return "?";

                }

            });

        }

    }

}
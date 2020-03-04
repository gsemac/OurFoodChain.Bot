using OurFoodChain.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OurFoodChain.Common.Utilities {

    public class LengthMeasurement {

        // Public members

        public double Value { get; set; } = 0.0;
        public ILengthUnit Units { get; set; }

        public LengthMeasurement() : this(0.0, "") { }
        public LengthMeasurement(double value, string units) :
            this(value, GetKnownUnit(units)) {
        }
        public LengthMeasurement(double value, ILengthUnit units) {

            this.Value = value;
            this.Units = units;

        }

        public double ToMeters() {

            return Value * Units.MeterConversionFactor;

        }
        public LengthMeasurement ToNearestUnits(LengthUnitType type) {

            if (Units.Type == type)
                return new LengthMeasurement(Value, Units);

            // Find the unit with the closest conversion factor to ours.

            ILengthUnit unit = knownUnits.Value.Values
                .Where(u => u.Type == type)
                .OrderBy(u => Math.Abs(Units.MeterConversionFactor - u.MeterConversionFactor))
                .First();

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

            return ConvertTo(GetKnownUnit(units));

        }

        public override string ToString() {

            return string.Format("{0} {1}", FormatValue(), FormatUnits());

        }

        // Private members

        private static readonly Lazy<IDictionary<string, ILengthUnit>> knownUnits = new Lazy<IDictionary<string, ILengthUnit>>(() => RegisterKnownUnits());

        private static IDictionary<string, ILengthUnit> RegisterKnownUnits() {

            IDictionary<string, ILengthUnit> knownUnits = new Dictionary<string, ILengthUnit>();

            RegisterUnit(knownUnits, new LengthUnit("yoctometer", "ym", 1.0 / Math.Pow(10, 24), LengthUnitType.Metric));
            RegisterUnit(knownUnits, new LengthUnit("zeptometer", "zm", 1.0 / Math.Pow(10, 21), LengthUnitType.Metric));
            RegisterUnit(knownUnits, new LengthUnit("attometer", "am", 1.0 / Math.Pow(10, 18), LengthUnitType.Metric));
            RegisterUnit(knownUnits, new LengthUnit("femtometer", "fm", 1.0 / Math.Pow(10, 15), LengthUnitType.Metric));
            RegisterUnit(knownUnits, new LengthUnit("picometer", "pm", 1.0 / Math.Pow(10, 12), LengthUnitType.Metric));
            RegisterUnit(knownUnits, new LengthUnit("nanometer", "nm", 1.0 / 1000000000.0, LengthUnitType.Metric));
            RegisterUnit(knownUnits, new LengthUnit("micrometer", "μm", 1.0 / 1000000.0, LengthUnitType.Metric));
            RegisterUnit(knownUnits, new LengthUnit("millimeter", "mm", 1.0 / 1000.0, LengthUnitType.Metric));
            RegisterUnit(knownUnits, new LengthUnit("centimeter", "cm", 1.0 / 100.0, LengthUnitType.Metric));
            RegisterUnit(knownUnits, new LengthUnit("decimeter", "dm", 1.0 / 10.0, LengthUnitType.Metric));
            RegisterUnit(knownUnits, new LengthUnit("meter", "m", 1.0, LengthUnitType.Metric));
            RegisterUnit(knownUnits, new LengthUnit("dekameter", "dam", Math.Pow(10, 1), LengthUnitType.Metric));
            RegisterUnit(knownUnits, new LengthUnit("hectometer", "hm", Math.Pow(10, 2), LengthUnitType.Metric));
            RegisterUnit(knownUnits, new LengthUnit("kilometer", "km", Math.Pow(10, 3), LengthUnitType.Metric));
            RegisterUnit(knownUnits, new LengthUnit("megameter", "Mm", Math.Pow(10, 6), LengthUnitType.Metric));
            RegisterUnit(knownUnits, new LengthUnit("gigameter", "Gm", Math.Pow(10, 9), LengthUnitType.Metric));
            RegisterUnit(knownUnits, new LengthUnit("terameter", "Tm", Math.Pow(10, 12), LengthUnitType.Metric));
            RegisterUnit(knownUnits, new LengthUnit("petameter", "Pm", Math.Pow(10, 15), LengthUnitType.Metric));
            RegisterUnit(knownUnits, new LengthUnit("exameter", "Em", Math.Pow(10, 18), LengthUnitType.Metric));
            RegisterUnit(knownUnits, new LengthUnit("zettameter", "Zm", Math.Pow(10, 21), LengthUnitType.Metric));
            RegisterUnit(knownUnits, new LengthUnit("yottameter", "Ym", Math.Pow(10, 24), LengthUnitType.Metric));

            RegisterUnit(knownUnits, new LengthUnit("inch", "in", 0.0254, LengthUnitType.Imperial));
            RegisterUnit(knownUnits, new LengthUnit("hand", "h", 0.1016, LengthUnitType.Imperial));
            RegisterUnit(knownUnits, new LengthUnit("feet", "ft", 0.3048, LengthUnitType.Imperial));
            RegisterUnit(knownUnits, new LengthUnit("yard", "yd", 0.9144, LengthUnitType.Imperial));
            RegisterUnit(knownUnits, new LengthUnit("fathom", "fathoms", 1.8288, LengthUnitType.Imperial));
            RegisterUnit(knownUnits, new LengthUnit("perch", "perches", 5.0292, LengthUnitType.Imperial));
            RegisterUnit(knownUnits, new LengthUnit("pole", "poles", 5.0292, LengthUnitType.Imperial));
            RegisterUnit(knownUnits, new LengthUnit("rod", "rods", 5.0292, LengthUnitType.Imperial));
            RegisterUnit(knownUnits, new LengthUnit("chain", "chains", 20.1168, LengthUnitType.Imperial));
            RegisterUnit(knownUnits, new LengthUnit("furlong", "furlongs", 201.168, LengthUnitType.Imperial));
            RegisterUnit(knownUnits, new LengthUnit("mile", "mi", 1609.34, LengthUnitType.Imperial));
            RegisterUnit(knownUnits, new LengthUnit("league", "leagues", 5556, LengthUnitType.Imperial));

            RegisterUnit(knownUnits, new LengthUnit("ångström", "Å", 1.0 / Math.Pow(10, 10)));
            RegisterUnit(knownUnits, new LengthUnit("angstrom", "Å", 1.0 / Math.Pow(10, 10)));
            RegisterUnit(knownUnits, new LengthUnit("nautical mile", "nmi", 1852));
            RegisterUnit(knownUnits, new LengthUnit("light year", "ly", 9.461 * Math.Pow(10, 15)));
            RegisterUnit(knownUnits, new LengthUnit("astronomical unit", "AU", 1.496 * Math.Pow(10, 11)));
            RegisterUnit(knownUnits, new LengthUnit("planck length", "ℓP", 1.6 / Math.Pow(10, 35)));
            RegisterUnit(knownUnits, new LengthUnit("planck", "ℓP", 1.6 / Math.Pow(10, 35)));

            return knownUnits;

        }
        private static void RegisterUnit(IDictionary<string, ILengthUnit> dictionary, ILengthUnit unit) {

            if (!string.IsNullOrEmpty(unit.Name)) {

                dictionary.Add(unit.Name.ToLowerInvariant().Replace("-", " "), unit);
                dictionary.Add(ToPlural(unit.Name).ToLowerInvariant().Replace("-", " "), unit);

            }

            if (!string.IsNullOrEmpty(unit.Abbreviation))
                dictionary.Add(unit.Abbreviation, unit);

        }
        private static ILengthUnit GetKnownUnit(string name) {

            name = name.ToLowerInvariant();
            name = Regex.Replace(name, @"metre(s?)$", "meter$1");
            name = name.Replace("-", " ");

            if (knownUnits.Value.TryGetValue(name, out ILengthUnit value))
                return value;
            else
                throw new ArgumentException(nameof(name));

        }
        private static string ToPlural(string name) {

            name = name.ToLowerInvariant();

            if (name.Equals("foot"))
                return "feet";
            else if (name.EndsWith("h")) // inch, perch
                return name + "es";
            else
                return name + "s";

        }

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
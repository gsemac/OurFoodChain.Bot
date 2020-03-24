using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OurFoodChain.Common.Utilities {

    public class LengthUnit :
        ILengthUnit {

        // Public members

        public string Name { get; }
        public string Abbreviation { get; }
        public double MeterConversionFactor { get; }
        public LengthUnitType Type { get; } = LengthUnitType.Unknown;

        public LengthUnit(string nameOrAbbreviation) :
            this(GetKnownUnitOrThrow(nameOrAbbreviation)) {
        }
        public LengthUnit(ILengthUnit other) :
            this(other.Name, other.Abbreviation, other.MeterConversionFactor, other.Type) {
        }
        public LengthUnit(string name, string abbreviation, double meterConversionFactor) :
            this(name, abbreviation, meterConversionFactor, LengthUnitType.Unknown) {
        }
        public LengthUnit(string name, string abbreviation, double meterConversionFactor, LengthUnitType type) {

            this.Name = name;
            this.Abbreviation = abbreviation;
            this.MeterConversionFactor = meterConversionFactor;
            this.Type = type;

        }

        public override bool Equals(object other) {

            if (other is string unit) {

                return Name.Equals(unit, StringComparison.OrdinalIgnoreCase) ||
                    ToPlural(Name).Equals(unit, StringComparison.OrdinalIgnoreCase) ||
                    Abbreviation.Equals(unit, StringComparison.OrdinalIgnoreCase);

            }
            else
                return this == other;

        }

        public ILengthUnit ToNearestUnits(LengthUnitType type, params string[] allowedUnits) {

            if (Type == type)
                return new LengthUnit(this);

            // Find the unit with the closest conversion factor to ours.

            ILengthUnit result = knownUnits.Value.Values
                .Where(unit => unit.Type == type)
                .Where(unit => allowedUnits.Count() <= 0 || allowedUnits.Any(allowedUnit => unit.Equals(allowedUnit)))
                .OrderBy(unitu => Math.Abs(MeterConversionFactor - unitu.MeterConversionFactor))
                .FirstOrDefault();

            return result;

        }

        public override string ToString() {

            return Abbreviation;

        }

        public static ILengthUnit Parse(string input) {

            if (TryParse(input, out ILengthUnit result))
                return result;

            throw new ArgumentException(nameof(input));

        }
        public static bool TryParse(string input, out ILengthUnit result) {

            result = GetKnownUnit(input);

            return result != null;

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
            RegisterUnit(knownUnits, new LengthUnit("palm", "palms", 0.0762, LengthUnitType.Imperial));
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
            RegisterUnit(knownUnits, new LengthUnit("light second", "ls", 2.998 * Math.Pow(10, 8)));
            RegisterUnit(knownUnits, new LengthUnit("light minute", "lm", 1.799 * Math.Pow(10, 10)));
            RegisterUnit(knownUnits, new LengthUnit("light year", "ly", 9.461 * Math.Pow(10, 15)));
            RegisterUnit(knownUnits, new LengthUnit("astronomical unit", "AU", 1.496 * Math.Pow(10, 11)));
            RegisterUnit(knownUnits, new LengthUnit("planck length", "ℓP", 1.6 / Math.Pow(10, 35)));
            RegisterUnit(knownUnits, new LengthUnit("planck", "ℓP", 1.6 / Math.Pow(10, 35)));
            RegisterUnit(knownUnits, new LengthUnit("attoparsec", "apc", 0.0308568));
            RegisterUnit(knownUnits, new LengthUnit("parsec", "pc", 3.086 * Math.Pow(10, 16)));
            RegisterUnit(knownUnits, new LengthUnit("megaparsec", "mpc", 3.086 * Math.Pow(10, 22)));
            RegisterUnit(knownUnits, new LengthUnit("gigaparsec", "gpc", 3.086 * Math.Pow(10, 25)));
            RegisterUnit(knownUnits, new LengthUnit("lunar distance", "LD", 3.843e+8));
            RegisterUnit(knownUnits, new LengthUnit("barleycorn", "barleycorns", 1.0 / 118.0));
            RegisterUnit(knownUnits, new LengthUnit("link", "l.", 1.0 / 4.975));
            RegisterUnit(knownUnits, new LengthUnit("american football field", "football fields", 91.44));
            RegisterUnit(knownUnits, new LengthUnit("football field", "football fields", 91.44));

            RegisterUnit(knownUnits, new LengthUnit("pica", "pica", 1.0 / 236.0));
            RegisterUnit(knownUnits, new LengthUnit("point", "pt", 1.0 / 2835.0));

            RegisterUnit(knownUnits, new LengthUnit("smoot", "smoots", 1.702));
            RegisterUnit(knownUnits, new LengthUnit("moot", "moots", 1.70));
            RegisterUnit(knownUnits, new LengthUnit("altuve", "altuves", 1.65));
            RegisterUnit(knownUnits, new LengthUnit("beard second", "beard-seconds", 1.0 / 5e-9));
            RegisterUnit(knownUnits, new LengthUnit("beard-second", "beard-seconds", 1.0 / 5e-9));
            RegisterUnit(knownUnits, new LengthUnit("beer can", "beer cans", 0.0762));
            RegisterUnit(knownUnits, new LengthUnit("mickey", "mickeys", 1.27 * Math.Pow(10, -4)));
            RegisterUnit(knownUnits, new LengthUnit("bloit", "bloits", 1075.04179));
            RegisterUnit(knownUnits, new LengthUnit("sheppey", "Sheppeys", 1400));
            RegisterUnit(knownUnits, new LengthUnit("wiffle", "wiffles", 0.089));

            RegisterUnit(knownUnits, new LengthUnit("li", "里", 576.0));
            RegisterUnit(knownUnits, new LengthUnit("lǐ", "里", 576.0));
            RegisterUnit(knownUnits, new LengthUnit("shìlǐ", "市里", 576.0));
            RegisterUnit(knownUnits, new LengthUnit("yin", "引", 32.0));
            RegisterUnit(knownUnits, new LengthUnit("yǐn", "引", 32.0));
            RegisterUnit(knownUnits, new LengthUnit("zhang", "丈", 3.2));
            RegisterUnit(knownUnits, new LengthUnit("zhàng", "丈", 3.2));
            RegisterUnit(knownUnits, new LengthUnit("bu", "步", 1.6));
            RegisterUnit(knownUnits, new LengthUnit("bù", "步", 1.6));
            RegisterUnit(knownUnits, new LengthUnit("chi", "呎", 0.32));
            RegisterUnit(knownUnits, new LengthUnit("chǐ", "呎", 0.32));
            RegisterUnit(knownUnits, new LengthUnit("cun", "寸", 0.032));
            RegisterUnit(knownUnits, new LengthUnit("cùn", "寸", 0.032));
            RegisterUnit(knownUnits, new LengthUnit("fen", "分", 0.0032));
            RegisterUnit(knownUnits, new LengthUnit("fēn", "分", 0.0032));
            RegisterUnit(knownUnits, new LengthUnit("lí", "釐", 0.00032));
            RegisterUnit(knownUnits, new LengthUnit("lí", "厘", 0.00032));
            RegisterUnit(knownUnits, new LengthUnit("hao", "毫", 0.00032));
            RegisterUnit(knownUnits, new LengthUnit("háo", "毫", 3.2e-5));

            // Also map the lowercase version of each unit abbreviation to the unit.
            // This is done last to avoid problems with conflicting units (e.g. "mm" and "Mm").

            foreach (ILengthUnit unit in knownUnits.Values.ToArray())
                if (!knownUnits.Keys.Contains(unit.Abbreviation.ToLowerInvariant()))
                    knownUnits.Add(unit.Abbreviation.ToLowerInvariant(), unit);

            return knownUnits;

        }
        private static void RegisterUnit(IDictionary<string, ILengthUnit> dictionary, ILengthUnit unit) {

            if (!string.IsNullOrEmpty(unit.Name)) {

                string pluralName = ToPlural(unit.Name).Replace("-", " ");

                dictionary[unit.Name.ToLowerInvariant().Replace("-", " ")] = unit;
                dictionary[pluralName] = unit;

            }

            dictionary[unit.Abbreviation] = unit;

        }
        private static ILengthUnit GetKnownUnit(string name) {

            name = Regex.Replace(name, @"metre(s?)$", "meter$1", RegexOptions.IgnoreCase);
            name = name.Replace("-", " ");

            ILengthUnit result;

            if (knownUnits.Value.TryGetValue(name, out result) || knownUnits.Value.TryGetValue(name.ToLowerInvariant(), out result))
                return result;

            return null;

        }
        private static ILengthUnit GetKnownUnitOrThrow(string name) {

            ILengthUnit knownUnit = GetKnownUnit(name);

            if (knownUnit is null)
                throw new ArgumentException("Unrecognized units.");

            return knownUnit;

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

    }

}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain {

    public enum LengthUnit {

        Unknown = 0,

        Nanometers,
        Micrometers,
        Millimeters,
        Centimeters,
        Meters,

        METRIC_END,

        Inches,
        Feet,

        IMPERIAL_END

    }

    public class Length {

        public Length() {

            Value = 0.0;
            Units = LengthUnit.Unknown;

        }
        public Length(double value, string units) {

            Value = value;
            Units = _parseUnits(units);

        }
        public Length(double value, LengthUnit units) {

            Value = value;
            Units = units;

        }

        public double Value { get; set; } = 0.0;
        public LengthUnit Units { get; set; } = 0;
        public string ValueString {
            get {
                return _formatValue();
            }
        }
        public string UnitsString {
            get {
                return _unitsToString();
            }
        }

        public double ToNanometers() {

            if (Units == LengthUnit.Nanometers)
                return Value;

            return ToMeters() * 1000000000.0;

        }
        public double ToMicrometers() {

            if (Units == LengthUnit.Micrometers)
                return Value;

            return ToMeters() * 1000000.0;

        }
        public double ToMillimeters() {

            if (Units == LengthUnit.Millimeters)
                return Value;

            return ToMeters() * 1000.0;

        }
        public double ToCentimeters() {

            if (Units == LengthUnit.Centimeters)
                return Value;

            return ToMeters() * 100.0;

        }
        public double ToMeters() {

            switch (Units) {

                case LengthUnit.Nanometers:
                    return Value / 1000000000.0;
                case LengthUnit.Micrometers:
                    return Value / 1000000.0;
                case LengthUnit.Millimeters:
                    return Value / 1000.0;
                case LengthUnit.Centimeters:
                    return Value / 100.0;
                case LengthUnit.Meters:
                    return Value;
                case LengthUnit.Feet:
                    return Value / 3.281;
                case LengthUnit.Inches:
                    return Value / 39.37;
                default:
                    return Value;

            }

        }

        public double ToFeet() {

            if (Units == LengthUnit.Feet)
                return Value;

            return ToMeters() * 3.281;

        }
        public double ToInches() {

            if (Units == LengthUnit.Inches)
                return Value;

            return ToMeters() * 39.37;

        }

        public bool IsMetric() {
            return IsMetric(Units);
        }
        public bool IsMetric(LengthUnit units) {
            return units < LengthUnit.METRIC_END;
        }
        public bool IsImperial() {
            return IsImperial(Units);
        }
        public bool IsImperial(LengthUnit units) {
            return units > LengthUnit.METRIC_END && units < LengthUnit.IMPERIAL_END;
        }

        public Length ToNearestMetricUnits() {

            if (IsMetric())
                return new Length { Value = Value, Units = Units };

            if (Units >= LengthUnit.Feet)
                return new Length { Value = ToMeters(), Units = LengthUnit.Meters };
            else
                return new Length { Value = ToCentimeters(), Units = LengthUnit.Centimeters };

        }
        public Length ToNearestImperialUnits() {

            if (IsImperial())
                return new Length { Value = Value, Units = Units };

            if (Units >= LengthUnit.Meters)
                return new Length { Value = ToFeet(), Units = LengthUnit.Feet };
            else
                return new Length { Value = ToInches(), Units = LengthUnit.Inches };

        }
        public Length ConvertTo(LengthUnit units) {

            return new Length { Value = _convertValueTo(units), Units = units };

        }

        public override string ToString() {

            return string.Format("{0} {1}",
                ValueString,
                UnitsString);

        }

        private string _formatValue() {

            if (Value < 0.1) {

                string formatted = Value.ToString("0.0e+0", System.Globalization.CultureInfo.InvariantCulture);
                Match match = Regex.Match(formatted, @"([\d.]+)e([+\-])([\d.]+)");

                return string.Format("{0} × 10{1}{2}",
                   match.Groups[1].Value,
                   match.Groups[2].Value == "-" ? "⁻" : "⁺",
                   _digitsToUnicodeExponents(match.Groups[3].Value));

            }
            else
                return Value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

        }
        private string _digitsToUnicodeExponents(string digits) {

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
        private string _unitsToString() {

            switch (Units) {

                case LengthUnit.Nanometers:
                    return "nm";
                case LengthUnit.Micrometers:
                    return "μm";
                case LengthUnit.Millimeters:
                    return "mm";
                case LengthUnit.Centimeters:
                    return "cm";
                case LengthUnit.Meters:
                    return "m";

                case LengthUnit.Inches:
                    return "in";
                case LengthUnit.Feet:
                    return "ft";

                default:
                    return "units";

            }

        }
        private LengthUnit _parseUnits(string value) {

            LengthUnit units = LengthUnit.Unknown;

            switch (value.ToLower()) {

                case "nm":
                case "nanometer":
                case "nanometers":
                    units = LengthUnit.Nanometers;
                    break;

                case "μm":
                case "micrometer":
                case "micrometers":
                    units = LengthUnit.Micrometers;
                    break;

                case "mm":
                case "millimeter":
                case "millimeters":
                    units = LengthUnit.Millimeters;
                    break;

                case "cm":
                case "centimeter":
                case "centimeters":
                    units = LengthUnit.Centimeters;
                    break;

                case "m":
                case "meter":
                case "meters":
                    units = LengthUnit.Meters;
                    break;

                case "in":
                case "inch":
                case "inches":
                    units = LengthUnit.Inches;
                    break;

                case "ft":
                case "foot":
                case "feet":
                    units = LengthUnit.Feet;
                    break;

            }

            return units;

        }
        private double _convertValueTo(LengthUnit units) {

            switch (units) {

                case LengthUnit.Nanometers:
                    return ToNanometers();
                case LengthUnit.Micrometers:
                    return ToMicrometers();
                case LengthUnit.Millimeters:
                    return ToMillimeters();
                case LengthUnit.Centimeters:
                    return ToCentimeters();
                case LengthUnit.Meters:
                    return ToMeters();

                case LengthUnit.Feet:
                    return ToFeet();
                case LengthUnit.Inches:
                    return ToInches();

                default:
                    throw new ArgumentException("Invalid units");

            }

        }

    }

    public class SpeciesSizeMatch {

        public const string UNKNOWN_SIZE_STRING = "unknown";

        public static SpeciesSizeMatch Match(string input) {

            SpeciesSizeMatch result = new SpeciesSizeMatch();

            // The first pass will look for things that are most likely to be the size of the species, with explicit keywords located around the size.

            // April 7th, 2019: I added an optional opening parenthesis to the "pass 1" pattern to pick up the size from phrases like "growing to ten centimeters (10 cm)".

            string number_pattern = @"(\d+(?:\.\d+)?(?:\-\d+(?:\.\d+)?)?)";
            string units_pattern = "(in(?:ch|ches)?|ft|feet|foot|[nμmc]?m|(?:nano|micro|milli|centi)?meters?)";
            string pass_1_pattern = @"(?:get|being|are|grow(?:ing)? up to|grow(?:ing)? to|up to|size:|max(?:imum)? size (?:of|is))[\s\w]*?\(?" + number_pattern + @"[\s]*?" + units_pattern + @"\b";

            Regex pass_1 = new Regex(pass_1_pattern, RegexOptions.IgnoreCase);
            Regex pass_2 = new Regex(number_pattern + @"[\s]*?" + units_pattern + @"\b", RegexOptions.IgnoreCase);

            // Before we make our passes, remove things that might be false positives (i.e. size of young, depth of burrows).

            input = Regex.Replace(input, @"(?:hatchlings|young|babies|eggs)\s*" + pass_1_pattern, string.Empty, RegexOptions.IgnoreCase); // "Young grow up to..."
            input = Regex.Replace(input, units_pattern + @"\s*(?:deep|in depth)", string.Empty, RegexOptions.IgnoreCase); // "12 inches deep..."
            input = Regex.Replace(input, @"zone\s*\d+", string.Empty, RegexOptions.IgnoreCase); // get rid of zone information ("Zone 3 in...")

            foreach (Regex r in new Regex[] { pass_1, pass_2 }) {

                MatchCollection matches = r.Matches(input);

                if (matches.Count > 0) {

                    Match best_match = matches[0];

                    double.TryParse(Regex.Match(best_match.Groups[1].Value, @"[\d\.]+").Value, out double max_size);

                    // If there's more than one match, pick the largest (in case other sizes are discussing the young).

                    foreach (Match m in matches) {

                        double.TryParse(Regex.Match(best_match.Groups[1].Value, @"[\d\.]+").Value, out double m_size);

                        if (m_size > max_size) {

                            max_size = m_size;
                            best_match = m;

                        }

                    }

                    string size_str = best_match.Groups[1].Value;
                    string units_str = best_match.Groups[2].Value.ToLower();

                    result._parseSizeString(size_str);
                    result._units = new Length(0.0, units_str).Units;

                    break;

                }

            }

            return result;

        }

        public override string ToString() {

            if (_min_size <= 0.0 && _max_size <= 0.0 || (_units == LengthUnit.Unknown))
                return UNKNOWN_SIZE_STRING;

            if (_min_size != _max_size) {

                // Represent the size as a range.

                Length min_size = new Length(_min_size, _units).ToNearestMetricUnits();
                Length max_size = new Length(_max_size, _units).ToNearestMetricUnits();

                return string.Format("**{0}-{1} {2}** ({3}-{4} {5})",
                    min_size.ValueString,
                    max_size.ValueString,
                    min_size.UnitsString,
                    min_size.ToNearestImperialUnits().ValueString,
                    max_size.ToNearestImperialUnits().ValueString,
                    max_size.ToNearestImperialUnits().UnitsString);

            }
            else {

                // Represent the size as a single value.

                Length size = new Length(_min_size <= 0.0 ? _max_size : _min_size, _units).ToNearestMetricUnits();

                return string.Format("**{0}** ({1})",
                    size.ToString(),
                    size.ToNearestImperialUnits().ToString());

            }

        }
        public string ToString(LengthUnit units) {

            if (_min_size <= 0.0 && _max_size <= 0.0 || (_units == LengthUnit.Unknown))
                return UNKNOWN_SIZE_STRING;

            if (_min_size != _max_size) {

                // Represent the size as a range.

                Length min_size = new Length(_min_size, _units).ConvertTo(units);
                Length max_size = new Length(_max_size, _units).ConvertTo(units);

                return string.Format("**{0}-{1} {2}**",
                    min_size.ValueString,
                    max_size.ValueString,
                    min_size.UnitsString);

            }
            else {

                // Represent the size as a single value.

                Length size = new Length(_min_size <= 0.0 ? _max_size : _min_size, _units).ConvertTo(units);

                return string.Format("**{0}**", size.ToString());

            }

        }

        public Length MinSize {
            get {
                return new Length(_min_size, _units);
            }
        }
        public Length MaxSize {
            get {
                return new Length(_max_size, _units);
            }
        }

        private double _min_size = 0.0;
        private double _max_size = 0.0;
        private LengthUnit _units = 0;

        private void _parseSizeString(string value) {

            if (value.Contains("-"))
                _parseSizeRangeString(value);
            else {

                if (double.TryParse(value, out double size)) {

                    _min_size = size;
                    _max_size = size;

                }

            }

        }
        private void _parseSizeRangeString(string value) {

            Match m = Regex.Match(value, @"([\d\.]+)\-([\d\.]+)");

            string min_str = m.Groups[1].Value;
            string max_str = m.Groups[2].Value;

            if (double.TryParse(min_str, out double min) && double.TryParse(max_str, out double max)) {

                _min_size = min;
                _max_size = max;

            }

        }

    }

}
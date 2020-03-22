using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OurFoodChain.Common.Utilities {

    public class SpeciesSizeMatch {

        // Public members

        public const string UnknownSize = "unknown";

        public bool Success => units != null;
        public LengthMeasurement MinSize => units is null ? new LengthMeasurement(0, "m") : new LengthMeasurement(minSize, units);
        public LengthMeasurement MaxSize => units is null ? new LengthMeasurement(0, "m") : new LengthMeasurement(maxSize, units);

        public static SpeciesSizeMatch Match(string input) {

            SpeciesSizeMatch result = new SpeciesSizeMatch();

            // The first pass will look for things that are most likely to be the size of the species, with explicit keywords located around the size.

            // April 7th, 2019: I added an optional opening parenthesis to the "pass 1" pattern to pick up the size from phrases like "growing to ten centimeters (10 cm)".

            string numberPattern = @"(\d+(?:\.\d+)?(?:\-\d+(?:\.\d+)?)?)";
            string unitsPattern = "(in(?:ch|ches)?|ft|feet|foot|[nμmc]?m|(?:nano|micro|milli|centi)?met(?:er|re)s?)";
            string pass1Pattern = @"(?:get|being|are|grow(?:ing)? up to|grow(?:ing)? to|up to|size:|max(?:imum)? size (?:of|is))[\s\w]*?\(?" + numberPattern + @"[\s]*?" + unitsPattern + @"\b";

            Regex pass1 = new Regex(pass1Pattern, RegexOptions.IgnoreCase);
            Regex pass2 = new Regex(numberPattern + @"[\s]*?" + unitsPattern + @"\b", RegexOptions.IgnoreCase);

            // Before we make our passes, remove things that might be false positives (i.e. size of young, depth of burrows).

            input = Regex.Replace(input, @"(?:hatchlings|young|babies|eggs)\s*" + pass1Pattern, string.Empty, RegexOptions.IgnoreCase); // "Young grow up to..."
            input = Regex.Replace(input, unitsPattern + @"\s*(?:deep|in depth)", string.Empty, RegexOptions.IgnoreCase); // "12 inches deep..."
            input = Regex.Replace(input, @"zone\s*\d+", string.Empty, RegexOptions.IgnoreCase); // get rid of zone information ("Zone 3 in...")

            foreach (Regex r in new Regex[] { pass1, pass2 }) {

                MatchCollection matches = r.Matches(input);

                if (matches.Count > 0) {

                    Match bestMatch = matches[0];

                    double.TryParse(Regex.Match(bestMatch.Groups[1].Value, @"[\d\.]+").Value, out double maxSize);

                    // If there's more than one match, pick the largest (in case other sizes are discussing the young).

                    foreach (Match m in matches) {

                        double.TryParse(Regex.Match(bestMatch.Groups[1].Value, @"[\d\.]+").Value, out double mSize);

                        if (mSize > maxSize) {

                            maxSize = mSize;
                            bestMatch = m;

                        }

                    }

                    string sizeString = bestMatch.Groups[1].Value;
                    string unitsString = bestMatch.Groups[2].Value.ToLowerInvariant();

                    result.ParseSizeString(sizeString);

                    LengthUnit.TryParse(unitsString, out result.units);

                    break;

                }

            }

            return result;

        }

        public override string ToString() {

            if ((minSize <= 0.0 && maxSize <= 0.0) || units is null)
                return UnknownSize;

            if (minSize != maxSize) {

                // Represent the size as a range.

                var minMeasurements = GetMetricAndImperialMeasurements(minSize);
                var maxMeasurements = GetMetricAndImperialMeasurements(maxSize);

                return string.Format("**{0}-{1} {2}** ({3}-{4} {5})",
                    minMeasurements.Item1.ToString(LengthMeasurementFormat.Value),
                    maxMeasurements.Item1.ToString(LengthMeasurementFormat.Value),
                    minMeasurements.Item1.Units,
                    minMeasurements.Item2.ToString(LengthMeasurementFormat.Value),
                    maxMeasurements.Item2.ToString(LengthMeasurementFormat.Value),
                    maxMeasurements.Item2.Units);

            }
            else {

                // Represent the size as a single value.

                double sizeValue = minSize <= 0.0 ? maxSize : minSize;

                var measurements = GetMetricAndImperialMeasurements(sizeValue);

                return string.Format("**{0}** ({1})", measurements.Item1, measurements.Item2);

            }

        }
        public string ToString(ILengthUnit newUnits) {

            if (newUnits is null)
                throw new ArgumentNullException(nameof(newUnits));

            if ((minSize <= 0.0 && maxSize <= 0.0) || newUnits is null)
                return UnknownSize;

            if (minSize != maxSize) {

                // Represent the size as a range.

                LengthMeasurement minLength = new LengthMeasurement(minSize, this.units).To(newUnits);
                LengthMeasurement maxLength = new LengthMeasurement(maxSize, this.units).To(newUnits);

                return string.Format("**{0}-{1} {2}**",
                    minLength.ToString(LengthMeasurementFormat.Value),
                    maxLength.ToString(LengthMeasurementFormat.Value),
                    minLength.Units.ToString());

            }
            else {

                // Represent the size as a single value.

                LengthMeasurement size = new LengthMeasurement(minSize <= 0.0 ? maxSize : minSize, this.units).To(newUnits);

                return string.Format("**{0}**", size.ToString());

            }

        }

        // Private members

        private double minSize = 0.0;
        private double maxSize = 0.0;
        private ILengthUnit units;

        private SpeciesSizeMatch() { }

        private void ParseSizeString(string value) {

            if (value.Contains("-"))
                ParseSizeRangeString(value);
            else {

                if (double.TryParse(value, out double size)) {

                    minSize = size;
                    maxSize = size;

                }

            }

        }
        private void ParseSizeRangeString(string value) {

            Match m = Regex.Match(value, @"([\d\.]+)\-([\d\.]+)");

            string minStr = m.Groups[1].Value;
            string maxStr = m.Groups[2].Value;

            if (double.TryParse(minStr, out double min) && double.TryParse(maxStr, out double max)) {

                minSize = min;
                maxSize = max;

            }

        }

        private Tuple<LengthMeasurement, LengthMeasurement> GetMetricAndImperialMeasurements(double sizeValue) {

            LengthMeasurement metricLength;
            LengthMeasurement imperialLength;

            // If the given length is >= 1 meter, show the length as meters.
            // If the given length >= 1 centimeter, show the length as centimeters.
            // If the unit is already metric, show the length as the given unit.
            // Otherwise, default to centimeters.

            if (sizeValue >= new LengthMeasurement(1, "m").To(units).Value)
                metricLength = new LengthMeasurement(sizeValue, units).To("m");
            else if (sizeValue >= new LengthMeasurement(1, "cm").To(units).Value)
                metricLength = new LengthMeasurement(sizeValue, units).To("cm");
            else if (units.Type == LengthUnitType.Metric)
                metricLength = new LengthMeasurement(sizeValue, units);
            else
                metricLength = new LengthMeasurement(sizeValue, units).To("cm");

            // If the given length is >= 1 foot, show the length as feet.
            // If the given length >= 1 inch, show the length as inches.
            // If the unit is already imperial, show the length as the given unit.
            // Otherwise, default to inches.

            if (sizeValue >= new LengthMeasurement(1, "ft").To(units).Value)
                imperialLength = new LengthMeasurement(sizeValue, units).To("ft");
            else if (sizeValue >= new LengthMeasurement(1, "in").To(units).Value)
                imperialLength = new LengthMeasurement(sizeValue, units).To("in");
            else if (units.Type == LengthUnitType.Imperial)
                imperialLength = new LengthMeasurement(sizeValue, units);
            else
                imperialLength = new LengthMeasurement(sizeValue, units).To("in");

            return new Tuple<LengthMeasurement, LengthMeasurement>(metricLength, imperialLength);

        }

    }

}
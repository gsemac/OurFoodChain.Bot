using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class SpeciesSizeMatch {

        public const string UNKNOWN_SIZE_STRING = "unknown";

        public static SpeciesSizeMatch Match(string input) {

            SpeciesSizeMatch result = new SpeciesSizeMatch();

            // The first pass will look for things that are most likely to be the size of the species, with explicit keywords located around the size.

            string number_pattern = @"(\d+(?:\.\d+)?(?:\-\d+(?:\.\d+)?)?)";
            string units_pattern = "(in(?:ch|ches)?|ft|feet|foot|c?m|(?:micro|milli|centi)?meters?|μm)";
            string pass_1_pattern = @"(?:get|being|are|grow(?:ing)? up to|grow to|up to|size:)[\s\w]*?" + number_pattern + @"[\s]*?" + units_pattern + @"\b";

            Regex pass_1 = new Regex(pass_1_pattern, RegexOptions.IgnoreCase);
            Regex pass_2 = new Regex(number_pattern + @"[\s]*?" + units_pattern + @"\b", RegexOptions.IgnoreCase);

            // Before we make our passes, remove things that might be false positives (i.e. size of young, depth of burrows).

            input = Regex.Replace(input, @"(?:hatchlings|young|babies)\s*" + pass_1_pattern, string.Empty, RegexOptions.IgnoreCase); // "Young grow up to..."
            input = Regex.Replace(input, units_pattern + @"\s*(?:deep)", string.Empty, RegexOptions.IgnoreCase); // "12 inches deep..."
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

                    result._size = best_match.Groups[1].Value;
                    result._units = best_match.Groups[2].Value.ToLower();

                    break;

                }

            }

            return result;

        }

        public override string ToString() {

            if (string.IsNullOrEmpty(_size))
                return UNKNOWN_SIZE_STRING;

            if (_size.Contains("-")) {

                // Represent the size as a range.

                Match m = Regex.Match(_size, @"([\d\.]+)\-([\d\.]+)");

                string min_str = m.Groups[1].Value;
                string max_str = m.Groups[2].Value;

                if (!double.TryParse(min_str, out double min) || !double.TryParse(max_str, out double max))
                    return UNKNOWN_SIZE_STRING;

                string metric_units = StringUtils.UnitsToAbbreviation(_units);

                if (_isImperialUnits(metric_units)) {

                    // Convert the units to metric so we can show that first.

                    min = _imperialToMetric(min, metric_units);
                    max = _imperialToMetric(max, metric_units);
                    metric_units = _imperialUnitsToMetricEquivalent(metric_units);

                }

                return string.Format("**{0:0.#}-{1:0.#} {2}** ({3:0.#}-{4:0.#} {5})",
                    min,
                    max,
                    metric_units,
                    _metricToImperial(min, metric_units),
                    _metricToImperial(max, metric_units),
                    _metricUnitsToImperialEquivalent(metric_units));

            }
            else {

                if (!double.TryParse(_size, out double size))
                    return UNKNOWN_SIZE_STRING;

                string metric_units = StringUtils.UnitsToAbbreviation(_units);

                if (_isImperialUnits(metric_units)) {

                    // Convert the units to metric so we can show that first.

                    size = _imperialToMetric(size, metric_units);
                    metric_units = _imperialUnitsToMetricEquivalent(metric_units);

                }

                return string.Format("**{0:0.#} {1}** ({2:0.#} {3})",
                    size,
                    metric_units,
                    _metricToImperial(size, metric_units),
                    _metricUnitsToImperialEquivalent(metric_units));

            }

        }

        private string _size = "";
        private string _units = "";

        private bool _isImperialUnits(string units) {

            switch (units.ToLower()) {

                case "in":
                case "ft":
                    return true;

            }

            return false;

        }
        private string _imperialUnitsToMetricEquivalent(string units) {

            switch (units.ToLower()) {

                case "in":
                    return "cm";

                case "ft":
                    return "m";

            }

            return units;

        }
        private string _metricUnitsToImperialEquivalent(string units) {

            switch (units.ToLower()) {

                case "μm":
                case "mm":
                case "cm":
                    return "in";

                case "m":
                    return "ft";

            }

            return units;

        }
        private double _imperialToMetric(double number, string units) {

            switch (units.ToLower()) {

                case "in":
                    return number * 2.54; // to cm

                case "ft":
                    return number / 3.281; // to m

            }

            return 0.0;

        }
        private double _metricToImperial(double number, string units) {

            switch (units.ToLower()) {

                case "μm":
                    return number / 25400.0; // to in

                case "mm":
                    return number / 25.4; // to in

                case "cm":
                    return number / 2.54; // to in

                case "m":
                    return number * 3.281; // to ft

            }

            return 0.0;

        }

    }

}
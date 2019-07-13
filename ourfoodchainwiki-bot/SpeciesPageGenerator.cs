using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChainWikiBot {

    public class SpeciesPageData {

        public OurFoodChain.Species Species { get; set; }
        public OurFoodChain.Species[] AllSpecies { get; set; } = new OurFoodChain.Species[] { };
        public string PictureFileName { get; set; }
        public Dictionary<string, string> LinkDictionary { get; set; } = new Dictionary<string, string>();

    }

    public class SpeciesPageGenerator {

        public string TemplateFilePath { get; set; } = "data/wiki/templates/species_template.txt";

        public string Generate(SpeciesPageData data) {

            string content = string.Empty;

            if (System.IO.File.Exists(TemplateFilePath)) {

                content = System.IO.File.ReadAllText(TemplateFilePath);

                content = _replaceTokens(content, data);

            }
            else
                throw new Exception(string.Format("{0} not found", TemplateFilePath));

            return content;

        }

        public static async Task<string> GenerateTitleAsync(OurFoodChain.Species species) {

            string page_title = string.Empty;
            OurFoodChain.CommonName[] commonNames = await OurFoodChain.SpeciesUtils.GetCommonNamesAsync(species);

            if (!string.IsNullOrWhiteSpace(species.CommonName))
                page_title = species.CommonName;
            else if (commonNames.Count() > 0)
                page_title = commonNames[0].Value;

            if (string.IsNullOrEmpty(page_title))
                page_title = species.GetFullName();

            return page_title;

        }

        private const string unlinked_pattern_format = @"(?<!\[\[|\|)\b{0}\b(?!\||\]\])";

        private string _replaceTokens(string content, SpeciesPageData data) {

            content = Regex.Replace(content, "%([^%]+)%", m => {

                string token = m.Groups[1].Value.ToLower();

                switch (token) {

                    case "picture":
                        return string.IsNullOrEmpty(data.PictureFileName) ? string.Empty : ("File:" + data.PictureFileName);

                    case "owner":
                        return data.Species.owner;

                    case "status":
                        return data.Species.isExtinct ? "Extinct" : "Extant";

                    case "common_names":
                        return string.Join(", ", OurFoodChain.SpeciesUtils.GetCommonNamesAsync(data.Species).Result.Select(x => x.Value));

                    case "zones":
                        return string.Join(", ", OurFoodChain.SpeciesUtils.GetZonesAsync(data.Species).Result.Select(x => x.Zone.GetShortName()));

                    case "roles":
                        return string.Join(", ", OurFoodChain.SpeciesUtils.GetRolesAsync(data.Species).Result.Select(x => x.Name));

                    case "genus":
                        return data.Species.GenusName;

                    case "species":
                        return data.Species.name.ToLower();

                    case "ancestor": {

                            OurFoodChain.Species ancestor = OurFoodChain.SpeciesUtils.GetAncestorAsync(data.Species).Result;

                            return ancestor is null ? "Unknown" : GenerateTitleAsync(ancestor).Result;

                        }

                    case "creation_date":
                        return _formatDate(DateTimeOffset.FromUnixTimeSeconds(data.Species.timestamp).Date);

                    case "extinction_date": {

                            OurFoodChain.ExtinctionInfo extinction_info = OurFoodChain.SpeciesUtils.GetExtinctionInfoAsync(data.Species).Result;

                            return extinction_info.IsExtinct ? _formatDate(extinction_info.Date) : "";

                        }

                    case "extinction_reason":
                        return OurFoodChain.SpeciesUtils.GetExtinctionInfoAsync(data.Species).Result.Reason;

                    case "description":
                        return _formatSpeciesDescription(data);

                    default:
                        throw new Exception(string.Format("unrecognized token \"{0}\"", token));

                }

            });

            return content;

        }
        private string _replaceLinks(string content, SpeciesPageData data) {

            // Keys to be replaced are sorted by length so longer strings are replaced before their substrings.
            // For example, "one two" should have higher priority over "one" and "two" individually.

            foreach (string key in data.LinkDictionary.Keys.OrderByDescending(x => x.Length)) {

                if (_stringMatchesSpeciesName(key, data.Species))
                    continue;

                content = Regex.Replace(content, string.Format(unlinked_pattern_format, Regex.Escape(key)), m => {

                    string page_title = data.LinkDictionary[key];
                    string match_value = m.Value;

                    if (page_title == match_value)
                        return string.Format("[[{0}]]", match_value);
                    else
                        return string.Format("[[{0}|{1}]]", page_title, match_value);

                }, RegexOptions.IgnoreCase);

            }

            return content;

        }
        private string _emboldenFirstMentionOfSpecies(string content, SpeciesPageData data) {

            List<string> to_match = new List<string> {
                string.Format(unlinked_pattern_format, Regex.Escape(data.Species.FullName.ToLower())),
                string.Format(unlinked_pattern_format, Regex.Escape(data.Species.ShortName.ToLower())),
                string.Format(unlinked_pattern_format, Regex.Escape(data.Species.Name.ToLower()))
            };

            if (!string.IsNullOrEmpty(data.Species.CommonName))
                string.Format(unlinked_pattern_format, Regex.Escape(data.Species.CommonName.ToLower()));

            Regex regex = new Regex(string.Join("|", to_match), RegexOptions.IgnoreCase);

            return regex.Replace(content, m => {
                return string.Format("'''{0}'''", m.Value);
            }, 1);

        }
        private string _italicizeBinomialNames(string content, SpeciesPageData data) {

            foreach (OurFoodChain.Species species in data.AllSpecies) {

                List<string> to_match = new List<string> {
                    string.Format(unlinked_pattern_format, Regex.Escape(species.FullName)),
                    string.Format(unlinked_pattern_format, Regex.Escape(species.ShortName))
                };

                Regex regex = new Regex(string.Join("|", to_match), RegexOptions.IgnoreCase);

                content = regex.Replace(content, m => {
                    return string.Format("''{0}''", m.Value);
                });

            }

            return content;

        }
        private string _formatSpeciesDescription(SpeciesPageData data) {

            string desc = data.Species.GetDescriptionOrDefault();

            desc = _italicizeBinomialNames(desc, data); // do this before replacing links so that links can be italicized more easily
            desc = _replaceLinks(desc, data);
            desc = _emboldenFirstMentionOfSpecies(desc, data);

            return desc;

        }

        private string _formatDate(DateTime date) {

            string day_string = date.Day.ToString();

            if (day_string.Last() == '1' && !day_string.EndsWith("11"))
                day_string += "st";
            else if (day_string.Last() == '2' && !day_string.EndsWith("12"))
                day_string += "nd";

            else if (day_string.Last() == '3' && !day_string.EndsWith("13"))
                day_string += "rd";
            else
                day_string += "th";

            return string.Format("{1:MMMM} {0}, {1:yyyy}", day_string, date);

        }
        private bool _stringMatchesSpeciesName(string name, OurFoodChain.Species species) {

            name = name.ToLower();

            return name == species.name.ToLower() ||
                name == species.GetShortName().ToLower() ||
                name == species.GetFullName().ToLower() ||
                (!string.IsNullOrEmpty(species.CommonName) && name == species.CommonName.ToLower());

        }

    }

}

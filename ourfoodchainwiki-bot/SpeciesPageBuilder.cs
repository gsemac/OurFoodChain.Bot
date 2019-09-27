using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChainWikiBot {

    public class SpeciesPageBuilderSpeciesData {

        public SpeciesPageBuilderSpeciesData(OurFoodChain.Species species) {
            Species = species;
        }

        public OurFoodChain.Species Species { get; set; }
        public OurFoodChain.CommonName[] CommonNames { get; set; } = new OurFoodChain.CommonName[] { };

    }

    public class SpeciesPageBuilder {

        // Public members

        public string Title {
            get {

                if (!string.IsNullOrEmpty(_title))
                    return _title;

                return _generateTitle(SpeciesData);

            }
            set {
                _title = value;
            }
        }
        public string PictureFileName { get; set; }

        public SpeciesPageBuilderSpeciesData SpeciesData { get; set; } = null;
        public SpeciesPageBuilderSpeciesData AncestorSpeciesData { get; set; } = null;
        public OurFoodChain.ExtinctionInfo ExtinctionInfo { get; set; } = null;
        public OurFoodChain.SpeciesZone[] Zones { get; set; } = new OurFoodChain.SpeciesZone[] { };
        public OurFoodChain.Role[] Roles { get; set; } = new OurFoodChain.Role[] { };

        public OurFoodChain.Species[] SpeciesList { get; set; } = new OurFoodChain.Species[] { };
        public LinkifyList LinkifyList { get; set; } = new LinkifyList();

        public SpeciesPageBuilder(SpeciesPageBuilderSpeciesData speciesData, PageTemplate pageTemplate) {

            SpeciesData = speciesData;

            _template = pageTemplate;

        }

        public string Build() {

            if (_template is null)
                throw new Exception("Page template is null");

            string content = _build(_template);

            return content;

        }

        public override string ToString() {
            return Build();
        }

        // Private members

        private string _title;
        private readonly PageTemplate _template;

        private string _build(PageTemplate template) {

            if (SpeciesData is null || SpeciesData.Species is null)
                throw new Exception("Species is null");

            template.ReplaceToken("picture", string.IsNullOrEmpty(PictureFileName) ? "" : string.Format("File:{0}", PictureFileName));
            template.ReplaceToken("owner", SpeciesData.Species.owner);
            template.ReplaceToken("status", SpeciesData.Species.isExtinct ? "Extinct" : "Extant");
            template.ReplaceToken("common_names", string.Join(", ", SpeciesData.CommonNames.Select(x => x.Value)));
            template.ReplaceToken("zones", string.Join(", ", Zones.Select(x => x.Zone.ShortName)));
            template.ReplaceToken("roles", string.Join(", ", Roles.Select(x => x.Name)));
            template.ReplaceToken("genus", SpeciesData.Species.GenusName);
            template.ReplaceToken("species", SpeciesData.Species.Name.ToLower());
            template.ReplaceToken("ancestor", AncestorSpeciesData is null ? "Unknown" : _generateTitle(AncestorSpeciesData));
            template.ReplaceToken("creation_date", _formatDate(DateTimeOffset.FromUnixTimeSeconds(SpeciesData.Species.timestamp).Date));
            template.ReplaceToken("extinction_date", ExtinctionInfo is null || !ExtinctionInfo.IsExtinct ? "" : _formatDate(ExtinctionInfo.Date));
            template.ReplaceToken("extinction_reason", ExtinctionInfo is null || !ExtinctionInfo.IsExtinct ? "" : ExtinctionInfo.Reason);
            template.ReplaceToken("description", _formatSpeciesDescription());

            return template.Text;

        }

        private static string _generateTitle(SpeciesPageBuilderSpeciesData speciesData) {

            string title = string.Empty;

            if (speciesData is null || speciesData.Species is null)
                throw new Exception("Species is null");

            if (!string.IsNullOrWhiteSpace(speciesData.Species.CommonName))
                title = speciesData.Species.CommonName;
            else if (speciesData.CommonNames.Count() > 0)
                title = speciesData.CommonNames.First().Value;

            // If the title is empty for whatever reason (common names set to whitespace, for example), use the species' binomial name.

            if (string.IsNullOrWhiteSpace(title))
                title = speciesData.Species.FullName;

            // Trim any surrounding whitespace from the title.

            if (!string.IsNullOrEmpty(title))
                title = title.Trim();

            return title;

        }

        private string _formatSpeciesDescription() {

            if (SpeciesData is null || SpeciesData.Species is null)
                throw new Exception("Species is null");

            string description = SpeciesData.Species.GetDescriptionOrDefault();

            description = _italicizeBinomialNames(description); // do this before replacing links so that links can be italicized more easily
            description = _replaceLinks(description);
            description = _emboldenFirstMentionOfSpecies(description);
            description = PageUtils.ReplaceMarkdownWithWikiMarkup(description);

            return description;

        }

        private string _replaceLinks(string Content) {

            // Only replace links that don't refer to the current species (either by name or by target).

            return PageUtils.FormatPageLinksIf(Content, LinkifyList, x => !_stringMatchesSpeciesName(x.Value, SpeciesData.Species) && !_stringMatchesSpeciesName(x.Target, SpeciesData.Species));

        }
        private string _emboldenFirstMentionOfSpecies(string content) {

            List<string> to_match = new List<string> {
                string.Format(PageUtils.UnlinkedWikiTextPatternFormat, Regex.Escape(SpeciesData.Species.FullName.ToLower())),
                string.Format(PageUtils.UnlinkedWikiTextPatternFormat, Regex.Escape(SpeciesData.Species.ShortName.ToLower())),
                string.Format(PageUtils.UnlinkedWikiTextPatternFormat, Regex.Escape(SpeciesData.Species.Name.ToLower()))
            };

            if (!string.IsNullOrEmpty(SpeciesData.Species.CommonName))
                string.Format(PageUtils.UnlinkedWikiTextPatternFormat, Regex.Escape(SpeciesData.Species.CommonName.ToLower()));

            Regex regex = new Regex(string.Join("|", to_match), RegexOptions.IgnoreCase);

            return PageUtils.FormatFirstMatch(content, regex, TextFormatting.Bold);

        }
        private string _italicizeBinomialNames(string content) {

            foreach (OurFoodChain.Species species in SpeciesList) {

                List<string> to_match = new List<string> {
                    string.Format(PageUtils.UnformattedWikiTextPatternFormat, Regex.Escape(species.FullName)),
                    string.Format(PageUtils.UnformattedWikiTextPatternFormat, Regex.Escape(species.ShortName)),
                };

                Regex regex = new Regex(string.Join("|", to_match), RegexOptions.IgnoreCase);

                content = PageUtils.FormatAllMatches(content, regex, TextFormatting.Italic);

                // Also italicize binomial names that might be using outdated genera (e.g. Species moved to a new genus since the description was written).
                // This might create some false-positives, so it could be a good idea to limit matches only to known genera (at the expense of a significantly longer regex).
                content = PageUtils.FormatAllMatches(content, new Regex(string.Format(PageUtils.UnformattedWikiTextPatternFormat, @"[A-Z](?:[a-z]+|\.)\s" + Regex.Escape(species.Name.ToLower()))), TextFormatting.Italic);

            }

            return content;

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

            return name == species.Name.ToLower() ||
                name == species.ShortName.ToLower() ||
                name == species.FullName.ToLower() ||
                (!string.IsNullOrEmpty(species.CommonName) && name == species.CommonName.ToLower());

        }

    }

}
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.Wiki {

    public class SpeciesPageBuilder {

        public ISpecies Species { get; set; }
        public ISpecies[] AllSpecies { get; set; }
        public WikiPageTemplate Template { get; set; }
        public WikiLinkList LinkList { get; set; }
        public List<string> PictureFilenames { get; set; } = new List<string>();

        public SpeciesPageBuilder(ISpecies species, WikiPageTemplate template) {

            if (species is null)
                throw new ArgumentNullException("species");

            if (template is null)
                throw new ArgumentNullException("template");

            Species = species;
            Template = template;

        }

        public async Task<WikiPage> BuildAsync() {

            return new WikiPage {
                Title = await BuildTitleAsync(),
                Body = await BuildBodyAsync()
            };

        }

        private async Task<string> BuildTitleAsync() {
            return await BuildTitleAsync(Species);
        }
        private async Task<string> BuildTitleAsync(ISpecies species) {

            if (species is null)
                throw new ArgumentNullException("species");

            // Pages are created based on the first/primary common name (where available).
            // The full species name is added as a redirect.

            string title = string.Empty;

            if (!string.IsNullOrWhiteSpace(species.GetCommonName()))
                title = species.GetCommonName();
            else {

                CommonName[] commonNames = await SpeciesUtils.GetCommonNamesAsync(species);

                if (commonNames.Count() > 0)
                    title = commonNames.First().Value;
                else
                    title = species.GetFullName();

            }

            // If the title is empty for whatever reason (common names set to whitespace, for example), use the species' binomial name.

            if (string.IsNullOrWhiteSpace(title))
                title = species.GetFullName();

            // Trim any surrounding whitespace from the title.

            if (!string.IsNullOrEmpty(title))
                title = title.Trim();

            return title;

        }
        private async Task<string> BuildBodyAsync() {

            if (Species is null)
                throw new Exception("Species cannot be null.");

            if (Template is null)
                throw new ArgumentNullException("Template cannot be null.");

            Template.ReplaceToken("picture", await GetPictureTokenValueAsync());
            Template.ReplaceToken("owner", await GetOwnerTokenValueAsync());
            Template.ReplaceToken("status", await GetStatusTokenValueAsync());
            Template.ReplaceToken("common_names", await GetCommonNamesTokenValueAsync());
            Template.ReplaceToken("zones", await GetZonesTokenValueAsync());
            Template.ReplaceToken("roles", await GetRolesTokenValueAsync());
            Template.ReplaceToken("genus", await GetGenusTokenValueAsync());
            Template.ReplaceToken("species", await GetSpeciesTokenValueAsync());
            Template.ReplaceToken("ancestor", await GetAncestorTokenValueAsync());
            Template.ReplaceToken("creation_date", await GetCreationDateTokenValueAsync());
            Template.ReplaceToken("extinction_date", await GetExtinctionDateTokenValueAsync());
            Template.ReplaceToken("extinction_reason", await GetExtinctionReasonTokenValueAsync());
            Template.ReplaceToken("description", await GetDescriptionTokenValueAsync());
            Template.ReplaceToken("gallery", await GetGalleryTokenValueAsync());

            return Template.Text.Trim();

        }

        private async Task<string> GetPictureTokenValueAsync() {

            return await Task.FromResult(PictureFilenames.Count() <= 0 ? string.Empty : string.Format("File:{0}", PictureFilenames.First()));

        }
        private async Task<string> GetOwnerTokenValueAsync() {
            return await Task.FromResult(Species.Creator.Name);
        }
        private async Task<string> GetStatusTokenValueAsync() {
            return await Task.FromResult(Species.IsExtinct() ? "Extinct" : "Extant");
        }
        private async Task<string> GetCommonNamesTokenValueAsync() {
            return await Task.FromResult(string.Join(", ", SpeciesUtils.GetCommonNamesAsync(Species).Result.Select(x => x.Value)));
        }
        private async Task<string> GetZonesTokenValueAsync() {
            return string.Join(", ", (await SpeciesUtils.GetZonesAsync(Species)).Select(x => x.Zone.ShortName));
        }
        private async Task<string> GetRolesTokenValueAsync() {
            return string.Join(", ", (await SpeciesUtils.GetRolesAsync(Species)).Select(x => x.Name));
        }
        private async Task<string> GetGenusTokenValueAsync() {
            return await Task.FromResult(Species.Genus.GetName());
        }
        private async Task<string> GetSpeciesTokenValueAsync() {
            return await Task.FromResult(Species.Name.ToLower());
        }
        private async Task<string> GetAncestorTokenValueAsync() {

            ISpecies ancestorSpecies = await SpeciesUtils.GetAncestorAsync(Species);

            if (ancestorSpecies != null)
                return await BuildTitleAsync(ancestorSpecies);
            else
                return await Task.FromResult("Unknown");

        }
        private async Task<string> GetCreationDateTokenValueAsync() {
            return await Task.FromResult(FormatDate(Species.CreationDate.DateTime));
        }
        private async Task<string> GetExtinctionDateTokenValueAsync() {

            ExtinctionInfo extinctionInfo = await SpeciesUtils.GetExtinctionInfoAsync(Species);

            if (extinctionInfo.IsExtinct)
                return await Task.FromResult(FormatDate(extinctionInfo.Date));
            else
                return await Task.FromResult(string.Empty);

        }
        private async Task<string> GetExtinctionReasonTokenValueAsync() {

            ExtinctionInfo extinctionInfo = await SpeciesUtils.GetExtinctionInfoAsync(Species);

            if (extinctionInfo.IsExtinct)
                return await Task.FromResult(extinctionInfo.Reason);
            else
                return await Task.FromResult(string.Empty);

        }
        private async Task<string> GetDescriptionTokenValueAsync() {

            string description = Species.GetDescriptionOrDefault();

            description = ItalicizeBinomialNames(description); // do this before replacing links so that links can be italicized more easily
            description = ReplaceLinks(description);
            description = EmboldenFirstMentionOfSpecies(description);
            description = WikiPageUtils.ReplaceMarkdownWithWikiMarkup(description);

            return await Task.FromResult(description);

        }
        private async Task<string> GetGalleryTokenValueAsync() {

            if (PictureFilenames.Count() <= 1)
                return await Task.FromResult(string.Empty);

            StringBuilder galleryBuilder = new StringBuilder();

            galleryBuilder.AppendLine("== Gallery ==");
            galleryBuilder.AppendLine("<gallery>");

            foreach (string filename in PictureFilenames.Skip(1))
                galleryBuilder.AppendLine(string.Format("File:{0}", filename));

            galleryBuilder.AppendLine("</gallery>");

            return await Task.FromResult(galleryBuilder.ToString());

        }

        private string FormatDate(DateTime date) {

            string dayString = date.Day.ToString();

            if (dayString.Last() == '1' && !dayString.EndsWith("11"))
                dayString += "st";
            else if (dayString.Last() == '2' && !dayString.EndsWith("12"))
                dayString += "nd";
            else if (dayString.Last() == '3' && !dayString.EndsWith("13"))
                dayString += "rd";
            else
                dayString += "th";

            return string.Format("{1:MMMM} {0}, {1:yyyy}", dayString, date);

        }

        private string ReplaceLinks(string input) {

            if (LinkList != null) {

                // Only replace links that don't refer to the current species (either by name or by target).

                input = WikiPageUtils.FormatPageLinksIf(input, LinkList, x => !StringMatchesSpeciesName(x.Value, Species) && !StringMatchesSpeciesName(x.Target, Species));

            }

            return input;

        }
        private string EmboldenFirstMentionOfSpecies(string input) {

            List<string> toMatch = new List<string> {
                string.Format(WikiPageUtils.UnlinkedWikiTextPatternFormat, Regex.Escape(Species.GetFullName().ToLower())),
                string.Format(WikiPageUtils.UnlinkedWikiTextPatternFormat, Regex.Escape(Species.GetShortName().ToLower())),
                string.Format(WikiPageUtils.UnlinkedWikiTextPatternFormat, Regex.Escape(Species.Name.ToLower()))
            };

            if (!string.IsNullOrEmpty(Species.GetCommonName()))
                string.Format(WikiPageUtils.UnlinkedWikiTextPatternFormat, Regex.Escape(Species.GetCommonName().ToLower()));

            Regex regex = new Regex(string.Join("|", toMatch), RegexOptions.IgnoreCase);

            return WikiPageUtils.FormatFirstMatch(input, regex, TextFormatting.Bold);

        }
        private string ItalicizeBinomialNames(string input) {

            if (AllSpecies != null) {

                foreach (ISpecies species in AllSpecies) {

                    List<string> to_match = new List<string> {
                    string.Format(WikiPageUtils.UnformattedWikiTextPatternFormat, Regex.Escape(species.GetFullName())),
                    string.Format(WikiPageUtils.UnformattedWikiTextPatternFormat, Regex.Escape(species.GetShortName())),
                };

                    Regex regex = new Regex(string.Join("|", to_match), RegexOptions.IgnoreCase);

                    input = WikiPageUtils.FormatAllMatches(input, regex, TextFormatting.Italic);

                    // Also italicize binomial names that might be using outdated genera (e.g. Species moved to a new genus since the description was written).
                    // This might create some false-positives, so it could be a good idea to limit matches only to known genera (at the expense of a significantly longer regex).
                    input = WikiPageUtils.FormatAllMatches(input, new Regex(string.Format(WikiPageUtils.UnformattedWikiTextPatternFormat, @"[A-Z](?:[a-z]+|\.)\s" + Regex.Escape(species.Name.ToLower()))), TextFormatting.Italic);

                }

            }

            return input;

        }
        private bool StringMatchesSpeciesName(string name, ISpecies species) {

            name = name.ToLower();

            return name == species.Name.ToLower() ||
                name == species.GetShortName().ToLower() ||
                name == species.GetFullName().ToLower() ||
                (!string.IsNullOrEmpty(species.GetCommonName()) && name == species.GetCommonName().ToLower());

        }

    }

}
using Discord;
using Discord.Commands;
using OurFoodChain.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Taxa {

    public class SearchQuery {

        // Public members

        public IEnumerable<string> Keywords { get; }
        public IEnumerable<ISearchModifier> Modifiers { get; }

        public SearchQuery(string queryString) {

            IEnumerable<string> searchTerms = GetSearchTerms(queryString);
            List<string> keywords = new List<string>();
            List<ISearchModifier> modifiers = new List<ISearchModifier>();

            foreach (string term in searchTerms
                .Select(term => term.Trim().ToLowerInvariant())
                .Where(term => !string.IsNullOrWhiteSpace(term))) {

                if (IsSearchModifier(term))
                    modifiers.Add(SearchModifier.Create(term));
                else
                    keywords.Add(term);

            }

            Keywords = keywords;
            Modifiers = modifiers;

        }

        // Private members

        private static bool IsSearchModifier(string input) {

            return input.Contains(":");

        }
        private static IEnumerable<string> GetSearchTerms(string queryString) {

            List<string> keywords = new List<string>();

            string keyword = "";
            bool insideQuotes = false;

            for (int i = 0; i < queryString.Length; ++i) {

                if (queryString[i] == '\"') {

                    insideQuotes = !insideQuotes;

                    keyword += queryString[i];

                }
                else if (!insideQuotes && char.IsWhiteSpace(queryString[i])) {

                    keywords.Add(keyword);
                    keyword = "";

                }
                else
                    keyword += queryString[i];

            }

            if (keyword.Length > 0)
                keywords.Add(keyword);

            return keywords.ToArray();

        }

    }

}
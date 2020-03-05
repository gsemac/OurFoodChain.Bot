using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OurFoodChain.Data.Queries {

    public class SearchQuery :
        ISearchQuery {

        // Public members

        public IEnumerable<string> Keywords { get; }
        public IEnumerable<string> Modifiers { get; }

        public SearchQuery(string query) {

            IEnumerable<string> searchTerms = StringUtilities.ParseArguments(query);
            List<string> keywords = new List<string>();
            List<string> modifiers = new List<string>();

            foreach (string term in searchTerms
                .Select(term => term.Trim().ToLowerInvariant())
                .Where(term => !string.IsNullOrWhiteSpace(term))) {

                if (IsSearchModifier(term))
                    modifiers.Add(term);
                else
                    keywords.Add(term);

            }

            this.query = query;
            this.Keywords = keywords;
            this.Modifiers = modifiers;

        }

        public override string ToString() {

            return query;

        }

        // Private members

        private static bool IsSearchModifier(string input) {

            return input.Contains(":");

        }

        private readonly string query;

    }

}
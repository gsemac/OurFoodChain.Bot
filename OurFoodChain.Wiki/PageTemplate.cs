using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChainWikiBot {

    public class PageTemplate {

        // Public members

        public string TemplateText { get; private set; }
        public string Text {
            get {
                return ToString();
            }
        }

        public PageTemplate(string templateText) {

            TemplateText = templateText;

            _scanTokens();

        }

        public void ReplaceToken(string token, string replacement) {

            if (!string.IsNullOrEmpty(token))
                token = token.Trim().ToLower();

            if (!_token_replacements.ContainsKey(token))
                throw new Exception(string.Format("No token named \"{0}\" exists in this template.", token));

            _token_replacements[token].Value = replacement;
            _token_replacements[token].IsSet = true;

        }

        public override string ToString() {
            return _build();
        }

        public static PageTemplate FromFile(string templateFilePath) {
            return new PageTemplate(System.IO.File.ReadAllText(templateFilePath));
        }

        // Private members

        private class TokenReplacement {
            public string Value { get; set; }
            public bool IsSet { get; set; } = false;
        }

        private const string _token_pattern = "%([^%]+)%";

        Dictionary<string, TokenReplacement> _token_replacements = new Dictionary<string, TokenReplacement>();

        private void _scanTokens() {

            foreach (Match m in Regex.Matches(TemplateText, _token_pattern)) {

                string token = m.Groups[1].Value.ToLower();

                _token_replacements[token] = new TokenReplacement();

            }

        }
        private string _build() {

            return Regex.Replace(TemplateText, _token_pattern, x => {

                string token = x.Groups[1].Value.ToLower();
                TokenReplacement replacement = _token_replacements[token];

                if (!replacement.IsSet)
                    throw new Exception(string.Format("No replacement provided for token \"{0}\".", token));

                return replacement.Value;

            });

        }

    }

}
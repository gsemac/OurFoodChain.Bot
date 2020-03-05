using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Common.Utilities {

    public class StringPaginator :
        IEnumerable<string> {

        // Public members

        public const int DefaultMaxPageLength = 2048;

        public int MaxPageLength { get; set; } = DefaultMaxPageLength;

        public StringPaginator(string input) :
            this(input, DefaultMaxPageLength) {
        }
        public StringPaginator(string input, int maxPageLength) {

            this.inputText = string.IsNullOrEmpty(input) ? "" : input.Trim();
            this.MaxPageLength = maxPageLength;

        }

        public IEnumerator<string> GetEnumerator() {
            return GetPages().GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() {
            return GetPages().GetEnumerator();
        }

        // Private members

        private readonly string inputText;

        private IEnumerable<string> GetPages() {

            List<string> pages = new List<string>();
            string ellipsis = "...";

            for (int i = 0; i < inputText.Length;) {

                StringBuilder sb = new StringBuilder();

                // Skip whitespace.

                while (i < inputText.Length && char.IsWhiteSpace(inputText[i]))
                    ++i;

                // Read words until we don't have any more room for them.

                int nextSubstringLength = 0;
                int maxPageLength = Math.Max(0, MaxPageLength);

                if (pages.Count() > 0)
                    maxPageLength -= ellipsis.Length + 1; // leading "... "

                if (inputText.Length - i > MaxPageLength)
                    maxPageLength -= ellipsis.Length + 1; // trailing " ..."

                for (int j = i; j <= inputText.Length; ++j) {

                    if (j == inputText.Length || char.IsWhiteSpace(inputText[j])) {

                        int len = j - i;

                        if (len > maxPageLength)
                            break;
                        else
                            nextSubstringLength = len;

                    }

                }

                if (nextSubstringLength > 0) {

                    // If there was a page before this, add a leading ellipsis.

                    if (pages.Count > 0)
                        sb.Append(ellipsis + " ");

                    sb.Append(inputText.Substring(i, nextSubstringLength));

                    i += nextSubstringLength;

                    // If there are pages after this, add a trailing ellipsis.

                    if (i < inputText.Length)
                        sb.Append(" " + ellipsis);

                    pages.Add(sb.ToString());

                }
                else
                    // Really long string without whitespace, or end of string?
                    break;

            }

            if (pages.Count <= 0)
                pages.Add("");

            return pages;

        }

    }

}
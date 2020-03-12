using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Wiki {

    public enum WikiLinkListDataType {
        Find,
        Regex
    }

    public class WikiLinkListData {

        public WikiLinkListData(string value, string link) {

            Value = value;
            Target = link;

        }

        public WikiLinkListDataType Type { get; set; } = WikiLinkListDataType.Find;
        public string Value { get; set; }
        public string Target { get; set; }

    }

    public class WikiLinkList :
        IEnumerable<WikiLinkListData> {

        // Public members

        public void Add(string value, string link) {
            _items.Add(new WikiLinkListData(value, link));
        }
        public void Add(string value, string link, WikiLinkListDataType type) {

            _items.Add(new WikiLinkListData(value, link) {
                Type = type
            });

        }

        public IEnumerator GetEnumerator() {
            return _items.GetEnumerator();
        }
        IEnumerator<WikiLinkListData> IEnumerable<WikiLinkListData>.GetEnumerator() {
            return _items.GetEnumerator();
        }

        // Private numbers

        private List<WikiLinkListData> _items = new List<WikiLinkListData>();

    }

}
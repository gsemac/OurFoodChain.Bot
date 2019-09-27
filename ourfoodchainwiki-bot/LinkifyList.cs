using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChainWikiBot {

    public enum LinkifyListDataType {
        Find,
        Regex
    }

    public class LinkifyListData {

        public LinkifyListData(string value, string link) {

            Value = value;
            Target = link;

        }

        public LinkifyListDataType Type { get; set; } = LinkifyListDataType.Find;
        public string Value { get; set; }
        public string Target { get; set; }

    }

    public class LinkifyList :
        IEnumerable<LinkifyListData> {

        // Public members

        public void Add(string value, string link) {
            _items.Add(new LinkifyListData(value, link));
        }
        public void Add(string value, string link, LinkifyListDataType type) {

            _items.Add(new LinkifyListData(value, link) {
                Type = type
            });

        }

        public IEnumerator GetEnumerator() {
            return _items.GetEnumerator();
        }
        IEnumerator<LinkifyListData> IEnumerable<LinkifyListData>.GetEnumerator() {
            return _items.GetEnumerator();
        }

        // Private numbers

        private List<LinkifyListData> _items = new List<LinkifyListData>();

    }

}
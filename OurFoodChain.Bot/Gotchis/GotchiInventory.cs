using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchis {

    public class GotchiInventoryItem {

        public GotchiItem Item { get; set; }
        public long Count { get; set; }

    }

    public class GotchiInventory :
        IEnumerable<GotchiInventoryItem> {

        // Public members

        public GotchiInventory(IEnumerable<GotchiInventoryItem> items) {

            this.items = new List<GotchiInventoryItem>(items.Where(i => i.Count > 0));

        }

        public IEnumerator<GotchiInventoryItem> GetEnumerator() {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return items.GetEnumerator();
        }

        public GotchiInventoryItem GetItemByIndex(int index) {

            --index; // 1-based

            if (index < 0 || index >= items.Count)
                return null;

            return items[index];

        }
        public GotchiInventoryItem GetItemByIdentifier(long itemId) {

            return GetItemByIdentifier(itemId.ToString());

        }
        public GotchiInventoryItem GetItemByIdentifier(string identifier) {

            return items
                .Where(i => i.Item.Name.Equals(identifier, StringComparison.OrdinalIgnoreCase) || i.Item.Id.ToString().Equals(identifier))
                .FirstOrDefault();

        }

        // Private members

        private readonly List<GotchiInventoryItem> items;

    }

}
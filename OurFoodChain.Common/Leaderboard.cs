using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OurFoodChain.Common {

    public class Leaderboard :
        ILeaderboard {

        // Public members

        public string Title { get; set; }

        public void Add(string name, long score) {

            items.Add(new LeaderboardItem() {
                Name = name,
                Score = score
            });

        }

        public Leaderboard(string title) {

            this.Title = title;

        }

        public IEnumerator<ILeaderboardItem> GetEnumerator() {

            return GetRankedItems().GetEnumerator();

        }
        IEnumerator IEnumerable.GetEnumerator() {

            return GetEnumerator();

        }

        // Private members

        private readonly List<ILeaderboardItem> items = new List<ILeaderboardItem>();

        private IEnumerable<ILeaderboardItem> GetRankedItems() {

            List<ILeaderboardItem> results = new List<ILeaderboardItem>();

            int currentRank = 0;

            foreach (ILeaderboardItem item in items.OrderByDescending(item => item.Score)) {

                ILeaderboardItem nextItem = new LeaderboardItem() {
                    Name = item.Name,
                    Score = item.Score
                };

                if (results.Count() <= 0 || nextItem.Score < results.Last().Score)
                    ++currentRank;

                item.Rank = currentRank;

                results.Add(item);

            }

            return results;

        }

    }

}
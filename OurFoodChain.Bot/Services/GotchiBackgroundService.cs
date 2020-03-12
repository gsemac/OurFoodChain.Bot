using OurFoodChain.Data;
using OurFoodChain.Extensions;
using OurFoodChain.Gotchis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OurFoodChain.Bot.Services {

    public class GotchiBackgroundService {

        // Public members

        public int DelayMilliseconds { get; set; } = (int)TimeSpan.FromMinutes(10).TotalMilliseconds;

        public GotchiBackgroundService(IOfcBotConfiguration botConfiguration, SQLiteDatabase database) {

            this.botConfiguration = botConfiguration;
            this.database = database;

        }

        public async Task InitializeAsync() {

            _ = Task.Run(async () => {

                while (!cancellationTokenSource.Token.IsCancellationRequested) {

                    try {
                        await DoBackgroundLoopAsync();
                    }
                    catch (Exception ex) {
                        // Exceptions shouldn't kill the task.
                        // #todo log the error
                    }

                    await Task.Delay(DelayMilliseconds, cancellationTokenSource.Token);

                }

            }, cancellationTokenSource.Token);

            await Task.CompletedTask;

        }
        public async Task StopAsync() {

            await Task.Run(() => cancellationTokenSource.Cancel());

        }

        // Private members

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly IOfcBotConfiguration botConfiguration;
        private readonly SQLiteDatabase database;

        private async Task DoBackgroundLoopAsync() {

            // If gotchis are not enabled, don't do anything.

            if (botConfiguration.GotchisEnabled) {

                foreach (IGrouping<ulong, Gotchi> userGotchis in (await database.GetGotchisAsync()).GroupBy(g => g.OwnerId)) {

                    await DoAutoFeederAsync(userGotchis.Key, userGotchis);
                    await DoEvolveAsync(userGotchis.Key, userGotchis);

                }

            }

        }
        private async Task DoEvolveAsync(ulong userId, IEnumerable<Gotchi> userGotchis) {

            // Gotchi evolution (time-based)

            foreach (Gotchi gotchi in userGotchis) {

                if (gotchi.CanEvolve)
                    await database.EvolveAndUpdateGotchiAsync(gotchi);

            }

        }
        private async Task DoAutoFeederAsync(ulong userId, IEnumerable<Gotchi> userGotchis) {

            // Auto-feeder

            if ((await database.GetItemFromInventoryAsync(userId, GotchiItemId.AutoFeeder)).Count > 0) {

                GotchiUserInfo userInfo = await database.GetUserInfoAsync(userId);
                const int costPerFeeding = 5;

                if (userInfo != null && userInfo.G >= costPerFeeding) {

                    int gotchisFed = userGotchis.Where(g => g.IsHungry).Count();

                    if (gotchisFed > 0) {

                        await database.FeedGotchisAsync(Global.GotchiContext, userId);

                        userInfo.G = Math.Max(0, userInfo.G - (costPerFeeding * gotchisFed));

                        await database.UpdateUserInfoAsync(userInfo);

                    }

                }

            }

        }

    }

}
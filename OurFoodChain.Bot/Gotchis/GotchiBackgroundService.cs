using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchis {

    public class GotchiBackgroundService {

        public int DelayMilliseconds { get; set; } = (int)TimeSpan.FromMinutes(10).TotalMilliseconds;

        public async Task StartAsync() {

            Task.Run(async () => {

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

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private async Task DoBackgroundLoopAsync() {

            // If gotchis are not enabled, don't do anything.

            if (Bot.OurFoodChainBot.Instance.Config.GotchisEnabled) {

                foreach (IGrouping<ulong, Gotchi> userGotchis in (await GotchiUtils.GetGotchisAsync()).GroupBy(g => g.OwnerId)) {

                    await DoAutoFeederAsync(userGotchis.Key, userGotchis);

                }

            }

        }
        private async Task DoAutoFeederAsync(ulong userId, IEnumerable<Gotchi> userGotchis) {

            // Auto-feeder

            if ((await GotchiUtils.GetItemFromInventoryAsync(userId, GotchiItemId.AutoFeeder)).Count > 0) {

                GotchiUserInfo userInfo = await GotchiUtils.GetUserInfoAsync(userId);
                const int costPerFeeding = 5;

                if (userInfo != null && userInfo.G >= costPerFeeding) {

                    int gotchisFed = userGotchis.Where(g => g.IsHungry()).Count();
                
                    await GotchiUtils.FeedGotchisAsync(userId);

                    userInfo.G = Math.Max(0, userInfo.G - (costPerFeeding * gotchisFed));

                    await GotchiUtils.UpdateUserInfoAsync(userInfo);

                }

            }

        }

    }

}
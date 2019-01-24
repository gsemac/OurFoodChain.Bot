using Discord;
using Discord.Commands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OurFoodChain.trophies {

    public class TrophyScanner {

        public static async Task AddToQueueAsync(ICommandContext context, ulong userId) {

            // Add the user to the scanner queue.
            _scan_queue.Enqueue(new ScannerQueueItem {
                context = context,
                userId = userId,
                timestamp = DateTimeOffset.Now.ToUnixTimeSeconds()
            });

            // Initialize the trophy registry (nothing will happen if it's already initialized).
            await TrophyRegistry.InitializeAsync();

            // Since there's something in the queue, start the trophy scanner (nothing will happen if it's already active).
            _startScanner();

        }


        private class ScannerQueueItem {
            public ICommandContext context;
            public ulong userId;
            public long timestamp;
        }

        // The scan delay is how long to wait before scanning trophies for the next user in the queue.
        private const long SCAN_DELAY = 0; // 5 minutes
        private static ConcurrentQueue<ScannerQueueItem> _scan_queue = new ConcurrentQueue<ScannerQueueItem>();
        private static bool _scanner_running = false;

        private static void _startScanner() {

            if (_scanner_running)
                return;

            _scanner_running = true;

            Task.Run(async () => {

                await OurFoodChainBot.GetInstance().Log(LogSeverity.Info, "Trophies", "Starting trophy scanner");

                while (_scan_queue.Count > 0) {

                    // Wait until the next item in the queue has been sitting for at least SCAN_DELAY.

                    long current_ts = DateTimeOffset.Now.ToUnixTimeSeconds();
                    long item_ts = _scan_queue.First().timestamp;

                    if (current_ts - item_ts < SCAN_DELAY)
                        await Task.Delay(TimeSpan.FromSeconds(SCAN_DELAY - (current_ts - item_ts)));

                    // Scan trophies for the next item in the queue.

                    if (_scan_queue.TryDequeue(out ScannerQueueItem item))
                        await _scanTrophiesAsync(item);

                }

                // When we've processed all users in the queue, shut down the scanner.
                _scanner_running = false;

                await OurFoodChainBot.GetInstance().Log(Discord.LogSeverity.Info, "Trophies", "Shutting down trophy scanner");

            });

        }
        private static async Task _scanTrophiesAsync(ScannerQueueItem item) {

            List<Trophy> unlocked = new List<Trophy>();

            foreach (Trophy trophy in TrophyRegistry.Trophies)
                if (await trophy.IsUnlocked(item.userId))
                    unlocked.Add(trophy);

            foreach (Trophy trophy in unlocked)
                await _popTrophyAsync(item, trophy);

        }
        private static async Task _popTrophyAsync(ScannerQueueItem item, Trophy trophy) {

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle(string.Format("🏆 Achievement unlocked!"));
            embed.WithDescription(string.Format("Congratulations {0}! You've earned the **{1}** achievement.", item.context.User.Mention, trophy.GetName()));
            embed.WithFooter(trophy.GetDescription());
            embed.WithColor(new Color(255, 204, 77));

            await item.context.Channel.SendMessageAsync("", false, embed.Build());

        }

    }

}
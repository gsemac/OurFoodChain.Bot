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

        public const long NO_DELAY = 0;

        public class ScannerQueueItem {
            public ICommandContext context;
            public ulong userId;
            public long timestamp;
        }

        public static async Task AddToQueueAsync(ICommandContext context, ulong userId) {
            await AddToQueueAsync(context, userId, DateTimeOffset.Now.ToUnixTimeSeconds());
        }
        public static async Task AddToQueueAsync(ICommandContext context, ulong userId, long timestamp) {

            // If the user already exists in the queue, don't add them again.
            foreach (ScannerQueueItem item in _scan_queue)
                if (item.userId == userId)
                    return;

            // Add the user to the scanner queue.
            _scan_queue.Enqueue(new ScannerQueueItem {
                context = context,
                userId = userId,
                timestamp = timestamp
            });

            // Initialize the trophy registry (nothing will happen if it's already initialized).
            await TrophyRegistry.InitializeAsync();

            // Since there's something in the queue, start the trophy scanner (nothing will happen if it's already active).
            _startScanner();

        }

        // The scan delay is how long to wait before scanning trophies for the next user in the queue.
        private const long SCAN_DELAY = 60 * 5; // 5 minutes
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

            // Get the trophies the user has already unlocked so we don't pop trophies that have already been popped.

            UnlockedTrophyInfo[] already_unlocked = await TrophyRegistry.GetUnlockedTrophiesAsync(item.userId);
            HashSet<string> already_unlocked_identifiers = new HashSet<string>();

            foreach (UnlockedTrophyInfo info in already_unlocked)
                already_unlocked_identifiers.Add(info.identifier);

            // Check for new trophies that the user has just unlocked.

            foreach (Trophy trophy in await TrophyRegistry.GetTrophiesAsync())
                if (!already_unlocked_identifiers.Contains(trophy.GetIdentifier()) && await trophy.IsUnlocked(item)) {

                    // Insert new trophy into the database.
                    await TrophyRegistry.SetUnlocked(item.userId, trophy);

                    // Pop the new trophy.
                    await _popTrophyAsync(item, trophy);

                }

        }
        private static async Task _popTrophyAsync(ScannerQueueItem item, Trophy trophy) {

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle(string.Format("🏆 Trophy unlocked!"));
            embed.WithDescription(string.Format("Congratulations {0}! You've earned the **{1}** trophy.", (await item.context.Guild.GetUserAsync(item.userId)).Mention, trophy.GetName()));
            embed.WithFooter(trophy.GetDescription());
            embed.WithColor(new Color(255, 204, 77));

            await item.context.Channel.SendMessageAsync("", false, embed.Build());

        }

    }

}
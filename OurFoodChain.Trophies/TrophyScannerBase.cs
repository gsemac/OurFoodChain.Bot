using OurFoodChain.Common;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Data;
using OurFoodChain.Debug;
using OurFoodChain.Trophies.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies {

    public abstract class TrophyScannerBase :
         ITrophyScanner {

        // Public members

        public event Func<ILogMessage, Task> Log;
        public event Func<TrophyUnlockedArgs, Task> TrophyUnlocked;

        public int ScanDelay => 60 * 5; // 5 minutes

        public async Task<bool> EnqueueAsync(ITrophyScannerContext context, bool scanImmediately = false) {

            // If the user already exists in the queue, don't add them again.

            if (context.Creator.UserId.HasValue && !queue.Any(item => item.Context.Creator.UserId == context.Creator.UserId)) {

                // Add the user to the scanner queue.

                queue.Enqueue(new QueueItem {
                    Context = context,
                    DateAdded = scanImmediately ? DateTimeOffset.MinValue : DateUtilities.GetCurrentDate()
                });

                // Since there's something in the queue, start the trophy scanner (nothing will happen if it's already active).

                await StartScannerAsync();

                return true;

            }

            return false;

        }

        // Protected members

        protected TrophyScannerBase(ITrophyService trophyService) {

            this.trophyService = trophyService;

        }

        protected async Task OnLogAsync(ILogMessage logMessage) {

            if (Log != null)
                await Log(logMessage);

        }
        protected async Task OnLogAsync(LogSeverity severity, string message) {

            await OnLogAsync(new LogMessage(severity, "Trophies", message));

        }
        protected async Task OnTrophyUnlockedAsync(TrophyUnlockedArgs info) {

            if (TrophyUnlocked != null)
                await TrophyUnlocked(info);

        }

        // Private members

        private class QueueItem {

            public ITrophyScannerContext Context { get; set; }
            public DateTimeOffset DateAdded { get; set; }

        }

        private readonly ITrophyService trophyService;
        private readonly ConcurrentQueue<QueueItem> queue = new ConcurrentQueue<QueueItem>();
        private bool scannerIsActive = false;

        private async Task StartScannerAsync() {

            if (scannerIsActive)
                return;

            scannerIsActive = true;

            _ = Task.Run(async () => {

                await OnLogAsync(LogSeverity.Info, "Starting trophy scanner");

                while (queue.Count > 0) {

                    // Wait until the next item in the queue has been sitting for at least SCAN_DELAY.

                    DateTimeOffset currentDate = DateUtilities.GetCurrentDate();
                    DateTimeOffset itemDate = queue.First().DateAdded;

                    if ((currentDate - itemDate).TotalSeconds < ScanDelay)
                        await Task.Delay(TimeSpan.FromSeconds(ScanDelay - (currentDate - itemDate).TotalSeconds));

                    // Scan trophies for the next item in the queue.

                    if (queue.TryDequeue(out QueueItem item))
                        await ScanTrophiesAsync(item);

                }

                // When we've processed all users in the queue, shut down the scanner.

                scannerIsActive = false;

                await OnLogAsync(LogSeverity.Info, "Shutting down trophy scanner");

            });

            await Task.CompletedTask;

        }
        private async Task ScanTrophiesAsync(QueueItem item) {

            // Get the trophies the user has already unlocked so we don't pop trophies that have already been popped.

            IEnumerable<IUnlockedTrophyInfo> alreadyUnlocked = await item.Context.Database.GetUnlockedTrophiesAsync(item.Context.Creator, trophyService.GetTrophies());
            HashSet<string> alreadyUnlockedIdentifiers = new HashSet<string>();

            foreach (UnlockedTrophyInfo info in alreadyUnlocked)
                alreadyUnlockedIdentifiers.Add(info.Trophy.Identifier);

            // Check for new trophies that the user has just unlocked.

            foreach (ITrophy trophy in trophyService.GetTrophies())

                try {

                    if (!alreadyUnlockedIdentifiers.Contains(trophy.Identifier) && await trophy.CheckTrophyAsync(item.Context)) {

                        // Insert new trophy into the database.

                        await item.Context.Database.UnlockTrophyAsync(item.Context.Creator, trophy);

                        // Pop the new trophy.

                        await OnTrophyUnlockedAsync(new TrophyUnlockedArgs(item.Context, new UnlockedTrophyInfo(item.Context.Creator, trophy)));

                    }

                }
                catch (Exception ex) {

                    // If an error occurs when checking a trophy, we'll just move on to the next one.

                    await OnLogAsync(LogSeverity.Error, $"Exception occured while checking \"{trophy.Name}\" trophy: {ex.ToString()}");

                }

        }

    }

}
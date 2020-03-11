using Discord;
using OurFoodChain.Discord.Utilities;
using OurFoodChain.Gotchis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Extensions {

    public static class OfcModuleBaseGotchiExtensions {

        // Public members

        public static async Task<bool> ReplyValidateGotchiAsync(this OfcModuleBase moduleBase, Gotchi gotchi) {

            if (!gotchi.IsValid()) {

                await moduleBase.ReplyInfoAsync("You don't have a gotchi yet! Get one with `gotchi get <species>`.");

                return false;

            }

            return true;

        }
        public static async Task<string> ReplyUploadGotchiGifAsync(this OfcModuleBase moduleBase, Gotchi gotchi) {

            string filePath = await moduleBase.Db.CreateGotchiGifAsync(gotchi);
            string uploadUrl = string.Empty;

            if (!string.IsNullOrEmpty(filePath))
                uploadUrl = await moduleBase.ReplyUploadFileToScratchChannelAsync(filePath);

            if (string.IsNullOrEmpty(uploadUrl))
                await moduleBase.ReplyErrorAsync("Failed to generate gotchi image.");

            return uploadUrl;

        }

        // Private members

        private static async Task<string> ReplyUploadFileToScratchChannelAsync(this OfcModuleBase moduleBase, string filePath) {

            ulong serverId = moduleBase.Config.ScratchServer;
            ulong channelId = moduleBase.Config.ScratchChannel;

            if (serverId <= 0 || channelId <= 0) {

                await moduleBase.ReplyErrorAsync("Cannot upload images because no scratch server/channel has been specified in the configuration file.");

                return string.Empty;

            }

            IGuild guild = moduleBase.DiscordClient.GetGuild(serverId);

            if (guild is null) {

                await moduleBase.ReplyErrorAsync("Cannot upload images because the scratch server is inaccessible.");

                return string.Empty;

            }

            ITextChannel channel = await guild.GetTextChannelAsync(channelId);

            if (channel is null) {

                await moduleBase.ReplyErrorAsync("Cannot upload images because the scratch channel is inaccessible.");

                return string.Empty;

            }

            return await DiscordUtilities.UploadFileAsync(channel, filePath, FileUploadOptions.DeleteFileAfterUpload);

        }

    }

}
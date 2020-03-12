using Discord;
using Discord.WebSocket;
using OurFoodChain.Bot;
using OurFoodChain.Discord.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Services {

    public class FileUploadService {

        // Public members

        public FileUploadService(DiscordSocketClient client, IOfcBotConfiguration config) {

            this.client = client;
            this.config = config;

        }

        public async Task<string> UploadFileAsync(string filePath, IMessageChannel channel = null, FileUploadOptions options = FileUploadOptions.None) {

            if (channel is null) {

                ulong serverId = config.ScratchServer;
                ulong channelId = config.ScratchChannel;

                if (serverId <= 0 || channelId <= 0)
                    throw new Exception("Cannot upload images because no scratch server/channel has been specified in the configuration file.");

                IGuild guild = client.GetGuild(serverId);

                if (guild is null)
                    throw new Exception("Cannot upload images because the scratch server is inaccessible.");

                channel = await guild.GetTextChannelAsync(channelId);

                if (channel is null)
                    throw new Exception("Cannot upload images because the scratch channel is inaccessible.");



            }

            return await DiscordUtilities.UploadFileAsync(channel, filePath, options);

        }

        // Private members

        private readonly DiscordSocketClient client;
        private readonly IOfcBotConfiguration config;

    }

}
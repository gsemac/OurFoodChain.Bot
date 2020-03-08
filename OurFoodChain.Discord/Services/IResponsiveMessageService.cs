using Discord;
using Discord.Commands;
using OurFoodChain.Discord.Messaging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public interface IResponsiveMessageService {

        Task<IResponsiveMessageResponse> GetResponseAsync(ICommandContext context, string message, bool allowCancellation = true);
        Task<IResponsiveMessageResponse> GetResponseAsync(ICommandContext context, Messaging.IMessage message, bool allowCancellation = true);

    }

}
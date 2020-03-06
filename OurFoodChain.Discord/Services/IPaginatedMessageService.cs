using Discord.Commands;
using OurFoodChain.Discord.Messaging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public interface IPaginatedMessageService {

        Task SendMessageAsync(ICommandContext context, IPaginatedMessage message);
        Task SendMessageAndWaitAsync(ICommandContext context, IPaginatedMessage message);

    }

}
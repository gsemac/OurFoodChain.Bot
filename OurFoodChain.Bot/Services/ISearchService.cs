using Discord.Commands;
using OurFoodChain.Taxa;
using System.Threading.Tasks;

namespace OurFoodChain.Bot.Services {

    public interface ISearchService {

        Task<Taxa.SearchResult> GetQueryResultAsync(ICommandContext context, SearchQuery searchQuery);

    }

}
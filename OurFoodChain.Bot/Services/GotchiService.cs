using Discord.Commands;
using OurFoodChain.Discord.Services;
using OurFoodChain.Extensions;
using OurFoodChain.Gotchis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Services {

    public class GotchiService {

        // Public members

        public GotchiService(FileUploadService uploadService, IDatabaseService databaseService) {

            this.uploadService = uploadService;
            this.databaseService = databaseService;

        }

        public async Task<string> CreateGifAsync(ICommandContext context, IEnumerable<GotchiGifCreatorParams> gifParams, GotchiGifCreatorExtraParams extraGifParams) {

            string gifFilePath = await (await databaseService.GetDatabaseAsync(context)).CreateGotchiGifAsync(gifParams.ToArray(), extraGifParams);
            string uploadUrl = string.Empty;

            if (!string.IsNullOrEmpty(gifFilePath))
                uploadUrl = await uploadService.UploadFileAsync(gifFilePath);

            if (string.IsNullOrEmpty(uploadUrl))
                throw new Exception("Failed to generate gotchi image.");

            return uploadUrl;

        }

        // Private members

        private readonly FileUploadService uploadService;
        private readonly IDatabaseService databaseService;

    }

}
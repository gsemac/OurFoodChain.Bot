using OurFoodChain.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    public interface ISearchContext {

        SQLiteDatabase Database { get; }

        Task<ICreator> GetCreatorAsync(string name);

    }

}
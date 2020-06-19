using OurFoodChain.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    public interface ISearchContext {

        SQLiteDatabase Database { get; }

        void RegisterSearchModifier<T>() where T : ISearchModifier, new();
        ISearchModifier GetSearchModifier(string modifier);
        ISearchModifier GetSearchModifier(string name, string value);

        Task<IUser> GetCreatorAsync(IUser creator);

    }

}
using OurFoodChain.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data {

    public interface IOfcDatabase {

        Task AddUserAsync(IUser creator);
        Task<IUser> GetUserAsync();

    }

}
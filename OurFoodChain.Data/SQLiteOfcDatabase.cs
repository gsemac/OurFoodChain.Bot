using OurFoodChain.Common;
using OurFoodChain.Data.Extensions;
using System.Threading.Tasks;

namespace OurFoodChain.Data {

    public class SQLiteOfcDatabase :
        SQLiteDatabase,
        IOfcDatabase {

        // Public members

        public SQLiteOfcDatabase(string connectionString) :
            base(connectionString) {
        }
        public SQLiteOfcDatabase(ISQLiteDatabase database) :
            this(database.ConnectionString) {
        }

        public async Task AddUserAsync(IUser user) => await (this as ISQLiteDatabase).AddUserAsync(user);
        public async Task<IUser> GetUserAsync() => await (this as ISQLiteDatabase).GetUserAsync();

    }

}
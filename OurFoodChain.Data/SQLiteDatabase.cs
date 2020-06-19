namespace OurFoodChain.Data {

    public class SQLiteDatabase :
        ISQLiteDatabase {

        // Public members

        public string ConnectionString { get; }

        public SQLiteDatabase(string connectionString) {

            this.ConnectionString = connectionString;

        }

        public static SQLiteDatabase FromFile(string filePath) {

            return new SQLiteDatabase(string.Format("Data Source={0}", filePath));

        }

    }

}
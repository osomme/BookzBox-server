using BooxBox.DataAccess;

namespace BooxBox.DataAccess.Repositories
{
    public class BaseRepository : IBaseRepository
    {
        protected readonly IDatabase _database;

        public BaseRepository(IDatabase database)
        {
            _database = database ?? throw new System.ArgumentNullException(nameof(database));
        }
    }
}
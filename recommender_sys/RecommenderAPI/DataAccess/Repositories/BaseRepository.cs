using BooxBox.DataAccess;

namespace BooxBox.DataAccess.Repositories
{
    public class BaseRepository
    {
        protected readonly static IDatabase _database = new Database();
    }
}
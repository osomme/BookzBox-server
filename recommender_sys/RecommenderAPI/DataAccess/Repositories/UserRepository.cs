using System.Threading.Tasks;
using Models;
using Neo4j.Driver;

namespace BooxBox.DataAccess.Repositories
{
    public class UserRepository : BaseRepository
    {

        public async Task AddAsync(User user)
        {
            if (user is null)
            {
                throw new System.ArgumentNullException(nameof(user));
            }

            try
            {
                IResultCursor cursor = await _database.Session.RunAsync("CREATE (u:User {id:'" + user.Id + "'})");
                await cursor.ConsumeAsync();
            }
            finally
            {
                await _database.CloseSessionAsync();
            }
        }
    }
}
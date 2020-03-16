using System.Collections.Generic;
using System.Threading.Tasks;
using Models;
using Neo4j.Driver;

namespace BooxBox.DataAccess.Repositories
{
    public class UserRepository : BaseRepository
    {

        /// Adds the passed user to the database if the user does not already exist.
        public async Task AddAsync(User user)
        {
            if (user is null)
            {
                throw new System.ArgumentNullException(nameof(user));
            }

            try
            {
                IResultCursor cursor = await _database.Session.RunAsync("MERGE (u:User {userId:'" + user.Id + "'})");
                await cursor.ConsumeAsync();
            }
            finally
            {
                await _database.CloseSessionAsync();
            }
        }

        public async Task AddPublisherRelationshipAsync(string userId, string boxId)
        {
            if (userId is null)
            {
                throw new System.ArgumentNullException(nameof(userId));
            }

            if (boxId is null)
            {
                throw new System.ArgumentNullException(nameof(boxId));
            }

            try
            {
                IResultCursor cursor = await _database.Session.RunAsync(
                    $"MATCH (u:User {{userId: '{userId}'}}),(b:Box {{boxId: '{boxId}'}}) MERGE (u)-[:PUBlISHED]-(b)"
                    );
                await cursor.ConsumeAsync();
            }
            finally
            {
                await _database.CloseSessionAsync();
            }

        }


    }
}
using System;
using System.Threading.Tasks;
using Models;
using Neo4j.Driver;

namespace BooxBox.DataAccess.Repositories
{
    public class LikeRepository : BaseRepository, ILikeRepository
    {
        public LikeRepository(IDatabase db)
            : base(db)
        {

        }

        public async Task LikeAsync(string userId, string boxId)
        {
            try
            {
                IResultCursor cursor = await _database.Session.RunAsync(
                    $"MATCH (u:User {{userId: '{userId}'}}),(b:Box {{boxId: '{boxId}'}}) MERGE (u)-[:LIKES]-(b)"
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
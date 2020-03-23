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
            if (userId is null || boxId is null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

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

        public async Task RemoveLikeAsync(string userId, string boxId)
        {
            if (userId is null || boxId is null)
            {
                throw new ArgumentNullException(nameof(userId));
            }
            try
            {
                IResultCursor cursor = await _database.Session.RunAsync(
                    $"MATCH (u:User {{userId: '{userId}'}})-[r:LIKES]-(b:Box {{boxId: '{boxId}'}}) DELETE r"
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
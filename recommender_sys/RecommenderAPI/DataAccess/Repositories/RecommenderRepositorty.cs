using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Models;
using Neo4j.Driver;

namespace BooxBox.DataAccess.Repositories
{
    public class RecommenderRepository : BaseRepository
    {
        private readonly IRecordMapper<Box> _boxMapper;

        public RecommenderRepository(IRecordMapper<Box> boxMapper)
        {
            _boxMapper = boxMapper ?? throw new ArgumentNullException(nameof(boxMapper));
        }

        /// Fetch recommendations for the passed user.
        public async Task<IEnumerable<Box>> FetchRecommendationsAsync(string userId)
        {

            return await FetchBoxesWithSubjectsMatchingThoseOfMyBoxes(userId); ;
        }


        /// Fetches boxes that have the same subjects as the boxes that the user with the
        /// given user id has published.
        private async Task<IEnumerable<Box>> FetchBoxesWithSubjectsMatchingThoseOfMyBoxes(string userId)
        {
            List<Box> result = null;
            try
            {
                IResultCursor cursor = await _database.Session.RunAsync(
                    $"MATCH (user:User {{userId: '{userId}'}})-[:PUBlISHED]-(:Box)-[:PART_OF]-(:Book)-[:HAS_SUBJECT]-(s:Subject)-[:IN_BOOK]->(book:Book)-[*0..1]-(box:Box) " +
                    "WHERE box.status = 0 " +
                    "RETURN box, collect(book) as books, collect(s) as subjects"
                );

                result = await cursor.ToListAsync(record => _boxMapper.Map(record));

                await cursor.ConsumeAsync();

            }
            finally
            {
                await _database.CloseSessionAsync();
            }
            return result;
        }
    }
}
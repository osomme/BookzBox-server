using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Models;
using Neo4j.Driver;

namespace BooxBox.DataAccess.Repositories
{
    public class RecommenderRepository : BaseRepository
    {
        /// Fetch recommendations for the passed user.
        public async Task<IEnumerable<Box>> FetchRecommendations(string userid)
        {
            // TODO implement recommendation query.
            return null;
        }
    }
}
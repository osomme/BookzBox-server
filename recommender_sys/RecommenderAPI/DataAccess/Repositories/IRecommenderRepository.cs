using System.Collections.Generic;
using System.Threading.Tasks;
using Models;

namespace BooxBox.DataAccess.Repositories
{
    public interface IRecommenderRepository : IBaseRepository
    {
        public Task<IEnumerable<Box>> FetchRecommendationsAsync(
            string userId,
            int limit,
            double latitude,
            double longitude,
            bool mark);
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using Models;

namespace BooxBox.DataAccess.Repositories
{
    public interface IRecommenderRepository : IBaseRepository
    {
        public Task<IEnumerable<Box>> FetchRecommendationsAsync(string userId, uint limit);
    }
}
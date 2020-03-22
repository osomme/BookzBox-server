using System.Threading.Tasks;
using Models;

namespace BooxBox.DataAccess.Repositories
{
    public interface IUserRepository : IBaseRepository
    {
        public Task AddAsync(User user);
        public Task AddPublisherRelationshipAsync(string userId, string boxId);
    }
}
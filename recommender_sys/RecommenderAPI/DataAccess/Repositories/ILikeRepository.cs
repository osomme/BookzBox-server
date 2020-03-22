using System.Threading.Tasks;
using Models;

namespace BooxBox.DataAccess.Repositories
{
    public interface ILikeRepository : IBaseRepository
    {
        public Task LikeAsync(string userId, string boxId);
    }
}
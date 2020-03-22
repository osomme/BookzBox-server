using System.Threading.Tasks;
using Models;

namespace BooxBox.DataAccess.Repositories
{
    public interface IBoxRepository : IBaseRepository
    {
        public Task AddAsync(Box box);
        public Task UpdateStatusAsync(string boxId, BoxStatus newStatus);

        public Task DeleteBoxAync(string boxId);
    }
}
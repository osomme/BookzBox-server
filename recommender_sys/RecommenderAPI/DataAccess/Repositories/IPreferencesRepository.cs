using System.Threading.Tasks;
using Models;

namespace BooxBox.DataAccess.Repositories
{
    public interface IPreferencesRepository : IBaseRepository
    {

        public Task UpdatePrefferedSubjectsAsync(string userId, string[] subjects);
    }
}
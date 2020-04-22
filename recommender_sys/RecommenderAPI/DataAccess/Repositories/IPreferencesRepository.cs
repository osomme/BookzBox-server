using System.Threading.Tasks;
using Models;

namespace BooxBox.DataAccess.Repositories
{
    public interface IPreferencesRepository : IBaseRepository
    {

        public Task UpdatePreferredSubjectsAsync(string userId, string[] subjects);
    }
}
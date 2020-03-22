using System.Threading.Tasks;
using Models;

namespace BooxBox.DataAccess.Repositories
{
    public interface ISubjectRepository : IBaseRepository
    {
        public Task AddAsync(string subject);
        public Task AddInBookRelationshipAsync(Book book, string subject);

    }
}
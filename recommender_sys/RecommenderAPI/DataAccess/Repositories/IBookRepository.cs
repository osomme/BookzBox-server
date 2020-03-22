using System.Threading.Tasks;
using Models;

namespace BooxBox.DataAccess.Repositories
{
    public interface IBookRepository : IBaseRepository
    {
        public Task AddAsync(Book book);
        public Task AddPartOfBoxRelationshipAsync(Book book, Box box);

        public Task AddHasSubjectRelationshipAsync(Book book, string subject);
    }
}
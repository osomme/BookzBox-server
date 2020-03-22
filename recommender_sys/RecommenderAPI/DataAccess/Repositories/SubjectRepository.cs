using System;
using System.Threading.Tasks;
using Models;
using Neo4j.Driver;

namespace BooxBox.DataAccess.Repositories
{
    /// Database handling for book subjects/genres
    public class SubjectRepository : BaseRepository, ISubjectRepository
    {
        public SubjectRepository(IDatabase db)
            : base(db)
        {

        }

        public async Task AddAsync(string subject)
        {
            if (subject is null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            try
            {
                IResultCursor cursor = await _database.Session.RunAsync(
                    $"MERGE (s:Subject {{name: '{subject}'}})"
                );
                await cursor.ConsumeAsync();
            }
            finally
            {
                await _database.CloseSessionAsync();
            }
        }

        public async Task AddInBookRelationshipAsync(Book book, string subject)
        {
            try
            {
                IResultCursor cursor = await _database.Session.RunAsync(

                        $"MATCH (b:Book {{thumbnailUrl: '{book.ThumbnailUrl}'}}),(s:Subject {{name: '{subject}'}}) MERGE (s)-[r:IN_BOOK]->(b)"

                );
                await cursor.ConsumeAsync();
            }
            finally
            {
                await _database.CloseSessionAsync();
            }
        }
    }
}
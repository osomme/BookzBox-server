using System;
using System.Threading.Tasks;
using Models;
using Neo4j.Driver;

namespace BooxBox.DataAccess.Repositories
{
    /// Database handling for book subjects/genres
    public class SubjectRepository : BaseRepository
    {
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
    }
}
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Models;
using Neo4j.Driver;
using Newtonsoft.Json;

namespace BooxBox.DataAccess.Repositories
{
    public class BookRepository : BaseRepository, IBookRepository
    {
        private readonly ISubjectRepository _subjectRepo;

        public BookRepository(IDatabase db, ISubjectRepository subjectRepo)
            : base(db)
        {
            _subjectRepo = subjectRepo ?? throw new ArgumentNullException(nameof(subjectRepo));
        }

        public async Task AddAsync(Book book)
        {
            if (book is null)
            {
                throw new ArgumentNullException(nameof(book));
            }

            await AddBookNodeAsync(book);

            foreach (string subject in book.Categories)
            {
                await _subjectRepo.AddAsync(subject);
                await _subjectRepo.AddInBookRelationshipAsync(book, subject);
                await AddHasSubjectRelationshipAsync(book, subject);
            }
        }

        public async Task AddPartOfBoxRelationshipAsync(Book book, Box box)
        {
            try
            {
                IResultCursor cursor = await _database.Session.RunAsync(

                        $"MATCH (book:Book {{isbn: '{book.Isbn}'}}),(box:Box {{boxId: '{box.Id}'}}) " +
                        "MERGE (book)-[:PART_OF]-(box)"

                );
                await cursor.ConsumeAsync();
            }
            finally
            {
                await _database.CloseSessionAsync();
            }
        }

        public async Task AddHasSubjectRelationshipAsync(Book book, string subject)
        {
            try
            {
                IResultCursor cursor = await _database.Session.RunAsync(

                        $"MATCH (b:Book {{isbn: '{book.Isbn}'}}),(s:Subject {{name: '{subject.ToLower().Trim()}'}}) MERGE (b)-[:HAS_SUBJECT]-(s)"

                );
                await cursor.ConsumeAsync();
            }
            finally
            {
                await _database.CloseSessionAsync();
            }
        }

        private async Task AddBookNodeAsync(Book book)
        {
            try
            {
                string subjectsJson = JsonConvert.SerializeObject(book.Categories);
                IResultCursor cursor = await _database.Session.RunAsync(
                    $"MERGE (b:Book {{isbn: '{book.Isbn}', thumbnailUrl: '{book.ThumbnailUrl}', subjects: '{subjectsJson}'}})"
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
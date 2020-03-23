using System;
using System.Threading.Tasks;
using Models;
using Neo4j.Driver;

namespace BooxBox.DataAccess.Repositories
{
    public class BoxRepository : BaseRepository, IBoxRepository
    {

        private readonly IUserRepository _userRepo;
        private readonly IBookRepository _bookRepo;

        public BoxRepository(IDatabase db, IUserRepository userRepo, IBookRepository bookRepo)
            : base(db)
        {
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _bookRepo = bookRepo ?? throw new ArgumentNullException(nameof(bookRepo));
        }

        /// Adds the passed box to the database if it does not already exist.
        public async Task AddAsync(Box box)
        {
            if (box is null)
            {
                throw new System.ArgumentNullException(nameof(box));
            }

            await AddBoxNodeAsync(box);
            await _userRepo.AddAsync(new User(box.publisher));
            await _userRepo.AddPublisherRelationshipAsync(box.publisher, box.Id);

            foreach (Book book in box.Books)
            {
                await _bookRepo.AddAsync(book);
                await _bookRepo.AddPartOfBoxRelationshipAsync(book, box);
            }
        }

        public async Task UpdateStatusAsync(string boxId, BoxStatus newStatus)
        {
            try
            {
                IResultCursor cursor = await _database.Session.RunAsync(
                    $"MATCH (b:Box {{boxId: '{boxId}'}}) SET b.status = {(int)newStatus}"
                );
                await cursor.ConsumeAsync();
            }
            finally
            {
                await _database.CloseSessionAsync();
            }
        }

        public async Task DeleteBoxAync(string boxId)
        {
            try
            {
                IResultCursor cursor = await _database.Session.RunAsync(
                    $"MATCH (box:Box {{boxId: '{boxId}'}}) DETACH DELETE box"
                );
                await cursor.ConsumeAsync();
            }
            finally
            {
                await _database.CloseSessionAsync();
            }
        }

        /// Adds the box node to the database.
        private async Task AddBoxNodeAsync(Box box)
        {
            try
            {
                IResultCursor cursor = await _database.Session.RunAsync(
                    $"MERGE (b:Box {{ boxId: '{box.Id}', publisherId: '{box.publisher}', publishedOn: {box.publishDateTime}, title: '{box.Title}', description: '{box.Description}', lat: {(box.Latitude <= 0 ? 0.1 : box.Latitude)}, lng: {(box.Longitude <= 0 ? 0.1 : box.Longitude)}, status: {(int)box.Status}}})"
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
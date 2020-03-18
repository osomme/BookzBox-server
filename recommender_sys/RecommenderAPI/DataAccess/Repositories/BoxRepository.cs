using System;
using System.Threading.Tasks;
using Models;
using Neo4j.Driver;

namespace BooxBox.DataAccess.Repositories
{
    public class BoxRepository : BaseRepository
    {

        private readonly UserRepository _userRepo;
        private readonly BookRepository _bookRepo;

        public BoxRepository(UserRepository userRepo, BookRepository bookRepo)
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
            await _userRepo.AddAsync(new User(box.PublisherId));
            await _userRepo.AddPublisherRelationshipAsync(box.PublisherId, box.Id);

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

        /// Adds the box node to the database.
        private async Task AddBoxNodeAsync(Box box)
        {
            try
            {
                IResultCursor cursor = await _database.Session.RunAsync(
                    $"MERGE (b:Box {{ boxId: '{box.Id}', publisherId: '{box.PublisherId}', publishedOn: {((DateTimeOffset)box.PublishedOn).ToUnixTimeSeconds()}, title: '{box.Title}', description: '{box.Description}', lat: {(box.Lat <= 0 ? 0.0 : box.Lat)}, lng: {(box.Lng <= 0 ? 0.0 : box.Lng)}, status: {(int)box.Status}}})"
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
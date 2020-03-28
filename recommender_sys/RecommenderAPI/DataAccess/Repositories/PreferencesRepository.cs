using System.Collections.Generic;
using System.Threading.Tasks;
using BooxBox.DataAccess.Repositories;
using Models;
using Neo4j.Driver;

public class PreferencesRepository : BaseRepository, IPreferencesRepository
{

    private readonly IUserRepository _userRepo;
    private readonly ISubjectRepository _subjectRepo;

    public PreferencesRepository(IDatabase db, IUserRepository userRepo, ISubjectRepository subjectRepo)
        : base(db)
    {
        _userRepo = userRepo ?? throw new System.ArgumentNullException(nameof(userRepo));
        _subjectRepo = subjectRepo ?? throw new System.ArgumentNullException(nameof(subjectRepo));
    }

    public async Task UpdatePrefferedSubjectsAsync(string userId, string[] subjects)
    {

        if (userId is null)
        {
            throw new System.ArgumentNullException(nameof(userId));
        }

        if (subjects is null)
        {
            throw new System.ArgumentNullException(nameof(subjects));
        }

        await _userRepo.AddAsync(new User(userId));

        foreach (string subject in subjects)
        {
            await _subjectRepo.AddAsync(subject);
        }

        await RemovePreferedSubjects(userId);

        foreach (string subject in subjects)
        {
            await AddPreferedSubject(userId, subject);
        }

    }

    /// Removes all 'PREFERS'-relationships on subjects for the
    /// passed user.
    private async Task RemovePreferedSubjects(string userId)
    {
        try
        {
            IResultCursor cursor = await _database.Session.RunAsync(
                $"MATCH (u:User {{userId: '{userId}'}})-[p:PREFERS]-(s:Subject) DELETE p"
                );
            await cursor.ConsumeAsync();
        }
        finally
        {
            await _database.CloseSessionAsync();
        }
    }

    /// Adds a 'PREFERS' relationship between the user with the given 
    /// id and the given subject. This method assumes that both the user
    /// and the subject exist as nodes in the database.
    private async Task AddPreferedSubject(string userId, string subject)
    {
        try
        {
            IResultCursor cursor = await _database.Session.RunAsync(
                $"MATCH (u:User {{userId: '{userId}'}}),(s:Subject {{name: '{subject.ToLower().Trim()}'}}) MERGE (u)-[:PREFERS]-(s)"
                );
            await cursor.ConsumeAsync();
        }
        finally
        {
            await _database.CloseSessionAsync();
        }

    }

}
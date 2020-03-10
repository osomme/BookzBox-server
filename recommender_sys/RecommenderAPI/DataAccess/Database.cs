using System.Threading.Tasks;
using Neo4j.Driver;

namespace BooxBox.DataAccess
{
    public class Database : IDatabase
    {
        private static string DB_URI = "neo4j://localhost:7687";
        private static string DB_NAME = "graph.db";

        private IDriver _driver;

        private IAsyncSession _session;

        private bool _hasOpenSession;

        public Database()
        {
            _driver = GraphDatabase.Driver(DB_URI, AuthTokens.Basic(DatabaseAuth.DB_USERNAME, DatabaseAuth.DB_PASSWORD));
            _session = null;
            _hasOpenSession = false;
        }


        public IDriver Driver => _driver;

        public IAsyncSession Session
        {
            get
            {
                if (!_hasOpenSession)
                {
                    _session = Driver.AsyncSession(o => o.WithDatabase(DB_NAME));
                    _hasOpenSession = true;
                }

                return _session;
            }
        }

        public async Task CloseSessionAsync()
        {
            await _session.CloseAsync();
            _hasOpenSession = false;
        }
    }
}
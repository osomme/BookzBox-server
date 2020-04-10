using System;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace BooxBox.DataAccess
{
    public class Database : IDatabase, IDisposable
    {
        private static string DB_URI = "bolt://localhost:7687";
        private static string DB_NAME = "neo4j";

        private IDriver _driver;

        private IAsyncSession _session;

        private bool _hasOpenSession;

        public Database(IDatabaseAuth auth)
        {
            _driver = GraphDatabase.Driver(DB_URI, AuthTokens.Basic(auth.Username, auth.Password));
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

        public void Dispose()
        {
            _driver?.Dispose();
        }
    }
}
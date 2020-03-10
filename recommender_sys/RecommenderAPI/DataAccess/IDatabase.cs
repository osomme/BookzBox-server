using System.Threading.Tasks;
using Neo4j.Driver;

public interface IDatabase
{
    IDriver Driver { get; }

    IAsyncSession Session { get; }

    Task CloseSessionAsync();

}
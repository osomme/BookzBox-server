
using Neo4j.Driver;

public interface IRecordMapper<T>
{
    T Map(IRecord record);
}
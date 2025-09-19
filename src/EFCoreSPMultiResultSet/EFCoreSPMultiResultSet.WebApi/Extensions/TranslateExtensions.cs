using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using EFCoreSPMultiResultSet.WebApi.Model;

public static class TranslateExtensions
{
    private static readonly ConcurrentDictionary<Type, object> _materializerMap = new();

    public static T Translate<T>(this DbDataReader reader) where T : new()
    {
        var record = (Func<IDataRecord, T>)Materializer.Materialize<T>;
        var materializer = (Func<IDataRecord, T>)_materializerMap.GetOrAdd(typeof(T), record);
        return reader.Translate(materializer, out var hasNextResults);
    }

    public static IList<T> TranslateList<T>(this DbDataReader reader) where T : new()
    {
        var record = (Func<IDataRecord, T>)Materializer.Materialize<T>;
        var materializer = (Func<IDataRecord, T>)_materializerMap.GetOrAdd(typeof(T), record);
        return reader.TransletList(materializer, out var hasNextResults);
    }

    private static IList<T> TransletList<T>(this DbDataReader reader, Func<IDataRecord, T> objectMaterializer, out bool hasNextResult)
    {
        var results = new List<T>();
        while (reader.Read())
        {
            var record = (IDataRecord)reader;
            var obj = objectMaterializer(record);
            results.Add(obj);
        }

        hasNextResult = reader.NextResult();
        return results;
    }

    private static T Translate<T>(this DbDataReader reader, Func<IDataRecord, T> objectMaterializer, out bool hasNextResult)
    {
        reader.Read();

        var record = (IDataRecord)reader;
        var obj = objectMaterializer(record);

        hasNextResult = reader.NextResult();
        return obj;
    }
}
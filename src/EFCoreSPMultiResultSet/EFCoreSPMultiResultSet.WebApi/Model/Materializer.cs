using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace EFCoreSPMultiResultSet.WebApi.Model;

public record Materializer
{
    public static T Materialize<T>(IDataRecord record) where T : new()
    {
        var t = new T();
        foreach (var prop in typeof(T).GetProperties())
        {
            if (prop.PropertyType.Namespace == typeof(T).Namespace)
            {
                continue;
            }

            if (prop.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(prop.PropertyType))
            {
                continue;
            }

            if (Attribute.IsDefined(prop, typeof(NotMappedAttribute)))
            {
                continue;
            }

            var dbValue = record[prop.Name];
            if (dbValue is DBNull) continue;

            if (prop.PropertyType.IsConstructedGenericType &&
                prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var baseType = prop.PropertyType.GetGenericArguments()[0];
                var baseValue = Convert.ChangeType(dbValue, baseType);
                var value = Activator.CreateInstance(prop.PropertyType, baseValue);
                prop.SetValue(t, value);
            }
            else
            {
                var value = Convert.ChangeType(dbValue, prop.PropertyType);
                prop.SetValue(t, value);
            }
        }
        return t;
    }
}
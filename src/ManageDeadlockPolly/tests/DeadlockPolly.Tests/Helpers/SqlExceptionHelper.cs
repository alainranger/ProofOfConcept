using System.Reflection;
using Microsoft.Data.SqlClient;

namespace DeadlockPolly.Tests.Helpers;

/// <summary>
/// Utilitaire pour créer des SqlException avec un numéro d'erreur précis via réflexion.
/// SqlException n'expose pas de constructeur public, ce helper contourne cette limitation
/// pour les tests unitaires.
/// </summary>
internal static class SqlExceptionHelper
{
    /// <summary>Crée une SqlException simulant un deadlock (erreur 1205).</summary>
    public static SqlException CreateDeadlockException()
        => CreateSqlException(1205, "Transaction was deadlocked on lock resources with another process and has been chosen as the deadlock victim.");

    /// <summary>Crée une SqlException avec le numéro et le message spécifiés.</summary>
    public static SqlException CreateSqlException(int errorNumber, string message = "SQL error")
    {
        var collectionCtor = typeof(SqlErrorCollection)
            .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null)
            ?? throw new InvalidOperationException("SqlErrorCollection constructor not found");

        var errorCtor = typeof(SqlError)
            .GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(int), typeof(byte), typeof(byte), typeof(string),
                        typeof(string), typeof(string), typeof(int), typeof(uint), typeof(Exception) },
                null)
            ?? throw new InvalidOperationException("SqlError constructor not found");

        var addMethod = typeof(SqlErrorCollection)
            .GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("SqlErrorCollection.Add method not found");

        var createExceptionMethod = typeof(SqlException)
            .GetMethod("CreateException", BindingFlags.NonPublic | BindingFlags.Static,
                null, new[] { typeof(SqlErrorCollection), typeof(string) }, null)
            ?? throw new InvalidOperationException("SqlException.CreateException method not found");

        var collection = collectionCtor.Invoke(null);

        var error = errorCtor.Invoke(new object[]
        {
            errorNumber, (byte)0, (byte)11, "server", message, "procedure", 0, (uint)0, null!
        });

        addMethod.Invoke(collection, new[] { error });

        return (SqlException)createExceptionMethod.Invoke(null, new[] { collection, "11.0.0.0" })!;
    }
}

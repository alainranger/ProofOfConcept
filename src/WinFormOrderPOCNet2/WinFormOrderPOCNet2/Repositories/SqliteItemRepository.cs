using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using WinFormOrderPOCNet2.Interfaces;
using WinFormOrderPOCNet2.Model;

namespace WinFormOrderPOCNet2.Repositories
{
    public class SqliteItemRepository : IItemRepository
    {
        private readonly string dbPath;
        private string ConnectionString => string.Format("Data Source={0};Version=3;", dbPath);

        public SqliteItemRepository(string databasePath)
        {
            dbPath = databasePath;
        }

        public void Initialize()
        {
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
                using (var conn = CreateOpenConnection())
                {
                    ExecuteNonQuery(conn, @"CREATE TABLE Items (
                        ItemId INTEGER PRIMARY KEY AUTOINCREMENT,
                        ItemDesc TEXT NOT NULL,
                        ItemOrder INT NOT NULL
                    );");

                    ExecuteNonQuery(conn, @"INSERT INTO Items (ItemDesc, ItemOrder) VALUES
                        ('Item A', 1),
                        ('Item B', 2),
                        ('Item C', 3);");
                }
            }
        }

        public List<Item> GetAll()
        {
            var result = new List<Item>();
            using (var conn = CreateOpenConnection())
            using (var cmd = new SQLiteCommand("SELECT ItemId, ItemDesc, ItemOrder FROM Items ORDER BY ItemOrder", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    result.Add(new Item
                    {
                        ItemId = reader.GetInt32(0),
                        ItemDesc = reader.GetString(1),
                        ItemOrder = reader.GetInt32(2)
                    });
                }
            }
            return result;
        }

        public void UpdateItemOrder(Item item)
        {
            using (var conn = CreateOpenConnection())
            using (var cmd = new SQLiteCommand("UPDATE Items SET ItemOrder = @order WHERE ItemId = @id", conn))
            {
                cmd.Parameters.AddWithValue("@order", item.ItemOrder);
                cmd.Parameters.AddWithValue("@id", item.ItemId);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateAllItemsOrder(List<Item> items)
        {
            using (var conn = CreateOpenConnection())
            {
                foreach (var item in items)
                {
                    using (var cmd = new SQLiteCommand("UPDATE Items SET ItemOrder = @order WHERE ItemId = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@order", item.ItemOrder);
                        cmd.Parameters.AddWithValue("@id", item.ItemId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private SQLiteConnection CreateOpenConnection()
        {
            var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            return conn;
        }

        private void ExecuteNonQuery(SQLiteConnection conn, string commandText)
        {
            using (var cmd = new SQLiteCommand(commandText, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }
}
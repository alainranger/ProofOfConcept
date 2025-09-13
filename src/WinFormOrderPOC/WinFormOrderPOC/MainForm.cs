using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using WinFormOrderPOC.Model;

namespace WinFormOrderPOC
{
	public partial class MainForm : Form
	{
        private readonly List<Item> items = new List<Item>();
        private readonly string dbPath = "data.db";
		private string ConnectionString => $"Data Source={dbPath};Version=3;";

        public MainForm()
		{
			InitializeComponent();

            InitializeDatabase();
            LoadItems();
        }

        private void InitializeDatabase()
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

                    // Données initiales
                    ExecuteNonQuery(conn, @"INSERT INTO Items (ItemDesc, ItemOrder) VALUES
                                            ('Item A', 1),
                                            ('Item B', 2),
                                            ('Item C', 3);");
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

        private List<Item> GetItems(SQLiteConnection conn)
        {
            var result = new List<Item>();
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

        private void LoadItems()
        {   
            items.Clear();
            using (var conn = CreateOpenConnection())
            {
                items.AddRange(GetItems(conn));
            }
            RefreshListBox();
        }

        private void RefreshListBox()
        {
            listBox1.DataSource = null;
            listBox1.DataSource = items.OrderBy(i => i.ItemOrder).ToList();
        }

        private void RecalculateOrders()
        {
            for (int i = 0; i < items.Count; i++)
            {
                items[i].ItemOrder = i + 1;
            }
        }

        private void BtnUp_Click(object sender, EventArgs e)
        {
            int index = listBox1.SelectedIndex;
            if (index > 0)
            {
                SwapItems(index, index - 1);
                listBox1.SelectedIndex = index - 1;
            }
        }

        private void BtnDown_Click(object sender, EventArgs e)
        {
            int index = listBox1.SelectedIndex;
            if (index < items.Count - 1 && index >= 0)
            {
                SwapItems(index, index + 1);
                listBox1.SelectedIndex = index + 1;
            }
        }

        private void SwapItems(int indexA, int indexB)
        {
			(items[indexB], items[indexA]) = (items[indexA], items[indexB]);
			RecalculateOrders();
            RefreshListBox();
        }

        private void BtnSave_Click(object sender, EventArgs e)
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
            MessageBox.Show("Ordre sauvegardé !");
        }
    }
}

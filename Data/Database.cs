using Microsoft.Data.Sqlite;
using DictionaryApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DictionaryApp.Data
{
    public static class Database
    {
        private static readonly string DbFile = "app.db";
        private static readonly string ConnectionString = $"Data Source={DbFile}";

        // Ініціалізація БД (створює файл і таблиці при першому запуску)
        public static void Initialize()
        {
            var first = !File.Exists(DbFile);

            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            // Увімкнення зовнішніх ключів БД
            using (var cmdFk = conn.CreateCommand())
            {
                cmdFk.CommandText = "PRAGMA foreign_keys = ON;";
                cmdFk.ExecuteNonQuery();
            }

            // Таблиця словників
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS DICT (
                    DICT_ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    PARENT_ID INTEGER,
                    NAME TEXT NOT NULL,
                    CODE TEXT NOT NULL,
                    DESCRIPTION TEXT,
                    FOREIGN KEY (PARENT_ID) REFERENCES DICT(DICT_ID)
                );";
                cmd.ExecuteNonQuery();
            }

            // Таблиця елементів словників
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS DICT_ITEM (
                    ITEM_ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    DICT_ID INTEGER NOT NULL,
                    CODE TEXT NOT NULL,
                    NAME TEXT NOT NULL,
                    FOREIGN KEY (DICT_ID) REFERENCES DICT(DICT_ID) ON DELETE CASCADE
                );";
                cmd.ExecuteNonQuery();
            }

            // Заповнення прикладовими даними при першому створенні
            if (first)
            {
                SeedSampleData(conn);
            }
        }

        // Отримати ID останнього вставленого рядка
        private static long GetLastInsertId(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT last_insert_rowid();";
            return (long)cmd.ExecuteScalar();
        }

        // Додати прикладові дані (2 словники і кілька елементів)
        private static void SeedSampleData(SqliteConnection conn)
        {
            using var tran = conn.BeginTransaction();

            var insertDictCmd = conn.CreateCommand();
            insertDictCmd.Transaction = tran;
            insertDictCmd.CommandText = "INSERT INTO DICT (PARENT_ID, NAME, CODE, DESCRIPTION) VALUES (@p, @name, @code, @desc);";
            insertDictCmd.Parameters.Add("@p", SqliteType.Integer);
            insertDictCmd.Parameters.Add("@name", SqliteType.Text);
            insertDictCmd.Parameters.Add("@code", SqliteType.Text);
            insertDictCmd.Parameters.Add("@desc", SqliteType.Text);

            // Додати словник "Країни"
            insertDictCmd.Parameters["@p"].Value = DBNull.Value;
            insertDictCmd.Parameters["@name"].Value = "Країни";
            insertDictCmd.Parameters["@code"].Value = "COUNTRIES";
            insertDictCmd.Parameters["@desc"].Value = "Список країн.";
            insertDictCmd.ExecuteNonQuery();
            long dict1Id = GetLastInsertId(conn);

            // Додати словник "Мови"
            insertDictCmd.Parameters["@p"].Value = dict1Id;
            insertDictCmd.Parameters["@name"].Value = "Мови";
            insertDictCmd.Parameters["@code"].Value = "LANGUAGES";
            insertDictCmd.Parameters["@desc"].Value = "Список мов.";
            insertDictCmd.ExecuteNonQuery();
            long dict2Id = GetLastInsertId(conn);

            // Додавання елементів
            var insertItemCmd = conn.CreateCommand();
            insertItemCmd.Transaction = tran;
            insertItemCmd.CommandText = "INSERT INTO DICT_ITEM (DICT_ID, CODE, NAME) VALUES (@dictId, @code, @name);";
            insertItemCmd.Parameters.Add("@dictId", SqliteType.Integer);
            insertItemCmd.Parameters.Add("@code", SqliteType.Text);
            insertItemCmd.Parameters.Add("@name", SqliteType.Text);

            // Елементи для "Країни"
            insertItemCmd.Parameters["@dictId"].Value = dict1Id;
            insertItemCmd.Parameters["@code"].Value = "UA";
            insertItemCmd.Parameters["@name"].Value = "Україна";
            insertItemCmd.ExecuteNonQuery();

            insertItemCmd.Parameters["@dictId"].Value = dict1Id;
            insertItemCmd.Parameters["@code"].Value = "PL";
            insertItemCmd.Parameters["@name"].Value = "Польща";
            insertItemCmd.ExecuteNonQuery();

            // Елементи для "Мови"
            insertItemCmd.Parameters["@dictId"].Value = dict2Id;
            insertItemCmd.Parameters["@code"].Value = "UA";
            insertItemCmd.Parameters["@name"].Value = "Українська";
            insertItemCmd.ExecuteNonQuery();

            insertItemCmd.Parameters["@dictId"].Value = dict2Id;
            insertItemCmd.Parameters["@code"].Value = "EN";
            insertItemCmd.Parameters["@name"].Value = "Англійська";
            insertItemCmd.ExecuteNonQuery();

            insertItemCmd.Parameters["@dictId"].Value = dict2Id;
            insertItemCmd.Parameters["@code"].Value = "JP";
            insertItemCmd.Parameters["@name"].Value = "Японська";
            insertItemCmd.ExecuteNonQuery();

            tran.Commit();
        }

        // --- Операції зі словниками ---
        public static List<Dict> GetAllDicts()
        {
            var list = new List<Dict>();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            // Переконатися, що включені зовнішні ключі
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();

            // Вибір усіх словників
            cmd.CommandText = "SELECT DICT_ID, PARENT_ID, NAME, CODE, DESCRIPTION FROM DICT ORDER BY DICT_ID ASC;";
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new Dict
                {
                    DictId = rdr.GetInt32(0),
                    ParentId = rdr.IsDBNull(1) ? null : rdr.GetInt32(1),
                    Name = rdr.GetString(2),
                    Code = rdr.GetString(3),
                    Description = rdr.IsDBNull(4) ? null : rdr.GetString(4)
                });
            }
            return list;
        }

        // Додати словник
        public static int CreateDict(Dict d)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO DICT (PARENT_ID, NAME, CODE, DESCRIPTION) VALUES (@p, @name, @code, @desc);";
            cmd.Parameters.AddWithValue("@p", d.ParentId.HasValue ? (object)d.ParentId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@name", d.Name);
            cmd.Parameters.AddWithValue("@code", d.Code);
            cmd.Parameters.AddWithValue("@desc", (object?)d.Description ?? DBNull.Value);
            cmd.ExecuteNonQuery();
            return (int)GetLastInsertId(conn);
        }

        // Оновити словник
        public static void UpdateDict(Dict d)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE DICT SET PARENT_ID = @p, NAME = @name, CODE = @code, DESCRIPTION = @desc WHERE DICT_ID = @id;";
            cmd.Parameters.AddWithValue("@p", d.ParentId.HasValue ? (object)d.ParentId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@name", d.Name);
            cmd.Parameters.AddWithValue("@code", d.Code);
            cmd.Parameters.AddWithValue("@desc", (object?)d.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", d.DictId);
            cmd.ExecuteNonQuery();
        }

        // Видалити словник (разом з елементами)
        public static void DeleteDict(int dictId)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            // Забезпечення каскадного видалення
            using (var cmdFk = conn.CreateCommand())
            {
                cmdFk.CommandText = "PRAGMA foreign_keys = ON;";
                cmdFk.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM DICT WHERE DICT_ID = @id;";
            cmd.Parameters.AddWithValue("@id", dictId);
            cmd.ExecuteNonQuery();
        }

        // --- Операції з елементами словника ---
        public static List<DictItem> GetItemsByDict(int dictId)
        {
            var list = new List<DictItem>();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();

            // Вибір елементів для конкретного словника
            cmd.CommandText = "SELECT ITEM_ID, DICT_ID, CODE, NAME FROM DICT_ITEM WHERE DICT_ID = @did ORDER BY ITEM_ID ASC;";
            cmd.Parameters.AddWithValue("@did", dictId);

            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new DictItem
                {
                    ItemId = rdr.GetInt32(0),
                    DictId = rdr.GetInt32(1),
                    Code = rdr.GetString(2),
                    Name = rdr.GetString(3)
                });
            }
            return list;
        }

        // Додати елемент словника
        public static int CreateItem(DictItem it)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO DICT_ITEM (DICT_ID, CODE, NAME) VALUES (@did, @code, @name);";
            cmd.Parameters.AddWithValue("@did", it.DictId);
            cmd.Parameters.AddWithValue("@code", it.Code);
            cmd.Parameters.AddWithValue("@name", it.Name);
            cmd.ExecuteNonQuery();
            return (int)GetLastInsertId(conn);
        }

        // Оновити елемент словника
        public static void UpdateItem(DictItem it)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE DICT_ITEM SET DICT_ID = @did, CODE = @code, NAME = @name WHERE ITEM_ID = @id;";
            cmd.Parameters.AddWithValue("@did", it.DictId);
            cmd.Parameters.AddWithValue("@code", it.Code);
            cmd.Parameters.AddWithValue("@name", it.Name);
            cmd.Parameters.AddWithValue("@id", it.ItemId);
            cmd.ExecuteNonQuery();
        }

        // Видалити елемент словника
        public static void DeleteItem(int itemId)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM DICT_ITEM WHERE ITEM_ID = @id;";
            cmd.Parameters.AddWithValue("@id", itemId);
            cmd.ExecuteNonQuery();
        }
    }
}

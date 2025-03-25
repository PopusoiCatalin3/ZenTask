using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using Dapper;

namespace ZenTask.Services.Data
{
    public class SQLiteDataService
    {
        private readonly string _dbPath;
        private readonly string _connectionString;
        private bool _isInitialized = false;

        public SQLiteDataService(string dbPath = "ZenTask.db")
        {
            _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbPath);
            _connectionString = $"Data Source={_dbPath};Version=3;";
        }

        public bool IsInitialized => _isInitialized;

        public void Initialize()
        {
            // Fast check if database exists
            _isInitialized = File.Exists(_dbPath);
            if (!_isInitialized)
            {
                // Create empty database file but defer table creation
                SQLiteConnection.CreateFile(_dbPath);
                _isInitialized = true;
            }
        }

        // This can be called after UI is shown
        public async Task InitializeTablesAsync()
        {
            // Skip if DB already exists
            if (File.Exists(_dbPath) && await CheckTablesExistAsync())
            {
                return;
            }

            await Task.Run(() => CreateTables());
        }

        private async Task<bool> CheckTablesExistAsync()
        {
            try
            {
                using (var connection = CreateConnection())
                {
                    connection.Open();
                    var result = await connection.QueryFirstOrDefaultAsync<int>(
                        "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='Tasks'");
                    return result > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private void CreateTables()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // Create Users table first since other tables reference it
                connection.Execute(@"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT NOT NULL UNIQUE,
                        Email TEXT NOT NULL UNIQUE,
                        PasswordHash TEXT NOT NULL,
                        PasswordSalt TEXT NOT NULL,
                        FirstName TEXT,
                        LastName TEXT,
                        CreatedDate TEXT NOT NULL,
                        LastLoginDate TEXT NOT NULL,
                        ProfileImagePath TEXT,
                        ThemePreference TEXT
                    )");

                // Create Categories table with UserId
                connection.Execute(@"
                    CREATE TABLE IF NOT EXISTS Categories (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Description TEXT,
                        ColorHex TEXT,
                        IconName TEXT,
                        UserId INTEGER NOT NULL,
                        FOREIGN KEY(UserId) REFERENCES Users(Id)
                    )");

                // Create Tags table with UserId
                connection.Execute(@"
                    CREATE TABLE IF NOT EXISTS Tags (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        ColorHex TEXT,
                        UserId INTEGER NOT NULL,
                        FOREIGN KEY(UserId) REFERENCES Users(Id)
                    )");

                // Create Tasks table with UserId
                connection.Execute(@"
                    CREATE TABLE IF NOT EXISTS Tasks (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        Description TEXT,
                        CreatedDate TEXT NOT NULL,
                        DueDate TEXT,
                        StartDate TEXT,
                        Priority INTEGER,
                        Status INTEGER,
                        CategoryId INTEGER,
                        IsRecurring INTEGER,
                        RecurrencePattern TEXT,
                        EstimatedDuration INTEGER,
                        UserId INTEGER NOT NULL,
                        FOREIGN KEY(CategoryId) REFERENCES Categories(Id),
                        FOREIGN KEY(UserId) REFERENCES Users(Id)
                    )");

                // Create SubTasks table
                connection.Execute(@"
                    CREATE TABLE IF NOT EXISTS SubTasks (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        TaskId INTEGER NOT NULL,
                        Title TEXT NOT NULL,
                        IsCompleted INTEGER,
                        DisplayOrder INTEGER,
                        FOREIGN KEY(TaskId) REFERENCES Tasks(Id)
                    )");

                // Create TaskTags (many-to-many) table
                connection.Execute(@"
                    CREATE TABLE IF NOT EXISTS TaskTags (
                        TaskId INTEGER,
                        TagId INTEGER,
                        PRIMARY KEY(TaskId, TagId),
                        FOREIGN KEY(TaskId) REFERENCES Tasks(Id),
                        FOREIGN KEY(TagId) REFERENCES Tags(Id)
                    )");

                // Check if Categories table is empty
                var categoryCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Categories");

                // Insert default categories if none exist - now with UserId=1 (admin/system user)
                if (categoryCount == 0)
                {
                    connection.Execute(@"
                        INSERT INTO Categories (Name, ColorHex, IconName, UserId) 
                        VALUES ('Work', '#FF4081', 'work', 1),
                               ('Personal', '#4CAF50', 'person', 1),
                               ('Study', '#2196F3', 'school', 1),
                               ('Health', '#9C27B0', 'favorite', 1),
                               ('Finance', '#FF9800', 'attach_money', 1)");
                }
            }
        }

        public IDbConnection CreateConnection()
        {
            return new SQLiteConnection(_connectionString);
        }
    }
}
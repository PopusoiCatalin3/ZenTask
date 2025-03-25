using System;
using System.Threading.Tasks;
using Dapper;
using ZenTask.Models;
using ZenTask.Services.Security;

namespace ZenTask.Services.Data
{
    public class UserRepository : Repository<User>
    {
        public UserRepository(SQLiteDataService dataService, AuthService authService)
            : base(dataService, "Users", authService)
        {
        }

        public override async Task<int> InsertAsync(User entity)
        {
            using (var connection = _dataService.CreateConnection())
            {
                var sql = @"
                    INSERT INTO Users (
                        Username, Email, PasswordHash, PasswordSalt, FirstName, LastName, 
                        CreatedDate, LastLoginDate, ProfileImagePath, ThemePreference
                    ) VALUES (
                        @Username, @Email, @PasswordHash, @PasswordSalt, @FirstName, @LastName, 
                        @CreatedDate, @LastLoginDate, @ProfileImagePath, @ThemePreference
                    );
                    SELECT last_insert_rowid();";

                return await connection.ExecuteScalarAsync<int>(sql, entity);
            }
        }

        public override async Task<bool> UpdateAsync(User entity)
        {
            using (var connection = _dataService.CreateConnection())
            {
                var sql = @"
                    UPDATE Users SET 
                        Username = @Username,
                        Email = @Email,
                        PasswordHash = @PasswordHash,
                        PasswordSalt = @PasswordSalt,
                        FirstName = @FirstName,
                        LastName = @LastName,
                        LastLoginDate = @LastLoginDate,
                        ProfileImagePath = @ProfileImagePath,
                        ThemePreference = @ThemePreference
                    WHERE Id = @Id";

                var affected = await connection.ExecuteAsync(sql, entity);
                return affected > 0;
            }
        }

        public async Task<User> GetByUsernameAsync(string username)
        {
            using (var connection = _dataService.CreateConnection())
            {
                return await connection.QuerySingleOrDefaultAsync<User>(
                    "SELECT * FROM Users WHERE Username = @Username",
                    new { Username = username });
            }
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            using (var connection = _dataService.CreateConnection())
            {
                return await connection.QuerySingleOrDefaultAsync<User>(
                    "SELECT * FROM Users WHERE Email = @Email",
                    new { Email = email });
            }
        }

        public async Task<bool> UpdateLastLoginAsync(int userId)
        {
            using (var connection = _dataService.CreateConnection())
            {
                var affected = await connection.ExecuteAsync(
                    "UPDATE Users SET LastLoginDate = @LastLoginDate WHERE Id = @Id",
                    new { Id = userId, LastLoginDate = DateTime.Now });

                return affected > 0;
            }
        }

        public async Task<bool> UpdatePasswordAsync(int userId, string passwordHash, string passwordSalt)
        {
            using (var connection = _dataService.CreateConnection())
            {
                var affected = await connection.ExecuteAsync(
                    "UPDATE Users SET PasswordHash = @PasswordHash, PasswordSalt = @PasswordSalt WHERE Id = @Id",
                    new { Id = userId, PasswordHash = passwordHash, PasswordSalt = passwordSalt });

                return affected > 0;
            }
        }

        public async Task<bool> UpdateThemePreferenceAsync(int userId, string themePreference)
        {
            using (var connection = _dataService.CreateConnection())
            {
                var affected = await connection.ExecuteAsync(
                    "UPDATE Users SET ThemePreference = @ThemePreference WHERE Id = @Id",
                    new { Id = userId, ThemePreference = themePreference });

                return affected > 0;
            }
        }
    }
}
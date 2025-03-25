using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenTask.Services.Security;

namespace ZenTask.Services.Data
{
    public interface IRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetByIdAsync(int id);
        Task<int> InsertAsync(T entity);
        Task<bool> UpdateAsync(T entity);
        Task<bool> DeleteAsync(int id);
    }

    public abstract class Repository<T> : IRepository<T> where T : class
    {
        protected readonly SQLiteDataService _dataService;
        protected readonly string _tableName;
        protected readonly AuthService _authService;

        protected Repository(SQLiteDataService dataService, string tableName, AuthService authService)
        {
            _dataService = dataService;
            _tableName = tableName;
            _authService = authService;
        }

        // Helper method to get the current user ID
        protected int GetCurrentUserId()
        {
            if (_authService == null || _authService.CurrentUser == null)
            {
                throw new InvalidOperationException("User is not authenticated");
            }
            return _authService.CurrentUser.Id;
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            int userId = GetCurrentUserId();
            using (var connection = _dataService.CreateConnection())
            {
                return await connection.QueryAsync<T>($"SELECT * FROM {_tableName} WHERE UserId = @UserId",
                    new { UserId = userId });
            }
        }

        public virtual async Task<T> GetByIdAsync(int id)
        {
            int userId = GetCurrentUserId();
            using (var connection = _dataService.CreateConnection())
            {
                return await connection.QuerySingleOrDefaultAsync<T>(
                    $"SELECT * FROM {_tableName} WHERE Id = @Id AND UserId = @UserId",
                    new { Id = id, UserId = userId });
            }
        }

        public abstract Task<int> InsertAsync(T entity);
        public abstract Task<bool> UpdateAsync(T entity);

        public virtual async Task<bool> DeleteAsync(int id)
        {
            int userId = GetCurrentUserId();
            using (var connection = _dataService.CreateConnection())
            {
                var affected = await connection.ExecuteAsync(
                    $"DELETE FROM {_tableName} WHERE Id = @Id AND UserId = @UserId",
                    new { Id = id, UserId = userId });
                return affected > 0;
            }
        }
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using ZenTask.Models;
using ZenTask.Services.Security;

namespace ZenTask.Services.Data
{
    public class CategoryRepository : Repository<Category>
    {
        public CategoryRepository(SQLiteDataService dataService, AuthService authService) : base(dataService, "Categories", authService)
        {
        }

        public override async Task<int> InsertAsync(Category entity)
        {
            using (var connection = _dataService.CreateConnection())
            {
                var sql = @"
                    INSERT INTO Categories (Name, Description, ColorHex, IconName)
                    VALUES (@Name, @Description, @ColorHex, @IconName);
                    SELECT last_insert_rowid();";

                return await connection.ExecuteScalarAsync<int>(sql, entity);
            }
        }

        public override async Task<bool> UpdateAsync(Category entity)
        {
            using (var connection = _dataService.CreateConnection())
            {
                var sql = @"
                    UPDATE Categories SET 
                        Name = @Name,
                        Description = @Description,
                        ColorHex = @ColorHex,
                        IconName = @IconName
                    WHERE Id = @Id";

                var affected = await connection.ExecuteAsync(sql, entity);
                return affected > 0;
            }
        }

        public async Task<IEnumerable<Category>> GetCategoriesWithTaskCountAsync()
        {
            using (var connection = _dataService.CreateConnection())
            {
                var categories = await connection.QueryAsync<Category>(@"
                    SELECT c.*, COUNT(t.Id) as TaskCount
                    FROM Categories c
                    LEFT JOIN Tasks t ON c.Id = t.CategoryId
                    GROUP BY c.Id, c.Name, c.Description, c.ColorHex, c.IconName");

                return categories;
            }
        }
    }
}
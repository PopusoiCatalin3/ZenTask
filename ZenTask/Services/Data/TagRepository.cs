using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using ZenTask.Models;
using ZenTask.Services.Security;

namespace ZenTask.Services.Data
{
    public class TagRepository : Repository<Tag>
    {
        public TagRepository(SQLiteDataService dataService, AuthService authService)
        : base(dataService, "Tags", authService)
        {
        }

        public override async Task<int> InsertAsync(Tag entity)
        {
            using (var connection = _dataService.CreateConnection())
            {
                var sql = @"
                    INSERT INTO Tags (Name, ColorHex)
                    VALUES (@Name, @ColorHex);
                    SELECT last_insert_rowid();";

                return await connection.ExecuteScalarAsync<int>(sql, entity);
            }
        }

        public override async Task<bool> UpdateAsync(Tag entity)
        {
            using (var connection = _dataService.CreateConnection())
            {
                var sql = @"
                    UPDATE Tags SET 
                        Name = @Name,
                        ColorHex = @ColorHex
                    WHERE Id = @Id";

                var affected = await connection.ExecuteAsync(sql, entity);
                return affected > 0;
            }
        }

        public async Task<IEnumerable<Tag>> GetTagsForTaskAsync(int taskId)
        {
            using (var connection = _dataService.CreateConnection())
            {
                var sql = @"
                    SELECT t.*
                    FROM Tags t
                    INNER JOIN TaskTags tt ON t.Id = tt.TagId
                    WHERE tt.TaskId = @TaskId";

                return await connection.QueryAsync<Tag>(sql, new { TaskId = taskId });
            }
        }

        public async Task<IEnumerable<Tag>> GetTagsWithTaskCountAsync()
        {
            using (var connection = _dataService.CreateConnection())
            {
                var sql = @"
                    SELECT t.*, COUNT(tt.TaskId) as TaskCount
                    FROM Tags t
                    LEFT JOIN TaskTags tt ON t.Id = tt.TagId
                    GROUP BY t.Id, t.Name, t.ColorHex";

                return await connection.QueryAsync<Tag>(sql);
            }
        }
    }
}
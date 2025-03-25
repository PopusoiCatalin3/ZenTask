using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ZenTask.Models;
using ZenTask.Services.Security;
using Task = ZenTask.Models.Task;

namespace ZenTask.Services.Data
{
    public class TaskRepository : Repository<Task>
    {
        public TaskRepository(SQLiteDataService dataService, AuthService authService)
            : base(dataService, "Tasks", authService)
        {
        }

        public override async Task<int> InsertAsync(Task entity)
        {
            entity.UserId = GetCurrentUserId();

            using (var connection = _dataService.CreateConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var sql = @"
                    INSERT INTO Tasks (
                        Title, Description, CreatedDate, DueDate, StartDate,
                        Priority, Status, CategoryId, IsRecurring, RecurrencePattern, 
                        EstimatedDuration, UserId
                    ) VALUES (
                        @Title, @Description, @CreatedDate, @DueDate, @StartDate,
                        @Priority, @Status, @CategoryId, @IsRecurring, @RecurrencePattern, 
                        @EstimatedDuration, @UserId
                    );
                    SELECT last_insert_rowid();";

                        var taskId = await connection.ExecuteScalarAsync<int>(sql, new
                        {
                            entity.Title,
                            entity.Description,
                            CreatedDate = entity.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                            DueDate = entity.DueDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                            StartDate = entity.StartDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                            Priority = (int)entity.Priority,
                            Status = (int)entity.Status,
                            entity.CategoryId,
                            IsRecurring = entity.IsRecurring ? 1 : 0,
                            entity.RecurrencePattern,
                            entity.EstimatedDuration,
                            entity.UserId
                        }, transaction);

                        // Process subtasks in the same transaction
                        if (entity.SubTasks != null && entity.SubTasks.Count > 0)
                        {
                            var subTaskSql = @"
                        INSERT INTO SubTasks (TaskId, Title, IsCompleted, DisplayOrder)
                        VALUES (@TaskId, @Title, @IsCompleted, @DisplayOrder)";

                            foreach (var subTask in entity.SubTasks)
                            {
                                subTask.TaskId = taskId;
                                await connection.ExecuteAsync(subTaskSql, new
                                {
                                    subTask.TaskId,
                                    subTask.Title,
                                    IsCompleted = subTask.IsCompleted ? 1 : 0,
                                    subTask.DisplayOrder
                                }, transaction);
                            }
                        }

                        // Process tags in the same transaction
                        if (entity.TagIds != null && entity.TagIds.Count > 0)
                        {
                            var tagInsertSql = "INSERT INTO TaskTags (TaskId, TagId) VALUES (@TaskId, @TagId)";
                            foreach (var tagId in entity.TagIds)
                            {
                                await connection.ExecuteAsync(tagInsertSql, new { TaskId = taskId, TagId = tagId }, transaction);
                            }
                        }

                        // Commit the transaction
                        transaction.Commit();

                        return taskId;
                    }
                    catch (Exception ex)
                    {
                        // Log the error
                        System.Diagnostics.Debug.WriteLine($"Error inserting task: {ex.Message}");

                        // Rollback on error
                        transaction.Rollback();

                        // Rethrow to let caller handle it
                        throw;
                    }
                }
            }
        }

        public override async Task<bool> UpdateAsync(Task entity)
        {
            using (var connection = _dataService.CreateConnection())
            {
                var sql = @"
                    UPDATE Tasks SET 
                        Title = @Title,
                        Description = @Description,
                        DueDate = @DueDate,
                        StartDate = @StartDate,
                        Priority = @Priority,
                        Status = @Status,
                        CategoryId = @CategoryId,
                        IsRecurring = @IsRecurring,
                        RecurrencePattern = @RecurrencePattern,
                        EstimatedDuration = @EstimatedDuration
                    WHERE Id = @Id";

                var affected = await connection.ExecuteAsync(sql, new
                {
                    entity.Id,
                    entity.Title,
                    entity.Description,
                    DueDate = entity.DueDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                    StartDate = entity.StartDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                    Priority = (int)entity.Priority,
                    Status = (int)entity.Status,
                    entity.CategoryId,
                    IsRecurring = entity.IsRecurring ? 1 : 0,
                    entity.RecurrencePattern,
                    entity.EstimatedDuration
                });

                // Actualizăm sub-task-urile
                if (entity.SubTasks != null && entity.SubTasks.Count > 0)
                {
                    // Mai întâi ștergem toate sub-task-urile existente
                    await connection.ExecuteAsync("DELETE FROM SubTasks WHERE TaskId = @TaskId", new { TaskId = entity.Id });

                    // Apoi le adăugăm pe cele noi
                    foreach (var subTask in entity.SubTasks)
                    {
                        subTask.TaskId = entity.Id;
                        await InsertSubTaskAsync(subTask);
                    }
                }

                // Actualizăm relațiile cu tag-uri
                if (entity.TagIds != null)
                {
                    // Ștergem toate relațiile existente
                    await connection.ExecuteAsync("DELETE FROM TaskTags WHERE TaskId = @TaskId", new { TaskId = entity.Id });

                    // Adăugăm relațiile noi
                    if (entity.TagIds.Count > 0)
                    {
                        var tagInsertSql = "INSERT INTO TaskTags (TaskId, TagId) VALUES (@TaskId, @TagId)";
                        foreach (var tagId in entity.TagIds)
                        {
                            await connection.ExecuteAsync(tagInsertSql, new { TaskId = entity.Id, TagId = tagId });
                        }
                    }
                }

                return affected > 0;
            }
        }

        public override async Task<bool> DeleteAsync(int id)
        {
            using (var connection = _dataService.CreateConnection())
            {
                // Ștergem sub-task-urile
                await connection.ExecuteAsync("DELETE FROM SubTasks WHERE TaskId = @TaskId", new { TaskId = id });

                // Ștergem relațiile cu tag-uri
                await connection.ExecuteAsync("DELETE FROM TaskTags WHERE TaskId = @TaskId", new { TaskId = id });

                // Ștergem task-ul
                var affected = await connection.ExecuteAsync("DELETE FROM Tasks WHERE Id = @Id", new { Id = id });
                return affected > 0;
            }
        }

        public override async Task<Task> GetByIdAsync(int id)
        {
            using (var connection = _dataService.CreateConnection())
            {
                // Obținem task-ul
                var task = await connection.QuerySingleOrDefaultAsync<Task>(
                    "SELECT * FROM Tasks WHERE Id = @Id", new { Id = id });

                if (task != null)
                {
                    // Convertim valorile din baza de date în tipurile corecte
                    task.Priority = (TaskPriority)task.Priority;
                    task.Status = (Models.TaskStatus)task.Status;

                    // Obținem sub-task-urile
                    var subTasks = await connection.QueryAsync<SubTask>(
                        "SELECT * FROM SubTasks WHERE TaskId = @TaskId ORDER BY DisplayOrder",
                        new { TaskId = id });
                    task.SubTasks = subTasks.ToList();

                    // Obținem tag-urile
                    var tagIds = await connection.QueryAsync<int>(
                        "SELECT TagId FROM TaskTags WHERE TaskId = @TaskId",
                        new { TaskId = id });
                    task.TagIds = tagIds.ToList();
                }

                return task;
            }
        }

        public override async Task<IEnumerable<Task>> GetAllAsync()
        {
            using (var connection = _dataService.CreateConnection())
            {
                var tasks = await connection.QueryAsync<Task>("SELECT * FROM Tasks");

                foreach (var task in tasks)
                {
                    // Convertim valorile din baza de date în tipurile corecte
                    task.Priority = (TaskPriority)task.Priority;
                    task.Status = (Models.TaskStatus)task.Status;

                    // Obținem sub-task-urile
                    var subTasks = await connection.QueryAsync<SubTask>(
                        "SELECT * FROM SubTasks WHERE TaskId = @TaskId ORDER BY DisplayOrder",
                        new { TaskId = task.Id });
                    task.SubTasks = subTasks.ToList();

                    // Obținem tag-urile
                    var tagIds = await connection.QueryAsync<int>(
                        "SELECT TagId FROM TaskTags WHERE TaskId = @TaskId",
                        new { TaskId = task.Id });
                    task.TagIds = tagIds.ToList();
                }

                return tasks;
            }
        }

        private async Task<int> InsertSubTaskAsync(SubTask subTask)
        {
            using (var connection = _dataService.CreateConnection())
            {
                var sql = @"
                    INSERT INTO SubTasks (TaskId, Title, IsCompleted, DisplayOrder)
                    VALUES (@TaskId, @Title, @IsCompleted, @DisplayOrder);
                    SELECT last_insert_rowid();";

                return await connection.ExecuteScalarAsync<int>(sql, new
                {
                    subTask.TaskId,
                    subTask.Title,
                    IsCompleted = subTask.IsCompleted ? 1 : 0,
                    subTask.DisplayOrder
                });
            }
        }

        // Metode specifice pentru task-uri



        public async Task<IEnumerable<Task>> GetTasksByStatusAsync(Models.TaskStatus status)
        {
            using (var connection = _dataService.CreateConnection())
            {
                var tasks = await connection.QueryAsync<Task>(
                    "SELECT * FROM Tasks WHERE Status = @Status",
                    new { Status = (int)status });

                foreach (var task in tasks)
                {
                    // Convertim valorile din baza de date în tipurile corecte
                    task.Priority = (TaskPriority)task.Priority;
                    task.Status = (Models.TaskStatus)task.Status;

                    // Obținem sub-task-urile
                    var subTasks = await connection.QueryAsync<SubTask>(
                        "SELECT * FROM SubTasks WHERE TaskId = @TaskId ORDER BY DisplayOrder",
                        new { TaskId = task.Id });
                    task.SubTasks = subTasks.ToList();

                    // Obținem tag-urile
                    var tagIds = await connection.QueryAsync<int>(
                        "SELECT TagId FROM TaskTags WHERE TaskId = @TaskId",
                        new { TaskId = task.Id });
                    task.TagIds = tagIds.ToList();
                }

                return tasks;
            }
        }

        public async Task<IEnumerable<Task>> GetTasksByCategoryAsync(int categoryId)
        {
            using (var connection = _dataService.CreateConnection())
            {
                var tasks = await connection.QueryAsync<Task>(
                    "SELECT * FROM Tasks WHERE CategoryId = @CategoryId",
                    new { CategoryId = categoryId });

                foreach (var task in tasks)
                {
                    // Convertim valorile din baza de date în tipurile corecte
                    task.Priority = (TaskPriority)task.Priority;
                    task.Status = (Models.TaskStatus)task.Status;

                    // Obținem sub-task-urile
                    var subTasks = await connection.QueryAsync<SubTask>(
                        "SELECT * FROM SubTasks WHERE TaskId = @TaskId ORDER BY DisplayOrder",
                        new { TaskId = task.Id });
                    task.SubTasks = subTasks.ToList();

                    // Obținem tag-urile
                    var tagIds = await connection.QueryAsync<int>(
                        "SELECT TagId FROM TaskTags WHERE TaskId = @TaskId",
                        new { TaskId = task.Id });
                    task.TagIds = tagIds.ToList();
                }

                return tasks;
            }
        }

        public async Task<IEnumerable<Task>> GetTasksByTagAsync(int tagId)
        {
            using (var connection = _dataService.CreateConnection())
            {
                var taskIds = await connection.QueryAsync<int>(
                    "SELECT TaskId FROM TaskTags WHERE TagId = @TagId",
                    new { TagId = tagId });

                var tasks = new List<Task>();
                foreach (var id in taskIds)
                {
                    var task = await GetByIdAsync(id);
                    if (task != null)
                    {
                        tasks.Add(task);
                    }
                }

                return tasks;
            }
        }

        public async Task<IEnumerable<Task>> GetTasksDueByDateAsync(DateTime date)
        {
            using (var connection = _dataService.CreateConnection())
            {
                var dateString = date.ToString("yyyy-MM-dd");
                var tasks = await connection.QueryAsync<Task>(
                    "SELECT * FROM Tasks WHERE date(DueDate) = date(@Date)",
                    new { Date = dateString });

                foreach (var task in tasks)
                {
                    // Convertim valorile din baza de date în tipurile corecte
                    task.Priority = (TaskPriority)task.Priority;
                    task.Status = (Models.TaskStatus)task.Status;

                    // Obținem sub-task-urile
                    var subTasks = await connection.QueryAsync<SubTask>(
                        "SELECT * FROM SubTasks WHERE TaskId = @TaskId ORDER BY DisplayOrder",
                        new { TaskId = task.Id });
                    task.SubTasks = subTasks.ToList();

                    // Obținem tag-urile
                    var tagIds = await connection.QueryAsync<int>(
                        "SELECT TagId FROM TaskTags WHERE TaskId = @TaskId",
                        new { TaskId = task.Id });
                    task.TagIds = tagIds.ToList();
                }

                return tasks;
            }
        }

        public async Task<IEnumerable<Task>> GetTasksInPeriodAsync(DateTime startDate, DateTime endDate)
        {
            using (var connection = _dataService.CreateConnection())
            {
                var startDateString = startDate.ToString("yyyy-MM-dd");
                var endDateString = endDate.ToString("yyyy-MM-dd");
                var tasks = await connection.QueryAsync<Task>(
                    @"SELECT * FROM Tasks 
                    WHERE (date(DueDate) BETWEEN date(@StartDate) AND date(@EndDate)) 
                    OR (date(StartDate) BETWEEN date(@StartDate) AND date(@EndDate))",
                    new { StartDate = startDateString, EndDate = endDateString });

                foreach (var task in tasks)
                {
                    // Convertim valorile din baza de date în tipurile corecte
                    task.Priority = (TaskPriority)task.Priority;
                    task.Status = (Models.TaskStatus)task.Status;

                    // Obținem sub-task-urile
                    var subTasks = await connection.QueryAsync<SubTask>(
                        "SELECT * FROM SubTasks WHERE TaskId = @TaskId ORDER BY DisplayOrder",
                        new { TaskId = task.Id });
                    task.SubTasks = subTasks.ToList();

                    // Obținem tag-urile
                    var tagIds = await connection.QueryAsync<int>(
                        "SELECT TagId FROM TaskTags WHERE TaskId = @TaskId",
                        new { TaskId = task.Id });
                    task.TagIds = tagIds.ToList();
                }

                return tasks;
            }
        }

        public async Task<bool> UpdateTaskStatusAsync(int taskId, Models.TaskStatus status)
        {
            if (taskId <= 0)
            {
                Debug.WriteLine($"TaskRepository: Invalid task ID: {taskId}");
                return false;
            }

            try
            {
                using (var connection = _dataService.CreateConnection())
                {
                    // First verify the task exists
                    var taskExists = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM Tasks WHERE Id = @Id",
                        new { Id = taskId }) > 0;

                    if (!taskExists)
                    {
                        Debug.WriteLine($"TaskRepository: Task with ID {taskId} not found in database");
                        return false;
                    }

                    // Now update the status
                    Debug.WriteLine($"TaskRepository: Updating task {taskId} to status {status}");
                    var affected = await connection.ExecuteAsync(
                        "UPDATE Tasks SET Status = @Status WHERE Id = @Id",
                        new { Id = taskId, Status = (int)status });

                    Debug.WriteLine($"TaskRepository: Update affected {affected} rows");
                    return affected > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TaskRepository: Error updating task status - {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateSubTaskCompletionAsync(int subTaskId, bool isCompleted)
        {
            using (var connection = _dataService.CreateConnection())
            {
                var affected = await connection.ExecuteAsync(
                    "UPDATE SubTasks SET IsCompleted = @IsCompleted WHERE Id = @Id",
                    new { Id = subTaskId, IsCompleted = isCompleted ? 1 : 0 });
                return affected > 0;
            }
        }
    }
}
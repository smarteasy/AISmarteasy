using AISmarteasy.Core.Connector.MicrosoftGraph;

namespace AISmarteasy.Core.Connector;

public interface ITaskManagementConnector
{
    Task<TaskManagementTask> AddTaskAsync(string listId, TaskManagementTask task, CancellationToken cancellationToken = default);

    Task DeleteTaskAsync(string listId, string taskId, CancellationToken cancellationToken = default);

    Task<TaskManagementTaskList?> GetDefaultTaskListAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<TaskManagementTaskList>> GetTaskListsAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<TaskManagementTask>> GetTasksAsync(string listId, bool includeCompleted, CancellationToken cancellationToken = default);
}

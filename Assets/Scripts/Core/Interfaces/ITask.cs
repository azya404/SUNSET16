namespace SUNSET16.Core
{
    public interface ITask
    {
        string TaskId { get; }
        TaskDifficulty Difficulty { get; }
        bool IsCompleted { get; }
        void InitializeTask(TaskData taskData);
        void CompleteTask();
    }
}

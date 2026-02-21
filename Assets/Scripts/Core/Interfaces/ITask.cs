/*
interface for daily mandatory tasks (the main gameplay loop)
every day the player has to complete a task before they can advance to Night phase
TaskManager spawns the task prefab and interacts with it through this interface

tasks are different from puzzles:
- tasks are MANDATORY (must complete to advance the day)
- tasks have DIFFICULTY (easy if on-pill, hard if off-pill)
- tasks are the "job" the player does on the space station (wire connecting, water pump repair, etc)

compared to puzzles which are OPTIONAL and only accessible off-pill at night

originally this was part of TaskManager.cs with an OnTaskCompleted event
but we split it out to its own interface and gave each task its own data via TaskData ScriptableObject
the difficulty is now stored in TaskData instead of being passed to Initialize like the old design

TODO: we havent built any actual task implementations yet (WirePuzzleController etc)
      these will be scripts that implement ITask and contain the actual mini-game logic
TODO: might want an OnTaskCompleted event here too (like we noted for IPuzzle)
*/
namespace SUNSET16.Core
{
    public interface ITask
    {
        string TaskId { get; }             //unique id like "task_day1_easy" - matches TaskData.taskId
        TaskDifficulty Difficulty { get; }  //Easy or Hard, determined by pill choice for that day
        bool IsCompleted { get; }           //true once the player has finished this task
        void InitializeTask(TaskData taskData); //called by TaskManager when spawning, sets up the task with its data
        void CompleteTask();                //called when the task is finished - notifies TaskManager -> DayManager
    }
}

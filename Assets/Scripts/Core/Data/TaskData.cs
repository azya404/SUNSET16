/*
ScriptableObject that configures a single daily task
each day the player has to complete a task, and TaskManager uses these to know
what to spawn and show on the tablet

originally we had separate easy and hard prefabs per TaskData
but we simplified it to one prefab per TaskData with the difficulty baked in
so you create two TaskData assets per day: "task_day1_easy" and "task_day1_hard"
TaskManager picks the right one based on PillStateManager.HasTakenPillToday()

create these in Unity: right-click -> Create -> SUNSET16 -> Task Data

the taskPrefab should be a GameObject with a script implementing ITask
TaskManager instantiates it, calls InitializeTask(this), and waits for completion

the instructions field shows up on the tablet (TabletUIController)
so the player knows what they need to do for this task

TODO: might want a completion time tracker for scoring/feedback
TODO: might want a "hint" field that only shows in Easy mode
*/
using UnityEngine;
namespace SUNSET16.Core
{
    [CreateAssetMenu(fileName = "NewTaskData", menuName = "SUNSET16/Task Data")]
    public class TaskData : ScriptableObject
    {
        [Header("task identity")]
        [Tooltip("a unique id for this task (like 'task_day1') so we can reference it in code")]
        public string taskId;

        [Tooltip("the day this task belongs to (1–5)")]
        [Range(1, 5)]
        public int day;

        [Tooltip("difficulty of the task: easy (pill taken) or hard (pill refused)")]
        public TaskDifficulty difficulty;

        [Header("task content")]
        [Tooltip("the name that shows up on the tablet")]
        public string taskName;

        [Tooltip("the instructions shown on the tablet for this task")]
        [TextArea(3, 8)]
        public string instructions;

        [Header("prefab")]
        [Tooltip("the task prefab that gets spawned in the task room (it should implement ITask)")]
        public GameObject taskPrefab;
    }
}

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

/*
manages the daily mandatory tasks the player does each morning
tasks are the "job" part of the loop - wake up, take pill, go do your task

pill choice directly maps to task difficulty which is the key mechanic:
took pill = Easy task (labeled, hints, fewer steps) cos the pill makes
you a compliant worker who follows instructions perfectly
refused pill = Hard task (unlabeled, no hints, more complex) cos your
mind is clearer but the "system" doesnt help you

SpawnTask looks up the right TaskData ScriptableObject based on
current day + difficulty, spawns the prefab at the spawn point,
and locks the player in place until they finish

IMPORTANT: CompleteCurrentTask also calls DayManager.TaskCompleted()
which triggers the Morning -> Night phase transition. completing
your task IS what advances the day

TODO: task prefabs need to be created (tech demo mode rn)
TODO: task instructions on the tablet (events already wired up)
TODO: partial/failing task completion (currently just done or not done)
*/
using UnityEngine;
using System;
using System.Collections.Generic;

namespace SUNSET16.Core
{
    public class TaskManager : Singleton<TaskManager>
    {
        [Header("Task Configuration")]
        [Tooltip("TaskData assets for each day/difficulty combo. TaskManager selects the correct one at runtime.")]
        [SerializeField] private TaskData[] _taskDataAssets;

        [Header("Task Spawn Point")]
        [Tooltip("Transform where task prefabs are instantiated in the task room.")]
        [SerializeField] private Transform _taskSpawnPoint;

        public bool IsInitialized { get; private set; }
        public bool IsTaskCompletedToday { get; private set; }
        public ITask ActiveTask { get; private set; }
        private Dictionary<int, bool> _taskCompletionByDay;
        public event Action<TaskData> OnTaskSpawned;
        public event Action<int> OnTaskCompleted;

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsInitialized)
            {
                Initialize();
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.OnInitializationComplete += Initialize;
            }
        }

        private void Initialize()
        {
            //set up completion tracking for all 5 days (game is 5 days long)
            _taskCompletionByDay = new Dictionary<int, bool>();
            for (int i = 1; i <= 5; i++)
            {
                _taskCompletionByDay[i] = false;
            }

            IsTaskCompletedToday = false;
            DayManager.Instance.OnDayChanged += OnDayChanged; //reset task state each new day
            SaveManager.Instance.OnSaveDeleted += OnSaveDeleted;

            IsInitialized = true;
            Debug.Log("[TASKMANAGER] Initialized");
        }

        //new day started, check if this day already has a completed task (from save)
        private void OnDayChanged(int newDay)
        {
            IsTaskCompletedToday = _taskCompletionByDay.ContainsKey(newDay) && _taskCompletionByDay[newDay];
            DestroyActiveTask(); //clean up any leftover task from previous day
            Debug.Log($"[TASKMANAGER] Day changed to {newDay} - task completed today: {IsTaskCompletedToday}");
        }

        private void OnSaveDeleted()
        {
            for (int i = 1; i <= 5; i++)
            {
                _taskCompletionByDay[i] = false;
            }

            IsTaskCompletedToday = false;
            DestroyActiveTask();
            Debug.Log("[TASKMANAGER] All task state reset (save deleted)");
        }

        //the big one - spawns a task based on current day + pill choice
        //has a LOT of guard clauses cos this can only happen in very specific conditions
        public void SpawnTask()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[TASKMANAGER] Cannot spawn task - not initialized");
                return;
            }

            if (DayManager.Instance.IsGameOver)
            {
                Debug.LogWarning("[TASKMANAGER] Cannot spawn task - game is over");
                return;
            }

            if (DayManager.Instance.CurrentPhase != DayPhase.Morning)
            {
                Debug.LogWarning("[TASKMANAGER] Cannot spawn task - not Morning phase");
                return;
            }

            if (!PillStateManager.Instance.HasTakenPillToday())
            {
                Debug.LogWarning("[TASKMANAGER] Cannot spawn task - no pill choice made today");
                return;
            }

            if (IsTaskCompletedToday)
            {
                Debug.LogWarning("[TASKMANAGER] Task already completed today");
                return;
            }

            if (ActiveTask != null)
            {
                Debug.LogWarning("[TASKMANAGER] A task is already active");
                return;
            }

            //pill choice -> difficulty mapping (THE key mechanic)
            int currentDay = DayManager.Instance.CurrentDay;
            PillChoice todayChoice = PillStateManager.Instance.GetPillChoice(currentDay);
            TaskDifficulty difficulty = (todayChoice == PillChoice.Taken)
                ? TaskDifficulty.Easy   //compliant worker = easy tasks
                : TaskDifficulty.Hard;  //free thinker = harder tasks

            //find the right TaskData for this day+difficulty combo
            TaskData taskData = FindTaskData(currentDay, difficulty);
            if (taskData == null)
            {
                Debug.LogWarning($"[TASKMANAGER] No TaskData found for Day {currentDay}, {difficulty}");
                return;
            }

            //spawn the prefab if one exists (tech demo mode if not)
            if (taskData.taskPrefab != null && _taskSpawnPoint != null)
            {
                GameObject taskObj = Instantiate(taskData.taskPrefab, _taskSpawnPoint.position, Quaternion.identity);
                ActiveTask = taskObj.GetComponent<ITask>();

                if (ActiveTask != null)
                {
                    ActiveTask.InitializeTask(taskData);
                    Debug.Log($"[TASKMANAGER] Spawned task prefab for Day {currentDay} ({difficulty})");
                }
                else
                {
                    //prefab exists but doesnt implement ITask, setup error
                    Debug.LogWarning("[TASKMANAGER] Task prefab does not implement ITask interface");
                    Destroy(taskObj);
                }
            }
            else
            {
                Debug.Log($"[TASKMANAGER] Task ready for Day {currentDay} ({difficulty}) - no prefab assigned (tech demo)");
            }

            //freeze the player so they cant walk away
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.LockMovement(true);
                Debug.Log("[TASKMANAGER] Player input locked (task active)");
            }
            else
            {
                Debug.LogWarning("[TASKMANAGER] PlayerController not found - cannot lock input");
            }

            OnTaskSpawned?.Invoke(taskData); //TabletUIController picks this up
        }

        //marks the current task as done, unlocks player, and advances to Night
        //this is the link between task system and day progression
        public void CompleteCurrentTask()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[TASKMANAGER] Cannot complete task - not initialized");
                return;
            }

            if (IsTaskCompletedToday)
            {
                Debug.LogWarning("[TASKMANAGER] Task already completed today");
                return;
            }

            if (DayManager.Instance.CurrentPhase != DayPhase.Morning)
            {
                Debug.LogWarning("[TASKMANAGER] Cannot complete task - not Morning phase");
                return;
            }

            int currentDay = DayManager.Instance.CurrentDay;
            IsTaskCompletedToday = true;
            _taskCompletionByDay[currentDay] = true;

            if (ActiveTask != null)
            {
                ActiveTask.CompleteTask(); //let the task do any cleanup
            }
            DestroyActiveTask();

            //unfreeze the player
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.LockMovement(false);
                Debug.Log("[TASKMANAGER] Player input unlocked (task complete)");
            }

            OnTaskCompleted?.Invoke(currentDay);
            Debug.Log($"[TASKMANAGER] Day {currentDay} task completed");

            DayManager.Instance.TaskCompleted(); //THIS triggers Morning -> Night transition
        }

        public bool IsTaskCompleted(int day)
        {
            if (day < 1 || day > 5)
            {
                Debug.LogWarning($"[TASKMANAGER] Invalid day {day}");
                return false;
            }
            return _taskCompletionByDay.ContainsKey(day) && _taskCompletionByDay[day];
        }

        //SaveManager calls this to restore completion state from save data
        public void SetTaskCompleted(int day, bool completed)
        {
            if (day < 1 || day > 5)
            {
                Debug.LogWarning($"[TASKMANAGER] Invalid day {day}");
                return;
            }
            _taskCompletionByDay[day] = completed;

            //if setting the current day, update the convenience flag too
            if (day == DayManager.Instance.CurrentDay)
            {
                IsTaskCompletedToday = completed;
            }
        }

        //searches the TaskData array for a match on day + difficulty
        private TaskData FindTaskData(int day, TaskDifficulty difficulty)
        {
            if (_taskDataAssets == null) return null;

            foreach (TaskData data in _taskDataAssets)
            {
                if (data != null && data.day == day && data.difficulty == difficulty)
                {
                    return data;
                }
            }
            return null;
        }

        //same pattern as PuzzleManager - cast to MonoBehaviour to destroy
        private void DestroyActiveTask()
        {
            if (ActiveTask != null)
            {
                MonoBehaviour taskMono = ActiveTask as MonoBehaviour;
                if (taskMono != null)
                {
                    Destroy(taskMono.gameObject);
                }
                ActiveTask = null;
            }
        }

        //unsub from everything
        private void OnDestroy()
        {
            if (DayManager.Instance != null)
            {
                DayManager.Instance.OnDayChanged -= OnDayChanged;
            }

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.OnSaveDeleted -= OnSaveDeleted;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnInitializationComplete -= Initialize;
            }
        }
    }
}
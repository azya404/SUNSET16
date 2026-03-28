using System.Collections;
using System.Collections.Generic;
using SUNSET16.Core;
using UnityEngine;

public class TaskDifficultySelector : MonoBehaviour
{
    public PillStateManager State;
    [SerializeField] private int taskIndex;          // 1 = Task1Panel, 2 = Task2Panel
    [SerializeField] private GameObject easyContent;
    [SerializeField] private GameObject mediumContent;
    [SerializeField] private GameObject hardContent; // only used on Task2Panel

    private void OnEnable()
    {
        update_task_difficulty();
    }

    private void update_task_difficulty()
    {
        // Turn everything off first
        set_content_active(easyContent, false);
        set_content_active(mediumContent, false);
        set_content_active(hardContent, false);

        if (PillStateManager.Instance == null || DayManager.Instance == null)
        {
            Debug.LogWarning("TaskDifficultySelector: Missing PillStateManager or DayManager.");
            return;
        }

        bool pillTaken =
            PillStateManager.Instance.GetPillChoice(DayManager.Instance.CurrentDay) == PillChoice.Taken;

        if (taskIndex == 1)
        {
            if (pillTaken)
            {
                set_content_active(easyContent, true);
            }
            else
            {
                set_content_active(mediumContent, true);
            }
        }
        else if (taskIndex == 2)
        {
            if (pillTaken)
            {
                set_content_active(mediumContent, true);
            }
            else
            {
                set_content_active(hardContent, true);
            }
        }
        else
        {
            Debug.LogWarning("TaskDifficultySelector: taskIndex must be 1 or 2 on " + gameObject.name);
        }
    }

    private void set_content_active(GameObject contentObject, bool shouldBeActive)
    {
        if (contentObject != null)
        {
            contentObject.SetActive(shouldBeActive);
        }
    }
}

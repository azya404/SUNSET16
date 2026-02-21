/*
difficulty level for daily tasks, directly tied to the players pill choice
this is one of the tangible gameplay consequences of the pill mechanic

- took pill (compliance) = Easy mode tasks (fewer wires, labeled terminals, hints available)
- refused pill (defiance) = Hard mode tasks (more wires, no labels, no hints)

TaskManager checks PillStateManager.HasTakenPillToday() and uses this enum
to spawn the appropriate version of the task prefab
each TaskData ScriptableObject has both an easy and hard prefab reference

originally we considered this as part of a larger TaskDifficulty system
with potentially more levels (Medium, Expert) but we kept it simple with just Easy/Hard
since the binary pill choice maps perfectly to binary difficulty

the ITask interface also uses this - InitializeTask receives the difficulty
so each task implementation knows how to configure itself for easy vs hard
*/
namespace SUNSET16.Core
{
    public enum TaskDifficulty
    {
        Easy = 0,  //on-pill: labeled wires, visible hints, fewer steps - player is a compliant worker
        Hard = 1   //off-pill: unlabeled, no hints, more complex - player is thinking for themselves
    }
}
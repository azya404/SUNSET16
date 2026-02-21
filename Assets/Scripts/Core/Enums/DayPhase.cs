/*
the two phases of each in-game day
originally in the brainstorming doc we had Morning, Day, AND Night (3 phases)
but during actual implementation we simplified it down to just Morning and Night
cos the "Day" phase was essentially the same as Morning gameplay-wise (you just do the task)
so now its: Morning = wake up, pill choice, do task | Night = post-task, optional puzzle, sleep

these are used by DayManager to track what phase the player is currently in
and other systems (like LightingController, AudioManager) listen for phase changes
to update visuals and audio accordingly

NOTE: the int values (0 and 1) matter cos we save them to PlayerPrefs as integers
so if you change the values here, itll break existing saves lol
*/
namespace SUNSET16.Core
{
    public enum DayPhase
    {
        Morning = 0, //wake up, talk to albert, make pill choice, navigate to task room, complete daily task
        Night = 1    //post-task phase: if on-pill you go straight to bed, if off-pill you can explore hidden rooms and do puzzles
    }
}
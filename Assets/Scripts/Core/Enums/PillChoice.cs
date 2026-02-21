/*
the pill choice is THE core mechanic of SUNSET16
every morning the player has to decide: take the pill or refuse it
this single choice affects EVERYTHING - task difficulty, visuals, audio, what rooms you can access, and ultimately the ending

the pill is a metaphor for conformity vs rebellion in this space station setting
- taking the pill = compliance, dull colors, easy tasks, bad ending path (mindless worker)
- refusing the pill = defiance, vibrant colors, hard tasks, good ending path (escape/freedom)

PillStateManager tracks these choices across all 5 days using a Dictionary<int, PillChoice>
and if you take 3+ pills OR refuse 3+ pills, an ending is triggered

originally in the brainstorming doc we used bools (true/false for taken/not taken)
but we switched to an enum so we could also represent "hasnt chosen yet" with None
which is important cos you need to know if the player simply hasnt made their choice today vs actively refusing

NOTE: the int values matter for save/load (PlayerPrefs stores them as ints)
None = -1 so it doesnt conflict with 0 or 1
*/
namespace SUNSET16.Core
{
    public enum PillChoice
    {
        None = -1,      //player hasnt made a choice yet today (default state each morning)
        NotTaken = 0,   //player REFUSED the pill - unlocks hidden rooms at night, harder tasks, vibrant visuals
        Taken = 1       //player TOOK the pill - forced to bedroom at night, easier tasks, dull/grayscale visuals
    }
}
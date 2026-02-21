/*
tracks the state of doors in the game, mainly used for hidden room doors
doors progress through states as the player discovers and enters rooms

the flow is: Locked -> Discovered -> Entered
- Locked: player cant interact with this door at all (or maybe doesnt even know its there)
- Discovered: player found the door (maybe off-pill vision revealed it), light turns on, camera pans to it
- Entered: player has actually gone through the door into the hidden room

Normal is for regular doors (like hallway doors, bedroom door) that are always accessible
HiddenRoomManager uses these states to control which hidden rooms the player can access
and DoorController checks the state to decide what happens when you press E on a door

this was added during the Task & Puzzle systems implementation (around feb 6)
the original core systems doc didnt have door states cos hidden rooms werent designed yet

NOTE: saved to PlayerPrefs as ints via SaveManager
*/
namespace SUNSET16.Core
{
    public enum DoorState
    {
        Locked = 0,       //door is locked/hidden, player cant interact with it
        Discovered = 1,   //player discovered this door (off-pill night vision), door light turns on
        Entered = 2,      //player has entered through this door at least once
        Normal = 3        //regular door thats always accessible (bedroom, hallway, task rooms)
    }
}
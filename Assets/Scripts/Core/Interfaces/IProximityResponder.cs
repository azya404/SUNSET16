/*
interface for objects that want to respond to player proximity entering/exiting their trigger zone
used alongside IInteractable - an object can implement both

currently used by DoorController to change light colour based on proximity:
- player enters zone -> OnPlayerEnterZone() -> light turns green (ready to interact)
- player exits zone  -> OnPlayerExitZone()  -> light reverts to orange (accessible but out of range)

InteractionSystem checks for this interface on the same GO and calls it automatically
on OnTriggerEnter2D / OnTriggerExit2D - zero changes needed to existing interactables
that dont need proximity feedback, they just dont implement this interface
*/
namespace SUNSET16.Core
{
    public interface IProximityResponder
    {
        void OnPlayerEnterZone();
        void OnPlayerExitZone();
    }
}

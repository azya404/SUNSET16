/*
interface for anything in the game the player can interact with by pressing E
this is the core of our interaction system - uses polymorphism so that
InteractionSystem doesnt need to care WHAT the object is, it just calls Interact()

any object that implements this can be interacted with:
- DoorController (opens doors, loads rooms)
- ComputerInteraction (opens tablet UI)
- MirrorInteraction (pill choice)
- BedroomDoorInteraction (TechDemo door)
- future: NPCs, beds, objects, etc

the original brainstorming doc had this defined inside InteractionSystem.cs
but we moved it to its own file in Interfaces/ to keep things clean
cos multiple scripts across different folders need to reference it

how it works:
1. player walks near an object -> InteractionSystem detects proximity via trigger collider
2. InteractionSystem shows the prompt from GetInteractionPrompt()
3. player presses E -> InteractionSystem calls Interact() on whatever implements IInteractable
4. the specific implementation handles what actually happens (load room, open UI, etc)

basically the same concept as how different USB devices all use the same USB port
the port (InteractionSystem) doesnt care whats plugged in, it just provides the connection
*/
namespace SUNSET16.Core
{
    public interface IInteractable
    {
        void Interact(); //called when player presses E while in range - each object does its own thing here
        string GetInteractionPrompt(); //returns the text to show like "Press E to open" or "Press E to examine" etc
        bool GetLocked();
    }
}

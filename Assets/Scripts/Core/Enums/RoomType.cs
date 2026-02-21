/*
categorizes the different types of rooms on the space station
each type has different gameplay rules and access restrictions

HiddenRoomManager and RoomManager use this to know what kind of room the player is in
which affects things like: can the player enter? what happens inside? what lighting to use?

the room types correspond to the space station layout:
- Normal rooms: bedroom, hallways - always accessible
- Task rooms: where you go each day to do your mandatory work task
- Hidden rooms: secret rooms only accessible off-pill at night (contain puzzles and lore)
- EscapePod: the endgame room (part of the good ending escape route)

TODO: we might need more room types later (like a "Cutscene" room type for ending sequences)
*/
namespace SUNSET16.Core
{
    public enum RoomType
    {
        Task = 0,       //rooms where daily mandatory tasks take place (wire puzzle room, water pump room, etc)
        Hidden = 1,     //secret rooms only accessible when off-pill during Night phase, contain puzzles and usb drives
        EscapePod = 2,  //the escape pod room - part of the good ending where player escapes the station
        Normal = 3      //regular rooms like bedroom, hallways, common areas - always accessible
    }
}
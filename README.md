# Arkham Hunters Demo

This is a project I wrote in the summer of 2025 to implement a top-down, 2D tile-based RPG with a realtime D20 combat system ala KotOR or Baldur's Gate 1/2. The system is almost feature complete, though the demo lacks content examples. It's only been lightly tested, and some features are more than a bit sketchy in their implementation; do not hold back with suggestions if you have them.

## Building

### Requirements
* Godot 4.x .NET
* .NET SDK
* Something with Vulkan or GL 3.3+ (any modern GPU)

1. Clone the project.
2. Import in Godot project browser.
3. Open the project.
4. In the top right, press build. If the build is successful, you may run the project.
5. Project -> Settings -> Plugins -> Enable EDI.

If Visual Studio gives you trouble, run Project -> Tools -> C# -> Create C# Solution from the Godot editor.

Finally, if you're using VSCode and are annoyed by the .uid files that Godot leaves everywhere, I added the following to my `files.exclude` setting: 
`**/*.uid`.

## Current Features

* Movement and collision on a sprite-based tilemap.
* Branching dialogue with typewriter dialogue boxes and a node-based editor.
* Arbitrary condition checks in dialogue, addable and visible in node editor.
* Arbitrary action invocations in dialogue, addable and visible in node editor.
* Data-driven items linked to the Godot editor.
* Player inventory with equipment slots and stat bonuses from equipped items.
* Containers with contents customizable in the Godot editor.
* Data-driven abilities linked to the Godot editor.
* Inventory UI with item display per-slot.
* Enemies with in-editor patrol route definition.
* Combat system with ability menu UI.
* Pathfinding and range handling during combat.
* Pause function
* Faction system and per-character hostility overrides.
* Quest system with journal display.

## Missing Features

This was pretty much a "for-fun" project when I did it, and some features we would need to make a game are missing, including:

* Sophisticated Enemy AI (by far the biggest thing missing).
* Saves

## Other Technical Considerations

Almost all of the demo's systems are written with events; in general, all of the classes in the "Scripts/Systems" directory are static classes that manage some mapping between instance ids and object state, and send updates to event channels.
This approach is nice because it makes reacting to changes in e.g. dialogue state or faction relationships very easy for any object to do. It also means that there's a single authoritative record of what, e.g., the inventory of a container being searched is, and the various displays and entities don't need to worry about keeping their records synchronized since Godot's processing is single threaded. The problem with the way it's currently implemented, however, is that Godot doesn't provide any sort of identifier that is persistent between in-game and editor states. All our event systems currently use instance IDs, which are invalidated the moment the game starts running and don't even exist in the editor. This is fine for now, but it poses two problems:
1. Saving data out from the systems such that we can reconstruct a scene state from disk is going to be a challenge.
2. If you do need to reference an entity from the editor, you have to use the scene path to do so, which is unstable.

We have to reckon with these eventually, though they're not pressing currently. One way I've found to address it that I used to make the dialogue scripting a bit less painless is by using resources to store character attributes, exploiting the persistence of resource IDs. This approach has worked thus far and shows promise.


## Art Credit

The art in this demo is placeholder. I did not make it; I bought it several years ago as part of an itch.io bundle in support of Palestine. The artist is Itch.io user Raou. You can find the set here. https://raou.itch.io/dungeon-tileset-top-down-rpg

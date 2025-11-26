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

If Visual Studio gives you trouble, run Project -> Tools -> C# -> Create C# Solution from the Godot editor.

## Current Features

* Movement and collision on a sprite-based tilemap.
* Branching dialogue with typewriter dialogue boxes.
* Data-driven items linked to the Godot editor.
* Player inventory with equipment slots and stat bonuses from equipped items.
* Containers with contents customizable in the Godot editor.
* Data-driven abilities linked to the Godot editor.
* Inventory UI with item display per-slot.
* Enemies with in-editor patrol route definition.
* Combat system with ability menu UI.
* Pathfinding and range handling during combat.

## Missing Features

This was pretty much a "for-fun" project when I did it, and some features we would need to make a game are missing, including:

* Sophisticated Enemy AI (by far the biggest thing missing).
* Skill checks in dialogue. 
* Global "Conditions" triggered from dialogue. 
* Saves
* Pause function

## Other Technical Considerations

A lot of the code handling dialogue, for example, is written to make use of Godot's object hierarchy in passing data between objects. This approach can work, but when multiple objects need access to the same data for any reason, it gets tedious. 
This is why the combat system is written with events instead of passing data around at the object level. Refactoring the dialogue system and probably some other parts of the code to use events would be a good thing to do once we start needing to extend these systems. 


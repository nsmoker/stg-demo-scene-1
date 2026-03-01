# RPG Demo Scene

This project is a playable slice of a tactical RPG with branching dialogue and turn-based combat. It was built in Godot 4 and C#. I drew most of the art as well - thanks to my friend Miles Hoyle for drawing a couple of the background NPC sprites!

The core of the project is a few key features, all of which I implemented in C#. I developed all functionality and non-default tooling myself without the use of any external addons using vanilla Godot features. 

* A flowchart-like branching dialogue editor, which supports arbitrary script invocations and condition checks, as well as a system for parsing and displaying the output of said tool.
* A turn-based combat system with an action economy and action timings that respect animation.
* An ability definition pipeline which includes functionality for definition in the editor, projectiles (which integrate with the turn-based system), effects, custom scripting, and software cursors for targeting.
* Crowd direction functionality, including path definition tools for large groups of generic NPCs and tools for randomized selection of scripted tasks. 

Thanks to my friend Miles Hoyle for drawing some of the generic NPC sprites.

## Gameplay videos

[Combat Demo](https://drive.google.com/file/d/1XbZbP0T1sXa2hGcKg7ENvdwftIK7ATGo/view?usp=sharing)
[Dialogue Demo](https://drive.google.com/file/d/1-3Qt_e_-ddjLhPe_DSi-G6eRiII3gmoI/view?usp=sharing)

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

## Core Systems

### Dialogue

This demo includes a graphical editing tool for dialogue which I developed, called EDI. I was inspired to build the tool after watching a GDC talk on Obsidian's OEI tools.

<img width="3817" height="2003" alt="image" src="https://github.com/user-attachments/assets/fe87540c-5dca-4eee-8361-95e3e5ad9403" />

Dialogue in the demo is branching, and requires the ability to kick off choreography, set condition variables, or trigger quests from the dialogue system itself. To that end, EDI allows the user to drag out connections between different dialogue phrases to signify connections. The image above represents the file for the conversation that was shown in the Dialogue Demo conversation. The dialogue editor exports these conversations as Godot resources which store an adjacency list (i.e. an encoding of a graph). 

Possibilities for the editor's script invocations are varied, some of which can be seen in the "lecture scene" and the combat encounter it triggers, which you can view by walking into the room to the north of the starting area in the demo. The editor allows the user to invoke a script action, potentially concurrently with display of dialogue, by either adding the action as its own node, or right clicking on an existing dialogue node to add an action. Conditions can also be added to each node, in which case the display system will not evaluate the node unless the condition is true. When the condition is false, the system checks for the first node with a true condition from the top down. In this way, the tool lets you encode the flow of the conversation through the visual layout. 

The tool also includes various standard and non-standard usability features, like undo-redo, clipboard support, and support for "linking" nodes (selecting a node in a different part of the graph to link to without having to clutter the visual layout with a new connection).

The demo includes code (in Scripts/Controls/DialogueController.cs) for parsing and displaying the file format emitted by the tool, and numerous examples (in Scripts/DialogueNodes) of custom conditions and script actions for use in the tool. Because script actions are Godot resources, they can be configured in the editor just like any other resource. This means that if the user wants to change the faction of a character in a script, they do not need to write multiple scripts to do it, but can write one and configure it to use it how they want in the dialogue editor/inspector.

### Combat system

Combat in the demo is turn-based and has an XCOM-like action economy. Movement is limited by distance and displayed via a line as the user moves their cursor. The user can attack only once per turn, and can do any permutation of attacking first, moving first then attacking, or moving twice, but attacking always ends their turn. The player's actions are displayed by pips under their character. Combat is entered automatically whenever the player is close to a hostile character (this is how the demo starts the combat encounter). 

Code for the combat system can be found in Scripts/Systems/CombatSystem.cs. Actions are only triggered when the appropriate animations have played (i.e. when the player attacks, the projectile hitting, not the initiation of the attack, is what consumes the action). Once an attack has been initiated, the system rolls a D20, modified by each character's attributes, to decide if the attack hit. If it did, the system rolls a damage dice, separately determined, to find out how much damage was dealt. 

The game has a cover system, demonstrated and explained in the combat encounted. If the player is close enough to a cover object (this is determined via a raycast), a cover indicator appears. If the player is behind cover relative to an enemy, attacks from that enemy are less likely to hit. 

As demonstrated in the demo, new objects can be placed into the encounter after the encounter begins, causing the navmesh to rebake.

Finally, there is a status effect system which you can see in the video. The vortex ability in the demo applies a slow to its target, which both visibly slows them and reduces their movement range for a single action. 

### Abilities
Abilities in the game are defined as custom resources. <img width="1060" height="1652" alt="image" src="https://github.com/user-attachments/assets/4c6c45d7-9d5f-4a70-bc5a-ed6f2f3f86e3" />

Different cooldowns, projectiles, areas of effect, and damage roles are supported. The user can specify scenes containing effects for area abilities, and software cursors for targeting. Targeting cursors can be animated, or simple sprites.

Because the functionality of the abilities is defined in a few key methods (see Scripts/Resources/Abilities/Ability.cs), the user can easily defined custom behaviors for abilities. <img width="3223" height="1104" alt="image" src="https://github.com/user-attachments/assets/d638de9c-435e-4944-a581-f2658ffc71f0" />

### Crowds
When thinking about the setting of the game, I decided that crowds of students at the magic school seemed important to the idea. As a result, I developed a simple crowd director. The crowd director allows you to define a set of tasks and probabilities for each tasks,
and select from that at random at specified intervals, e.g., either walking in one of two directions, or talking to each other. <img width="860" height="1250" alt="image" src="https://github.com/user-attachments/assets/26eeb547-c55c-4424-a1fe-4455929a3b23" />

I also developed a simple method of defining NPC paths. I didn't feel it was necessary to create a complex crowd simulation system, so I instead developed a tool for letting you define a crowd flow visually in the godot editor by clicking and dragging your mouse. You can view the code for it in addons/aeolus. The crowds in the hallway scene of the demo work via this tool. <img width="3124" height="1126" alt="image" src="https://github.com/user-attachments/assets/50cc67d5-01a9-4d05-8d8c-176c286be3e0" />





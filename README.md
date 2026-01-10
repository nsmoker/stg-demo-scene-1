# STG Demo Scene 1

This project is a tiny chunk of a scene for one of my other settings. I primarily made it so I would have a place where I could make some simple content and experiment with getting a scene to play out in a reasonably fluid way. 

The highlights are:

* More sophisticated animation handling for characters, including a separate animation state machine.
* A new notion of "blocking" nodes in the dialogue controller, which allow you to more easily and flexibly tell characters or props to do stuff during dialogue.
* A scene with an actual exit condition/completion state. 
* A bunch of bugfixes for combat.
* An exit menu. 

The actual player controller is really simplified as well since I cut out the inventory and a bunch of other stuff I wasn't working on for the demo, whihc might make it a better jumping off point than the code in inspired. Depending on where we end up with inspired, we can hopefully end up bringing some changes over from it soon.

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
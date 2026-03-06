using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using STGDemoScene1.Scripts.Characters;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Systems;
using System;

namespace STGDemoScene1.Scripts.DialogueNodes;

[Tool]
[GlobalClass]
public partial class VanishOutOfSight : DialogueAction
{
    [Export]
    public CharacterData CharacterToVanish;

    [Export]
    public CharacterData VisionRangeOf;

    private Character _vanishInstance;
    private Character _visionInstance;
    private Area2D _visionRange;

    public override void Execute(Action onComplete)
    {
        _vanishInstance = CharacterSystem.GetInstance(CharacterToVanish.ResourcePath);
        _visionInstance = CharacterSystem.GetInstance(VisionRangeOf.ResourcePath);

        _visionRange = _visionInstance.GetSenseArea();

        _visionRange.BodyExited += VisionHandler;
        onComplete?.Invoke();
    }

    private void VisionHandler(Node2D body)
    {
        if (body is not Character character ||
            !character.CharacterData.ResourcePath.Equals(CharacterToVanish.ResourcePath))
        {
            return;
        }

        _vanishInstance.QueueFree();
        _visionRange.BodyExited -= VisionHandler;
    }
}


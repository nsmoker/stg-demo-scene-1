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
    public CharacterData characterToVanish;

    [Export]
    public CharacterData visionRangeOf;

    private Character _vanishInstance;
    private Character _visionInstance;
    private Area2D _visionRange;

    public override void Execute(Action onComplete)
    {
        _vanishInstance = CharacterSystem.GetInstance(characterToVanish.ResourcePath);
        _visionInstance = CharacterSystem.GetInstance(visionRangeOf.ResourcePath);

        _visionRange = _visionInstance.GetSenseArea();

        _visionRange.BodyExited += VisionHandler;
        onComplete?.Invoke();
    }

    public void VisionHandler(Node2D body)
    {
        if (body is Character character && character.CharacterData.ResourcePath.Equals(characterToVanish.ResourcePath))
        {
            _vanishInstance.QueueFree();
            _visionRange.BodyExited -= VisionHandler;
        }
    }
}


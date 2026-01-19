using Godot;
using System;

public partial class TutorialSeqTrigger : Area2D
{
    [Export]
    public CharacterData Player;

    [Export]
    public Conversation IntroConversation;

    [Export] 
    public Quest IntroQuest;


    private Timer _introTimer;
    
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    public void OnBodyEntered(Node2D body)
    {
        if (QuestSystem.TryGetQuest(IntroQuest.ResourcePath, out Quest introQuest))
        {
            if (body is Player && introQuest.GetCurrentStage().StageNumber == 0)
            {
                _introTimer = new Timer();
                AddChild(_introTimer);
                _introTimer.Timeout += () =>
                {
                    DialogueSystem.StartDialogue(IntroConversation, 0);
                    _introTimer.QueueFree();
                };
                _introTimer.Start(1.0f);
            }
        }
    }
}
using ArkhamHunters.Scripts;
using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;

public partial class DialogueDisplay : PanelContainer
{

    public Conversation Conversation;

    private DialogueGraphNode _runningGraph;

    private double _timeSinceLastWrite;
    private Label _dialogueLabel;
    private VBoxContainer _container;
    private List<Label> _choiceLabels = [];

    [Export]
    private float _typewriterSpeed;

    public override void _Ready()
    {
        _container = GetNode<VBoxContainer>("VBoxContainer");
        _dialogueLabel = _container.GetNode<Label>("DialogueLabel");
    }

    public override void _Process(double delta)
    {
        var currentPhrase = _runningGraph.Content;
        var writingDone = currentPhrase.Length == _dialogueLabel.Text.Length;
        if (!writingDone)
        {
            // Continue writing
            _timeSinceLastWrite += delta;
            if (_timeSinceLastWrite >= _typewriterSpeed)
            {
                _timeSinceLastWrite = 0;
                var currentChar = currentPhrase[_dialogueLabel.Text.Length];
                _dialogueLabel.Text += currentChar;
            }
        }
        else
        {
            foreach (Label label in _choiceLabels)
            {
                label.Visible = true;
            }
        }
    }

    public void SetActiveNode(DialogueGraphNode graph)
    {
        _runningGraph = graph;
        _timeSinceLastWrite = 0;
        foreach (var label in _choiceLabels)
        {
            label.QueueFree();
        }
        _choiceLabels.Clear();
        _dialogueLabel.Text = "";

        var responses = Conversation.GetResponsesForNode(_runningGraph);

        for (int i = 0; i < responses.Count; ++i)
        {
            var choice = responses[i];
            if (choice.NodeType == EverydayDialogueEditor.DialogueNodeType.PlayerResponse)
            {
                var choiceLabel = new Label();
                choiceLabel.Visible = false;
                choiceLabel.Text = $"{i + 1}. {choice.Content}";
                choiceLabel.CustomMinimumSize = _dialogueLabel.CustomMinimumSize;
                choiceLabel.LabelSettings = (LabelSettings) _dialogueLabel.LabelSettings.DuplicateDeep();
                choiceLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                choiceLabel.JustificationFlags = TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound;
                choiceLabel.AutowrapTrimFlags = TextServer.LineBreakFlag.TrimStartEdgeSpaces | TextServer.LineBreakFlag.TrimEndEdgeSpaces;
                choiceLabel.ClipText = true;
                choiceLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
                choiceLabel.MouseFilter = MouseFilterEnum.Stop;
                choiceLabel.MouseEntered += () =>
                {
                    choiceLabel.LabelSettings.FontColor = new Color(1, 1, 0, 1);
                };
                choiceLabel.MouseExited += () =>
                {
                    choiceLabel.LabelSettings.FontColor = new Color(1, 1, 1, 1);
                };
                choiceLabel.GuiInput += (InputEvent e) =>
                {
                    if (e is InputEventMouseButton mouseEvent && mouseEvent.IsPressed() && mouseEvent.ButtonIndex == MouseButton.Left)
                    {
                        SetActiveNode(choice);
                    }
                };
                _container.AddChild(choiceLabel);
                _choiceLabels.Add(choiceLabel);
            }
        }
    }

    public bool Advance()
    {
        var currentPhrase = _runningGraph.Content;

        if (currentPhrase.Length == _dialogueLabel.Text.Length)
        {
            var continuations = Conversation.GetResponsesForNode(_runningGraph);
            if (continuations.Count == 0)
            {
                _timeSinceLastWrite = 0;
                foreach (var label in _choiceLabels)
                {
                    label.QueueFree();
                }
                _dialogueLabel.Text = "";
                return false;
            }
            else
            {
                foreach (var continuation in continuations)
                {
                    if (continuation.Condition == null || continuation.Condition.Evaluate())
                    {
                        SetActiveNode(continuation);
                        break;
                    }
                }
            }
        }
        else
        {   
            _dialogueLabel.Text = currentPhrase;
        }

        return true;
    }
}

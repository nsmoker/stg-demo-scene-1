using ArkhamHunters.Scripts;
using Godot;
using System;
using System.Collections.Generic;

public partial class DialogueDisplay : PanelContainer
{
    private DialogueGraph _runningGraph;

    private int _currentPhraseIndex = 0;
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
        var currentPhrase = _runningGraph.Phrases[_currentPhraseIndex];
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

    public void SetActiveGraph(DialogueGraph graph)
    {
        _runningGraph = graph;
        _currentPhraseIndex = 0;
        _timeSinceLastWrite = 0;
        foreach (var label in _choiceLabels)
        {
            label.QueueFree();
        }
        _choiceLabels.Clear();
        _dialogueLabel.Text = "";

        for (int i = 0; i < _runningGraph.Choices.Count; ++i)
        {
            DialogueChoice choice = _runningGraph.Choices[i];
            var choiceLabel = new Label();
            choiceLabel.Visible = false;
            choiceLabel.Text = $"{i + 1}. {choice.Phrase}";
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
                    SetActiveGraph(choice.Continuation);
                }
            };
            _container.AddChild(choiceLabel);
            _choiceLabels.Add(choiceLabel);
        }
    }

    public bool Advance()
    {
        var currentPhrase = _runningGraph.Phrases[_currentPhraseIndex];

        if (currentPhrase.Length == _dialogueLabel.Text.Length && _runningGraph.Choices.Count == 0)
        {
            _currentPhraseIndex = 0;
            _timeSinceLastWrite = 0;
            foreach (var label in _choiceLabels)
            {
                label.QueueFree();
            }
            _dialogueLabel.Text = "";
            return false;
        }

        _dialogueLabel.Text = currentPhrase;

        return true;
    }
}

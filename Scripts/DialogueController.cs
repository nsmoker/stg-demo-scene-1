using ArkhamHunters.Scripts;
using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;

public partial class DialogueController : ScrollContainer
{

    private Conversation _conversation;

    private DialogueGraphNode _runningGraph;

    private Label _dialogueLabel;
    private VBoxContainer _container;
    private List<Label> _choiceLabels = [];

    [Export]
    private float _typewriterSpeed;

    public interface IDialogueControllerState
    {
        void Process(double delta, DialogueController controller);
    }

    protected IDialogueControllerState State;

    public static bool TryGetNodeById(Conversation conversation, ulong id, out DialogueGraphNode node)
    {
        node = conversation.Nodes.First(x => x.DNodeId == id);
        return conversation.Nodes.Any(x => x.DNodeId == id);
    }

    public static bool TryGetContinuation(Conversation conversation, ulong rootId, int i, out DialogueGraphNode node)
    {
        var contained = TryGetNodeById(conversation, rootId, out var n);
        var continuations = conversation.GetContinuationsForNode(n);
        if (continuations.Count > i)
        {
            node = continuations[i];
            return true;
        }
        else
        {
            node = null;
            return false;
        }
    }

    // Returns how many possible continuations there are. If there is a definite continuation, it will be returned in nodeOut.
    public static int EvaluateContinuation(Conversation conversation, DialogueGraphNode nodeIn, out DialogueGraphNode nodeOut)
    {
        var continuations = conversation.GetContinuationsForNode(nodeIn);
        var possibleContinuationCount = 0;
        nodeOut = null;

        if (continuations.Count > 0)
        {
            foreach (var continuation in continuations)
            {
                if (continuation.NodeType == EverydayDialogueEditor.DialogueNodeType.PlayerResponse && (continuation.Condition == null || continuation.Condition.Evaluate()))
                {
                    possibleContinuationCount += 1;
                }
                else if (possibleContinuationCount == 0 && (continuation.Condition == null || continuation.Condition.Evaluate()))
                {
                    possibleContinuationCount = 1;
                    nodeOut = continuation;
                    break;
                }
            }

        }
        return possibleContinuationCount;
    }

    private class IdleState : IDialogueControllerState
    {
        public IdleState(DialogueController controller)
        {
            controller.Visible = false;
            controller.ProcessMode = ProcessModeEnum.Disabled;
            controller._choiceLabels.ForEach(x => x.QueueFree());
            controller._choiceLabels.Clear();
        }

        public void Process(double delta, DialogueController controller) { }
    }

    private class EvalState : IDialogueControllerState
    {
        private DialogueGraphNode _node;
        public EvalState(DialogueGraphNode node, DialogueController controller) 
        { 
            _node = node;
            
        }

        public void Process(double delta, DialogueController controller) 
        {
            var possibleContinuations = EvaluateContinuation(controller._conversation, _node, out var nodeOut);

            if (possibleContinuations == 0)
            {
                controller.State = new IdleState(controller);
                controller.ConversationEnded?.Invoke(controller._conversation);
            }
            else if (possibleContinuations == 1)
            {
                switch (nodeOut.NodeType)
                {
                    case EverydayDialogueEditor.DialogueNodeType.Node:
                        controller.State = new WriteState(controller, nodeOut);
                        break;
                    case EverydayDialogueEditor.DialogueNodeType.ScriptAction:
                        nodeOut.Action?.Execute();
                        controller.State = new EvalState(nodeOut, controller);
                        break;
                    case EverydayDialogueEditor.DialogueNodeType.ScriptEntry:
                        controller.State = new EvalState(nodeOut, controller);
                        break;
                    case EverydayDialogueEditor.DialogueNodeType.PlayerResponse:
                        controller.State = new WriteState(controller, nodeOut);
                        break;
                }

                controller.DialogueNodeReached?.Invoke(controller, nodeOut);
            }
            else // Must be player responses
            {
                controller.State = new ChoiceState(controller, _node);
            }
        }
    }

    private class WriteState : IDialogueControllerState
    {
        private DialogueGraphNode _node;
        private double _timeSinceLastWrite = 0;

        public WriteState(DialogueController controller, DialogueGraphNode node) 
        {
            _node = node;
            controller._dialogueLabel.Visible = true;
            controller._dialogueLabel.Text = "";
            controller._choiceLabels.ForEach(x => x.QueueFree());
            controller._choiceLabels.Clear();
        }

        public void Process(double delta, DialogueController controller)
        {
            var currentPhrase = _node.Content;
            var writingDone = currentPhrase.Length == controller._dialogueLabel.Text.Length;
            if (!writingDone)
            {
                // Continue writing
                _timeSinceLastWrite += delta;
                if (_timeSinceLastWrite >= controller._typewriterSpeed)
                {
                    _timeSinceLastWrite = 0;
                    var currentChar = currentPhrase[controller._dialogueLabel.Text.Length];
                    controller._dialogueLabel.Text += currentChar;
                }
            }

            if (Input.IsActionJustPressed("Interact"))
            {
                if (!writingDone)
                {
                    controller._dialogueLabel.Text = _node.Content;
                }
                else
                {
                    controller.State = new EvalState(_node, controller);
                }
            }
        }
    }

    private class ChoiceState : IDialogueControllerState
    {
        private DialogueController _controller;
        private DialogueGraphNode _node;

        private void AddChoiceLabel(DialogueController controller, DialogueGraphNode choice, int number)
        {
            var choiceLabel = new Label
            {
                Visible = true,
                Text = $"{number + 1}. {choice.Content}",
                CustomMinimumSize = controller._dialogueLabel.CustomMinimumSize,
                LabelSettings = (LabelSettings) controller._dialogueLabel.LabelSettings.DuplicateDeep(),
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                JustificationFlags = TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound,
                AutowrapTrimFlags = TextServer.LineBreakFlag.TrimStartEdgeSpaces | TextServer.LineBreakFlag.TrimEndEdgeSpaces,
                ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
                MouseFilter = MouseFilterEnum.Stop
            };
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
                    controller.State = new WriteState(controller, choice);
                    controller.DialogueNodeReached?.Invoke(controller, choice);
                }
            };
            controller._container.AddChild(choiceLabel);
            controller._choiceLabels.Add(choiceLabel);
        }

        public ChoiceState(DialogueController controller, DialogueGraphNode node)
        {
            _controller = controller;
            _node = node;

            var choices = controller._conversation.GetContinuationsForNode(node).Where(x => x.NodeType == EverydayDialogueEditor.DialogueNodeType.PlayerResponse).ToList();
            for (int i = 0; i < choices.Count; ++i)
            {
                AddChoiceLabel(controller, choices[i], i);
            }
        }

        public void Process(double delta, DialogueController controller)
        {

        }
    }

    public delegate void ConversationBeganEvent(Conversation conversation);
    public delegate void ConversationEndedEvent(Conversation conversation);
    public delegate void DialogueNodeReachedEvent(DialogueController controller, DialogueGraphNode node);

    public ConversationBeganEvent ConversationBegan;
    public ConversationEndedEvent ConversationEnded;
    public DialogueNodeReachedEvent DialogueNodeReached;

    public override void _Ready()
    {
        _container = GetNode<VBoxContainer>("PanelContainer/VBoxContainer");
        _dialogueLabel = _container.GetNode<Label>("DialogueLabel");
        State = new IdleState(this);
    }

    public override void _Process(double delta)
    {
        State.Process(delta, this);
    }

    public void BeginConversation(Conversation conversation, DialogueGraphNode entryPoint)
    {
        _conversation = conversation;

        Visible = true;
        State = new EvalState(entryPoint, this);

        ProcessMode = ProcessModeEnum.Always;

        ConversationBegan?.Invoke(conversation);
    }
}

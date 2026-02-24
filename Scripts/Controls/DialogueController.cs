using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class DialogueController : ScrollContainer
{

    private Conversation _conversation;

    private DialogueGraphNode _runningGraph;

    private Label _dialogueLabel;
    private Label _speakerLabel;
    private VBoxContainer _container;
    private readonly List<Label> _choiceLabels = [];

    private bool actionDone = true;

    [Export]
    private float _typewriterSpeed;

    [Export]
    public Color HoveredColor;
    [Export]
    public Color NormalColor;

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
        _ = TryGetNodeById(conversation, rootId, out var n);
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

        if (nodeIn.LinkDNodeId != 0)
        {
            if (TryGetNodeById(conversation, nodeIn.LinkDNodeId, out var linkedNode))
            {
                if (linkedNode.Condition == null || linkedNode.Condition.Evaluate())
                {
                    nodeOut = linkedNode;
                    return 1;
                }
            }
        }

        if (continuations.Count > 0)
        {
            foreach (var continuation in continuations)
            {
                if (continuation.NodeType == EverydayDialogueEditor.DialogueNodeType.PlayerResponse && (continuation.Condition == null || continuation.Condition.Evaluate()))
                {
                    nodeOut = continuation;
                    possibleContinuationCount += 1;
                }
                else if (possibleContinuationCount == 0 && (continuation.Condition == null || continuation.Condition.Evaluate()))
                {
                    nodeOut = continuation;
                    possibleContinuationCount = 1;
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

    private class EvalState(DialogueGraphNode node) : IDialogueControllerState
    {
        private readonly DialogueGraphNode _node = node;

        public void Process(double delta, DialogueController controller)
        {
            if (!controller.actionDone)
            {
                return;
            }

            var possibleContinuations = EvaluateContinuation(controller._conversation, _node, out var nodeOut);

            if (possibleContinuations == 0)
            {
                controller.State = new IdleState(controller);
                DialogueSystem.CompleteDialogue();
            }
            else if (possibleContinuations == 1)
            {
                switch (nodeOut.NodeType)
                {
                    case EverydayDialogueEditor.DialogueNodeType.Node:
                        controller.State = new WriteState(controller, nodeOut);
                        break;
                    case EverydayDialogueEditor.DialogueNodeType.ScriptAction:
                        controller.actionDone = false;
                        nodeOut.Action?.Execute(() => controller.actionDone = true);
                        controller.State = new EvalState(nodeOut);
                        break;
                    case EverydayDialogueEditor.DialogueNodeType.ScriptEntry:
                        controller.State = new EvalState(nodeOut);
                        break;
                    case EverydayDialogueEditor.DialogueNodeType.PlayerResponse:
                        controller.State = new WriteState(controller, nodeOut);
                        break;
                }
            }
            else // Must be player responses
            {
                controller.State = new ChoiceState(controller, _node);
            }
        }
    }

    private class WriteState : IDialogueControllerState
    {
        private readonly DialogueGraphNode _node;
        private double _timeSinceLastWrite = 0;

        public WriteState(DialogueController controller, DialogueGraphNode node)
        {
            _node = node;
            controller._dialogueLabel.Visible = true;
            controller._dialogueLabel.Text = "";
            controller._choiceLabels.ForEach(x => x.QueueFree());
            controller._choiceLabels.Clear();

            // Update the speaker label
            controller._speakerLabel.Text = node.Speaker;
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
                    controller.State = new EvalState(_node);
                }
            }
        }
    }

    private class ChoiceState : IDialogueControllerState
    {
        private readonly DialogueController _controller;
        private readonly DialogueGraphNode _node;

        private void AddChoiceLabel(DialogueController controller, DialogueGraphNode choice, int number)
        {
            var choiceLabel = new DialogueLabel
            {
                CustomMinimumSize = controller._dialogueLabel.CustomMinimumSize,
                LabelSettings = (LabelSettings) controller._dialogueLabel.LabelSettings.DuplicateDeep(),
                NormalColor = controller.NormalColor,
                HoveredColor = controller.HoveredColor
            };
            choiceLabel.SetIndex(number + 1);
            choiceLabel.SetDialogue(choice.Content);
            choiceLabel.SelectionCallback = () => controller.State = new WriteState(_controller, choice);
            controller._container.AddChild(choiceLabel);
            controller._choiceLabels.Add(choiceLabel);
        }

        public ChoiceState(DialogueController controller, DialogueGraphNode node)
        {
            _controller = controller;
            _node = node;

            var choices = controller._conversation.GetContinuationsForNode(node).Where(x => x.NodeType == EverydayDialogueEditor.DialogueNodeType.PlayerResponse && (x.Condition == null || x.Condition.Evaluate())).ToList();
            for (int i = 0; i < choices.Count; ++i)
            {
                AddChoiceLabel(controller, choices[i], i);
            }
        }

        public void Process(double delta, DialogueController controller)
        {

        }
    }

    public override void _Ready()
    {
        _container = GetNode<VBoxContainer>("PanelContainer/VBoxContainer");
        _dialogueLabel = _container.GetNode<Label>("DialogueLabel");
        _speakerLabel = _container.GetNode<Label>("SpeakerLabel");
        DialogueSystem.OnDialogueStarted += BeginConversation;
        State = new IdleState(this);
    }

    public override void _Process(double delta) => State.Process(delta, this);

    public void BeginConversation(Conversation conversation, int entryPoint)
    {
        _conversation = conversation;
        _dialogueLabel.Text = "";
        _speakerLabel.Text = "";
        Visible = true;
        State = new EvalState(_conversation.EntryPoints[entryPoint]);

        ProcessMode = ProcessModeEnum.Always;
    }
}

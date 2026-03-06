using Godot;

namespace STGDemoScene1.Scripts.Controls;

public partial class DialogueLabel : Label
{
    private Shortcut _shortcut;
    private int _index;
    private string _dialogue;

    public System.Action SelectionCallback;
    public Color HoveredColor;
    public Color NormalColor;

    public override void _Ready()
    {
        MouseEntered += OnMouseEnter;
        MouseExited += OnMouseExit;
        MouseFilter = MouseFilterEnum.Stop;
        TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        ClipText = false;
        AutowrapTrimFlags = TextServer.LineBreakFlag.TrimStartEdgeSpaces | TextServer.LineBreakFlag.TrimEndEdgeSpaces;
        JustificationFlags = TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound;
        AutowrapMode = TextServer.AutowrapMode.WordSmart;
    }

    public void SetDialogue(string text)
    {
        _dialogue = text;
        Text = $"{_index}. {text}";
    }

    public void SetIndex(int i)
    {
        _index = i;
        Text = $"{_index}. {_dialogue}";
        _shortcut = new Shortcut()
        {
            Events = [
                new InputEventKey()
                {
                    Keycode = Key.Key0 + _index,
                }
            ]
        };
    }

    public override void _ShortcutInput(InputEvent @event)
    {
        if (_shortcut.MatchesEvent(@event) && @event.IsReleased())
        {
            SelectionCallback?.Invoke();
        }
        else if (_shortcut.MatchesEvent(@event) && @event.IsPressed())
        {
            LabelSettings.FontColor = HoveredColor;
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.IsPressed() && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            SelectionCallback?.Invoke();
        }
    }

    private void OnMouseEnter() => LabelSettings.FontColor = HoveredColor;

    private void OnMouseExit() => LabelSettings.FontColor = NormalColor;
}

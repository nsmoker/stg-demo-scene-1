#if TOOLS
using Godot;
using STGDemoScene1.Addons.Edi.Scripts;

namespace STGDemoScene1.Addons.Edi;

[Tool]
public partial class Edi : EditorPlugin
{
    private Control _dockContent;
    private DialogueEditor _editor;
    private EditorDock _dock;
    public override void _EnterTree()
    {
        // Initialization of the plugin goes here.
        _dockContent = GD.Load<PackedScene>(ProjectSettings.GlobalizePath("res://addons/edi/Scenes/graph_edit.tscn")).Instantiate<Control>();
        _editor = (DialogueEditor) _dockContent;
        _dock = new EditorDock
        {
            Title = "Dialogue Editor"
        };
        _dock.AddChild(_dockContent);
        AddDock(_dock);
        _editor.undoRedoManager = GetUndoRedo();
    }

    public override void _ExitTree()
    {
        // Clean-up of the plugin goes here.
        // Remove the dock.
        RemoveDock(_dock);
        // Erase the control from the memory.
        _dockContent.Free();

    }

    public override bool _Handles(GodotObject @object) => @object is Conversation;

    public override void _SaveExternalData()
    {
        if (_editor.EditorNode.HasFocus() || _editor.HasFocus())
        {
            _editor.Save();
        }
    }

    public override void _Edit(GodotObject @object)
    {
        if (@object is Conversation conversation)
        {
            _editor.SetConversationResource(conversation);
        }
    }

    public override void _Clear() => _editor.SetConversationResource(null, false);
}
#endif

#if TOOLS
using Godot;
using System;

[Tool]
public partial class Edi : EditorPlugin
{
	private Control _dock;
    private DialogueEditor _editor;
	public override void _EnterTree()
	{
        // Initialization of the plugin goes here.
        _dock = GD.Load<PackedScene>(ProjectSettings.GlobalizePath("res://addons/edi/Scenes/graph_edit.tscn")).Instantiate<Control>();
        _editor = (DialogueEditor) _dock;
        _editor.undoRedoManager = GetUndoRedo();
        AddControlToDock(DockSlot.RightUr, _dock);
    }

    public override void _ExitTree()
	{
        // Clean-up of the plugin goes here.
        // Remove the dock.
        RemoveControlFromDocks(_dock);
        // Erase the control from the memory.
        _dock.Free();

    }

    public override bool _Handles(GodotObject @object)
    {
        return @object is Conversation;
    }

    public override void _SaveExternalData()
    {
        if (_editor.EditorNode.HasFocus())
        {
            _editor.Save();
        }
    }

    public override void _ApplyChanges()
    {
        _editor.Save();
    }

    public override void _Edit(GodotObject @object)
    {
        if (@object is Conversation conversation)
        {
            _editor.SetConversationResource(conversation);
        }
    }

    public override void _Clear()
    {
        _editor.SetConversationResource(null);
    }
}
#endif

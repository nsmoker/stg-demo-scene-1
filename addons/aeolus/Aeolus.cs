#if TOOLS
using Godot;
using System;

[Tool]
public partial class Aeolus : EditorPlugin
{
    private FlowField _editedField;
    private Node2D _editedScene;

    private Vector2 _controlPointOrigin = Vector2.Zero;
    private Vector2 _controlPointDestination = Vector2.Zero;

    private bool _controlPointInCreation = false;


    public override void _EnablePlugin()
    {
        SceneChanged += OnSceneChanged;
    }

    public override bool _Handles(GodotObject @object)
    {
        return @object is FlowField;
    }

    public override void _Edit(GodotObject @object)
    {
        if (@object is FlowField field)
        {
            _editedField = field;
            _editedScene = EditorInterface.Singleton.GetEditedSceneRoot() as Node2D;
            _editedScene.QueueRedraw();
        }
    }

    private void AddControlPoint(Vector2 controlPoint, Vector2 gradient)
    {
        var controlPointInstance = new FlowFieldControlPoint
        {
            ControlPoint = controlPoint,
            Gradient = gradient
        };
        _editedField.ControlPoints.Add(controlPointInstance);
        _controlPointInCreation = false;
        _editedScene.QueueRedraw();
        _editedField.NotifyPropertyListChanged();
    }

    private void RemoveControlPoint(int i)
    {
        _editedField.ControlPoints.RemoveAt(i);
        _editedScene.QueueRedraw();
        _editedField.NotifyPropertyListChanged();
    }

    public override bool _ForwardCanvasGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton button)
        {
            if (button.IsPressed() && button.ButtonIndex == MouseButton.Right && _editedField != null)
            {
                _controlPointInCreation = true;
                _controlPointOrigin = _editedScene.GetLocalMousePosition();
                _editedScene.QueueRedraw();
                return true;
            } 
            else if (button.IsReleased() && button.ButtonIndex == MouseButton.Right && _controlPointInCreation)
            {
                var undoRedo = GetUndoRedo();
                undoRedo.CreateAction($"Add point at {_controlPointOrigin}");
                undoRedo.AddDoMethod(this, "AddControlPoint", _controlPointOrigin, _controlPointDestination - _controlPointOrigin);
                undoRedo.AddUndoMethod(this, "RemoveControlPoint", _editedField.ControlPoints.Count);
                undoRedo.CommitAction();
                return true;
            }
        }
        else if (@event is InputEventMouseMotion && _controlPointInCreation)
        {
            _controlPointDestination = _editedScene.GetLocalMousePosition();
            _editedScene.QueueRedraw();
            return true;
        }

        return false;
    }

    public void DrawOverScene()
    {
        if (_editedField == null)
        {
            return;
        }

        Vector2 sceneSize = _editedScene.GetNode<Sprite2D>("SceneBackdrop").Texture.GetSize();

        Vector2I sceneSizeOffset = new Vector2I((int) sceneSize.X, (int) sceneSize.Y) / 2;

        int rasterSize = (int) (sceneSize.Y / 18.0f);

        if (_controlPointInCreation)
        {
            // Add to the flow field for sampling purposes.
            _editedField.ControlPoints.Add(new FlowFieldControlPoint
            {
                Gradient = _controlPointDestination - _controlPointOrigin,
                ControlPoint = _controlPointOrigin
            });
            var dest = _controlPointDestination;
            var gradient = _controlPointDestination - _controlPointOrigin;
            var col = new Color(1.0f, 1.0f, 1.0f, gradient.Length() * 5.0f);
            _editedScene.DrawLine(_controlPointOrigin, dest, col, 1.1f);
            var head = -gradient.Normalized() * 4.0f;
            _editedScene.DrawLine(dest + head * 0.05f, dest + head.Rotated(0.5f), col, 1.1f);
            _editedScene.DrawLine(dest + head * 0.05f, dest + head.Rotated(-0.5f), col, 1.1f);
        }

        for (int x = -sceneSizeOffset.X; x <= sceneSizeOffset.X; x += rasterSize)
        {
            for (int y = -sceneSizeOffset.Y; y <= sceneSizeOffset.Y; y += rasterSize)
            {
                var point = new Vector2(x, y);
                var gradient = _editedField.SampleFlowField(point);
                var dest = point + gradient.Normalized() * 15.0f;
                var col = new Color(1.0f, 1.0f, 1.0f, gradient.Length() * 5.0f);
                _editedScene.DrawLine(point, dest, col, 1.1f);
                var head = -gradient.Normalized() * 4.0f;
                _editedScene.DrawLine(dest + head * 0.05f, dest + head.Rotated(0.5f), col, 1.1f);
                _editedScene.DrawLine(dest + head * 0.05f, dest + head.Rotated(-0.5f), col, 1.1f);
            }
        }

        if (_controlPointInCreation)
        {
            // Remove the in-creation controlpoint.
            _editedField.ControlPoints.RemoveAt(_editedField.ControlPoints.Count - 1);
        }
    }

    public void OnSceneChanged(Node _sceneRoot)
    {
        if (_sceneRoot is Node2D sceneRoot)
        {
            _editedScene = sceneRoot;
        }
    }

    public override void _MakeVisible(bool visible)
    {
        if (visible && _editedScene != null)
        {
            _editedScene.Draw += DrawOverScene;
        }
        else if (_editedScene != null)
        {
            _editedScene.Draw -= DrawOverScene;
            _editedScene.QueueRedraw();
        }
    }

    public override void _Clear()
    {
        ZeroEditorFields();
    }

    public override void _DisablePlugin()
    {
        ZeroEditorFields();
    }

    private void ZeroEditorFields()
    {
        _editedField = null;
    }
}
#endif
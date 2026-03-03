using Godot;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Systems;

namespace STGDemoScene1.Scripts.Controls;

public partial class MapDisplay : PanelContainer
{
    [Export]
    public PackedScene MapMarkerScene;

    [Export]
    public MapCaptureResource MapCaptureRes;

    private TextureRect _textureRect;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _textureRect = GetNode<TextureRect>("VBoxContainer/TextureRect");
        _textureRect.Texture = ImageTexture.CreateFromImage(MapCaptureRes.MapImage);

        QuestSystem.OnQuestUpdated += OnQuestUpdated;
    }

    public void AddMarker(Vector2 globalPosition)
    {
        var localPos = MapCaptureRes.LocalTransform * globalPosition;
        var trueTexSize = new Vector2(_textureRect.Size.Y * MapCaptureRes.MapImage.GetSize().Aspect(), _textureRect.Size.Y);
        var rat = trueTexSize / MapCaptureRes.MapImage.GetSize();
        localPos *= rat;
        var markerObj = MapMarkerScene.Instantiate<Sprite2D>();
        _textureRect.AddChild(markerObj);
        markerObj.Position = localPos;
    }

    public void OnQuestUpdated(Quest quest)
    {
        var stage = quest.GetCurrentStage();
        if (stage.Objective != null && QuestSystem.TryGetMarkerPosition(stage.Objective.ResourcePath, out var pos))
        {
            AddMarker(pos);
        }
    }
}

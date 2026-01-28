using Godot;
using System.Linq;

[Tool]
[GlobalClass]
public partial class MapCapture : EditorScript
{
    public override async void _Run()
    {
        var mapCaptureViewport = new SubViewport
        {
            Size = new Vector2I(800, 800),
            TransparentBg = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Once,
            Name = "MapCaptureViewport"
        };
        EditorInterface.Singleton.GetEditedSceneRoot().AddChild(mapCaptureViewport);

        Node2D sceneDup = EditorInterface.Singleton.GetEditedSceneRoot().Duplicate() as Node2D;
        var sceneRect = new Rect2();
        sceneDup.FindChildren("*", recursive: true).OfType<CollisionShape2D>().ToList().ForEach(n => n.Visible = false);
        sceneDup.FindChildren("*", recursive: true).OfType<Camera2D>().ToList().ForEach(n => n.Visible = false);
        sceneDup.FindChildren("*", recursive: true).OfType<CharacterBody2D>().ToList().ForEach(n => n.Visible = false);
        sceneDup.FindChildren("*", recursive: true).ToList().ForEach(n =>
        {
            if (n is TileMapLayer layer)
            {
                sceneRect = sceneRect.Expand(layer.ToGlobal(layer.MapToLocal(layer.GetUsedRect().End)));
                sceneRect = sceneRect.Expand(layer.ToGlobal(layer.MapToLocal(layer.GetUsedRect().Position)));
            }
            if (n is Sprite2D spr)
            {
                _ = sceneRect.Expand(spr.ToGlobal(spr.GetRect().End));
                _ = sceneRect.Expand(spr.ToGlobal(spr.GetRect().Position));
            }
        });
        mapCaptureViewport.AddChild(sceneDup);

        var viewportTrans = sceneDup.GetViewportTransform();
        var viewportOrigin = viewportTrans.Origin;
        var visibleRect = mapCaptureViewport.GetVisibleRect();
        sceneDup.Position = Vector2.Zero;
        sceneDup.Scale = Vector2.One / (sceneRect.Size / visibleRect.Size);
        sceneRect.Position = visibleRect.Position;

        _ = await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var mapCaptureResource = new MapCaptureResource
        {
            MapImage = mapCaptureViewport.GetTexture().GetImage(),
            LocalTransform = sceneDup.Transform
        };
        _ = ResourceSaver.Save(mapCaptureResource, "res://map_capture.tres");

        mapCaptureViewport.RemoveChild(sceneDup);
        EditorInterface.Singleton.GetEditedSceneRoot().RemoveChild(mapCaptureViewport);
        sceneDup.QueueFree();
        mapCaptureViewport.QueueFree();
    }
}

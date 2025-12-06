using System.Collections.Generic;
using System.Linq;
using Godot;

[Tool]
[GlobalClass]
public partial class MapCapture : EditorScript
{
    public override async void _Run()
    {
        Dictionary<ulong, bool> WasVisible = [];
        var mapCaptureViewport = new SubViewport
        {
            Size = new Vector2I(800, 800),
            TransparentBg = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Once,
            Name = "MapCaptureViewport"
        };
        GetScene().AddChild(mapCaptureViewport);

        Node2D sceneDup = GetScene().Duplicate() as Node2D;
        var sceneRect = new Rect2();
        sceneDup.FindChildren("*", recursive: true).OfType<CollisionShape2D>().ToList().ForEach(n =>
        {
            WasVisible[n.GetInstanceId()] = n.Visible;
            n.Visible = false;
        });
        sceneDup.FindChildren("*", recursive: true).ToList().ForEach(n =>
        {
            if (n is TileMapLayer layer)
            {
                sceneRect = sceneRect.Expand(layer.ToGlobal(layer.MapToLocal(layer.GetUsedRect().End)));
                sceneRect = sceneRect.Expand(layer.ToGlobal(layer.MapToLocal(layer.GetUsedRect().Position)));
            }
            if (n is Sprite2D spr)
            {
                sceneRect.Expand(spr.ToGlobal(spr.GetRect().End));
                sceneRect.Expand(spr.ToGlobal(spr.GetRect().Position));
            }
        });
        mapCaptureViewport.AddChild(sceneDup);

        var viewportTrans = sceneDup.GetViewportTransform();
        var viewportOrigin = viewportTrans.Origin;
        var visibleRect = mapCaptureViewport.GetVisibleRect();
        sceneDup.Position = Vector2.Zero;
        sceneDup.Scale = Vector2.One / (sceneRect.Size / visibleRect.Size);
        sceneRect.Position = visibleRect.Position;

        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var img = mapCaptureViewport.GetTexture().GetImage();
        img.SavePng("res://map_capture.png");

        mapCaptureViewport.RemoveChild(sceneDup);
        GetScene().RemoveChild(mapCaptureViewport);
        sceneDup.QueueFree();
        mapCaptureViewport.QueueFree();
    }
}
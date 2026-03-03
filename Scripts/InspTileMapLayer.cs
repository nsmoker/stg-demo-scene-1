using Godot;
using System.Collections.Generic;

namespace STGDemoScene1.Scripts;

public partial class InspTileMapLayer : Node2D
{
    private readonly List<Vector2> _horizontalPoints = [];
    private readonly List<Vector2> _verticalPoints = [];

    [Export]
    private Color _gridColor;

    [Export]
    private TileMapLayer _tileMapLayer;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        var tilemap_rect = _tileMapLayer.GetUsedRect();
        var tilemap_cell_size = _tileMapLayer.TileSet.TileSize;

        for (int y = 0; y < tilemap_rect.Size.Y; ++y)
        {
            _horizontalPoints.Add(tilemap_rect.Position + new Vector2(0, y * tilemap_cell_size.Y));
            _horizontalPoints.Add(tilemap_rect.Position + new Vector2(tilemap_rect.Size.X * tilemap_cell_size.X, y * tilemap_cell_size.Y));
        }

        for (int x = 0; x < tilemap_rect.Size.X; ++x)
        {
            _verticalPoints.Add(tilemap_rect.Position + new Vector2(x * tilemap_cell_size.X, 0));
            _verticalPoints.Add(tilemap_rect.Position + new Vector2(x * tilemap_cell_size.X, tilemap_rect.Size.Y * tilemap_cell_size.Y));
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawMultiline([.. _horizontalPoints], _gridColor);
        DrawMultiline([.. _verticalPoints], _gridColor);
    }
}

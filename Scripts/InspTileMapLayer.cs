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
        var tilemapRect = _tileMapLayer.GetUsedRect();
        var tilemapCellSize = _tileMapLayer.TileSet.TileSize;

        for (int y = 0; y < tilemapRect.Size.Y; ++y)
        {
            _horizontalPoints.Add(tilemapRect.Position + new Vector2(0, y * tilemapCellSize.Y));
            _horizontalPoints.Add(tilemapRect.Position + new Vector2(tilemapRect.Size.X * tilemapCellSize.X, y * tilemapCellSize.Y));
        }

        for (int x = 0; x < tilemapRect.Size.X; ++x)
        {
            _verticalPoints.Add(tilemapRect.Position + new Vector2(x * tilemapCellSize.X, 0));
            _verticalPoints.Add(tilemapRect.Position + new Vector2(x * tilemapCellSize.X, tilemapRect.Size.Y * tilemapCellSize.Y));
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawMultiline([.. _horizontalPoints], _gridColor);
        DrawMultiline([.. _verticalPoints], _gridColor);
    }
}

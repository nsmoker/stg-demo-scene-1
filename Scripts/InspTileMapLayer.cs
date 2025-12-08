using Godot;
using System;
using System.Collections.Generic;

public partial class InspTileMapLayer : Node2D
{
	List<Vector2> HorizontalPoints = [];
	List<Vector2> VerticalPoints = [];

	[Export]
	Color GridColor;

	[Export]
	TileMapLayer TileMapLayer;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
        var tilemap_rect = TileMapLayer.GetUsedRect();
		var tilemap_cell_size = TileMapLayer.TileSet.TileSize;

		for (int y = 0; y < tilemap_rect.Size.Y; ++y)
		{
			HorizontalPoints.Add(tilemap_rect.Position + new Vector2(0, y * tilemap_cell_size.Y));
			HorizontalPoints.Add(tilemap_rect.Position + new Vector2(tilemap_rect.Size.X * tilemap_cell_size.X, y * tilemap_cell_size.Y));
		}
		
		for (int x = 0; x < tilemap_rect.Size.X; ++x)
		{
			VerticalPoints.Add(tilemap_rect.Position + new Vector2(x * tilemap_cell_size.X, 0));
			VerticalPoints.Add(tilemap_rect.Position + new Vector2(x * tilemap_cell_size.X, tilemap_rect.Size.Y * tilemap_cell_size.Y));
		}

		QueueRedraw();
    }

	public override void _Draw()
    {
        DrawMultiline([.. HorizontalPoints], GridColor);
		DrawMultiline([.. VerticalPoints], GridColor);
    }
}

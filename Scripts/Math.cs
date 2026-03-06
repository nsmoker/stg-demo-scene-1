using Godot;
using System.Collections.Generic;
using System.Linq;

namespace STGDemoScene1.Scripts;

public static class Math
{
    public static Vector2[] TrimPath(Vector2 start, Vector2[] path, float maxLength)
    {
        Vector2 loc = start;
        float remainingLength = maxLength;
        List<Vector2> trimmedPath = [];
        foreach (Vector2 p in path)
        {
            if (!(remainingLength > 0))
            {
                continue;
            }

            float length = Mathf.Min((p - loc).Length(), remainingLength);
            remainingLength -= length;
            Vector2 targetVector = p - loc;
            Vector2 newLoc = loc + targetVector.Normalized() * length;
            trimmedPath.Add(newLoc);
            loc = newLoc;
        }

        return [.. trimmedPath];
    }

    public static float ComputePathLength(Vector2[] path, Vector2 origin)
    {
        Vector2 start = origin;
        float len = 0;
        foreach (var vertex in path)
        {
            len += vertex.DistanceTo(start);
            start = vertex;
        }

        return len;
    }

    public static Vector2 GetCardinalQuantization(Vector2 fromDirection)
    {
        var toDirection = fromDirection.Normalized();
        List<Vector2> cardinals = [Vector2.Up, Vector2.Down, Vector2.Right, Vector2.Left];
        // Use the cover level of the cardinal direction with the minimum angular distance to the attacker's target vector.
        return cardinals.MinBy(cardinal => Mathf.Abs(toDirection.AngleTo(cardinal)));
    }
}

using Godot;
using System.Collections.Generic;

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
}

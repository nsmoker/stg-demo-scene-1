using Godot;

[Tool]
[GlobalClass]
public partial class FlowField : Resource
{
    [Export]
    public Godot.Collections.Array<FlowFieldControlPoint> ControlPoints;

    public static float CalculateAttenuation(Vector2 controlPoint, Vector2 samplePoint)
    {
        return 1.0f / controlPoint.DistanceSquaredTo(samplePoint);
    }

    public Vector2 SampleFlowField(Vector2 samplePoint)
    {
        Vector2 gradientRet = Vector2.Zero;

        foreach (FlowFieldControlPoint controlPoint in ControlPoints)
        {
            float attenuation = CalculateAttenuation(controlPoint.ControlPoint, samplePoint);
            gradientRet += controlPoint.Gradient * attenuation;
        }

        return gradientRet;
    }
}
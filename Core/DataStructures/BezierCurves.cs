using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace TheExtraordinaryAdditions.Core.DataStructures;

public class BezierCurves(params Vector2[] controls)
{
    public Vector2[] ControlPoints = controls;

    public Vector2 Evaluate(float interpolant)
    {
        return PrivateEvaluate(ControlPoints, MathHelper.Clamp(interpolant, 0f, 1f));
    }

    public List<Vector2> GetPoints(int amount)
    {
        if (amount < 2)
            amount = 2;

        float perStep = 1f / (amount - 1);

        var points = new List<Vector2>();

        for (int i = 0; i < amount; i++)
        {
            points.Add(Evaluate(perStep * i));
        }

        return points;
    }

    private static Vector2 PrivateEvaluate(Vector2[] points, float T)
    {
        while (points.Length > 2)
        {
            Vector2[] nextPoints = new Vector2[points.Length - 1];
            for (int i = 0; i < points.Length - 1; i++)
            {
                nextPoints[i] = Vector2.Lerp(points[i], points[i + 1], T);
            }
            points = nextPoints;
        }
        if (points.Length <= 1)
        {
            return Vector2.Zero;
        }
        return Vector2.Lerp(points[0], points[1], T);
    }

    public Vector2 this[int x]
    {
        get => ControlPoints[x];
        set => ControlPoints[x] = value;
    }
}
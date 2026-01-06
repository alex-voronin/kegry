namespace SharedCore.Math;

using System;

public static class MathFunctions
{
    public static double Gaussian(double value, double origin, double standardDeviation)
    {
        if (standardDeviation <= 0)
        {
            throw new ArgumentException("Scale must be greater than zero.");
        }

        var distanceSquared = Math.Pow(value - origin, 2);
        var denominator = 2 * Math.Pow(standardDeviation, 2);

        return Math.Exp(-distanceSquared / denominator);
    }
}
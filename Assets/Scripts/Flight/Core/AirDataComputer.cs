using UnityEngine;

public struct AirData
{
    public float altitudeMach;
    public float qBar;
}

public class AirDataComputer
{
    /// <summary>
    /// Density in slugs/ft^3
    /// </summary>
    public const float SeaLevelDensity = 2.377e-3f;
    public const float MaxAltitude = 35000.0f;

    /// <summary>
    /// Calculates air data based on velocity and altitude.
    /// </summary>
    /// <param name="velocity">Velocity in ft/s</param>
    /// <param name="altitude">Altitude in ft</param>
    /// <returns>Air data</returns>
    public AirData CalculateAirData(float velocity, float altitude)
    {
        const float baseTemperature = 519.0f; // Rankine
        const float minTemperature = 390.0f; // Rankine
        const float temperatureGradient = 0.703e-5f; // Rankine/ft

        const float gamma = 1.4f; // Ratio of specific heats
        const float gasConstant = 1716.3f; // ft*lbf/(slug*R)
        const float densityPower = 4.14f; // Exponent for density calculation

        altitude = Mathf.Clamp(altitude, 0, MaxAltitude);

        float temperatureFactor = 1.0f - (temperatureGradient * altitude);
        float T = Mathf.Max(minTemperature, baseTemperature * temperatureFactor);

        float speedOfSound = Mathf.Sqrt(gamma * gasConstant * T);
        float altitudeMach = velocity / speedOfSound;

        float rho = SeaLevelDensity * Mathf.Pow(temperatureFactor, densityPower);
        float qBar = 0.5f * rho * velocity * velocity;

        return new AirData()
        {
            altitudeMach = altitudeMach,
            qBar = qBar
        };
    }
}

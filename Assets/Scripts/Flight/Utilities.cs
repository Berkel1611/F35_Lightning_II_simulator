using UnityEngine;

public static class Utilities
{
    public static Vector3 ConvertVectorToAerospace(Vector3 vector)
    {
        return new Vector3(vector.z, vector.x, -vector.y);
    }
    public static Vector3 ConvertVectorToUnity(Vector3 vector)
    {
        return new Vector3(vector.y, -vector.z, vector.x);
    }
    public static Vector3 ConvertAngleToAerospace(Vector3 angle)
    {
        return -ConvertVectorToAerospace(angle);
    }
    public static Vector3 ConvertAngleToUnity(Vector3 angle)
    {
        return -ConvertVectorToUnity(angle);
    }

    public static float CalculateAlpha(Vector3 localVelocity)
    {
        return Mathf.Atan2(-localVelocity.y, localVelocity.z);
    }

    public static float CalculateBeta(Vector3 localVelocity)
    {
        return Mathf.Atan2(localVelocity.x, localVelocity.z);
    }

    public static float MoveTo(float value, float target, float speed, float deltaTime, float min = 0, float max = 1)
    {
        float diff = target - value;
        float delta = Mathf.Clamp(diff, -speed * deltaTime, speed * deltaTime);
        return Mathf.Clamp(value + delta, min, max);
    }

    public static float ConvertAngle360To180(float angle)
    {
        // convert 0 - 360 range to -180 - 180
        if (angle > 180)
        {
            angle -= 360f;
        }

        return angle;
    }
}

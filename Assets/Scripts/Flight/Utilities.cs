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

    public static float TransformAngle(float angle, float fov, float pixelHeight)
    {
        return (Mathf.Tan(angle * Mathf.Deg2Rad) / Mathf.Tan(fov / 2 * Mathf.Deg2Rad)) * pixelHeight / 2;
    }

    public static Vector3 FirstOrderIntercept(
        Vector3 shooterPosition,
        Vector3 shooterVelocity,
        float shotSpeed,
        Vector3 targetPosition,
        Vector3 targetVelocity
    )
    {
        Vector3 targetRelativePosition = targetPosition - shooterPosition;
        Vector3 targetRelativeVelocity = targetVelocity - shooterVelocity;
        float t = FirstOrderInterceptTime(
            shotSpeed,
            targetRelativePosition,
            targetRelativeVelocity
        );
        return targetPosition + t * (targetRelativeVelocity);
    }

    public static float FirstOrderInterceptTime(
        float shotSpeed,
        Vector3 targetRelativePosition,
        Vector3 targetRelativeVelocity
    )
    {
        float velocitySquared = targetRelativeVelocity.sqrMagnitude;
        if (velocitySquared < 0.001f)
        {
            return 0f;
        }

        float a = velocitySquared - shotSpeed * shotSpeed;

        //handle similar velocities
        if (Mathf.Abs(a) < 0.001f)
        {
            float t = -targetRelativePosition.sqrMagnitude / (2f * Vector3.Dot(targetRelativeVelocity, targetRelativePosition));
            return Mathf.Max(t, 0f); //don't shoot back in time
        }

        float b = 2f * Vector3.Dot(targetRelativeVelocity, targetRelativePosition);
        float c = targetRelativePosition.sqrMagnitude;
        float determinant = b * b - 4f * a * c;

        if (determinant > 0f)
        { //determinant > 0; two intercept paths (most common)
            float t1 = (-b + Mathf.Sqrt(determinant)) / (2f * a),
                    t2 = (-b - Mathf.Sqrt(determinant)) / (2f * a);
            if (t1 > 0f)
            {
                if (t2 > 0f)
                    return Mathf.Min(t1, t2); //both are positive
                else
                {
                    return t1; //only t1 is positive
                }
            }
            else
            {
                return Mathf.Max(t2, 0f); //don't shoot back in time
            }
        }
        else if (determinant < 0f)
        { //determinant < 0; no intercept path
            return 0f;
        }
        else
        { //determinant = 0; one intercept path, pretty much never happens
            return Mathf.Max(-b / (2f * a), 0f); //don't shoot back in time
        }
    }
}

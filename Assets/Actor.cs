using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Windows.WebCam;

public class Actor : MonoBehaviour
{
    public Transform target;
    public Vector3 velocity;
    public float maxSpeed = 5f;
    public float maxAcc = 1f;

    void Start()
    {
        Application.targetFrameRate = 60;
    }

    // find the roots of a quadratic equation
    // ax² + bx + c = 0
    static int FindRoots(float a, float b, float c, out float x1, out float x2)
    {
        int roots = 0;
        float square = b * b - 4f * a * c;
        if (a != 0f && square >= 0f)
        {
            float sqrt = Mathf.Sqrt(square);
            x1 = (-b + sqrt) / (2f * a);
            x2 = (-b - sqrt) / (2f * a);
            roots = 2;
        }
        else
        {
            // imaginary roots or a is zero
            x1 = x2 = 0f;
        }
        return roots;
    }

    // Find the acceleration (or deceleration) required to reach the
    // maximum brake curve defined by:
    //  v = √(-2as)
    // where:
    //  v - velocity
    //  a - acceleration
    //  s - distance to target

    // In this function:
    //  s - distance to target
    //  u - current velocity
    //  t - time step
    //  a - maximum brake (deceleration) - should be negative
    static float FindAcceleration(float s, float u, float a, float t)
    {
        float a1, a2;
        float t2 = t * t;
        float ut = u * t;
        FindRoots(t2, 2f * ut - a * t2, u * u + 2f * a * (s - ut), out a1, out a2);
        return a1;
    }

    // Can we kill the velocity in time?
    //  u - current velocity
    //  t - time step
    //  a - maximum acceleration
    static bool CanStop(float u, float a, float t)
    {
        return Mathf.Abs(u) < Mathf.Abs(a * t);
    }

    // In this function:
    //  s - distance to target
    //  u - current velocity
    //  a - maximum brake (deceleration) - should be negative
    //  t - time if possible
    static bool CanReachTarget(float s, float u, float a, float t, out float ttt)
    {
        bool canReach = false;
        int roots = FindRoots(0.5f * a, u, -s, out float t1, out float t2);
        ttt = t1;
        if (roots > 0)
        {
            if (t1 >= 0f && t1 <= t)
            {
                canReach = true;
            }
        }
        return canReach;
    }

    // Split a vector into directional and orthogonal components
    // v - vector to split
    // t - target direction vector
    // o - output orthogonal vector
    // returns length of directional component
    static float SplitVector(Vector3 v, Vector3 t, out Vector3 o)
    {
        float d = Vector3.Dot(v, t);
        o = v - t * d;
        return d;
    }

    // Convert a vector to a unit vector and a length
    // v - vector
    // direction - output unit vector
    // returns length of vector
    static float VectorLength(Vector3 v, out Vector3 direction)
    {
        float speed = v.magnitude;
        direction = v.normalized;
        return speed;
    }

    float ReduceVelocity(Vector3 velocity, float deltaTime, float maxA, out Vector3 acceleration)
    {
        float u = VectorLength(velocity, out Vector3 direction);
        float a = Mathf.Max(-u / deltaTime, -maxA);
        acceleration = direction * a;
        return Mathf.Sqrt(maxA * maxA - a * a);
    }

    void UpdatePosition(ref Vector3 p, ref Vector3 u, Vector3 a, float t, float maxSpeed)
    {
        Vector3 v = u + a * t;
        if (v.magnitude > maxSpeed)
        {
            v = v.normalized * maxSpeed;
        }
        p += 0.5f * (u + v) * t;
        u = v;
    }

    void Update()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime > 0f)
        {
            float remainingTime = deltaTime;
            Vector3 toTarget = target.position - transform.position;
            Vector3 position = transform.position;
            if (toTarget != Vector3.zero)
            {
                float distance = VectorLength(toTarget, out Vector3 direction);
                float closingSpeed = SplitVector(velocity, direction, out Vector3 orthogonal);
                float remainingAcceleration = ReduceVelocity(orthogonal, deltaTime, maxAcc, out Vector3 orthogonalAcceleration);
                float a;
                if (CanReachTarget(distance, closingSpeed, -remainingAcceleration, deltaTime, out float ttt))
                {
                    remainingTime = deltaTime - ttt;
                    deltaTime = ttt;
                    a = -remainingAcceleration;
                }
                else
                {
                    a = FindAcceleration(distance, closingSpeed, -remainingAcceleration, deltaTime);
                    a = Mathf.Clamp(a, -remainingAcceleration, remainingAcceleration);
                    remainingTime = 0f;
                }
                Vector3 acceleration = direction * a + orthogonalAcceleration;
                Vector3 newVelocity = velocity + acceleration * deltaTime;
                if (newVelocity.magnitude > maxSpeed)
                {
                    newVelocity = newVelocity.normalized * maxSpeed;
                }
                Vector3 displacement = 0.5f * (velocity + newVelocity) * deltaTime;
                position += displacement;
                velocity = newVelocity;
            }

            if (remainingTime > 0f && toTarget == Vector3.zero)
            {
                deltaTime = remainingTime;
                float u = velocity.magnitude;
                if (u > 0f)
                {
                    float timeToStop = u / maxAcc;
                    Vector3 newVelocity = velocity + velocity.normalized * -maxAcc * deltaTime;
                    if (timeToStop < deltaTime)
                    {
                        newVelocity = Vector3.zero;
                        deltaTime = timeToStop;
                    }
                    position += 0.5f * (velocity + newVelocity) * deltaTime;
                    velocity = newVelocity;
                }
            }
            transform.position = position;
        }
    }
}

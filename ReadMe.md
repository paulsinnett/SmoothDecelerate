# Smooth following

The task here is a trickier problem than it first looks. You can't just accelerate in the direction of the target or you'll just end up orbiting it.

Instead, you need to find the velocity that you need to be at for any given position relative to your target, and then accelerate to get your velocity to match that. (I'm assuming your `acceleration` variable is the maximum acceleration allowed.)

For any point relative to the target, the critical velocity value can be thought of such that if you were applying maximum acceleration in the opposite direction you would come to a halt at the target point. Imagine slamming on the brakes as hard as you can.

To calculate that, we need to employ the SUVAT equations:

- $s$ - displacement
- $u$ - current velocity
- $v$ - new velocity
- $a$ - acceleration
- $t$ - time step

In particular, we want one that gives us the current velocity $u$ given the acceleration $a$ and the displacement $s$.

$$ v^2 = u^2 + as $$

Which we can re-arrange as:

$$ u = \sqrt{v^2 - 2as} $$

And since our desired target velocity at the destination is $0$, we can remove the $v$ term:

$$ u = \sqrt{-2as} $$

(Don't worry too much about the negative here as it will cancel out in the case we are interested in where a $a$ is negative, that is, a deceleration.)

That will tell us the velocity we want to be at, given our distance from the target. However, to get to that velocity we need to accelerate toward it, and by the time we reach the desired velocity, the position will have changed and the target velocity will be different. This means that over time we will be slightly in front of the target velocity at any distance. And because we require maximum deceleration to stop on the target we will always somewhat overshoot.

The acceleration we want is an amount needed to take us to a position and velocity after delta time where the critical velocity at new position matches the new velocity. Given a velocity, an acceleration, and time, we can calculate that position by removing:

$$ ut + \frac{1}{2}at^2 $$

from the displacement.

If we plug that into desired velocity function we get:

$$  v = \sqrt{-2a_{max}(s - ut - \frac{1}{2}at^2)} $$

and:

$$ v = u + at $$

So we want a value for $a$ such that:

$$ u + at = \sqrt{-2a_{max}(s - ut - \frac{1}{2}at^2)} $$

That looks pretty gnarly, but we can square both sides to get rid of the square root:

$$ u^2 + 2uat + a^2t^2 = -2a_{max}(s - ut - \frac{1}{2}at^2) $$

And then we can rearrange to get everything on the left:

$$ u^2 + 2uat + a^2t^2 + 2a_{max}(s - ut - \frac{1}{2}at^2) = 0 $$

Extract and collect the $a^2$ and $a$ terms:

$$ u^2 + 2uat + a^2t^2 + 2a_{max}(s - ut) - a_{max}at^2 = 0 $$

$$ (t^2)a^2 + (2ut - a_{max}t^2)a + (u^2 + 2a_{max}(s - ut)) = 0 $$

Now we can find the solutions for $a$ using the quadratic formula. When we have an equation that looks like:

$$ ax^2 + bx + c = 0 $$

We can solve, for $x$ with:

$$ x = \frac{-b \pm \sqrt{b^2 - 4ac}}{2a} $$

Note, don't get confused by the $a$, $b$, and $c$ here. They are the standard labels for the coefficients of the quadratic equation. We can avoid that confusion in code by separating out this into its own function:

```csharp
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
```

I'm returning the number of roots found. We can have up to two roots and they can be real or imaginary. I'm going to ignore imaginary roots here as we don't need them. And I'm ignoring the case where $a$ is zero because we should never have that case.

Now we can use this to find the acceleration we need to be at to reach the target velocity at the target position. We just pass the coefficients of $a^2$, $a$, and the constant term into the function and it will return the two possible values for $a$.

We do need to choose between the two solutions. In our case, the first coefficient is $t^2$ which should always be positive and greater than zero, and so the first root will always be the bigger (more positive) of the two values. And that's the one we want because as we are approaching the curve we want to be accelerating as fast as we can, and when we are on the curve we want to be decelerating as little as we can get away with. For the most part, it won't matter because the values will be greater than our maximum acceleration, but at the transition point, it will make a difference.

```csharp
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
    //  a - maximum brake (deceleration) - should be negative
    //  t - time step
    static float FindAcceleration(float s, float u, float a, float t)
    {
        float a1, a2;
        float t2 = t * t;
        float ut = u * t;
        FindRoots(t2, 2f * ut - a * t2, u * u + 2f * a * (s - ut), out a1, out a2);
        return a1;
    }
```

With that we should be able to move towards a stationary target from a standing start. However, we still need to handle the case where we reach the target. Our curve finding function assumes we will be moving for $t$ time, however we could reach the target in less than that if we are close. If that happens, FindAcceleration will return an incorrect result and we may over or under shoot the target.

To detect if we can reach the target in the given time, we just need to calculate the time we expect to arrive at the target and see if that is less.

We can use the SUVAT expression:

$$ s = ut + \frac{1}{2}at^2 = 0 $$

Gather the terms into the quadratic form as before:

$$ (\frac{1}{2}a)t^2 + (u)t + (-s) = 0 $$

And this time solve for $t$. If the value returned is between $0$ and our time step, then we can reach the target. This may leave residual velocity, but since we won't have a target vector we'll have to leave that for the next frame. We can, however, reduce the residual velocity using our remaining time and that should lower the value until it hits zero.

So that handles the straight line case. But in practice we will want to be accelerating in two or three dimensions. However, we can split our velocity vectors into direct and orthogonal vectors and handle them separately. The direct vector we can use with the above functions. For the orthogonal vector we can reduce the velocity to zero using some proportion of our available acceleration.

To split a vector into orthogonal and directional components, we can use the dot product of the vector with a normalized vector to the target. The dot product is defined as:

$$ \vec{a} \cdot \vec{b} = |\vec{a}||\vec{b}|cos(\theta) $$

So if we find that value and multiply it by our target direction vector, we have the component of the vector in the direction of the target.

Then we can simply subtract the direct component from the vector to get the orthogonal component.

```csharp
    // Split a vector into directional and orthogonal components
    // v - vector to split
    // t - target direction vector
    // d - output length of directional component
    // o - output orthogonal vector
    static void SplitVector(Vector3 v, Vector3 t, out float d, out Vector3 o)
    {
        d = Vector3.Dot(v, t);
        o = v - t * d;
    }
```

Now we need to consider what proportion of the available acceleration to give to the orthogonal component. If we give it all, we will stop all orthogonal movement before beginning to make progress towards the target. If we give it none, we will orbit the target and likely never reach it. We need to find a balance.



With $ v = 0 $, we can rearrange to find $a$.

$$ a = -\frac{u}{t} $$

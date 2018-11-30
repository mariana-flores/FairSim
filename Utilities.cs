/// <summary>
/// Common math operations here
/// </summary>
public class Utilities
{
    /// <summary>
    /// Converts revolution speed (seconds per revolution) to angular speed (radians per second)
    /// </summary>
    /// <param name="secondsPerRevolution">How long it takes 1 revolution in seconds</param>
    /// <returns>How many radians it moves per second</returns>
    public static double RevolutionsToAngularSpeed(double secondsPerRevolution)
    {
        return 2 * Math.PI / secondsPerRevolution;
    }

    /// <summary>
    /// Calculates the arc length distance in respect to the angle
    /// </summary>
    /// <param name="radius">radius </param>
    /// <param name="radians">angle in radians </param>
    /// <returns></returns>
    public static double FindArcLength(double radius, double radians)
    {
        return radius * radians;
    }

    /// <summary>
    /// find hypotenuse by pythagorean theorem.
    /// </summary>
    /// <param name="side1"></param>
    /// <param name="side2"></param>
    /// <returns></returns>
    public static double Pythagorean(double side1, double side2)
    {
        double sum = side1 * side1 + side2 * side2;
        return Math.Sqrt(sum);
    }
    ...
}

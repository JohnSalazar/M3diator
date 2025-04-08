using System.Diagnostics.CodeAnalysis;

namespace M3diator;

/// <summary>
/// Represents a void type, since <see cref="void"/> is not a valid return type in C#.
/// </summary>
[SuppressMessage("Design", "CA1036:Override methods on comparable types", Justification = "Comparable for backwards compatibility")]
[SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "Unit does not benefit from named alternates")]
public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>, IComparable
{
    /// <summary>
    /// Gets the single value of the <see cref="Unit"/> type.
    /// </summary>
    public static readonly Unit Value = default;

    /// <summary>
    /// Gets a completed task that returns the <see cref="Unit"/> value.
    /// </summary>
    public static Task<Unit> Task { get; } = System.Threading.Tasks.Task.FromResult(Value);

    /// <summary>
    /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
    /// </summary>
    /// <param name="other">An object to compare with this instance.</param>
    /// <returns>A value that indicates the relative order of the objects being compared.</returns>
    public int CompareTo(Unit other) => 0;

    int IComparable.CompareTo(object? obj) => 0;

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode() => 0;

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.</returns>
    public bool Equals(Unit other) => true;

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="obj">An object to compare with this object.</param>
    /// <returns>true if the current object is equal to the <paramref name="obj"/> parameter; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is Unit;

    /// <summary>
    /// Determines whether two specified instances of <see cref="Unit"/> are equal.
    /// </summary>
    /// <param name="first">The first <see cref="Unit"/> to compare.</param>
    /// <param name="second">The second <see cref="Unit"/> to compare.</param>
    /// <returns>true if both <see cref="Unit"/> instances are equal; otherwise, false.</returns>
    public static bool operator ==(Unit first, Unit second) => true;

    /// <summary>
    /// Determines whether two specified instances of <see cref="Unit"/> are not equal.
    /// </summary>
    /// <param name="first">The first <see cref="Unit"/> to compare.</param>
    /// <param name="second">The second <see cref="Unit"/> to compare.</param>
    /// <returns>true if both <see cref="Unit"/> instances are not equal; otherwise, false.</returns>
    public static bool operator !=(Unit first, Unit second) => false;

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => "()";
}

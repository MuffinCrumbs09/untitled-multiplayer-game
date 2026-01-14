/// <summary>
/// Defines a contract for any game object that can have its health reduced.
/// This allows damage-dealing systems to affect any object that implements
/// this interface, regardless of its specific type.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Applies damage to the object.
    /// </summary>
    /// <param name="amount">The amount of damage to apply.</param>
    void TakeDamage(float amount);
}
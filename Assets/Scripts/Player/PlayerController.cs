using UnityEngine;

/// <summary>
/// The main coordinating component for the player character.
/// This class holds references to other player-related components and acts
/// as a central point for player state and high-level logic.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerMovement), typeof(PlayerLook))]
public class PlayerController : MonoBehaviour
{
    // This class will be expanded later to manage player state, health,
    // and interactions between other components. For now, its primary
    // role is to ensure the necessary components are attached to the
    // player GameObject via the [RequireComponent] attribute.
}
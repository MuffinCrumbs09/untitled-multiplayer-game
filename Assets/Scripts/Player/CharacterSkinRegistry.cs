using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Configuration asset holding the list of available character meshes.
/// </summary>
[CreateAssetMenu(fileName = "CharacterSkinRegistry",
    menuName = "Game/Character Skin Registry")]
public class CharacterSkinRegistry : ScriptableObject
{
    [Tooltip("List of meshes available for character customization.")]
    public List<Mesh> skins = new List<Mesh>();
}
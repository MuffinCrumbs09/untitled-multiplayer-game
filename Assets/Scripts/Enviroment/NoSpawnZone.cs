using UnityEngine;

public class NoSpawnZone : MonoBehaviour
{
    [SerializeField] private Vector3 zoneSize = new Vector3(20f, 10f, 20f);
    
    public bool IsPositionInside(Vector3 position)
    {
        Bounds bounds = new Bounds(transform.position, zoneSize);
        return bounds.Contains(position);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawCube(transform.position, zoneSize);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, zoneSize);
    }
}

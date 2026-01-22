using UnityEngine;

public class ShootLaser : MonoBehaviour
{
    [Header("Laser Settings")]
    public Material laserMaterial;
    private LaserBeam laserBeam;

    void Start()
    {
        laserBeam = new LaserBeam(gameObject.transform.position, gameObject.transform.forward, laserMaterial);
    }

    private void Update()
    {
        laserBeam.laser.positionCount = 0;
        laserBeam.laserPoints.Clear();
        laserBeam.CastRay(transform.position, transform.forward, laserBeam.laser);

        if(GameLoopManager.Instance.PuzzleManager.StatuesDestroyed.Value)
        {
            Destroy(laserBeam.laser);
            Destroy(this);
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

public class LaserBeam
{
    private Vector3 pos, dir;

    private GameObject laserObj;
    public LineRenderer laser;
    public List<Vector3> laserPoints = new();

    public LaserBeam(Vector3 position, Vector3 direction, Material material)
    {
        laser = new LineRenderer();
        laserObj = new GameObject("LaserBeam");
        pos = position;
        dir = direction;

        laser = laserObj.AddComponent<LineRenderer>();
        laser.startWidth = 0.1f;
        laser.endWidth = 0.1f;
        laser.material = material;
        laser.startColor = Color.green;
        laser.endColor = Color.green;

        CastRay(pos, dir, laser);
    }

    public void CastRay(Vector3 pos, Vector3 dir, LineRenderer laser)
    {
        laserPoints.Add(pos);

        Ray ray = new Ray(pos, dir);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 30f))
        {
            CheckHit(hit, dir, laser);
        }
        else
        {
            laserPoints.Add(ray.GetPoint(30f));
            UpdateLaser();
        }
    }

    private void UpdateLaser()
    {
        int count = 0;
        laser.positionCount = laserPoints.Count;

        foreach (Vector3 point in laserPoints)
        {
            laser.SetPosition(count, point);
            count++;
        }
    }

    private void CheckHit(RaycastHit hit, Vector3 dir, LineRenderer laser)
    {
        // Placeholder for future interactions
        // if(hit.collider.CompareTag("Mirror"))
        // {
        //     Vector3 reflectDir = Vector3.Reflect(dir, hit.normal);

        //     CastRay(hit.point, reflectDir, laser);
        // }
        // else
        // {
        //     laserPoints.Add(hit.point);
        //     UpdateLaser();
        // }

        if(hit.collider.TryGetComponent(out Statue statue))
        {
            statue.ToggleBeam(true);
        }

        laserPoints.Add(hit.point);
        UpdateLaser();
    }
}

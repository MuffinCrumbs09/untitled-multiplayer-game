using UnityEngine;
using Unity.Netcode;

public class Statue : MonoBehaviour
{
    public int statueIndex;
    public float beamDurationRequired = 2f;

    private float beamTimer = 0f;
    private bool isBeingBeamed = false;

    public void DestroyStatue()
    {
        Destroy(gameObject);
    }

    private void Update()
    {
        if (isBeingBeamed && NetworkManager.Singleton.IsServer)
        {
            beamTimer += Time.deltaTime;

            if (beamTimer >= beamDurationRequired)
            {
                DestroyStatue();
                GameLoopManager.Instance.PuzzleManager.DestroyStatueServerRpc(statueIndex);
                isBeingBeamed = false;
                beamTimer = 0f;
            }
        }
    }

    public void ToggleBeam(bool status)
    {
        isBeingBeamed = status;
    }
}
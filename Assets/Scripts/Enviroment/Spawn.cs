using TMPro;
using UnityEngine;

public class Spawn : MonoBehaviour
{
    public Alter[] alters;
    public int timeToSurvive = 60;
    public TMP_Text timer;

    private float time;
    private bool standoffStarted = false;

    private void Update()
    {
        if (!standoffStarted)
        {
            if (ReadyToStart())
            {
                standoffStarted = true;
                timer.gameObject.SetActive(true);
                time = timeToSurvive;
            }
        }
        else
        {
            time -= Time.deltaTime;
            timer.text = string.Format("{0}", (int)time);

            if (time <= 0f)
            {
                GameLoopManager.Instance.EndGame("Survivors Win!");
                standoffStarted = false;
            }
        }
    }

    private bool ReadyToStart()
    {
        foreach (Alter alter in alters)
        {
            if (!alter.Activated.Value)
                return false;
        }
        return true;
    }
}
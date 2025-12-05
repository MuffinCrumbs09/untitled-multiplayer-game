using UnityEngine;


public enum CurrentCanvas
{
    MainMenu,
    InLobby
}
public class CanvasManager : MonoBehaviour
{
    // Instance
    public static CanvasManager Instance;

    [SerializeField] private CurrentCanvas Current = CurrentCanvas.MainMenu;
    [SerializeField] private Canvas[] Canvases;

    private void Awake()
    {
        if(Instance != null && Instance != this)
            Destroy(this);
        
        Instance = this;
    }

    /// <summary>
    /// Pick between the canvases
    /// </summary>
    /// <param name="canvas"></param>
    public void PickCanvas(CurrentCanvas canvas)
    {
        if(canvas == Current) return;
        Canvases[(int)Current].enabled = false; // Disable Old
        Canvases[(int)canvas].enabled = true; // Enable New
        canvas = Current;
    }
}
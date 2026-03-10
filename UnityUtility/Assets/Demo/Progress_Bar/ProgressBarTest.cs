using UIComponent;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBarTest : MonoBehaviour
{
    public ProgressBar progressBar;
    public int amount = 1;

    void Start()
    {
        progressBar.MaxValue = 100;
        progressBar.CurrentValue = 0;
    }
    
    void Update()
    {
        progressBar.CurrentValue += Time.deltaTime * amount;
        if (progressBar.CurrentValue >= progressBar.MaxValue)
        {
            progressBar.CurrentValue = 0;
        }
    }
}

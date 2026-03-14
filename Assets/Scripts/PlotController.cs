using UnityEngine;

public class PlotController : MonoBehaviour
{
    public enum PlotState { Empty, Planted, Ready }
    public PlotState state = PlotState.Empty;

    public float growTime = 5f; // seconds to grow (set longer in Inspector later)
    private float timer = 0f;

    public GameObject seedVisual;   // small sphere you'll assign
    public GameObject cropVisual;   // bigger sphere you'll assign

    void Start()
    {
        if (seedVisual) seedVisual.SetActive(false);
        if (cropVisual) cropVisual.SetActive(false);
    }

    void Update()
    {
        if (state == PlotState.Planted)
        {
            timer += Time.deltaTime;
            if (timer >= growTime)
            {
                state = PlotState.Ready;
                if (seedVisual) seedVisual.SetActive(false);
                if (cropVisual) cropVisual.SetActive(true);
                Debug.Log(gameObject.name + " is ready to harvest!");
            }
        }
    }

    void OnMouseDown()
    {
        if (state == PlotState.Empty)
        {
            if (GameManager.Instance.BuySeed())
            {
                state = PlotState.Planted;
                timer = 0f;
                if (seedVisual) seedVisual.SetActive(true);
                Debug.Log("Planted a seed on " + gameObject.name);
            }
        }
        else if (state == PlotState.Ready)
        {
            Harvest();
        }
        else
        {
            Debug.Log("Growing... wait " + (growTime - timer).ToString("F1") + "s more");
        }
    }

    void Harvest()
    {
        state = PlotState.Empty;
        if (cropVisual) cropVisual.SetActive(false);
        GameManager.Instance.SellCrop();
        Debug.Log("Harvested and sold!");
    }
}
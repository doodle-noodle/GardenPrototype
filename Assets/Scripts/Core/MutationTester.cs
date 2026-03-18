using UnityEngine;

public class MutationTester : MonoBehaviour
{
    [Header("Drag mutation assets here to test")]
    public MutationData[] testMutations;

    [Header("Key to apply mutation")]
    public KeyCode applyKey = KeyCode.M;

    private int _mutationIndex = 0;

    void Update()
    {
        if (!Input.GetKeyDown(applyKey)) return;
        if (testMutations == null || testMutations.Length == 0) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f)) return;

        FarmPlot plot = hit.collider.GetComponent<FarmPlot>();
        if (plot == null)
        {
            TutorialConsole.Warn("No farm plot under cursor.");
            return;
        }

        if (plot.State == FarmPlot.PlotState.Empty)
        {
            TutorialConsole.Warn("Plant something first.");
            return;
        }

        MutationData mutation = testMutations[_mutationIndex % testMutations.Length];
        plot.ApplyMutation(mutation);
        _mutationIndex++;
    }
}
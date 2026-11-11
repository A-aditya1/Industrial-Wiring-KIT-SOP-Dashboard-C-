using WiringKitSOP.Models;

namespace WiringKitSOP.Services;

public sealed class SopManager
{
    public List<SopStep> Steps { get; }
    public int CurrentIndex { get; private set; }
    public int Attempts { get; set; } = 1;
    public HashSet<int> CompletedSteps { get; } = new();
    public bool IsFinished => CurrentIndex >= Steps.Count;
    public SopStep? Current => IsFinished ? null : Steps[CurrentIndex];
    public int CompletedCount => CompletedSteps.Count;
    public SopManager(List<SopStep> steps) => Steps = steps;
    public void MarkCurrentComplete()
    {
        if (IsFinished) return;
        CompletedSteps.Add(Current.Number);
        CurrentIndex++;
        Attempts = 1;
    }
    public void Reset() { CurrentIndex = 0; Attempts = 1; CompletedSteps.Clear(); }
    public void Retry() { Attempts++; }
}

using UnityEngine;
using UnityEngine.InputSystem;

public static class InputBudgetBoot
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RaiseInputBudget()
    {
        // 0 = v�gtelen; vagy �ll�ts be nagy keretet:
        InputSystem.settings.maxEventBytesPerUpdate = 0;
    }
}

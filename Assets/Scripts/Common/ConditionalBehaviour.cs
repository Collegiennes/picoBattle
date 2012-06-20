using System;
using UnityEngine;
using Object = UnityEngine.Object;

class ConditionalBehaviour : MonoBehaviour
{
    public float SinceAlive;

    public Action Action;
    public Condition Condition;

    public bool Volatile;

    void Update()
    {
        SinceAlive += Time.deltaTime;
        if (Condition(SinceAlive))
        {
            if (Action != null) Action();
            Destroy(gameObject);
            Action = null;
            Condition = null;
        }
    }

    public static void KillSwitch()
    {
        foreach (var go in FindObjectsOfType(typeof(ConditionalBehaviour)))
            if ((go as ConditionalBehaviour).Volatile)
                Destroy((go as ConditionalBehaviour).gameObject);
    }
}

public delegate bool Condition(float elapsedSeconds);

public static class Wait
{
    public static void Until(Condition condition, Action action)
    {
        var go = new GameObject("Waiter");
        var w = go.AddComponent<ConditionalBehaviour>();
        w.Condition = condition;
        w.Action = action;
    }
    public static void Until(Condition condition, Action action, bool @volatile)
    {
        var go = new GameObject("Waiter");
        var w = go.AddComponent<ConditionalBehaviour>();
        w.Condition = condition;
        w.Action = action;
        w.Volatile = @volatile;
    }
}

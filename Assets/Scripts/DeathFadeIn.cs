using UnityEngine;

class DeathFadeIn : MonoBehaviour
{
    float sinceStarted;

    void Update()
    {
        sinceStarted += Time.deltaTime;

        var step = Mathf.Clamp01(sinceStarted / 3);
        var easedStep = Easing.EaseInOut(Easing.EaseOut(step, EasingType.Quintic), EasingType.Quadratic);

        var opacity = easedStep;

        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            var c = r.material.color;
            r.material.color = new Color(c.r, c.g, c.b, opacity);
        }

        if (GameFlow.State != GameState.Lost)
            Destroy(gameObject);
    }
}

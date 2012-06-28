using UnityEngine;

public class GrowFade : MonoBehaviour 
{
    float sinceStarted;

    void Start()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.material.SetColor("_TintColor", new Color(1, 1, 1, 1));
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        sinceStarted += Time.deltaTime;

	    var step = Mathf.Clamp01(sinceStarted / 4);
	    var easedStep = Easing.EaseInOut(Easing.EaseOut(step, EasingType.Quintic), EasingType.Quadratic);

        transform.localScale = Vector3.Lerp(Vector3.zero, new Vector3(3, 3, 3), easedStep);

        var opacity = 1 - step;

        foreach (var r in GetComponentsInChildren<Renderer>())
            r.material.SetColor("_TintColor", new Color(opacity, opacity, opacity, 1));

        if (GameFlow.State != GameState.Won)
            Destroy(gameObject);
    }
}

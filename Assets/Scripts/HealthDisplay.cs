using UnityEngine;

class HealthDisplay : MonoBehaviour
{
    const float Distance = 160;
    const float Segments = 64;

    public bool IsDeath;

    float currentHealth, currentSat, currentHue, currentDistance, currentOpacity;

    LineRenderer lr;

    void Start()
    {
        lr = GetComponent<LineRenderer>();
        lr.SetWidth(2.5f, 2.5f);

        currentHealth = 1;
        currentSat = 0;
        currentHue = 0;
        currentDistance = 46f;
    }

    void FixedUpdate()
    {
        if (ShieldGenerator.Instance == null) return;

        var powered = ShieldGenerator.Instance.IsPowered;
        var gameStarted = GameFlow.State >= GameState.Gameplay;

        var opacity = (powered ? 1 : 0.5f) * (IsDeath ? 0.1f : 0.625f);
        if (!gameStarted)
            opacity = 0;
        currentOpacity = Mathf.Lerp(currentOpacity, opacity, 0.1f);

        currentHue = Mathf.LerpAngle(currentHue, ShieldGenerator.Instance.Hue, 0.1f);
        currentSat = Mathf.Lerp(currentSat, powered ? 1 : 0, 0.1f);

        var color = ColorHelper.ColorFromHSV(currentHue, currentSat, currentOpacity) * 0.5f;

        lr.material.SetColor("_TintColor", color);

        var health = ShieldGenerator.Instance.Health / 500f;
        currentHealth = Mathf.Lerp(currentHealth, health, 0.1f);

        var distance = Distance;

        if (GameFlow.State < GameState.Gameplay)
        {
            currentHealth = 0;
            distance = 46f;
        }

        currentDistance = Mathf.Lerp(currentDistance, distance, 0.1f);

        var start = IsDeath ? currentHealth : 0;
        var end = IsDeath ? 1 : currentHealth;

        if (currentHealth == 1 && IsDeath) return;

        var diff = (end - start) * Mathf.PI * 2;
        start *= Mathf.PI * 2;

        for (int i = 0; i <= Segments; i++)
        {
            lr.SetPosition(i, new Vector3(Mathf.Cos(i / Segments * diff + start) * currentDistance, Mathf.Sin(i / Segments * diff + start) * currentDistance, 0));
        }
    }
}

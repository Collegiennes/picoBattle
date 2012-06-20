using UnityEngine;

class ShieldImpactEffect : MonoBehaviour
{
    float SinceAlive;

    public float Scale = 1;
    public float Hue;
    public float Velocity;
    public Vector3 Direction;
    public float MatchFactor;
    Vector3 initialPos;

    void Start()
    {
        initialPos = transform.position;

        // Calculate absorption
        float power;

        if (ShieldGenerator.Instance.IsPowered)
        {
            var shieldV = new Vector2(Mathf.Cos(Mathf.Deg2Rad * ShieldGenerator.Instance.Hue), Mathf.Sin(Mathf.Deg2Rad * ShieldGenerator.Instance.Hue)).normalized;
            var bulletV = new Vector2(Mathf.Cos(Mathf.Deg2Rad * Hue), Mathf.Sin(Mathf.Deg2Rad * Hue)).normalized;
            power = (Vector3.Dot(bulletV, shieldV) + 1) / 2;
            power = Mathf.Clamp01(power);
        }
        else
            power = 0;

        MatchFactor = (1 - power) * 0.75f + 0.25f;
        Velocity *= MatchFactor;

        AudioRouter.Instance.PlayImpact(Hue);
    }

    void Update()
    {
        var opacity = Mathf.Clamp01(1 - SinceAlive / (3f * MatchFactor));

        SinceAlive += Time.deltaTime;

        Velocity *= (1 - Time.deltaTime);
        Velocity = Mathf.Max(0, Velocity);
        Scale += Velocity;

        transform.localScale = new Vector3(Scale, Scale, Scale);

        // 45 shield radius
        // 15 scale max
        transform.position = initialPos + Direction * Easing.EaseInOut(Scale / 32, EasingType.Quadratic) * 45;

        var color = ColorHelper.ColorFromHSV(Hue, 1, 1);

        GetComponentInChildren<Renderer>().material.SetColor("_TintColor", new Color(color.r * opacity * 0.5f, color.g * opacity * 0.5f, color.b * opacity * 0.5f, 1));
        if (opacity <= 0)
            Destroy(gameObject);
    }
}

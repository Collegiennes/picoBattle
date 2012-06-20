using UnityEngine;

class TrailEffect : MonoBehaviour
{
    float SinceAlive;

    public Vector3 Direction;
    public float Scale = 1;

    void Start()
    {
        transform.localScale = new Vector3(0.5f, 0.5f, 0.5f) * Scale;
    }

    void Update()
    {
        var shieldColor = Color.Lerp(ColorHelper.ColorFromHSV(ShieldGenerator.Instance.Hue, 1, 1), new Color(1, 1, 1, 1), 0.125f);

        var opacity = Mathf.Clamp01(1 - SinceAlive / 2f);

        SinceAlive += Time.deltaTime;

        transform.position += Direction * 10 * Time.deltaTime * (1 - opacity);

        GetComponentInChildren<Renderer>().material.SetColor("_TintColor", new Color(shieldColor.r * opacity * 0.6f, shieldColor.g * opacity * 0.6f, shieldColor.b * opacity * 0.6f, 1));
        if (opacity <= 0)
            Destroy(gameObject);
    }
}

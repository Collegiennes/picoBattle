using UnityEngine;

class PlanetImpact : MonoBehaviour
{
    float sinceAlive;
    Vector3 originalPosition;
    public Vector3 Direction;
    public float Power;
    public float BulletHue;

    void Start()
    {
        //GetComponentInChildren<Renderer>().material.SetColor("_TintColor", new Color(0.25f, 0.25f, 0.25f, 1));
        GetComponentInChildren<Renderer>().material.SetColor("_TintColor", new Color(0, 0, 0, 0.5f));
        originalPosition = transform.position;
        transform.localScale = new Vector3(1, 1, 1) * Power;
        transform.position = originalPosition;

        gameObject.FindChild("Particle System").GetComponent<ParticleSystem>().renderer.material.SetColor("_TintColor", ColorHelper.ColorFromHSV(BulletHue, 1, 1));
    }

    void Update()
    {
        sinceAlive += Time.deltaTime;

        var step = Mathf.Clamp01(sinceAlive / 2f);
        var invStep = 1 - step;

        transform.localScale = new Vector3(1, 1 - 0.75f * Easing.EaseOut(step, EasingType.Quadratic), 1) * Power;
        //GetComponentInChildren<Renderer>().material.SetColor("_TintColor", new Color(0.25f * invStep, 0.25f * invStep, 0.25f * invStep, 1));
        GetComponentInChildren<Renderer>().material.SetColor("_TintColor", new Color(0, 0, 0, 0.5f * invStep));

        transform.position = originalPosition + Direction * 2 * Easing.EaseOut(step, EasingType.Quadratic);

        if (step >= 1)
            Destroy(gameObject);
    }
}

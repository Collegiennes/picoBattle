using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    Vector3 direction;
    float power;
    public float Speed = 1;
    public float CurrentScale;
    float sinceDestroyed;

    public bool IsAutoDestructed;
    public bool IsInitialized;
    public bool IsAbsorbed;

    public GameObject DestructEffect, ShieldImpactEffect, PlanetImpactEffect;
    TrailRenderer trailRenderer;

    float SinceEmitDestruct;
    float sinceStarted;

    void Start()
    {
        transform.position = Placement.Instance.GetCleanVector() * 400;
        direction = -transform.position.normalized;
        transform.LookAt(Vector3.zero);

        (trailRenderer = GetComponent<TrailRenderer>()).materials[0].SetColor("_TintColor", ColorHelper.ColorFromHSV(hue, 1, 1));

        IsInitialized = true;
    }

    void Update()
    {
        if (GameFlow.State != GameState.Gameplay) return;

        sinceStarted += Time.deltaTime;

        if (IsAutoDestructed)
        {
            sinceDestroyed += Time.deltaTime;

            var c = ColorHelper.ColorFromHSV(Hue, 1, 0.5f);
            gameObject.FindChild("Sphere").renderer.material.color = new Color(0, 0, 0, 1 - sinceDestroyed / 5f);

            if (sinceDestroyed > 5)
                Destroy(gameObject);
            return;
        }

        transform.position += direction * Time.deltaTime * 30 * Speed;
        CurrentScale = Mathf.Lerp(CurrentScale, Power * 1.5f, 0.25f * Time.deltaTime / (1 / 60f));
        transform.localScale = new Vector3(CurrentScale, CurrentScale, CurrentScale);

        trailRenderer.startWidth = trailRenderer.endWidth = Power;

        if (transform.position.magnitude < 45)
        {
            if (!IsAbsorbed)
            {
                for (int i = 0; i < 5; i++)
                {
                    var fx = Instantiate(ShieldImpactEffect, transform.position,
                                         Quaternion.LookRotation(-transform.position.normalized) *
                                         Quaternion.Euler(90, 0, 0)) as GameObject;
                    var shieldEffect = fx.GetComponent<ShieldImpactEffect>();
                    shieldEffect.Hue = Hue;
                    shieldEffect.Scale = (i + 0.5f) / 4.5f * (Power / 4f);
                    shieldEffect.Velocity = Easing.EaseIn((i + 0.5f) / 4.5f, EasingType.Quadratic) / 5f * (Power / 4f);
                    shieldEffect.Direction = -transform.position.normalized;
                }
            }
            IsAbsorbed = true;

            if (ShieldGenerator.Instance.IsPowered)
            {
                SinceEmitDestruct += Time.deltaTime;
                if (SinceEmitDestruct > 0.15f)
                {
                    var fx = Instantiate(DestructEffect, transform.position,
                                         Quaternion.LookRotation(-transform.position.normalized) *
                                         Quaternion.Euler(90, 0, 0)) as GameObject;
                    var traileffect = fx.GetComponent<TrailEffect>();
                    traileffect.Direction = transform.position.normalized;
                    traileffect.Scale = Power * 0.5f;

                    SinceEmitDestruct = 0;
                }
            }
        }

        if (transform.position.magnitude < 45 && ShieldGenerator.Instance.IsPowered)
        {
            Speed = 0.05f;
        }

        if (transform.position.magnitude < 30 + (Power * 1.5f) / 2)
        {
            AudioRouter.Instance.PlayHit(Hue);
            ShieldGenerator.Instance.Health -= Power * 25;
            ShieldGenerator.Instance.Health = Mathf.Max(0, ShieldGenerator.Instance.Health);
            if (ShieldGenerator.Instance.Health <= 0)
                ShieldGenerator.Instance.FinishGame();
            Debug.Log("player health is now " + ShieldGenerator.Instance.Health);
            Networking.RpcUpdateHealth(ShieldGenerator.Instance.Health);
            IsAutoDestructed = true;

            var go = Instantiate(PlanetImpactEffect, -direction * 32.3f, Quaternion.LookRotation(-transform.position.normalized) * Quaternion.Euler(90, 0, 0)) as GameObject;
            go.GetComponent<PlanetImpact>().Direction = direction;
            go.GetComponent<PlanetImpact>().Power = (Power / 4f) * 0.5f + 0.5f;
            go.GetComponent<PlanetImpact>().BulletHue = Hue;

            Debug.Log("Since start : " + sinceStarted);

            renderer.enabled = false;
            transform.position = -direction * 30;
        }
    }

    float hue;
    public float Hue
    {
        set 
        { 
            gameObject.FindChild("Sphere").renderer.material.SetColor("_Emission", ColorHelper.ColorFromHSV(value, 1, 0.5f));
            hue = value;
        }
        get { return hue; }
    }
    public float Power
    {
        set { power = Mathf.Max(value, 0); }
        get { return power; }
    }
}

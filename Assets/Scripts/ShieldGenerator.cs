using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShieldGenerator : Structure
{
    public float CurrentHue;

    public float Health;
    public float CurrentHealth;

    GameObject lightGo, sphere, fountain, shieldInAir;
    public List<EnemyBullet> DefendingAgainst = new List<EnemyBullet>();

    public static ShieldGenerator Instance;

    public bool IsPowered
    {
        get { return Hues.Count > 0; }
    }
    float currentPower;

    public override void Reset()
    {
        base.Reset();

        foreach (var da in DefendingAgainst)
        {
            if (!da.IsAutoDestructed)
                Destroy(da.gameObject);
        }
        DefendingAgainst.Clear();
        CurrentHealth = Health = 500;
    }

    protected override void Start()
    {
        base.Start();

        name = "Shield Generator";

        Instance = this;

        CurrentHealth = Health = 500;

        lightGo = gameObject.FindChild("Point light");
        sphere = gameObject.FindChild("shieldgenerator").FindChild("Sphere");
        fountain = gameObject.FindChild("shieldgenerator").FindChild("Shield");
        shieldInAir = gameObject.FindChild("Shield");
    }

    public override void LinkHue(Structure hue)
    {
        base.LinkHue(hue);

        Networking.RpcUpdateShieldHue(Hue);

        if (Hues.Count == 1)
            CurrentHue = Hue;
    }


    void FixedUpdate()
    {
        currentPower = Mathf.Lerp(currentPower, IsPowered ? 0.8f + Mathf.Sin(Time.timeSinceLevelLoad * 1.75f) * 0.2f : 0, 0.1f);
        lightGo.light.intensity = currentPower;

        if (IsPowered)
        {
            CurrentHue = Mathf.LerpAngle(CurrentHue, Hue, 0.1f);
            if (CurrentHue < 0) CurrentHue += 360;
            if (CurrentHue > 360) CurrentHue -= 360;
        }
    }

    void Update()
    {
        CurrentHealth = Mathf.Lerp(CurrentHealth, Health, 0.1f * Time.deltaTime / (1 / 60f));

        var addColor = ColorHelper.ColorFromHSV(CurrentHue, 1, currentPower * 0.5f);
        var alphaColor = ColorHelper.ColorFromHSV(CurrentHue, 1, 0.5f);
        alphaColor.a = currentPower;

        foreach (var r in shieldInAir.GetComponentsInChildren<Renderer>())
            r.material.SetColor("_TintColor", addColor);

        sphere.renderer.material.color = new Color(1, 1, 1, GameFlow.State == GameState.Gameplay ? currentPower : 0);
        sphere.renderer.material.SetColor("_Emission", alphaColor);
        fountain.renderer.material.SetColor("_TintColor", addColor);
        lightGo.light.color = ColorHelper.ColorFromHSV(CurrentHue, 1, 0.5f);

        //Debug.Log(Hues.Count + " structures connected, avg hue is " + Hue);

        // Defend against bullets
        if (!IsPowered || GameFlow.State != GameState.Gameplay) return;
        for (int i = DefendingAgainst.Count - 1; i >= 0; i--)
        {
            var da = DefendingAgainst[i];

            if (!da.IsInitialized) continue;
            if (da.IsAutoDestructed)
            {
                DefendingAgainst.RemoveAt(i);
                continue;
            }

            if (da.transform.position.magnitude < 45)
            {
                var shieldV = new Vector2(Mathf.Cos(Mathf.Deg2Rad * Hue), Mathf.Sin(Mathf.Deg2Rad * Hue)).normalized;
                var bulletV = new Vector2(Mathf.Cos(Mathf.Deg2Rad * da.Hue), Mathf.Sin(Mathf.Deg2Rad * da.Hue)).normalized;

                var malus = (Vector3.Dot(bulletV, shieldV) + 1) / 2;
                if (malus < 0.25f) malus = 0;

                da.Power -= malus * Time.deltaTime * 0.75f;
                da.Power = Mathf.Max(0, da.Power);

                //Debug.Log("dp = " + Vector3.Dot(bulletV, shieldV));
                //Debug.Log("actual malus = " + Easing.EaseIn((Vector3.Dot(bulletV, shieldV) + 1) / 2, EasingType.Quadratic));

                if (da.Power <= 0 && da.CurrentScale <= 0.1f)
                {
                    DefendingAgainst.RemoveAt(i);
                    Destroy(da.gameObject);
                }
            }
        }
    }

    public void FinishGame()
    {
        Networking.RpcEndGame();
        transform.parent.BroadcastMessage("OnDie", SendMessageOptions.DontRequireReceiver);
    }

    public float? AssaultHue
    {
        get
        {
            if (DefendingAgainst.Count == 0) return null;

            for (int i = 0; i < DefendingAgainst.Count; i++)
                if (!DefendingAgainst[i].IsAbsorbed)
                    return DefendingAgainst[i].Hue;

            return DefendingAgainst[DefendingAgainst.Count - 1].Hue;
        }
    }
}

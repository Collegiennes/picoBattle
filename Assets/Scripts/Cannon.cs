using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Cannon : Structure
{
    public float CurrentHue;

    public GameObject LaunchingBulletTemplate;

    GameObject ray1, ray2, bullet;
    float GlobalCooldown, LocalCooldown;
    public float AccumulatedPower;

    Mesh Mesh;

    public static Cannon Instance;

    public bool IsPowered
    {
        get { return Hues.Count > 0; }
    }

    protected override void Start()
    {
        name = "Cannon";
        Instance = this;

        //var meshFilter = GetComponent<MeshFilter>();
        //Mesh = (meshFilter.mesh = new Mesh());
        //RebuildMesh();

        base.Start();
        ray1 = gameObject.FindChild("canon").FindChild("ray1");
        ray2 = gameObject.FindChild("canon").FindChild("ray2");
        bullet = gameObject.FindChild("canon").FindChild("Bullet").FindChild("itm_sphere_invert");

        ray1.renderer.enabled = ray2.renderer.enabled = bullet.renderer.enabled = false;
    }

    public override void LinkHue(float hue)
    {
        base.LinkHue(hue);

        if (Hues.Count == 1)
        {
            //ray1.renderer.enabled = ray2.renderer.enabled = bullet.renderer.enabled = true;
            bullet.renderer.enabled = true;

            if (ray1 != null)
            {
                ray1.renderer.material.SetColor("_Emission", ColorHelper.ColorFromHSV(Hue, 1, 0.5f));
                ray2.renderer.material.SetColor("_Emission", ColorHelper.ColorFromHSV(Hue, 1, 0.5f));
                bullet.renderer.material.SetColor("_Emission", ColorHelper.ColorFromHSV(Hue, 1, 0.5f));
            }
            CurrentHue = Hue;
        }
    }

    public override void UnlinkHue(float hue)
    {
        base.UnlinkHue(hue);

        if (Hues.Count == 0)
            //ray1.renderer.enabled = ray2.renderer.enabled = bullet.renderer.enabled = false;
            bullet.renderer.enabled = false;
    }

    void Update()
    {
        CurrentHue = Mathf.LerpAngle(CurrentHue, Hue, 0.1f);
        if (CurrentHue < 0) CurrentHue += 360;
        if (CurrentHue > 360) CurrentHue -= 360;

        ray1.renderer.material.SetColor("_Emission", ColorHelper.ColorFromHSV(CurrentHue, 1, 0.5f));
        ray2.renderer.material.SetColor("_Emission", ColorHelper.ColorFromHSV(CurrentHue, 1, 0.5f));
        bullet.renderer.material.SetColor("_Emission", ColorHelper.ColorFromHSV(CurrentHue, 1, 0.5f));

        var basePower = AccumulatedPower == 0 ? 0 : 250;
        bullet.transform.localScale = (new Vector3(basePower, basePower, basePower) + new Vector3(250, 250, 250) * AccumulatedPower); //* RandomHelper.Between(0.9f, 1f);
        AccumulatedPower = Mathf.Max(0, AccumulatedPower - Time.deltaTime * 0.625f);

        GlobalCooldown += Time.deltaTime;

        if (AccumulatedPower > 1)
        {
            LocalCooldown += Time.deltaTime;

            if (GlobalCooldown > 5 && LocalCooldown > 1)
            {
                AudioRouter.Instance.PlayShoot(Hue);

                var go = Instantiate(LaunchingBulletTemplate) as GameObject;
                go.transform.localScale = bullet.transform.localScale * 0.0025f;
                go.transform.position = transform.position;
                go.GetComponent<LaunchingBullet>().Hue = Hue;

                GlobalCooldown = 0;
                LocalCooldown = 0;

                Networking.RpcShootBullet(AccumulatedPower, Hue);
                AccumulatedPower = 0;
            }
        }
        else
            LocalCooldown = 0;
    }

    //void RebuildMesh()
    //{
    //    const float Segments = 16;

    //    var vertices = new List<Vector3>();
    //    var backTris = new List<int>();
    //    //var pieTris = new List<int>();
    //    var uv = new List<Vector2>();

    //    Mesh.Clear();
    //    Mesh.subMeshCount = 1;

    //    vertices.Add(new Vector3(0, 0, 0));
    //    uv.Add(new Vector2(0, 0));

    //    // Make backing circle
    //    for (int i = 1; i <= Segments; i++)
    //    {
    //        var angle = (i - 1) / Segments * Mathf.PI * 2;

    //        vertices.Add(new Vector3((float)Math.Cos(angle), (float)Math.Sin(angle), 0));
    //        uv.Add(new Vector2(1, 1));

    //        backTris.Add(0); backTris.Add(i); backTris.Add(i == Segments ? 1 : i + 1);
    //    }

    //    if (vertices.Count > 0)
    //    {
    //        try
    //        {
    //            Mesh.vertices = vertices.ToArray();
    //            Mesh.uv = uv.ToArray();
    //            Mesh.SetTriangles(backTris.ToArray(), 0);
    //            //Mesh.SetTriangles(pieTris.ToArray(), 1);

    //            Mesh.RecalculateNormals();
    //            Mesh.RecalculateBounds();
    //            Mesh.Optimize();
    //        }
    //        catch (Exception)
    //        {
    //        }
    //    }
    //}

    public override void OnBullet(Bullet bullet)
    {
        base.OnBullet(bullet);
        AccumulatedPower++;
    }
}
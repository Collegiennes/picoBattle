using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

public class OverlayUI : MonoBehaviour
{
    const float Segments = 16;
    const float OutlineRadius = 20;
    const float OuterRadius = 16;
    const float InnerRadius = 12;
    const float InnerMaskRadius = 9;
    const float CoreRadius = 4.5f;

    public GUIStyle style;

    class Enemy
    {
        public Vector3 Location;
        public Vector2 LastKnownLocation;
        public Vector3 LastArrow;
        public bool IsAI;
        public HostData HostData;
    }

    readonly List<Enemy> Enemies = new List<Enemy>();

    Material mat;
    float lastPower;

    void Start()
    {
        mat = new Material( "Shader \"Lines/Colored Blended\" {" +
            "SubShader { Pass { " +
            "    Blend SrcAlpha OneMinusSrcAlpha " +
            "    ZWrite Off Cull Off Fog { Mode Off } " +
            "    BindChannels {" +
            "      Bind \"vertex\", vertex Bind \"color\", color }" +
            "} } }" );
        mat.hideFlags = HideFlags.HideAndDontSave;
        mat.shader.hideFlags = HideFlags.HideAndDontSave;

        Enemies.Add(new Enemy { Location = Random.onUnitSphere * 400, IsAI = true });

        Networking.Instance.HostsUpdated += UpdateStars;
    }

    void UpdateStars(IEnumerable<HostData> newHosts, IEnumerable<HostData> deletedHosts)
    {
        if (newHosts.Count() != 0 || deletedHosts.Count() != 0)
        {
            Debug.Log("Updated hosts : " + newHosts.Count() + " new, " + deletedHosts.Count() + " deleted");

            int i = 0;
            foreach (var h in newHosts)
                Debug.Log("New host #" + i++ + " : guid = " + h.guid + ", ip = " + h.ip.Aggregate("", (a, b) => a + (a == "" ? "" : ".") + b) + ", gameName = " + h.gameName);
            i = 0;
            foreach (var h in deletedHosts)
                Debug.Log("Deleted host #" + i++ + " : guid = " + h.guid + ", ip = " + h.ip.Aggregate("", (a, b) => a + (a == "" ? "" : ".") + b) + ", gameName = " + h.gameName);
        }

        Enemies.RemoveAll(x => deletedHosts.Any(y => x.HostData != null && y.guid == x.HostData.guid));
        Enemies.AddRange(newHosts.Select(x => new Enemy { HostData = x, Location = Random.onUnitSphere * 400 }));
    }

    IEnumerator OnPostRender()
    {
        yield return new WaitForEndOfFrame();

        GL.PushMatrix();
        GL.LoadPixelMatrix();

        mat.SetPass(0);

        GL.Begin(GL.TRIANGLES);

        if (GameFlow.State == GameState.Gameplay)
        {
            CannonUI();
            ShieldUI();

            EnemyUI(Networking.Instance.LocalMode ? Enemies.First(x => x.IsAI) : Enemies.First(x => x.HostData != null && x.HostData.guid == Networking.Instance.ChosenHost.guid));
        }
        else
            foreach (var e in Enemies.Where(x => x.HostData == null || x.HostData.gameName != Networking.MyGuid))
                EnemyUI(e);

        foreach (var b in ShieldGenerator.Instance.DefendingAgainst.Where(x => !x.IsAutoDestructed).Take(3))
            IncomingBulletUI(b);

        GL.End();

        GL.PopMatrix();
    }

    void Update()
    {
        var mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        if (Input.GetMouseButtonDown(0))
        {
            foreach (var e in Enemies)
            {
                if ((mousePos - e.LastKnownLocation).magnitude < 10)
                {
                    AudioRouter.Instance.PlayShoot(Random.value * 360);
                    GameFlow.State = GameState.ReadyToConnect;
                    Networking.Instance.LocalMode = e.IsAI;
                    Networking.Instance.ChosenHost = e.HostData;
                    break;
                }
            }
        }
    }

    void CannonUI()
    {
        const int scaleFactor = 1;

        var camPos = Camera.main.transform.position;
        var diff = Cannon.Instance.transform.position - camPos;
        if (Physics.RaycastAll(camPos, diff, diff.magnitude).Any(x => x.collider.gameObject.name == "Planet"))
            return;

        var ssPos = camera.WorldToScreenPoint(Cannon.Instance.gameObject.FindChild("Globe").transform.position);

        GL.Color(Color.white);

        // Make backing circle
        for (int i = 0; i < Segments; i++)
        {
            var thisA = i / Segments * Mathf.PI * 2;
            var nextA = (i + 1) / Segments * Mathf.PI * 2;

            GL.Vertex3(ssPos.x, ssPos.y, 0);
            GL.Vertex3(ssPos.x + (float)Math.Cos(thisA) * OutlineRadius * scaleFactor, ssPos.y + (float)Math.Sin(thisA) * OutlineRadius * scaleFactor, 0);
            GL.Vertex3(ssPos.x + (float)Math.Cos(nextA) * OutlineRadius * scaleFactor, ssPos.y + (float)Math.Sin(nextA) * OutlineRadius * scaleFactor, 0);
        }

        var power = Mathf.Lerp(lastPower, Cannon.Instance.AccumulatedPower, 0.15f * Time.deltaTime / (1 / 60f));
        power = Math.Min(power, 4);

        var bulletColor = ColorHelper.ColorFromHSV(Cannon.Instance.CurrentHue, 1, 1);
        if (power < 1)
            bulletColor = Color.Lerp(bulletColor, Color.black, 0.625f);
        var bgColor = new Color(36 / 255f, 34 / 255f, 34 / 255f);
        //var bgColor = Color.Lerp(new Color(36 / 255f, 34 / 255f, 34 / 255f), bulletColor, Mathf.Clamp01(Mathf.FloorToInt(power) / 5f));
        GL.Color(bgColor);

        // Make backing circle
        for (int i = 0; i < Segments; i++)
        {
            var thisA = i / Segments * Mathf.PI * 2;
            var nextA = (i + 1) / Segments * Mathf.PI * 2;

            GL.Vertex3(ssPos.x, ssPos.y, 0);
            GL.Vertex3(ssPos.x + (float)Math.Cos(thisA) * OuterRadius * scaleFactor, ssPos.y + (float)Math.Sin(thisA) * OuterRadius * scaleFactor, 0);
            GL.Vertex3(ssPos.x + (float)Math.Cos(nextA) * OuterRadius * scaleFactor, ssPos.y + (float)Math.Sin(nextA) * OuterRadius * scaleFactor, 0);
        }

        //bulletColor = Color.Lerp(new Color(36 / 255f, 34 / 255f, 34 / 255f), bulletColor, 1 - Mathf.Clamp01(Mathf.FloorToInt(power) / 5f));
        GL.Color(bulletColor);

        // Make smaller circle
        for (int i = 0; i < Segments; i++)
        {
            var thisA = i / Segments * Mathf.PI * -2 * Mathf.Clamp01(power / 4f) + Mathf.PI / 2;
            var nextA = (i + 1) / Segments * Mathf.PI * -2 * Mathf.Clamp01(power / 4f) + Mathf.PI / 2;

            GL.Vertex3(ssPos.x, ssPos.y, 0);
            GL.Vertex3(ssPos.x + (float)Math.Cos(thisA) * InnerRadius * scaleFactor, ssPos.y + (float)Math.Sin(thisA) * InnerRadius * scaleFactor, 0);
            GL.Vertex3(ssPos.x + (float)Math.Cos(nextA) * InnerRadius * scaleFactor, ssPos.y + (float)Math.Sin(nextA) * InnerRadius * scaleFactor, 0);
        }

        lastPower = power;
    }

    void ShieldUI()
    {
        var shieldPosition = ShieldGenerator.Instance.transform.position + ShieldGenerator.Instance.transform.position.normalized * 6;

        var ssPos = camera.WorldToScreenPoint(shieldPosition);
        var camPos = Camera.main.transform.position;
        var diff = shieldPosition - camPos;
        if (Physics.RaycastAll(camPos, diff, diff.magnitude).Any(x => x.collider.gameObject.name == "Planet"))
            return;

        var assaultHue = ShieldGenerator.Instance.AssaultHue.HasValue ? ShieldGenerator.Instance.AssaultHue.Value : ShieldGenerator.Instance.CurrentHue;
        var shieldHue = ShieldGenerator.Instance.CurrentHue;

        GL.Color(Color.white);

        // Make backing circle
        for (int i = 0; i < Segments; i++)
        {
            var thisA = i / Segments * Mathf.PI * 2;
            var nextA = (i + 1) / Segments * Mathf.PI * 2;

            GL.Vertex3(ssPos.x, ssPos.y, 0);
            GL.Vertex3(ssPos.x + (float)Math.Cos(thisA) * OutlineRadius, ssPos.y + (float)Math.Sin(thisA) * OutlineRadius, 0);
            GL.Vertex3(ssPos.x + (float)Math.Cos(nextA) * OutlineRadius, ssPos.y + (float)Math.Sin(nextA) * OutlineRadius, 0);
        }

        var bgColor = new Color(36 / 255f, 34 / 255f, 34 / 255f);
        GL.Color(bgColor);

        // Make backing circle
        for (int i = 0; i < Segments; i++)
        {
            var thisA = i / Segments * Mathf.PI * 2;
            var nextA = (i + 1) / Segments * Mathf.PI * 2;

            GL.Vertex3(ssPos.x, ssPos.y, 0);
            GL.Vertex3(ssPos.x + (float)Math.Cos(thisA) * OuterRadius, ssPos.y + (float)Math.Sin(thisA) * OuterRadius, 0);
            GL.Vertex3(ssPos.x + (float)Math.Cos(nextA) * OuterRadius, ssPos.y + (float)Math.Sin(nextA) * OuterRadius, 0);
        }

        var shieldColor = ColorHelper.ColorFromHSV(shieldHue, 1, 1);
        GL.Color(shieldColor);

        // Calculate absorption
        var shieldV = new Vector2(Mathf.Cos(Mathf.Deg2Rad * shieldHue), Mathf.Sin(Mathf.Deg2Rad * shieldHue)).normalized;
        var bulletV = new Vector2(Mathf.Cos(Mathf.Deg2Rad * assaultHue), Mathf.Sin(Mathf.Deg2Rad * assaultHue)).normalized;
        var power = (Vector3.Dot(bulletV, shieldV) + 1) / 2;
        power = Mathf.Max(0, power);

        // Make pointe de tarte
        if (ShieldGenerator.Instance.IsPowered)
        {
            if (power < 0.25f)
                shieldColor = Color.Lerp(shieldColor, Color.black, 0.625f);
            GL.Color(shieldColor);

            for (int i = 0; i < Segments; i++)
            {
                var thisA = i / Segments * Mathf.PI * -2 * Mathf.Clamp01(power) + Mathf.PI / 2;
                var nextA = (i + 1) / Segments * Mathf.PI * -2 * Mathf.Clamp01(power) + Mathf.PI / 2;

                GL.Vertex3(ssPos.x, ssPos.y, 0);
                GL.Vertex3(ssPos.x + (float)Math.Cos(thisA) * InnerRadius, ssPos.y + (float)Math.Sin(thisA) * InnerRadius, 0);
                GL.Vertex3(ssPos.x + (float)Math.Cos(nextA) * InnerRadius, ssPos.y + (float)Math.Sin(nextA) * InnerRadius, 0);
            }

            GL.Color(bgColor);

            // Make hole
            for (int i = 0; i < Segments; i++)
            {
                var thisA = i / Segments * Mathf.PI * 2;
                var nextA = (i + 1) / Segments * Mathf.PI * 2;

                GL.Vertex3(ssPos.x, ssPos.y, 0);
                GL.Vertex3(ssPos.x + (float)Math.Cos(thisA) * InnerMaskRadius, ssPos.y + (float)Math.Sin(thisA) * InnerMaskRadius, 0);
                GL.Vertex3(ssPos.x + (float)Math.Cos(nextA) * InnerMaskRadius, ssPos.y + (float)Math.Sin(nextA) * InnerMaskRadius, 0);
            }
        }

        if (ShieldGenerator.Instance.AssaultHue.HasValue)
        {
            GL.Color(ColorHelper.ColorFromHSV(assaultHue, 1, 1));

            // Make core (assault)
            for (int i = 0; i < Segments; i++)
            {
                var thisA = i / Segments * Mathf.PI * 2;
                var nextA = (i + 1) / Segments * Mathf.PI * 2;

                GL.Vertex3(ssPos.x, ssPos.y, 0);
                GL.Vertex3(ssPos.x + (float)Math.Cos(thisA) * CoreRadius, ssPos.y + (float)Math.Sin(thisA) * CoreRadius, 0);
                GL.Vertex3(ssPos.x + (float)Math.Cos(nextA) * CoreRadius, ssPos.y + (float)Math.Sin(nextA) * CoreRadius, 0);
            }
        }
    }

    void EnemyUI(Enemy enemy)
    {
        var scaleFactor = 1.125f;

        var ssPos = camera.WorldToScreenPoint(enemy.Location);

        var arrowDirection = Vector3.zero;
        if (ssPos.x < 25) arrowDirection.x -= 1;
        if (ssPos.x > Screen.width - 25) arrowDirection.x += 1;
        if (ssPos.y < 25) arrowDirection.y -= 1;
        if (ssPos.y > Screen.height - 25) arrowDirection.y += 1;
        arrowDirection.Normalize();

        ssPos.x = Mathf.Clamp(ssPos.x, 25, Screen.width - 25);
        ssPos.y = Mathf.Clamp(ssPos.y, 25, Screen.height - 25);

        var camPos = Camera.main.transform.position;
        var diff = enemy.Location - camPos;
        if (Physics.RaycastAll(camPos, diff, diff.magnitude).Any(x => x.collider.gameObject.name == "Planet"))
            return;

        if (Vector3.Dot(camPos - enemy.Location, Camera.main.transform.forward) > 0)
        {
            if (enemy.LastKnownLocation == Vector2.zero)
                return;

            ssPos = enemy.LastKnownLocation;
            arrowDirection = enemy.LastArrow;
        }

        var ringColor = Networking.Instance.EnemyShieldHue.HasValue ? ColorHelper.ColorFromHSV(Networking.Instance.EnemyShieldHue.Value, 1, 0.4f) : Color.white;

        GL.Color(ringColor);

        var segments = enemy.IsAI ? 6 : Segments;

        // Make backing circle
        for (int i = 0; i < segments; i++)
        {
            var thisA = i / segments * Mathf.PI * 2 + Mathf.PI / 2;
            var nextA = (i + 1) / segments * Mathf.PI * 2 + Mathf.PI / 2;

            GL.Vertex3(ssPos.x, ssPos.y, 0);
            GL.Vertex3(ssPos.x + (float)Math.Cos(thisA) * InnerRadius * scaleFactor, ssPos.y + (float)Math.Sin(thisA) * InnerRadius * scaleFactor, 0);
            GL.Vertex3(ssPos.x + (float)Math.Cos(nextA) * InnerRadius * scaleFactor, ssPos.y + (float)Math.Sin(nextA) * InnerRadius * scaleFactor, 0);
        }

        if (Networking.Instance.EnemyShieldHue.HasValue)
        {
            GL.Color(ColorHelper.ColorFromHSV(Networking.Instance.EnemyShieldHue.Value, 1, 1));

            var healthOnOne = Mathf.Clamp01(Networking.Instance.EnemyHealth / 500f);
            bool clampNext = false;
            float lastNextFrac = 0;

            // Make pointe de tarte
            for (int i = 0; i <= Segments; i++)
            {
                var thisA = i / Segments * Mathf.PI * -2 * healthOnOne + Mathf.PI / 2;
                var nextA = (i + 1) / Segments * Mathf.PI * -2 * healthOnOne + Mathf.PI / 2;

                float cosThis = Mathf.Cos(thisA), sinThis = Mathf.Cos(thisA);
                float cosNext = Mathf.Cos(nextA), sinNext = Mathf.Cos(nextA);

                if (enemy.IsAI)
                {
                    float h1ThisStep = Mathf.Floor((i / Segments * healthOnOne) * 6f) / 6f, h2ThisStep = Mathf.Ceil((i / Segments * healthOnOne) * 6f) / 6f;
                    float h1NextStep = Mathf.Floor(((i + 1) / Segments * healthOnOne) * 6f) / 6f, h2NextStep = Mathf.Ceil(((i + 1) / Segments * healthOnOne) * 6f) / 6f;
                    var thisFrac = MathHelper.Frac(i / Segments * healthOnOne * 6f);
                    var nextFrac = MathHelper.Frac((i + 1) / Segments * healthOnOne * 6f);

                    float h1ThisA = h1ThisStep * Mathf.PI * -2 + Mathf.PI / 2, h1NextA = h1NextStep * Mathf.PI * -2 + Mathf.PI / 2;
                    float h2ThisA = h2ThisStep * Mathf.PI * -2 + Mathf.PI / 2, h2NextA = h2NextStep * Mathf.PI * -2 + Mathf.PI / 2;

                    if (i == Segments && clampNext)
                        nextFrac = lastNextFrac;
                    else if (i == Segments && !clampNext)
                        continue;

                    if (clampNext)
                    {
                        thisFrac = 0;
                        clampNext = false;
                    }
                    else if (h1ThisStep != h1NextStep)
                    {
                        lastNextFrac = nextFrac;
                        nextFrac = 0;
                        clampNext = true;
                    }

                    cosThis = Mathf.Lerp(Mathf.Cos(h1ThisA), Mathf.Cos(h2ThisA), thisFrac);
                    sinThis = Mathf.Lerp(Mathf.Sin(h1ThisA), Mathf.Sin(h2ThisA), thisFrac);
                    cosNext = Mathf.Lerp(Mathf.Cos(h1NextA), Mathf.Cos(h2NextA), nextFrac);
                    sinNext = Mathf.Lerp(Mathf.Sin(h1NextA), Mathf.Sin(h2NextA), nextFrac);
                }

                GL.Vertex3(ssPos.x, ssPos.y, 0);
                GL.Vertex3(ssPos.x + cosThis * InnerRadius * scaleFactor, ssPos.y + sinThis * InnerRadius * scaleFactor, 0);
                GL.Vertex3(ssPos.x + cosNext * InnerRadius * scaleFactor, ssPos.y + sinNext * InnerRadius * scaleFactor, 0);
            }
        }

        var bgColor = new Color(36 / 255f, 34 / 255f, 34 / 255f);
        GL.Color(bgColor);

        // Make hole
        for (int i = 0; i < segments; i++)
        {
            var thisA = i / segments * Mathf.PI * 2 + Mathf.PI / 2;
            var nextA = (i + 1) / segments * Mathf.PI * 2 + Mathf.PI / 2;

            GL.Vertex3(ssPos.x, ssPos.y, 0);
            GL.Vertex3(ssPos.x + (float)Math.Cos(thisA) * InnerMaskRadius * scaleFactor, ssPos.y + (float)Math.Sin(thisA) * InnerMaskRadius * scaleFactor, 0);
            GL.Vertex3(ssPos.x + (float)Math.Cos(nextA) * InnerMaskRadius * scaleFactor, ssPos.y + (float)Math.Sin(nextA) * InnerMaskRadius * scaleFactor, 0);
        }

        GL.Color(Color.white);

        if (GameFlow.State == GameState.Gameplay)
        {
            // Make core
            for (int i = 0; i < segments; i++)
            {
                var thisA = i / segments * Mathf.PI * 2 + Mathf.PI / 2;
                var nextA = (i + 1) / segments * Mathf.PI * 2 + Mathf.PI / 2;

                GL.Vertex3(ssPos.x, ssPos.y, 0);
                GL.Vertex3(ssPos.x + (float)Math.Cos(thisA) * CoreRadius * scaleFactor, ssPos.y + (float)Math.Sin(thisA) * CoreRadius * scaleFactor, 0);
                GL.Vertex3(ssPos.x + (float)Math.Cos(nextA) * CoreRadius * scaleFactor, ssPos.y + (float)Math.Sin(nextA) * CoreRadius * scaleFactor, 0);
            }
        }

        if (Vector3.Dot(camPos - enemy.Location, Camera.main.transform.forward) < 0)
        {
            enemy.LastKnownLocation = ssPos;
            enemy.LastArrow = arrowDirection;
        }

        // Make arrow?
        if (arrowDirection != Vector3.zero)
            DrawArrow(arrowDirection, ssPos);
    }

    void IncomingBulletUI(EnemyBullet bullet)
    {
        var camPos = Camera.main.transform.position;
        var diff = bullet.transform.position - camPos;
        if (Physics.RaycastAll(camPos, diff, diff.magnitude).Any(x => x.collider.gameObject.name == "Planet"))
            return;

        var ssPos = camera.WorldToScreenPoint(bullet.transform.position);
        //Debug.Log("x = " + ssPos.x + ", y = " + ssPos.y);

        var arrowDirection = Vector3.zero;
        if (ssPos.x < 25) arrowDirection.x -= 1;
        if (ssPos.x > Screen.width - 25) arrowDirection.x += 1;
        if (ssPos.y < 25) arrowDirection.y -= 1;
        if (ssPos.y > Screen.height - 25) arrowDirection.y += 1;
        arrowDirection.Normalize();

        ssPos.x = Mathf.Clamp(ssPos.x, 25, Screen.width - 25);
        ssPos.y = Mathf.Clamp(ssPos.y, 25, Screen.height - 25);

        GL.Color(Color.white);

        // Make backing circle
        for (int i = 0; i < Segments; i++)
        {
            var thisA = i / Segments * Mathf.PI * 2;
            var nextA = (i + 1) / Segments * Mathf.PI * 2;

            GL.Vertex3(ssPos.x, ssPos.y, 0);
            GL.Vertex3(ssPos.x + (float)Math.Cos(thisA) * InnerRadius, ssPos.y + (float)Math.Sin(thisA) * InnerRadius, 0);
            GL.Vertex3(ssPos.x + (float)Math.Cos(nextA) * InnerRadius, ssPos.y + (float)Math.Sin(nextA) * InnerRadius, 0);
        }

        var power = bullet.Power;

        var bulletColor = ColorHelper.ColorFromHSV(bullet.Hue, 1, 1);
        var bgColor = new Color(36 / 255f, 34 / 255f, 34 / 255f);
        //var bgColor = Color.Lerp(new Color(36 / 255f, 34 / 255f, 34 / 255f), bulletColor, Mathf.Clamp01(Mathf.FloorToInt(power) / 5f));
        GL.Color(bgColor);

        // Make backing circle
        for (int i = 0; i < Segments; i++)
        {
            var thisA = i / Segments * Mathf.PI * 2;
            var nextA = (i + 1) / Segments * Mathf.PI * 2;

            GL.Vertex3(ssPos.x, ssPos.y, 0);
            GL.Vertex3(ssPos.x + (float)Math.Cos(thisA) * InnerMaskRadius, ssPos.y + (float)Math.Sin(thisA) * InnerMaskRadius, 0);
            GL.Vertex3(ssPos.x + (float)Math.Cos(nextA) * InnerMaskRadius, ssPos.y + (float)Math.Sin(nextA) * InnerMaskRadius, 0);
        }

        //bulletColor = Color.Lerp(new Color(36 / 255f, 34 / 255f, 34 / 255f), bulletColor, 1 - Mathf.Clamp01(Mathf.FloorToInt(power) / 5f));
        GL.Color(bulletColor);

        // Make smaller circle
        for (int i = 0; i < Segments; i++)
        {
            var thisA = i / Segments * Mathf.PI * -2 * Mathf.Clamp01(power / 4f) + Mathf.PI / 2;
            var nextA = (i + 1) / Segments * Mathf.PI * -2 * Mathf.Clamp01(power / 4f) + Mathf.PI / 2;

            GL.Vertex3(ssPos.x, ssPos.y, 0);
            GL.Vertex3(ssPos.x + (float)Math.Cos(thisA) * CoreRadius * 1.25f, ssPos.y + (float)Math.Sin(thisA) * CoreRadius * 1.25f, 0);
            GL.Vertex3(ssPos.x + (float)Math.Cos(nextA) * CoreRadius * 1.25f, ssPos.y + (float)Math.Sin(nextA) * CoreRadius * 1.25f, 0);
        }

        // Make arrow?
        if (arrowDirection != Vector3.zero)
            DrawArrow(arrowDirection, ssPos);
    }

    static void DrawArrow(Vector3 arrowDirection, Vector3 ssPos)
    {
        var center = ssPos + arrowDirection * 19f;

        var angle = Mathf.Rad2Deg * Mathf.Atan2(arrowDirection.y, arrowDirection.x);
        var quaterion = Quaternion.AngleAxis(angle, new Vector3(0, 0, 1));

        var v1 = quaterion * new Vector3(3f, 0, 0) + center;
        var v2 = quaterion * new Vector3(-3f, -3f, 0) + center;
        var v3 = quaterion * new Vector3(-3f, 3f, 0) + center;

        GL.Vertex3(v1.x, v1.y, 0);
        GL.Vertex3(v2.x, v2.y, 0);
        GL.Vertex3(v3.x, v3.y, 0);
    }

    //void OnGUI()
    //{
    //    var ssPos = camera.WorldToScreenPoint(Cannon.Instance.gameObject.FindChild("Point light").transform.position);

    //    if (!Cannon.Instance.IsPowered)
    //    {
    //        style.normal.textColor = Color.white;
    //        var text = "--";
    //        style.fontSize = 28;
    //        var size = style.CalcSize(new GUIContent(text));
    //        GUI.Label(new Rect(ssPos.x + 30, Screen.height - ssPos.y - 26, size.x, size.y), text, style);

    //        style.normal.textColor = Color.white;
    //        text = "UNPOWERED ";
    //        style.fontSize = 10;
    //        size = style.CalcSize(new GUIContent(text));
    //        GUI.Label(new Rect(ssPos.x + 31, Screen.height - ssPos.y + 3, size.x, size.y), text, style);
    //    }
    //    else
    //    {
    //        var text = Mathf.RoundToInt(Cannon.Instance.CurrentHue).ToString();
    //        style.fontSize = 28;
    //        var size = style.CalcSize(new GUIContent(text));
    //        //style.normal.textColor = new Color(0, 0, 0, 0.5f);
    //        //GUI.Label(new Rect(ssPos.x + 30 - 1, Screen.height - ssPos.y - 26 + 1, size.x, size.y), text, style);
    //        style.normal.textColor = ColorHelper.ColorFromHSV(Cannon.Instance.CurrentHue, 1, 1);
    //        GUI.Label(new Rect(ssPos.x + 30, Screen.height - ssPos.y - 26, size.x, size.y), text, style);

    //        style.normal.textColor = Color.white;
    //        text = "POWER " + (int)Cannon.Instance.AccumulatedPower;
    //        style.fontSize = 10;
    //        size = style.CalcSize(new GUIContent(text));
    //        GUI.Label(new Rect(ssPos.x + 32, Screen.height - ssPos.y + 3, size.x, size.y), text, style);
    //    }
    //}
}

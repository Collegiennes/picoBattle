using System;
using System.Collections.Generic;
using UnityEngine;

public class ArcLink : MonoBehaviour
{
    public GameObject BulletTemplate;

    Mesh Mesh;

    public bool Initialized;
    public float Hue;
    public float CurrentHue;
    public GameObject From;
    public GameObject To;
    public bool IsUnlinked;
    public Resource OldResource;

    void Start()
    {
        var meshFilter = GetComponent<MeshFilter>();
        Mesh = (meshFilter.mesh = new Mesh());
        Initialized = true;
    }

    public void SpawnBullet()
    {
        if (To == null) return;

        var bullet = Instantiate(BulletTemplate) as GameObject;
        var bc = bullet.GetComponent<Bullet>();
        bc.From = From;
        bc.To = To;
        bc.Hue = Hue;
        bc.Link = this;
    }

    public void Rebuild(Vector3 from, Vector3 to)
    {
        const float SphereRadius = 38;
        const float SegmentRate = 0.5f;
        const float Width = 0.5f;

        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var shadowTris = new List<int>();
        var uv = new List<Vector2>();

        Mesh.Clear();
        Mesh.subMeshCount = 2;

        var arcLength = Vector3.Angle(from, to) * Mathf.Deg2Rad * SphereRadius;
        var segments = Math.Round(arcLength * SegmentRate);

        for (int i = 0; i <= segments; i++)
        {
            var s = i / (float) segments;
            var center = Vector3.Slerp(from, to, s);

            Vector3 nextCenter = Vector3.Slerp(from, to, (i + 1) / (float)segments);

            var diff = Vector3.Normalize(nextCenter - center);
            var tangent = Vector3.Cross(diff, center);

            var c = vertices.Count;

            vertices.Add(center * SphereRadius - tangent * Width); uv.Add(new Vector2(0, s));
            vertices.Add(center * SphereRadius + tangent * Width); uv.Add(new Vector2(1, s));

            if (i != segments)
            {
                triangles.Add(c); triangles.Add(c + 1); triangles.Add(c + 2);
                triangles.Add(c + 2); triangles.Add(c + 1); triangles.Add(c + 3);

                triangles.Add(c); triangles.Add(c + 2); triangles.Add(c + 1);
                triangles.Add(c + 2); triangles.Add(c + 3); triangles.Add(c + 1);
            }
        }

        for (int i = 0; i <= segments; i++)
        {
            var s = i / (float)segments;
            var center = Vector3.Slerp(from, to, s);

            var c = vertices.Count;

            vertices.Add(center * SphereRadius - center * Width); uv.Add(new Vector2(0, s));
            vertices.Add(center * SphereRadius + center * Width); uv.Add(new Vector2(1, s));

            if (i != segments)
            {
                triangles.Add(c); triangles.Add(c + 1); triangles.Add(c + 2);
                triangles.Add(c + 2); triangles.Add(c + 1); triangles.Add(c + 3);

                triangles.Add(c); triangles.Add(c + 2); triangles.Add(c + 1);
                triangles.Add(c + 2); triangles.Add(c + 3); triangles.Add(c + 1);
            }
        }

        for (int i = 0; i <= segments; i++)
        {
            var s = i / (float)segments;
            var center = Vector3.Slerp(from, to, s);

            Vector3 nextCenter = Vector3.Slerp(from, to, (i + 1) / (float)segments);

            var diff = Vector3.Normalize(nextCenter - center);
            var tangent = Vector3.Cross(diff, center);

            var c = vertices.Count;
            var r = SphereRadius * 0.8375f;

            vertices.Add(center * r - tangent * Width); uv.Add(new Vector2(0, s));
            vertices.Add(center * r + tangent * Width); uv.Add(new Vector2(1, s));

            if (i != segments)
            {
                shadowTris.Add(c); shadowTris.Add(c + 1); shadowTris.Add(c + 2);
                shadowTris.Add(c + 2); shadowTris.Add(c + 1); shadowTris.Add(c + 3);
            }
        }

        if (arcLength > 1 && vertices.Count > 0)
        {
            try
            {
                Mesh.vertices = vertices.ToArray();
                Mesh.uv = uv.ToArray();
                Mesh.SetTriangles(triangles.ToArray(), 0);
                Mesh.SetTriangles(shadowTris.ToArray(), 1);

                Mesh.RecalculateNormals();
                Mesh.RecalculateBounds();
                Mesh.Optimize();

                GetComponent<MeshCollider>().sharedMesh = Mesh;
                GetComponent<MeshCollider>().convex = false;
                GetComponent<MeshCollider>().convex = true;
            }
            catch (Exception)
            {
            }

            CurrentHue = Hue;
            renderer.material.SetColor("_Emission", ColorHelper.ColorFromHSV(Hue, 1, 0.5f));
        }
    }

    public void Unlink()
    {
        From.GetComponent<Structure>().IsEmitting = false;
        var fromResouce = From.GetComponent<Resource>();
        if (fromResouce != null)
            fromResouce.Reset();

        To.GetComponent<Structure>().UnlinkHue(Hue);
        From.GetComponent<Structure>().LinkTo = null;
        IsUnlinked = true;

        transform.parent.BroadcastMessage("LinkRemoved", this);

        Destroy(gameObject);
    }

    void Update()
    {
        if (To != null)
        {
            CurrentHue = Mathf.LerpAngle(CurrentHue, Hue, 0.1f);
            if (CurrentHue < 0) CurrentHue += 360;
            if (CurrentHue > 360) CurrentHue -= 360;
            renderer.material.SetColor("_Emission", ColorHelper.ColorFromHSV(CurrentHue, 1, 0.5f));
        }
    }
}

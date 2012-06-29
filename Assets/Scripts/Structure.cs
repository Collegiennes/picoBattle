using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class Structure : MonoBehaviour
{
    public bool IsEmitting;

    public ArcLink LinkFrom, LinkTo;
    public readonly List<Structure> Hues = new List<Structure>();

    public virtual void Reset()
    {
        IsEmitting = false;
        LinkFrom = LinkTo = null;
        Hues.Clear();
    }

    protected virtual void Start()
    {
        transform.parent.BroadcastMessage("StructureAdded", this);
    }

    protected void Dispose()
    {
        transform.parent.BroadcastMessage("StructureRemoved", this);
    }

    public virtual void LinkHue(Structure hue)
    {
        Hues.Add(hue);
    }

    public virtual void UnlinkHue(Structure hue)
    {
        Hues.Remove(hue);
    }

    public virtual void OnBullet(Bullet bullet)
    {
        if (LinkTo != null)
            LinkTo.SpawnBullet();
    }

    public static HashSet<Structure> StructuresVisited = new HashSet<Structure>(); 

    public virtual float Hue
    {
        get
        {
            if (Hues.Count == 0) return 0; // TODO : Figure out why this gets used sometimes

            if (StructuresVisited.Contains(this))
                return StructuresVisited.First(x => x is Resource).Hue;

            StructuresVisited.Add(this);

            if (Hues.Count == 1)
            {
                var hue = Hues[0].Hue;
                StructuresVisited.Remove(this);
                return hue;
            }

            Vector2 acc = Vector2.zero;
            foreach (var h in Hues.OrderBy(x => x is Capsule))
                acc += new Vector2(Mathf.Cos(Mathf.Deg2Rad * h.Hue), Mathf.Sin(Mathf.Deg2Rad * h.Hue));
            acc.Normalize();
            var angle = Mathf.Rad2Deg * Mathf.Atan2(acc.y, acc.x);
            if (angle < 0) angle += 360;
            if (angle >= 360) angle -= 360;

            StructuresVisited.Remove(this);

            return angle;
        }
    }
}
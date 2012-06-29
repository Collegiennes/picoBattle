using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class MousePicking : MonoBehaviour
{
    //IGamepads Gamepads;
    IMouse Mouse;
    Camera Camera;

    public GameObject Selected;
    float SinceSelected;

    public GameObject Planet;
    public GameObject LinkTemplate;
    public GameObject CapsuleTemplate;

    GameObject CurrentLink;
    Vector3? DragOrigin;

    bool outer, inner;

    public readonly List<Structure> Structures = new List<Structure>();
    public readonly List<ArcLink> Links = new List<ArcLink>();

    public static MousePicking Instance;

	void Start()
	{
        Instance = this;

        //Gamepads = GamepadsManager.Instance;
	    Mouse = MouseManager.Instance;
	    Camera = Camera.main;
	}

    public void Reset()
    {
        foreach (var l in Links.ToArray())
            l.Unlink();
        Links.Clear();

        // Destroy remaining arclinks
        foreach (var o in FindObjectsOfType(typeof(ArcLink)))
            Destroy((o as ArcLink).gameObject);
    }

    void Update()
    {
        if (GameFlow.State != GameState.Gameplay && GameFlow.State > GameState.WaitingForChallenge) return;

        var position = Mouse.Position;
        //var agp = Gamepads.Any;

        var ray = Camera.ScreenPointToRay(new Vector3(position.X, position.Y, Camera.nearClipPlane));
        RaycastHit info;
        Vector3 currentProjection = Vector3.zero;
        if (Planet.collider.Raycast(ray, out info, 1000)) currentProjection = info.point.normalized;
        else
        {
            float nearestDistance = float.MaxValue;
            GameObject nearestObject = null;
            foreach (var structure in Structures)
                foreach (var c in structure.GetComponentsInChildren<Collider>())
                    if (!structure.IsEmitting && c.Raycast(ray, out info, float.MaxValue) && info.distance < nearestDistance &&
                        Vector3.Dot(Vector3.Normalize(structure.transform.position), Vector3.Normalize(Camera.main.transform.position)) > 0)
                    {
                        nearestDistance = info.distance;
                        nearestObject = structure.gameObject;
                    }

            if (nearestObject != null)
                currentProjection = nearestObject.transform.position.normalized;
            else if (CurrentLink != null)
            {
                if (Selected != null)
                    Selected.GetComponent<Structure>().LinkTo = null;
                Destroy(CurrentLink);
                CurrentLink = null;
            }
        }

        var consideredDrag = DragOrigin.HasValue && Vector3.Angle(currentProjection, DragOrigin.Value) > 2;

        if (Mouse.RightButton.State == MouseButtonState.Clicked)
        {
            float nearestDistance = float.MaxValue;
            ArcLink nearestLink = null;

            foreach (var link in Links)
                foreach (var c in link.GetComponentsInChildren<Collider>())
                    if (c.Raycast(ray, out info, float.MaxValue) && info.distance < nearestDistance)
                    {
                        nearestDistance = info.distance;
                        nearestLink = link;
                    }

            if (nearestLink != null)
            {
                var or = nearestLink.OldResource;

                nearestLink.Unlink();

                if (or != null)
                {
                    //Structures.Add(or);
                    foreach (var c in or.GetComponentsInChildren<Collider>()) c.enabled = true;
                    foreach (var r in or.GetComponentsInChildren<Renderer>()) r.enabled = true;
                    or.Reset();
                }
            }
        }

        if (Mouse.LeftButton.State == MouseButtonState.Idle)
        {
            float nearestDistance = float.MaxValue;
            GameObject nearestObject = null;
            foreach (var structure in Structures)
                foreach (var c in structure.GetComponentsInChildren<Collider>())
                    if (!structure.IsEmitting && structure.GetComponent<Cannon>() == null && c.Raycast(ray, out info, float.MaxValue) && info.distance < nearestDistance &&
                        Vector3.Dot(Vector3.Normalize(structure.transform.position), Vector3.Normalize(Camera.main.transform.position)) > 0)
                    {
                        nearestDistance = info.distance;
                        nearestObject = structure.gameObject;
                    }

            if (nearestObject != null)
            {
                if (nearestObject != Selected)
                {
                    Deselect();
                    Select(nearestObject);
                }
            }
            else if (Selected != null)
            {
                SinceSelected += Time.deltaTime;
                if (SinceSelected > 0.125f)
                    Deselect();
            }

            if (Selected != null && Selected.GetComponent<Structure>() is Resource)
            {
                var resource = Selected.GetComponent<Resource>();
                if (!resource.IsEmitting)
                {
                    if (resource.Inner.collider.Raycast(ray, out info, float.MaxValue))
                    {
                        resource.HighlightSphere(resource.Inner);
                        inner = true;
                        outer = false;
                    }
                    else if (resource.Outer.collider.Raycast(ray, out info, float.MaxValue))
                    {
                        resource.HighlightSphere(resource.Outer);
                        outer = true;
                        inner = false;
                    }
                    else
                        resource.HighlightSphere(null);
                }
                else
                {
                    inner = outer = false;
                }
            }
        }

        if (Mouse.LeftButton.State == MouseButtonState.DragStarted)
        {
            DragOrigin = currentProjection;
            if (DragOrigin.Value == Vector3.zero && Selected != null)
                DragOrigin = Selected.transform.position.normalized;
        }

        if (Selected != null && (Mouse.LeftButton.State == MouseButtonState.Down || Mouse.LeftButton.State == MouseButtonState.Dragging))
        {
            if (CurrentLink == null && consideredDrag)
            {
                var resource = Selected.GetComponent<Resource>();
                if (resource != null)
                {
                    float hue = outer ? resource.OuterHue : inner ? resource.InnerHue : 0;

                    CurrentLink = (GameObject)Instantiate(LinkTemplate);
                    CurrentLink.transform.parent = transform;
                    CurrentLink.GetComponent<ArcLink>().Hue = hue;
                    CurrentLink.GetComponent<ArcLink>().From = Selected;
                    resource.LinkTo = CurrentLink.GetComponent<ArcLink>();
                }

                var capsule = Selected.GetComponent<Capsule>();
                if (capsule != null)
                {
                    CurrentLink = (GameObject)Instantiate(LinkTemplate);
                    CurrentLink.transform.parent = transform;
                    CurrentLink.GetComponent<ArcLink>().Hue = capsule.Hue;
                    CurrentLink.GetComponent<ArcLink>().From = Selected;
                    capsule.LinkTo = CurrentLink.GetComponent<ArcLink>();
                }
            }
            else if (CurrentLink != null && !consideredDrag)
            {
                if (Selected != null)
                    Selected.GetComponent<Structure>().LinkTo = null;
                Destroy(CurrentLink);
                CurrentLink = null;
            }
        }

        if (consideredDrag && CurrentLink != null && CurrentLink.GetComponent<ArcLink>().Initialized)
        {
            float nearestDistance = float.MaxValue;
            GameObject nearestObject = null;
            foreach (var structure in Structures)
            {
                if (GameFlow.State != GameState.Gameplay && structure.GetComponent<Cannon>() != null)
                    continue;

                if (structure.GetComponentInChildren<Collider>().Raycast(ray, out info, float.MaxValue))
                {
                    if (info.distance < nearestDistance && structure.gameObject != Selected && Vector3.Angle(structure.transform.position.normalized, Camera.main.transform.position.normalized) < 90)
                    {
                        nearestDistance = info.distance;
                        nearestObject = structure.gameObject;
                    }
                }
            }
            if (nearestObject != null)
            {
                var origin = Selected.transform.position;
                var no = Vector3.Normalize(Camera.main.transform.position);
                var nd = Vector3.Normalize(nearestObject.transform.position);
                if (Vector3.Dot(no, nd) > 0)
                    CurrentLink.GetComponent<ArcLink>().Rebuild(Vector3.Normalize(origin), nd);
            }
            else
            {
                var origin = Selected.transform.position;
                var no = Vector3.Normalize(Camera.main.transform.position);
                var nd = Vector3.Normalize(currentProjection);
                if (Vector3.Dot(no, nd) > 0)
                    CurrentLink.GetComponent<ArcLink>().Rebuild(Vector3.Normalize(origin), nd);
            }
        }

        if (Mouse.LeftButton.State == MouseButtonState.DragEnded)
        {
            if (consideredDrag && CurrentLink != null)
            {
                float nearestDistance = float.MaxValue;
                GameObject nearestObject = null;
                foreach (var structure in Structures)
                {
                    if (GameFlow.State != GameState.Gameplay && structure.GetComponent<Cannon>() != null)
                        continue;

                    if (structure.GetComponentInChildren<Collider>().Raycast(ray, out info, float.MaxValue))
                    {
                        if (info.distance < nearestDistance && structure.gameObject != Selected && Vector3.Angle(structure.transform.position.normalized, DragOrigin.Value) < 90)
                        {
                            nearestDistance = info.distance;
                            nearestObject = structure.gameObject;
                        }
                    }
                }
                if (nearestObject != null)
                {
                    var p = nearestObject.transform.position;
                    CurrentLink.GetComponent<ArcLink>().Rebuild(Vector3.Normalize(Selected.transform.position),
                                                                Vector3.Normalize(p));

                    var nearestCannon = nearestObject.GetComponent<Cannon>();
                    var nearestShield = nearestObject.GetComponent<ShieldGenerator>();
                    var nearestCapsule = nearestObject.GetComponent<Capsule>();

                    AudioRouter.Instance.PlayLink(CurrentLink.GetComponent<ArcLink>().Hue);

                    Selected.GetComponent<Structure>().LinkFrom = CurrentLink.GetComponent<ArcLink>();

                    if (nearestShield != null)
                    {
                        var s = CurrentLink.GetComponent<ArcLink>().From.GetComponent<Structure>();
                        if (s is Resource)
                        {
                            (s as Resource).IsEmitting = true;
                            (s as Resource).ChooseSphere(CurrentLink.GetComponent<ArcLink>().Hue);
                        }

                        CurrentLink.GetComponent<ArcLink>().To = nearestObject;
                        nearestShield.LinkFrom = CurrentLink.GetComponent<ArcLink>();
                        Links.Add(CurrentLink.GetComponent<ArcLink>());

                        nearestShield.LinkHue(CurrentLink.GetComponent<ArcLink>().From.GetComponent<Structure>());
                    }
                    else if (nearestCannon != null)
                    {
                        var s = CurrentLink.GetComponent<ArcLink>().From.GetComponent<Structure>();
                        if (s is Resource)
                        {
                            (s as Resource).IsEmitting = true;
                            (s as Resource).ChooseSphere(CurrentLink.GetComponent<ArcLink>().Hue);
                        }

                        CurrentLink.GetComponent<ArcLink>().To = nearestObject;
                        nearestCannon.LinkFrom = CurrentLink.GetComponent<ArcLink>();
                        Links.Add(CurrentLink.GetComponent<ArcLink>());

                        nearestCannon.LinkHue(CurrentLink.GetComponent<ArcLink>().From.GetComponent<Structure>());
                    }
                    else if (nearestCapsule != null)
                    {
                        CurrentLink.GetComponent<ArcLink>().To = nearestObject;
                        nearestCapsule.LinkFrom = CurrentLink.GetComponent<ArcLink>();
                        Links.Add(CurrentLink.GetComponent<ArcLink>());

                        var s = CurrentLink.GetComponent<ArcLink>().From.GetComponent<Structure>();
                        if (s is Resource)
                        {
                            (s as Resource).IsEmitting = true;
                            (s as Resource).ChooseSphere(CurrentLink.GetComponent<ArcLink>().Hue);
                        }

                        nearestCapsule.LinkHue(CurrentLink.GetComponent<ArcLink>().From.GetComponent<Structure>());
                    }
                    else
                    {
                        if (nearestObject.GetComponent<Resource>() != null &&
                            nearestObject.GetComponent<Resource>().IsEmitting)
                        {
                            Deselect();
                            if (Selected != null)
                                Selected.GetComponent<Structure>().LinkTo = null;
                            Destroy(CurrentLink);
                        }
                        else
                        {
                            var go =
                                (GameObject)
                                Instantiate(CapsuleTemplate, p,
                                            Quaternion.LookRotation(Vector3.Normalize(p)) *
                                            Quaternion.AngleAxis(90, Vector3.right));
                            go.transform.parent = transform;
                            
                            go.GetComponent<Structure>().LinkFrom = CurrentLink.GetComponent<ArcLink>();

                            CurrentLink.GetComponent<ArcLink>().To = go;

                            var s = CurrentLink.GetComponent<ArcLink>().From.GetComponent<Structure>();
                            if (s is Resource)
                            {
                                (s as Resource).IsEmitting = true;
                                (s as Resource).ChooseSphere(CurrentLink.GetComponent<ArcLink>().Hue);
                            }

                            go.GetComponent<Capsule>().LinkHue(CurrentLink.GetComponent<ArcLink>().From.GetComponent<Structure>());

                            Links.Add(CurrentLink.GetComponent<ArcLink>());

                            s = nearestObject.GetComponent<Structure>();
                            if (s is Resource)
                            {
                                CurrentLink.GetComponent<ArcLink>().OldResource = s as Resource;

                                //Structures.Remove(s);
                                foreach (var c in s.GetComponentsInChildren<Collider>())    c.enabled = false;
                                foreach (var r in s.GetComponentsInChildren<Renderer>())    r.enabled = false;
                            }
                        }
                    }

                    Deselect();

                    if (CurrentLink.GetComponent<ArcLink>().From.GetComponent<Resource>() != null)
                        CurrentLink.GetComponent<ArcLink>().SpawnBullet();
                }
                else
                {
                    if (Selected != null)
                        Selected.GetComponent<Structure>().LinkTo = null;
                    Destroy(CurrentLink);
                    Deselect();
                }

                CurrentLink = null;
            }
            DragOrigin = null;
        }
    }

    void Select(GameObject toSelect)
    {
        SinceSelected = 0;

        Selected = toSelect;

        var s = Selected.GetComponent<Structure>();
        if (s is Resource)
            (s as Resource).ShowSpheres();
    }

    void Deselect()
    {
        if (Selected == null) return;

        var s = Selected.GetComponent<Structure>();
        if (s is Resource)
            (s as Resource).HideSpheres();

        Selected = null;
    }

    public void StructureAdded(Structure structure)
    {
        Structures.Add(structure);
    }
    public void StructureRemoved(Structure structure)
    {
        Structures.Remove(structure);
        Debug.Log("removing structure");
    }
    public void LinkRemoved(ArcLink link)
    {
        Links.Remove(link);
        Debug.Log("removing link");
    }
}

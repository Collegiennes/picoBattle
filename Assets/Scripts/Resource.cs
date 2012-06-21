using System.Linq;
using UnityEngine;

public class Resource : Structure
{
    public GameObject Inner, Outer, Collider;
    public float InnerHue, OuterHue;
    public float? ChosenHue;
    bool SphereChosen;
    bool showing, hiding, highlighting;

    public override void Reset()
    {
        base.Reset();

        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = true;
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = true;

        ChosenHue = null;
        SphereChosen = false;
        HideSpheres();
    }

    protected override void Start()
    {
        base.Start();

        Inner = gameObject.FindChild("Inner");
        Outer = gameObject.FindChild("Outer");
        Collider = gameObject.FindChild("Collider");

        Outer.renderer.enabled = false;
        Inner.renderer.enabled = false;

        RandomizeColor();
    }

    public void ShowSpheres()
    {
        showing = true;
        Outer.renderer.enabled = true;
        Inner.renderer.enabled = true;
        //Collider.collider.enabled = false;

        Inner.transform.localScale = Vector3.zero;
        Outer.transform.localScale = Vector3.zero;

        highlighting = false;

        Wait.Until(t =>
        {
            if (!highlighting)
                Inner.transform.localScale = Vector3.Lerp(Vector3.zero, new Vector3(450, 450, 450), Easing.EaseOut(Mathf.Clamp01(t * 6), EasingType.Cubic));

            Outer.transform.localScale = Vector3.Lerp(Vector3.zero, new Vector3(852.2012f, 852.2012f, 852.2012f), Easing.EaseOut(Mathf.Clamp01(t * 6), EasingType.Cubic));

            return t >= 1 || hiding;
        }, () => { showing = false; });
    }

    public void HighlightSphere(GameObject go)
    {
        if (SphereChosen || hiding) return;

        float destinationInner;

        highlighting = true;

        if (go == Inner)
            destinationInner = 600;
        else if (go == Outer)
            destinationInner = 250;
        else
            destinationInner = 450;

        Inner.transform.localScale = Vector3.Lerp(Inner.transform.localScale, new Vector3(destinationInner, destinationInner, destinationInner), 0.25f * Time.deltaTime / (1 / 60f));
    }
    public void ChooseSphere(float hue)
    {
        SphereChosen = true;

        ChosenHue = hue;

        if (hue == InnerHue)
        {
            var OriginalScale = Outer.transform.localScale;

            Wait.Until(t =>
            {
                var step = Easing.EaseOut(Mathf.Clamp01(t * 6), EasingType.Cubic);
                Outer.transform.localScale = Vector3.Lerp(OriginalScale, Vector3.zero, step);
                return step >= 1;
            }, () =>
            {
                Outer.renderer.enabled = false;
            });
        }
        else if (hue == OuterHue)
        {
            var OriginalScale = Inner.transform.localScale;

            Wait.Until(t =>
            {
                var step = Easing.EaseOut(Mathf.Clamp01(t * 6), EasingType.Cubic);
                Inner.transform.localScale = Vector3.Lerp(OriginalScale, Vector3.zero, step);
                return step >= 1;
            }, () =>
            {
                Inner.renderer.enabled = false;
            });
        }
    }

    public void RandomizeColor()
    {
        InnerHue = RandomHelper.Random.Next(0, 360);
        OuterHue = (InnerHue + 180) % 360;

        Inner.renderer.material.SetColor("_Emission", ColorHelper.ColorFromHSV(InnerHue, 1, 0.5f));
        Outer.renderer.material.SetColor("_Emission", ColorHelper.ColorFromHSV(OuterHue, 1, 0.5f));
    }

    public void HideSpheres()
    {
        if (IsEmitting || SphereChosen) return;

        hiding = true;

        var fromInner = 450;
        var fromOuter = 852.2012f;

        Collider.collider.enabled = true;

        Wait.Until(t =>
        {
            var step = Easing.EaseOut(Mathf.Clamp01(t * 6), EasingType.Cubic);

            Inner.transform.localScale = Vector3.Lerp(Vector3.one * fromInner, Vector3.zero, step);
            Outer.transform.localScale = Vector3.Lerp(Vector3.one * fromOuter, Vector3.zero, step);

            return step >= 1 || SphereChosen || IsEmitting || showing;
        }, () =>
        {
            if (!IsEmitting && !SphereChosen && !showing)
            {
                Outer.renderer.enabled = false;
                Inner.renderer.enabled = false;
            }
            hiding = false;
        });
    }

    public override float Hue
    {
        get { return ChosenHue.HasValue ? ChosenHue.Value : 0; }
    }
}

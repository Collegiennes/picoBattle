using UnityEngine;

public class LaunchingBullet : MonoBehaviour
{
    public float Hue;

    float step;

    void Start()
    {
    }

    void Update()
    {
        if (transform.position.magnitude > 250)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += transform.position.normalized;

        renderer.material.SetColor("_Emission", ColorHelper.ColorFromHSV(Hue, 1, 0.5f));
    }
}

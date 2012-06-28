using UnityEngine;

public class Bullet : MonoBehaviour
{
    public GameObject From;
    public GameObject To;
    public ArcLink Link;
    public float Hue;

    float step;

    void Start()
    {
        //var diff = To.transform.position - From.transform.position;
        transform.LookAt(To.transform);
    }

    void Update()
    {
        if (GameFlow.State == GameState.Lost)
            Destroy(gameObject);

        if (GameFlow.State != GameState.Gameplay) return;

        if (Link.IsUnlinked)
        {
            Destroy(gameObject);
            return;
        }

        if (From != null && To != null)
        {
            var speed = Vector3.Angle(From.transform.position.normalized, To.transform.position.normalized);
            step += Time.deltaTime / speed * 10;

            if (step > 1)
            {
                var receivingStructure = To.GetComponent<Structure>();
                if (receivingStructure != null)
                    receivingStructure.OnBullet(this);

                var sendingResource = From.GetComponent<Resource>();
                if (sendingResource != null && sendingResource.LinkTo != null)
                    sendingResource.LinkTo.SpawnBullet();

                Destroy(gameObject);
                return;
            }

            renderer.transform.localPosition =
                Vector3.Slerp(From.transform.position.normalized, To.transform.position.normalized, step) * 38;

            renderer.material.SetColor("_Emission", ColorHelper.ColorFromHSV(Hue, 1, 0.5f));
        }
    }
}

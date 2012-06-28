using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

class Placement : MonoBehaviour
{
    const float SphereRadius = 31.5f;
    const float ExclusionArc = 15;

    public GameObject ResourceTemplate;
    public GameObject CanonTemplate;
    public GameObject ShieldTemplate;

    List<GameObject> Resources = new List<GameObject>();

    public static Placement Instance;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Place();

        // Generate canon & shield
        var canonGo = (GameObject)Instantiate(CanonTemplate, Vector3.up * SphereRadius, Quaternion.LookRotation(Vector3.up) * Quaternion.AngleAxis(90, Vector3.right));
        canonGo.transform.parent = transform;

        var shieldGo = (GameObject)Instantiate(ShieldTemplate, Vector3.down * SphereRadius, Quaternion.LookRotation(Vector3.down) * Quaternion.AngleAxis(90, Vector3.right));
        shieldGo.transform.parent = transform;
    }

    public void Reset()
    {
        foreach (var go in Resources)
        {
            var r = go.GetComponent<Resource>();
            r.Reset();
        }

        foreach (var o in FindObjectsOfType(typeof(Bullet)))
            Destroy((o as Bullet).gameObject);

        foreach (var o in FindObjectsOfType(typeof(EnemyBullet)))
        {
            var eb = o as EnemyBullet;
            if (eb.IsAbsorbed)
                Destroy(eb.gameObject);
        }
    }
    
    void Place()
    {
        var resourceVectors = new List<Vector3> { Vector3.up, Vector3.down };

        const int Attempts = 200;
        for (int i = 0; i < Attempts; i++)
        {
            var randomTest = Random.onUnitSphere;
            bool allAway = true;
            foreach (var m in resourceVectors)
                if (!(allAway &= Vector3.Angle(randomTest, m) * Mathf.Deg2Rad * SphereRadius > ExclusionArc))
                    break;
            if (allAway)
            {
                var go = (GameObject)Instantiate(ResourceTemplate, randomTest * SphereRadius, Quaternion.LookRotation(randomTest) * Quaternion.AngleAxis(90, Vector3.right));
                go.transform.parent = transform;
                resourceVectors.Add(randomTest);
                Resources.Add(go);
            }
        }
    }

    public Vector3 GetCleanVector()
    {
        var resourceVectors = new List<Vector3> { Vector3.up, Vector3.down };

        const int Attempts = 200;
        for (int i = 0; i < Attempts; i++)
        {
            var randomTest = Random.onUnitSphere;
            bool allAway = true;
            foreach (var m in resourceVectors)
                if (!(allAway &= Vector3.Angle(randomTest, m) * Mathf.Deg2Rad * SphereRadius > ExclusionArc))
                    break;
            if (allAway)
                return randomTest;
        }

        Debug.Log("Failed");
        return Random.onUnitSphere;
    }
}

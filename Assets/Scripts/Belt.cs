using System;
using UnityEngine;

class Belt : MonoBehaviour
{
    void Update()
    {
        transform.Rotate(new Vector3(Time.deltaTime / 6, Time.deltaTime / 2, Time.deltaTime / 12));
    }
}

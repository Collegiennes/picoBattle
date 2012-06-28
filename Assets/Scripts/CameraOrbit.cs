using System;
using System.Collections;
using UnityEngine;

class CameraOrbit : MonoBehaviour
{
    IMouse Mouse;
    //IGamepads Gamepads;
    float DestinationDistance;
    public float CurrentDistance;
    Quaternion DestinationRotation, CurrentRotation;

    AudioSource introTrack;

    void Start()
    {
        Mouse = MouseManager.Instance;
        //Gamepads = GamepadsManager.Instance;
        DestinationDistance = 100;

        introTrack = GetComponentsInChildren<AudioSource>()[1];
    }

    public void Reset()
    {
        DestinationDistance = 100;
    }

    void FixedUpdate()
    {
        if (introTrack == null) return;
        introTrack.volume = Mathf.Lerp(introTrack.volume, GameFlow.State <= GameState.Connecting ? 0.625f : 0, 0.05f);
        if (introTrack.volume < 0.01f && introTrack.isPlaying)
            introTrack.Stop();
        if (introTrack.volume > 0.01f && !introTrack.isPlaying)
            introTrack.Play();
    }

    void Update()
    {
        if (GameFlow.State >= GameState.Won)
        {
            DestinationDistance = Mathf.Lerp(DestinationDistance, 400, Time.deltaTime * (GameFlow.State == GameState.Won ? 15 : 1));

            camera.transform.localRotation = DestinationRotation;
            camera.transform.RotateAround(Vector3.zero, camera.transform.right, -Time.deltaTime * 2 * (float)Math.Sqrt(CurrentDistance) / 15);
            camera.transform.RotateAround(Vector3.zero, camera.transform.up, Time.deltaTime * (float)Math.Sqrt(CurrentDistance) / 15);
            camera.transform.RotateAround(Vector3.zero, camera.transform.forward, -Time.deltaTime * 2 * (float)Math.Sqrt(CurrentDistance) / 4);
            DestinationRotation = camera.transform.localRotation;
        }
        else
        {
            if (Mouse.RightButton.State == MouseButtonState.Dragging)
            {
                var diff = Mouse.RightButton.DragState.Movement;
                camera.transform.localRotation = DestinationRotation;
                camera.transform.RotateAround(Vector3.zero, camera.transform.right, -diff.Y * (float)Math.Sqrt(CurrentDistance) / 15);
                camera.transform.RotateAround(Vector3.zero, camera.transform.up, diff.X * (float)Math.Sqrt(CurrentDistance) / 15);
                DestinationRotation = camera.transform.localRotation;
            }
            //if (Gamepads.Any.Connected)
            //{
            //    var diff = Gamepads.Any.LeftStick.Position * 0.75f;
            //    camera.transform.localRotation = DestinationRotation;
            //    camera.transform.RotateAround(Vector3.zero, camera.transform.right, diff.y * (float)Math.Sqrt(CurrentDistance) / 4);
            //    camera.transform.RotateAround(Vector3.zero, camera.transform.up, -diff.x * (float)Math.Sqrt(CurrentDistance) / 4);

            //    camera.transform.RotateAround(Vector3.zero, camera.transform.forward, Gamepads.Any.RightStick.Position.x * (float)Math.Sqrt(CurrentDistance) / 4);
            //    DestinationRotation = camera.transform.localRotation;
            //}

            var zoom = Input.GetAxis("Mouse ScrollWheel");
            //if (Gamepads.Any.Connected)
            //{
            //    zoom -= Gamepads.Any.LeftTrigger.Value / 25;
            //    zoom += Gamepads.Any.RightTrigger.Value / 25;
            //}
            DestinationDistance *= -zoom * 0.75f + 1;
        }

        CurrentRotation = Quaternion.Slerp(CurrentRotation, DestinationRotation, 0.25f * Time.deltaTime / (1 / 60f));
        CurrentDistance = Mathf.Lerp(CurrentDistance, DestinationDistance, 0.25f * Time.deltaTime / (1 / 60f));

        camera.transform.localRotation = CurrentRotation;

        DestinationDistance = Mathf.Clamp(DestinationDistance, 40, 600);
        
        camera.transform.position = -camera.transform.forward * CurrentDistance;
    }
}

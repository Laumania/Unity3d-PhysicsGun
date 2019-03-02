using System;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class FPSControllerBridge : MonoBehaviour
{
    private FirstPersonController _fpsController;

    private void Start()
    {
        _fpsController = GetComponent<FirstPersonController>();

        var gun = FindObjectOfType<PhysicsGunInteractionBehavior>();

        if (gun != null && _fpsController != null)
        {
            gun.OnRotation.AddListener(OnRotation);
        }
    }

    private void OnRotation(bool rotation)
    {
        _fpsController.LockRotation = rotation;
    }
}

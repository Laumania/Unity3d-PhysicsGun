using System;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class FPSControllerBridge : MonoBehaviour
{
    private FirstPersonController _fpsController;

    private void Start()
    {
        _fpsController = FindObjectOfType<FirstPersonController>();
        if(_fpsController == null)
        {
            Debug.LogError($"{nameof(FPSControllerBridge)} is missing {nameof(FirstPersonController)}", this);
            return;
        }

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

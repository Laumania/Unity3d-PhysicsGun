using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

/* Original script "Gravity Gun": https://pastebin.com/w1G8m3dH
 * Original author: Jake Perry, reddit.com/user/nandos13
 * 
 * February 2019, above script was used as the starting point of this current script.
 * This improved script can be found here: https://github.com/Laumania/Unity3d-PhysicsGun
 * Repository created and script enhanced by Mads Laumann, http://laumania.net
 */
public class PhysicsGunInteractionBehavior : MonoBehaviour
{
    /// <summary>For easy enable/disable mouse look when rotating objects, we store this reference</summary>
    private FirstPersonController   _firstPersonController;
    /// <summary>The rigidbody we are currently holding</summary>
    private Rigidbody               _grabbedRigidbody;
    /// <summary>The offset vector from the object's position to hit point, in local space</summary>
    private Vector3                 _hitOffsetLocal;
    /// <summary>The distance we are holding the object at</summary>
    private float                   _currentGrabDistance;
    /// <summary>The interpolation state when first grabbed</summary>
    private RigidbodyInterpolation  _initialInterpolationSetting;
    /// <summary>The difference between player & object rotation, updated when picked up or when rotated by the player</summary>
    private Quaternion              _rotationDifference;
    /// <summary>Tracks player input to rotate current object. Used and reset every fixedupdate call</summary>
    private Vector2                 _rotationInput;
    /// <summary>The maximum distance at which a new object can be picked up</summary>
    private const float             _maxGrabDistance = 50;
    
    void Start()
    {
        _firstPersonController = GetComponent<FirstPersonController>();

        if(_firstPersonController == null)
            Debug.LogError($"{nameof(_firstPersonController)} is null and the gravity gun won't work properly!", this);
    }
    
	void Update ()
    {
        _firstPersonController.enabled = !Input.GetKey(KeyCode.R); 

        if (!Input.GetMouseButton(0))
        {
            // We are not holding the mouse button. Release the object and return before checking for a new one
            if (_grabbedRigidbody != null)
            {
                // Reset the rigidbody to how it was before we grabbed it
                _grabbedRigidbody.interpolation = _initialInterpolationSetting;
                _grabbedRigidbody.freezeRotation = false;
                _grabbedRigidbody = null;
            }
            return;
        }

        if (_grabbedRigidbody == null)
        {
            // We are not holding an object, look for one to pick up

            Ray ray = CenterRay();
            RaycastHit hit;

            Debug.DrawRay(ray.origin, ray.direction * _maxGrabDistance, Color.blue, 0.01f);

            if (Physics.Raycast(ray, out hit, _maxGrabDistance))
            {
                // Don't pick up kinematic rigidbodies (they can't move)
                if (hit.rigidbody != null && !hit.rigidbody.isKinematic)
                {
                    // Track rigidbody's initial information
                    _grabbedRigidbody                   = hit.rigidbody;                    
                    _grabbedRigidbody.freezeRotation    = true;
                    _initialInterpolationSetting        = _grabbedRigidbody.interpolation;
                    _rotationDifference                 = Quaternion.Inverse(transform.rotation) * hit.rigidbody.rotation;
                    _hitOffsetLocal                     = hit.transform.InverseTransformVector(hit.point - hit.transform.position);
                    _currentGrabDistance                = Vector3.Distance(ray.origin, hit.point);

                    // Set rigidbody's interpolation for proper collision detection when being moved by the player
                    _grabbedRigidbody.interpolation     = RigidbodyInterpolation.Interpolate;
                }
            }
        }
        else
        {
            // We are already holding an object, listen for rotation input
            if (Input.GetKey(KeyCode.R))
            {
                _rotationInput += new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            }
        }
	}

    void FixedUpdate()
    {
        if (_grabbedRigidbody)
        {
            // We are holding an object, time to rotate & move it

            Ray ray = CenterRay();

            // Apply any intentional rotation input made by the player & clear tracked input
            var intentionalRotation         = Quaternion.AngleAxis(_rotationInput.y, transform.right) * Quaternion.AngleAxis(-_rotationInput.x, transform.up) * _grabbedRigidbody.rotation; 
            var relativeToPlayerRotation    = _rotationDifference * transform.rotation;

            // Rotate the object to remain consistent with any changes in player's rotation
            //rigidbody.MoveRotation(intentionalRotation);
            _grabbedRigidbody.MoveRotation(relativeToPlayerRotation);


            // Remove all torque, reset rotation input & store the rotation difference for next FixedUpdate call
            _grabbedRigidbody.angularVelocity   = Vector3.zero;
            _rotationInput               = Vector2.zero;
            _rotationDifference          = Quaternion.Inverse(transform.rotation) * _grabbedRigidbody.rotation;
            
            // Calculate object's center position based on the offset we stored
            // NOTE: We need to convert the local-space point back to world coordinates
            // Get the destination point for the point on the object we grabbed
            Vector3 holdPoint           = ray.GetPoint(_currentGrabDistance);
            Vector3 centerDestination   = holdPoint - _grabbedRigidbody.transform.TransformVector(_hitOffsetLocal);
            Debug.DrawLine(ray.origin, holdPoint, Color.blue, Time.fixedDeltaTime);

            // Find vector from current position to destination
            Vector3 toDestination = centerDestination - _grabbedRigidbody.transform.position;

            // Calculate force
            Vector3 force = toDestination / Time.fixedDeltaTime * 0.8f;

            // Remove any existing velocity and add force to move to final position
            _grabbedRigidbody.velocity = Vector3.zero;
            _grabbedRigidbody.AddForce(force, ForceMode.VelocityChange);
        }
    }

    /// <returns>Ray from center of the main camera's viewport forward</returns>
    private Ray CenterRay()
    {
        return Camera.main.ViewportPointToRay(Vector3.one * 0.5f);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

/* "Gravity Gun" script I quickly threw together to help another user out on Reddit.
 * When clicking the mouse button, you will grab a rigidbody object in front of the
 * main camera's view. 
 * Some initial information is recorded about where you grabbed the object, and
 * the difference between it's rotation and yours.
 * 
 * The object will be moved around according to the offset point you initially
 * picked up.
 * Moving around, the object will rotate with the player so that the player will
 * always be viewing the object at the same angle. 
 * 
 * 
 * Feel free to use or modify this script however you see fit.
 * I hope you guys can learn something from this script. Enjoy :)
 * 
 * Original author: Jake Perry, reddit.com/user/nandos13
 * 
 * February 2019, this script was used as the base for a Unity3d Physics Gun Sample 
 * project on Github to make it even better. 
 * https://github.com/Laumania/Unity3d-PhysicsGun
 * Repo created and script enhanced by Mads Laumann, http://laumania.net
 */
public class PhysicsGunInteractionBehavior : MonoBehaviour
{
    /// <summary>For easy enable/disable mouse look when rotating objects, we store this reference</summary>
    private FirstPersonController _firstPersonController;

    /// <summary>The rigidbody we are currently holding</summary>
    private new Rigidbody rigidbody;

    /// <summary>The offset vector from the object's position to hit point, in local space</summary>
    private Vector3 hitOffsetLocal;
    /// <summary>The distance we are holding the object at</summary>
    private float currentGrabDistance;
    /// <summary>The interpolation state when first grabbed</summary>
    private RigidbodyInterpolation initialInterpolationSetting;
    /// <summary>The difference between player & object rotation, updated when picked up or when rotated by the player</summary>
    private Vector3 rotationDifferenceEuler;
    private Quaternion rotationDifference;
    
    /// <summary>Tracks player input to rotate current object. Used and reset every fixedupdate call</summary>
    private Vector2 rotationInput;

    /// <summary>The maximum distance at which a new object can be picked up</summary>
    private const float maxGrabDistance = 30;
    
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
            if (rigidbody != null)
            {
                // Reset the rigidbody to how it was before we grabbed it
                rigidbody.interpolation = initialInterpolationSetting;
                rigidbody.freezeRotation = false;
                rigidbody = null;
            }
            
            return;
        }

        if (rigidbody == null)
        {
            // We are not holding an object, look for one to pick up

            Ray ray = CenterRay();
            RaycastHit hit;

            Debug.DrawRay(ray.origin, ray.direction * maxGrabDistance, Color.blue, 0.01f);

            if (Physics.Raycast(ray, out hit, maxGrabDistance))
            {
                // Don't pick up kinematic rigidbodies (they can't move)
                if (hit.rigidbody != null && !hit.rigidbody.isKinematic)
                {
                    // Track rigidbody's initial information
                    rigidbody = hit.rigidbody;                    
                    rigidbody.freezeRotation = true;
                    
                    initialInterpolationSetting = rigidbody.interpolation;
                    rotationDifferenceEuler     = hit.transform.rotation.eulerAngles - transform.rotation.eulerAngles;
                    rotationDifference          = Quaternion.Inverse(transform.rotation) * hit.rigidbody.rotation;
                    hitOffsetLocal              = hit.transform.InverseTransformVector(hit.point - hit.transform.position);
                    currentGrabDistance         = Vector3.Distance(ray.origin, hit.point);

                    // Set rigidbody's interpolation for proper collision detection when being moved by the player
                    rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                }
            }
        }
        else
        {
            // We are already holding an object, listen for rotation input
            if (Input.GetKey(KeyCode.R))
            {
                rotationInput += new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            }
        }
	}

    void FixedUpdate()
    {
        if (rigidbody)
        {
            // We are holding an object, time to rotate & move it

            Ray ray = CenterRay();

            // Apply any intentional rotation input made by the player & clear tracked input
            var intentionalRotation         = Quaternion.AngleAxis(rotationInput.y, transform.right) * Quaternion.AngleAxis(-rotationInput.x, transform.up) * rigidbody.rotation; 
            var relativeToPlayerRotation    = rotationDifference * transform.rotation;

            // Rotate the object to remain consistent with any changes in player's rotation
            //rigidbody.MoveRotation(intentionalRotation);
            rigidbody.MoveRotation(relativeToPlayerRotation);


            // Remove all torque, reset rotation input & store the rotation difference for next FixedUpdate call
            rigidbody.angularVelocity   = Vector3.zero;
            rotationInput               = Vector2.zero;
            rotationDifferenceEuler     = rigidbody.rotation.eulerAngles - transform.rotation.eulerAngles;
            rotationDifference          = Quaternion.Inverse(transform.rotation) * rigidbody.rotation;
            
            // Calculate object's center position based on the offset we stored
            // NOTE: We need to convert the local-space point back to world coordinates
            // Get the destination point for the point on the object we grabbed
            Vector3 holdPoint           = ray.GetPoint(currentGrabDistance);
            Vector3 centerDestination   = holdPoint - rigidbody.transform.TransformVector(hitOffsetLocal);
            Debug.DrawLine(ray.origin, holdPoint, Color.blue, Time.fixedDeltaTime);

            // Find vector from current position to destination
            Vector3 toDestination = centerDestination - rigidbody.transform.position;

            // Calculate force
            Vector3 force = toDestination / Time.fixedDeltaTime * 0.8f;

            // Remove any existing velocity and add force to move to final position
            rigidbody.velocity = Vector3.zero;
            rigidbody.AddForce(force, ForceMode.VelocityChange);
        }
    }

    /// <returns>Ray from center of the main camera's viewport forward</returns>
    private Ray CenterRay()
    {
        return Camera.main.ViewportPointToRay(Vector3.one * 0.5f);
    }
}
using UnityEngine;

public class AxisArrows : MonoBehaviour
{
    public GameObject XAxis;
    public GameObject YAxis;
    public GameObject ZAxis;

    private bool _arrowActive;
    private PhysicsGunInteractionBehavior _gun;

    public void Start()
    {
        EnableArrows(false);
        _gun = FindObjectOfType<PhysicsGunInteractionBehavior>();

        if(_gun != null)
        {
            _gun.OnRotation.AddListener(EnableArrows);
        }
    }

    public void EnableArrows(bool enable)
    {
        _arrowActive = enable;
        XAxis.SetActive(enable);
        YAxis.SetActive(enable);
        ZAxis.SetActive(enable);

        if (!enable)
        {
            XAxis.transform.localPosition = Vector3.zero;
            YAxis.transform.localPosition = Vector3.zero;
            ZAxis.transform.localPosition = Vector3.zero;
        }
    }

    private void Update()
    {
        if(_arrowActive && _gun != null && _gun.CurrentGrabbedTransform != null)
        {
            SetArrowPos(_gun.CurrentUp, _gun.CurrentRight, _gun.CurrentForward, _gun.CurrentGrabbedTransform);
        }
    }

    private void SetArrowPos(Vector3 up, Vector3 right, Vector3 forward, Transform t)
    {
        XAxis.transform.position = t.position;
        YAxis.transform.position = t.position;
        ZAxis.transform.position = t.position;

        var v = up - t.position;
        XAxis.transform.rotation = Quaternion.FromToRotation(Vector3.up, right);
        v = right - t.position;
        YAxis.transform.rotation = Quaternion.FromToRotation(Vector3.up, up);
        v = forward - t.position;
        ZAxis.transform.rotation = Quaternion.FromToRotation(Vector3.up, forward);
    }
}

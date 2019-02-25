using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AxisArrows : MonoBehaviour
{
    public GameObject XAxis;
    public GameObject YAxis;
    public GameObject ZAxis;

    public void Start()
    {
        DisableArrows();
    }

    public void EnableArrows()
    {
        XAxis.SetActive(true);
        YAxis.SetActive(true);
        ZAxis.SetActive(true);
    }

    public void DisableArrows()
    {
        XAxis.SetActive(false);
        YAxis.SetActive(false);
        ZAxis.SetActive(false);
    }

    public void SetArrowPos(Vector3 up, Vector3 right, Vector3 forward, Transform t)
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

using UnityEngine;
using UnityEngine.UI;

public class GUIController : MonoBehaviour
{
    [SerializeField]
    private Text _rotationText;

    private string _displayText;
    private bool _objectAxis;
    private void Start()
    {
        var physicsGun = FindObjectOfType<PhysicsGunInteractionBehavior>();

        if (physicsGun == null)
            return;

        physicsGun.OnRotationSnapped.AddListener(OnRotationSnapped);
        physicsGun.OnAxisChanged.AddListener(OnAxisChange);

        _rotationText.gameObject.SetActive(false);
    }

    private void OnAxisChange(bool axis)
    {
        _objectAxis = axis;
        UpdateText();
    }

    private void OnRotationSnapped(bool snapped)
    {
        _rotationText.gameObject.SetActive(snapped);
        UpdateText();
    }

    private void UpdateText()
    {
        if (_rotationText == null)
            return;

        _rotationText.text = "Snapped Axis = " + (_objectAxis ? "Object Axis" : "Player Axis");
    }
}

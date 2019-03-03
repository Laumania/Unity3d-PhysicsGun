using UnityEngine;
using UnityEngine.UI;

public class AngleSlider : MonoBehaviour
{
    private Slider _slider;
    private Text _sliderText;
    private PhysicsGunInteractionBehavior _guncontroller;

    private void Start()
    {
        _slider         = GetComponent<Slider>();
        _sliderText     = GetComponentInChildren<Text>();
        _guncontroller  = FindObjectOfType<PhysicsGunInteractionBehavior>();

        _slider.onValueChanged.AddListener(OnSliderUpdated);
        UpdateText(_slider.value);
    }

    private void OnSliderUpdated(float value)
    {
        if (_guncontroller == null)
            return;

        _guncontroller.SnapRotationDegrees = value;
        UpdateText(value);
    }

    private void UpdateText(float value)
    {
        _sliderText.text = value.ToString();
    }
}

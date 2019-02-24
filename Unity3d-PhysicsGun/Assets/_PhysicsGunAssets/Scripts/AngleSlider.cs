using UnityEngine;
using UnityEngine.UI;

public class AngleSlider : MonoBehaviour
{
    private Slider _slider;
    private Text _sliderText;
    private PhysicsGunInteractionBehavior _guncontroller;

    private void Start()
    {
        _slider = GetComponent<Slider>();
        _sliderText = GetComponentInChildren<Text>();
        _guncontroller = FindObjectOfType<PhysicsGunInteractionBehavior>();

        _slider.onValueChanged.AddListener(OnSliderUpdated);
        UpdateText(_slider.value);
    }

    private void OnSliderUpdated(float value)
    {
        _guncontroller._snapRotationDegrees = value;
        UpdateText(value);
    }

    private void UpdateText(float value)
    {
        _sliderText.text = value.ToString();
    }
}

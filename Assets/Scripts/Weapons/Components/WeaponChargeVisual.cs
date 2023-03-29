using UnityEngine;
using UnityEngine.UI;

public class WeaponChargeVisual : MonoBehaviour
{
    [SerializeField] private WeaponChargeTrigger weaponChargeTrigger;
    [SerializeField] private Image image;

    void Awake()
    {
        image.fillAmount = 0;
    }

    void OnEnable()
    {
        weaponChargeTrigger.OnChargeChanged += HandleChargeChanged;
    }

    void OnDisable()
    {
        weaponChargeTrigger.OnChargeChanged -= HandleChargeChanged;
    }

    private void HandleChargeChanged(float charge)
    {
        image.fillAmount = charge;
    }
}

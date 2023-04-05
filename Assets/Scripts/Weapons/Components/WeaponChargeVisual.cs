using UnityEngine;
using UnityEngine.UI;

public class WeaponChargeVisual : MonoBehaviour
{
    [SerializeField] private WeaponChargeTrigger weaponChargeTrigger;
    [SerializeField] private Image image;

    [SerializeField] private GameObject[] toDeactivateWhenNotCharging;

    private bool isDeactivated = true;

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

        if(!isDeactivated && charge <= 0)
        {
            isDeactivated = true;
            foreach (GameObject go in toDeactivateWhenNotCharging)
            {
                go.SetActive(false);
            }
        }
        else if(isDeactivated && charge > 0)
        {
            isDeactivated = false;
            foreach (GameObject go in toDeactivateWhenNotCharging)
            {
                go.SetActive(true);
            }
        }
    }
}

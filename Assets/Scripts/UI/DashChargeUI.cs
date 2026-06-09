using UnityEngine;
using UnityEngine.UI;

public class DashChargeUI : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Player used as the source for dash points and charge count.")]
    private PlayerController player;

    [SerializeField]
    [Tooltip("Image used as the filled dash bar. Set Image Type to Filled.")]
    private Image dashFillImage;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("Fill amount used when dash charge is empty.")]
    private float minimumFillAmount = 0f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("Fill amount used when dash charge is full.")]
    private float maximumFillAmount = 1f;

    [SerializeField]
    [Tooltip("Objects enabled when each dash charge is available.")]
    private GameObject[] availableChargeSprites = new GameObject[3];

    private void Awake()
    {
        Refresh();
    }

    private void OnValidate()
    {
        minimumFillAmount = Mathf.Clamp01(minimumFillAmount);
        maximumFillAmount = Mathf.Clamp01(maximumFillAmount);
    }

    private void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (player == null)
        {
            return;
        }

        UpdateFill();
        UpdateChargeSprites();
    }

    private void UpdateFill()
    {
        if (dashFillImage == null)
        {
            return;
        }

        float normalizedDashPoints = player.MaxDashPoints > 0f
            ? Mathf.Clamp01(player.CurrentDashPoints / player.MaxDashPoints)
            : 0f;

        dashFillImage.fillAmount = Mathf.Lerp(minimumFillAmount, maximumFillAmount, normalizedDashPoints);
    }

    private void UpdateChargeSprites()
    {
        int availableCharges = player.AvailableDashCharges;

        for (int i = 0; i < availableChargeSprites.Length; i++)
        {
            if (availableChargeSprites[i] == null)
            {
                continue;
            }

            availableChargeSprites[i].SetActive(i < availableCharges);
        }
    }
}

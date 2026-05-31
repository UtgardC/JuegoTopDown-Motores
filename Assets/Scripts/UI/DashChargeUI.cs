using UnityEngine;

public class DashChargeUI : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Player used as the source for dash points and charge count.")]
    private PlayerController player;

    [SerializeField]
    [Tooltip("RectTransform used as the vertical fill of the dash bar.")]
    private RectTransform dashFill;

    [SerializeField]
    [Tooltip("If active, the initial height of Dash Fill is used as the full height.")]
    private bool useInitialFillHeightAsMax = true;

    [SerializeField]
    [Tooltip("Maximum fill height used when Use Initial Fill Height As Max is disabled.")]
    private float maxFillHeight = 300f;

    [SerializeField]
    [Tooltip("Objects enabled when each dash charge is available.")]
    private GameObject[] availableChargeSprites = new GameObject[3];

    private void Awake()
    {
        if (useInitialFillHeightAsMax && dashFill != null)
        {
            maxFillHeight = dashFill.sizeDelta.y;
        }

        Refresh();
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
        if (dashFill == null)
        {
            return;
        }

        float normalizedDashPoints = player.MaxDashPoints > 0f
            ? Mathf.Clamp01(player.CurrentDashPoints / player.MaxDashPoints)
            : 0f;

        Vector2 sizeDelta = dashFill.sizeDelta;
        sizeDelta.y = Mathf.Lerp(0f, maxFillHeight, normalizedDashPoints);
        dashFill.sizeDelta = sizeDelta;
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

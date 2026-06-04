using UnityEngine;

public class BulletTrailEffect : MonoBehaviour
{
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float travelDuration = 0.04f;
    [SerializeField] private float lingerDuration = 0.08f;
    [SerializeField] private AnimationCurve lineFadeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    [SerializeField] private bool destroyWhenFinished = true;

    private Vector3 startPosition;
    private Vector3 endPosition;
    private float timer;
    private bool initialized;
    private bool travelFinished;
    private Color initialLineStartColor;
    private Color initialLineEndColor;

    private void Awake()
    {
        if (trailRenderer == null)
        {
            trailRenderer = GetComponentInChildren<TrailRenderer>(true);
        }

        if (lineRenderer == null)
        {
            lineRenderer = GetComponentInChildren<LineRenderer>(true);
        }

        if (lineRenderer != null)
        {
            initialLineStartColor = lineRenderer.startColor;
            initialLineEndColor = lineRenderer.endColor;
        }
    }

    private void OnValidate()
    {
        travelDuration = Mathf.Max(0f, travelDuration);
        lingerDuration = Mathf.Max(0f, lingerDuration);
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        if (!travelFinished)
        {
            TickTravel();
            return;
        }

        TickLinger();
    }

    public void Play(Vector3 start, Vector3 end)
    {
        startPosition = start;
        endPosition = end;
        timer = 0f;
        initialized = true;
        travelFinished = false;

        transform.position = startPosition;
        transform.rotation = GetRotation(startPosition, endPosition);

        if (trailRenderer != null)
        {
            trailRenderer.Clear();
            trailRenderer.emitting = true;
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, startPosition);
            lineRenderer.SetPosition(1, startPosition);
            SetLineAlpha(1f);
        }
    }

    private void TickTravel()
    {
        timer += Time.deltaTime;
        float t = travelDuration <= 0f ? 1f : Mathf.Clamp01(timer / travelDuration);
        Vector3 position = Vector3.Lerp(startPosition, endPosition, t);

        transform.position = position;

        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, startPosition);
            lineRenderer.SetPosition(1, position);
        }

        if (t < 1f)
        {
            return;
        }

        travelFinished = true;
        timer = 0f;

        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
        }
    }

    private void TickLinger()
    {
        timer += Time.deltaTime;
        float t = lingerDuration <= 0f ? 1f : Mathf.Clamp01(timer / lingerDuration);

        if (lineRenderer != null)
        {
            SetLineAlpha(lineFadeCurve != null ? lineFadeCurve.Evaluate(t) : 1f - t);
        }

        if (t < 1f)
        {
            return;
        }

        if (destroyWhenFinished)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void SetLineAlpha(float alpha)
    {
        Color startColor = initialLineStartColor;
        Color endColor = initialLineEndColor;
        startColor.a *= alpha;
        endColor.a *= alpha;
        lineRenderer.startColor = startColor;
        lineRenderer.endColor = endColor;
    }

    private static Quaternion GetRotation(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        if (direction.sqrMagnitude <= 0.001f)
        {
            return Quaternion.identity;
        }

        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }
}

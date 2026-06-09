using System;
using UnityEngine;

[Serializable]
public class AudioCue
{
    [SerializeField] private AudioClip[] clips;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private Vector2 pitchRange = Vector2.one;
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 30f;

    public bool HasClips => clips != null && clips.Length > 0;
    public float Volume => volume;
    public float SpatialBlend => spatialBlend;
    public float MinDistance => Mathf.Max(0.01f, minDistance);
    public float MaxDistance => Mathf.Max(MinDistance, maxDistance);

    public AudioClip GetClip()
    {
        if (!HasClips)
        {
            return null;
        }

        return clips[UnityEngine.Random.Range(0, clips.Length)];
    }

    public float GetPitch()
    {
        float minPitch = Mathf.Max(0.01f, Mathf.Min(pitchRange.x, pitchRange.y));
        float maxPitch = Mathf.Max(minPitch, Mathf.Max(pitchRange.x, pitchRange.y));
        return Mathf.Approximately(minPitch, maxPitch)
            ? minPitch
            : UnityEngine.Random.Range(minPitch, maxPitch);
    }
}

using UnityEngine;

public class AudioOnEnable : MonoBehaviour
{
    [SerializeField] private AudioCue cue;
    [SerializeField] private Transform soundPoint;
    [SerializeField] private bool playOnEnable = true;

    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    public void Play()
    {
        Transform source = soundPoint != null ? soundPoint : transform;
        AudioManager.PlaySfx(cue, source.position);
    }
}

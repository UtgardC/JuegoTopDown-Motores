using UnityEngine;

public class WeaponVisualEffects : MonoBehaviour
{
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private bool restartMuzzleFlashBeforePlay = true;

    private void Awake()
    {
        if (muzzleFlash == null)
        {
            muzzleFlash = GetComponentInChildren<ParticleSystem>(true);
        }
    }

    public void PlayMuzzleFlash()
    {
        if (muzzleFlash == null)
        {
            return;
        }

        if (restartMuzzleFlashBeforePlay)
        {
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        muzzleFlash.Play(true);
    }
}

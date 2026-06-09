using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup musicMixerGroup;

    [Header("SFX")]
    [SerializeField] private Transform sfxRoot;

    [Header("Combat Music")]
    [SerializeField] private AudioClip combatIntro;
    [SerializeField] private AudioClip combatLoop;
    [SerializeField, Range(0f, 1f)] private float combatMusicVolume = 1f;
    [SerializeField] private float combatFadeOutDuration = 1.5f;
    [SerializeField] private AnimationCurve combatFadeOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private readonly HashSet<UnityEngine.Object> activeCombatGroups = new HashSet<UnityEngine.Object>();

    private AudioSource introSource;
    private AudioSource loopSource;
    private Coroutine fadeCoroutine;
    private bool fadingCombatMusic;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        CreateMusicSources();
    }

    private void OnValidate()
    {
        combatFadeOutDuration = Mathf.Max(0f, combatFadeOutDuration);
    }

    public static void PlaySfx(AudioCue cue, Vector3 position, float volumeMultiplier = 1f)
    {
        if (Instance != null)
        {
            Instance.PlaySfxInternal(cue, position, volumeMultiplier);
        }
    }

    public static void NotifyCombatGroupActivated(EnemyActivationGroup group)
    {
        if (Instance != null)
        {
            Instance.HandleCombatGroupActivated(group);
        }
    }

    public static void NotifyCombatGroupActivated(BossActivationGroup group)
    {
        if (Instance != null)
        {
            Instance.HandleCombatGroupActivated(group);
        }
    }

    public static void NotifyCombatGroupCleared(EnemyActivationGroup group)
    {
        if (Instance != null)
        {
            Instance.HandleCombatGroupCleared(group);
        }
    }

    public static void NotifyCombatGroupCleared(BossActivationGroup group)
    {
        if (Instance != null)
        {
            Instance.HandleCombatGroupCleared(group);
        }
    }

    private void PlaySfxInternal(AudioCue cue, Vector3 position, float volumeMultiplier)
    {
        if (cue == null || !cue.HasClips)
        {
            return;
        }

        AudioClip clip = cue.GetClip();
        if (clip == null)
        {
            return;
        }

        GameObject sourceObject = new GameObject($"SFX_{clip.name}");
        sourceObject.transform.SetParent(sfxRoot != null ? sfxRoot : transform);
        sourceObject.transform.position = position;

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = sfxMixerGroup;
        source.clip = clip;
        source.volume = cue.Volume * Mathf.Max(0f, volumeMultiplier);
        source.pitch = cue.GetPitch();
        source.spatialBlend = cue.SpatialBlend;
        source.minDistance = cue.MinDistance;
        source.maxDistance = cue.MaxDistance;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.Play();

        float lifetime = clip.length / Mathf.Max(0.01f, Mathf.Abs(source.pitch));
        Destroy(sourceObject, lifetime + 0.1f);
    }

    private void CreateMusicSources()
    {
        introSource = CreateMusicSource("Music_Intro", false);
        loopSource = CreateMusicSource("Music_Loop", true);
    }

    private AudioSource CreateMusicSource(string sourceName, bool loop)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = musicMixerGroup;
        source.playOnAwake = false;
        source.loop = loop;
        source.volume = combatMusicVolume;
        return source;
    }

    private void HandleCombatGroupActivated(UnityEngine.Object group)
    {
        if (group == null)
        {
            return;
        }

        bool wasOutOfCombat = activeCombatGroups.Count == 0;
        activeCombatGroups.Add(group);

        if (wasOutOfCombat || fadingCombatMusic)
        {
            StartCombatMusicFromIntro();
        }
    }

    private void HandleCombatGroupCleared(UnityEngine.Object group)
    {
        if (group != null)
        {
            activeCombatGroups.Remove(group);
        }

        if (activeCombatGroups.Count == 0)
        {
            FadeOutCombatMusic();
        }
    }

    private void StartCombatMusicFromIntro()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        fadingCombatMusic = false;
        introSource.Stop();
        loopSource.Stop();
        introSource.volume = combatMusicVolume;
        loopSource.volume = combatMusicVolume;

        double startTime = AudioSettings.dspTime;

        if (combatIntro != null)
        {
            introSource.clip = combatIntro;
            introSource.loop = false;
            introSource.PlayScheduled(startTime);
            startTime += combatIntro.length;
        }

        if (combatLoop != null)
        {
            loopSource.clip = combatLoop;
            loopSource.loop = true;
            loopSource.PlayScheduled(startTime);
        }
    }

    private void FadeOutCombatMusic()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeOutCombatMusicRoutine());
    }

    private IEnumerator FadeOutCombatMusicRoutine()
    {
        fadingCombatMusic = true;
        float startIntroVolume = introSource.volume;
        float startLoopVolume = loopSource.volume;
        float timer = 0f;

        if (combatFadeOutDuration <= 0f)
        {
            StopCombatMusic();
            yield break;
        }

        while (timer < combatFadeOutDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / combatFadeOutDuration);
            float curveT = combatFadeOutCurve != null ? combatFadeOutCurve.Evaluate(t) : t;
            float fade = 1f - curveT;
            introSource.volume = startIntroVolume * fade;
            loopSource.volume = startLoopVolume * fade;
            yield return null;
        }

        StopCombatMusic();
    }

    private void StopCombatMusic()
    {
        introSource.Stop();
        loopSource.Stop();
        introSource.volume = combatMusicVolume;
        loopSource.volume = combatMusicVolume;
        fadingCombatMusic = false;
        fadeCoroutine = null;
    }
}

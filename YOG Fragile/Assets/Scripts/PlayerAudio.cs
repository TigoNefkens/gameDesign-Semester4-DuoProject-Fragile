using System.Collections;
using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    private PlatformerController controller;
    private StaminaSystem stamina;
    private AudioSource audioSource;

    [Header("Audio Files")]
    public AudioClip sfxSheet;
    public AudioClip shatterClip;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float shatterVolume = 0.5f;

    [Header("Jump Sound Settings")]
    public float jumpStartTime;
    public float jumpDuration;

    [Header("Land Sound Settings")]
    public float landStartTime;
    public float landDuration;

    private Coroutine stopCoroutine;

    private bool isShattering = false;

    void Awake()
    {
        controller = GetComponent<PlatformerController>();
        stamina = GetComponent<StaminaSystem>();
        audioSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        controller.OnJump += PlayJumpSound;
        //controller.OnLand += PlayLandSound;

        stamina.OnShatterSound += PlayShatterSound;
    }

    void OnDisable()
    {
        controller.OnJump -= PlayJumpSound;
        //controller.OnLand -= PlayLandSound;

        stamina.OnShatterSound -= PlayShatterSound;
    }

    void PlayJumpSound()
    {
        PlaySegment(jumpStartTime, jumpDuration, "Jump");
    }

    void PlayLandSound()
    {
        PlaySegment(landStartTime, landDuration, "Land");
    }

    private void PlaySegment(float startTime, float duration, string type)
    {
        if (stopCoroutine != null) StopCoroutine(stopCoroutine);
        if (isShattering) return;

        audioSource.Stop();

        audioSource.clip = sfxSheet;
        audioSource.volume = sfxVolume;
        audioSource.time = startTime;


        audioSource.Play();
        stopCoroutine = StartCoroutine(StopAfterDelay(duration, type));
    }

    private IEnumerator StopAfterDelay(float delay, string type)
    {
        yield return new WaitForSeconds(delay);
        audioSource.Stop();
    }

    void PlayShatterSound()
    {
        isShattering = true;
        if (stopCoroutine != null) StopCoroutine(stopCoroutine);

        audioSource.Stop();
        audioSource.clip = shatterClip;
        audioSource.volume = shatterVolume;
        audioSource.time = 0;
        audioSource.Play();

        StartCoroutine(StopShattering());
    }

    private IEnumerator StopShattering()
    {
        yield return new WaitForSeconds(2f);
        isShattering = false;
    }
}
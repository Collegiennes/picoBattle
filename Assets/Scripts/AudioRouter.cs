using UnityEngine;

public class AudioRouter : MonoBehaviour
{
    public static AudioRouter Instance;

    AudioSource linkAS, impactAS, shootAS, hitAS;

    void Awake()
    {
        Instance = this;
        linkAS = GetComponents<AudioSource>()[0];
        impactAS = GetComponents<AudioSource>()[1];
        shootAS = GetComponents<AudioSource>()[2];
        hitAS = GetComponents<AudioSource>()[3];
    }

    public void PlayLink(float hue)
    {
        linkAS.PlayOneShot(GetSoundForHue(hue));
    }
    public void PlayImpact(float hue)
    {
        impactAS.PlayOneShot(GetSoundForHue(hue));
    }
    public void PlayShoot(float hue)
    {
        shootAS.PlayOneShot(GetSoundForHue(hue));
    }
    public void PlayHit(float hue)
    {
        hitAS.PlayOneShot(GetSoundForHue(hue));
    }

    public AudioClip GetSoundForHue(float hue)
    {
        return CapsuleSounds[Mathf.RoundToInt(hue / 360f * 7) % 7];
    }

    public AudioClip[] CapsuleSounds;
}

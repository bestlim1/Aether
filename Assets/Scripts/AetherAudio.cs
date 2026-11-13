using UnityEngine;

/// <summary>Original, synthesized soundscape. No imported audio assets are required.</summary>
public sealed class AetherAudio : MonoBehaviour
{
    public static AetherAudio I;
    private AudioSource ambience;
    private AudioSource ambienceNext;
    private AudioSource effects;
    private AudioClip[] regionalAmbience;
    private AudioClip[] notes;
    private AudioClip solvedClip, stepClip, clickClip;
    private float masterVolume = 0.7f;
    private float nextStep;
    private int currentRegion, nextRegion;
    private bool crossFading;
    private float regionBlend;
    private const int SampleRate = 22050;

    private void Awake()
    {
        I = this;
        ambience = gameObject.AddComponent<AudioSource>();
        ambience.playOnAwake = false;
        ambience.loop = true;
        ambience.spatialBlend = 0f;
        ambience.priority = 180;
        ambienceNext = gameObject.AddComponent<AudioSource>();
        ambienceNext.playOnAwake = false;
        ambienceNext.loop = true;
        ambienceNext.spatialBlend = 0f;
        ambienceNext.priority = 180;
        effects = gameObject.AddComponent<AudioSource>();
        effects.playOnAwake = false;
        effects.spatialBlend = 0f;
        effects.priority = 32;
        notes = new AudioClip[4];
        // D, F sharp, A, high D: each puzzle key has a recognisable bell voice.
        float[] frequencies = { 293.6648f, 369.9944f, 440f, 587.3295f };
        for (int i = 0; i < notes.Length; i++) notes[i] = Bell("Aether tone " + (i + 1), frequencies[i], 1.7f);
        solvedClip = MakeResolution();
        stepClip = MakeStep();
        clickClip = Bell("Aether interface", 880f, 0.095f);
        regionalAmbience = new AudioClip[9];
        for (int region = 0; region < regionalAmbience.Length; region++) regionalAmbience[region] = MakeAmbience(region);
        ambience.clip = regionalAmbience[0];
        SetVolume(masterVolume);
        ambience.Play();
    }

    public void SetVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        ApplyAmbienceLevels();
        if (effects != null) effects.volume = masterVolume * 0.65f;
    }

    private void Update()
    {
        AetherGame game = AetherGame.I;
        if (game == null || game.Player == null || regionalAmbience == null) return;
        float position = game.Player.transform.position.x;
        bool southern = game.Player.transform.position.z < -26f;
        bool horizon = game.Player.transform.position.z < -76f;
        int region = game.Phase == GamePhase.Menu ? 0 : horizon ? (position < -22f ? 6 : position > 22f ? 8 : 7) : southern ? (position < -22f ? 3 : position > 22f ? 5 : 4) : position < -22f ? 1 : position > 22f ? 2 : 0;
        if (!crossFading && region != currentRegion)
        {
            nextRegion = region;
            regionBlend = 0f;
            crossFading = true;
            ambienceNext.clip = regionalAmbience[region];
            ambienceNext.volume = 0;
            ambienceNext.Play();
        }
        if (!crossFading) return;
        regionBlend = Mathf.Clamp01(regionBlend + Time.unscaledDeltaTime / 4.5f);
        ApplyAmbienceLevels();
        if (regionBlend >= 1f)
        {
            ambience.Stop();
            AudioSource previous = ambience;
            ambience = ambienceNext;
            ambienceNext = previous;
            currentRegion = nextRegion;
            crossFading = false;
            ApplyAmbienceLevels();
        }
    }

    private void ApplyAmbienceLevels()
    {
        if (ambience != null) ambience.volume = masterVolume * 0.34f * (crossFading ? Mathf.Sqrt(1f - regionBlend) : 1f);
        if (ambienceNext != null) ambienceNext.volume = crossFading ? masterVolume * 0.34f * Mathf.Sqrt(regionBlend) : 0f;
    }

    public void Chime(int tone = 0)
    {
        if (effects != null && notes != null) effects.PlayOneShot(notes[Mathf.Clamp(tone, 0, 3)], 0.64f);
    }

    public void Solve()
    {
        if (effects != null) effects.PlayOneShot(solvedClip, 0.74f);
    }

    public void Step()
    {
        if (effects == null || Time.unscaledTime < nextStep) return;
        nextStep = Time.unscaledTime + 0.20f;
        effects.PlayOneShot(stepClip, 0.20f);
    }

    public void Click()
    {
        if (effects != null) effects.PlayOneShot(clickClip, 0.22f);
    }

    private static AudioClip Bell(string label, float frequency, float duration)
    {
        int count = Mathf.CeilToInt(duration * SampleRate);
        float[] data = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            float attack = Mathf.Clamp01(t / 0.008f);
            float fadeOut = Mathf.Clamp01((duration - t) / 0.07f);
            float body = Mathf.Sin(t * frequency * Mathf.PI * 2f) * Mathf.Exp(-t * 3.4f);
            float overtone = Mathf.Sin(t * frequency * 2.005f * Mathf.PI * 2f) * Mathf.Exp(-t * 5.1f) * 0.26f;
            float shimmer = Mathf.Sin(t * frequency * 3.999f * Mathf.PI * 2f) * Mathf.Exp(-t * 9f) * 0.09f;
            data[i] = (body + overtone + shimmer) * attack * fadeOut * 0.45f;
        }
        AudioClip clip = AudioClip.Create(label, count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip MakeAmbience(int region)
    {
        const float duration = 16f;
        int count = (int)(duration * SampleRate);
        float[] data = new float[count * 2];
        // Integer cycles make the loop sample-continuous, including modulation.
        float[][] harmonies = {
            new float[] { 73.4162f, 110f, 146.8324f, 184.9972f, 220f, 329.6276f },
            new float[] { 73.4162f, 110f, 146.8324f, 174.6141f, 220f, 293.6648f },
            new float[] { 82.4069f, 123.4708f, 164.8138f, 207.6523f, 246.9416f, 369.9944f },
            new float[] { 65.4064f, 98f, 130.8128f, 164.8138f, 196f, 261.6256f },
            new float[] { 98f, 146.8324f, 196f, 233.0819f, 293.6648f, 440f },
            new float[] { 110f, 164.8138f, 220f, 277.1826f, 329.6276f, 493.8833f },
            new float[] { 82.4069f, 123.4708f, 164.8138f, 196f, 246.9416f, 392f },
            new float[] { 61.7354f, 92.4986f, 123.4708f, 146.8324f, 184.9972f, 277.1826f },
            new float[] { 87.3071f, 130.8128f, 174.6141f, 220f, 261.6256f, 392f }
        };
        float[] hz = harmonies[Mathf.Clamp(region, 0, harmonies.Length - 1)];
        float[] gain = { 0.18f, 0.11f, 0.10f, 0.055f, 0.037f, 0.025f };
        for (int n = 0; n < hz.Length; n++) hz[n] = Mathf.Round(hz[n] * duration) / duration;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            float cycle = t * Mathf.PI * 2f / duration;
            float left = 0f, right = 0f;
            for (int n = 0; n < hz.Length; n++)
            {
                float breathing = 0.70f + 0.15f * Mathf.Sin(cycle * (n % 2 + 1) + n * 1.7f);
                float phase = t * hz[n] * Mathf.PI * 2f;
                left += Mathf.Sin(phase + 0.022f * Mathf.Sin(cycle + n)) * gain[n] * breathing;
                right += Mathf.Sin(phase + 0.025f * Mathf.Sin(cycle + n + 0.7f)) * gain[n] * breathing;
            }
            // Quiet upper air; no percussive beat, leaving space for exploration.
            float airFrequency = region == 1 ? 293.6875f : region == 2 ? 494f : region == 3 ? 523.25f : region == 4 ? 587.3125f : region == 5 ? 659.25f : region == 6 ? 784f : region == 7 ? 369.9375f : region == 8 ? 698.4375f : 440f;
            float air = Mathf.Sin(t * airFrequency * Mathf.PI * 2f) * 0.009f * (0.5f + 0.5f * Mathf.Sin(cycle));
            data[i * 2] = left + air;
            data[i * 2 + 1] = right - air;
        }
        string[] titles = { "The sleeping garden", "The lunar archive", "The solar observatory", "The rain arboretum", "The tidal cloister", "The wind atelier", "The frost conservatory", "The starlit cavern", "The dawn sanctuary" };
        AudioClip clip = AudioClip.Create("Aether - " + titles[region], count, 2, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip MakeResolution()
    {
        const float duration = 3.5f;
        int count = (int)(duration * SampleRate);
        float[] data = new float[count];
        float[] hz = { 293.6648f, 369.9944f, 440f, 587.3295f, 880f };
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            float value = 0f;
            for (int n = 0; n < hz.Length; n++)
            {
                float local = t - n * 0.14f;
                if (local < 0f) continue;
                float envelope = Mathf.Clamp01(local / 0.012f) * Mathf.Exp(-local * 1.65f);
                value += (Mathf.Sin(local * hz[n] * Mathf.PI * 2f) + 0.15f * Mathf.Sin(local * hz[n] * 2f * Mathf.PI * 2f)) * envelope * 0.14f;
            }
            data[i] = value * Mathf.Clamp01((duration - t) / 0.15f);
        }
        AudioClip clip = AudioClip.Create("Aether - A beacon awakens", count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip MakeStep()
    {
        const float duration = 0.16f;
        int count = (int)(duration * SampleRate);
        float[] data = new float[count];
        System.Random random = new System.Random(7319);
        float filtered = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            filtered = filtered * 0.86f + ((float)random.NextDouble() * 2f - 1f) * 0.14f;
            float envelope = Mathf.Clamp01(t / 0.004f) * Mathf.Exp(-t * 31f) * Mathf.Clamp01((duration - t) / 0.03f);
            data[i] = (filtered * 0.62f + Mathf.Sin(t * 87f * Mathf.PI * 2f) * 0.23f) * envelope;
        }
        AudioClip clip = AudioClip.Create("Aether stone footstep", count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private void OnDestroy()
    {
        if (I == this) I = null;
        if (notes != null) foreach (AudioClip clip in notes) if (clip != null) Destroy(clip);
        if (regionalAmbience != null) foreach (AudioClip clip in regionalAmbience) if (clip != null) Destroy(clip);
        if (solvedClip != null) Destroy(solvedClip);
        if (stepClip != null) Destroy(stepClip);
        if (clickClip != null) Destroy(clickClip);
    }
}

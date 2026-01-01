using System;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]

public class GroupMixFadingQuantized : MonoBehaviour
{
    public double bpm = 134.0F;
    public int signatureHi = 4;
    public int signatureLo = 4;

    public AudioSource[] baseLayers;
    public AudioSource[] additionalLayers;
    public AudioClip sourceClip;
    public AudioMixer MainMixer;
    private string exposedMixerParameter = "AdditionalLayersGroupVolume";
    public float additionalLayerVolume;
    private int toggle = 0;

    private double nextTick = 0.0F;
    private double sampleRate = 0.0F;
    private int accent;
    private bool running = false;
    private double beatLength;
    private double barLength;
    private int barCount;
    private double nextBar;
    private double nextBeat;
    private double timeToNextBar;
    private double timeToNextBeat;

    private double clipDuration;
    private double clipDurationInBars;
    private double startTime;
    private double nextStartTime;
    private bool queueNextTrack;

    void Start()
    {
        accent = signatureHi;
        beatLength = 60d / bpm;
        barLength = beatLength * signatureLo;
        barCount = 1;
        double startTick = AudioSettings.dspTime + 0.5;
        sampleRate = AudioSettings.outputSampleRate;
        nextTick = startTick * sampleRate;
        running = true;

        clipDuration = (double)sourceClip.samples / sourceClip.frequency;
        clipDurationInBars = clipDuration / barLength;

        startTime = AudioSettings.dspTime + 0.5f;
        nextStartTime = startTime + clipDuration;
        for (int i = 0; i < baseLayers.Length / 2; i++)
        {
            baseLayers[i].PlayScheduled(startTime);
        }
        for (int i = 0; i < additionalLayers.Length / 2; i++)
        {
            additionalLayers[i].PlayScheduled(startTime);
        }
        additionalLayerVolume = 0;
        MainMixer.SetFloat(exposedMixerParameter, -80.0f);
        toggle = 1 - toggle;
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!running)
            return;

        double samplesPerTick = sampleRate * 60.0F / bpm * 4.0F / signatureLo;
        double sample = AudioSettings.dspTime * sampleRate;
        int dataLen = data.Length / channels;

        int n = 0;
        while (n < dataLen)
        {
            while (sample + n >= nextTick)
            {
                nextTick += samplesPerTick;
                nextBeat = nextTick / sampleRate;
                //Debug.Log("Current time is " + AudioSettings.dspTime);
                //Debug.Log("Next tick is at " + nextTick / sampleRate);
                if (++accent > signatureHi)
                {
                    accent = 1;
                    Debug.Log("Bar " + barCount);
                    barCount++;
                    nextBar = AudioSettings.dspTime + barLength;
                    if (barCount > (int)clipDurationInBars)
                    {
                        nextStartTime = AudioSettings.dspTime + barLength;
                        queueNextTrack = true;
                        barCount = 1;
                    }
                }
                Debug.Log("Tick: " + accent + "/" + signatureHi);
            }
            n++;
        }
    }

    private void Update()
    {
        if (queueNextTrack)
        {
            // Schedule the next audio sources to play
            switch (toggle)
            {
                case 0:
                    //schedule base layers
                    for (int i = 0; i < baseLayers.Length / 2; i++)
                    {
                        baseLayers[i].PlayScheduled(nextStartTime);
                    }
                    //schedule additional layers and set volume
                    for (int i = 0; i < additionalLayers.Length / 2; i++)
                    {
                        additionalLayers[i].PlayScheduled(nextStartTime);
                    }
                    break;
                case 1:
                    //schedule base layers
                    for (int i = baseLayers.Length / 2; i < baseLayers.Length; i++)
                    {
                        baseLayers[i].PlayScheduled(nextStartTime);
                    }
                    //schedule additional layers and set volume
                    for (int i = additionalLayers.Length / 2; i < additionalLayers.Length; i++)
                    {
                        additionalLayers[i].PlayScheduled(nextStartTime);
                    }
                    break;
            }
            toggle = 1 - toggle;
            queueNextTrack = false;
        }

        //quantized fade-in/out of additional layer to next bar using o key
        if (Input.GetKeyDown("o"))
        {
            timeToNextBar = nextBar - AudioSettings.dspTime;

            if (additionalLayerVolume > 0)
            {
                additionalLayerVolume = 0;
                StartCoroutine(FadeMixerGroupQuantized.StartFadeQuantized(MainMixer, exposedMixerParameter, 1.0f, additionalLayerVolume, (float)timeToNextBar));
            }
            else
            {
                additionalLayerVolume = 1;
                StartCoroutine(FadeMixerGroupQuantized.StartFadeQuantized(MainMixer, exposedMixerParameter, 1.0f, additionalLayerVolume, (float)timeToNextBar));
            }
        }

        //quantized fade-in/out of additional layer to next beat using p key
        if (Input.GetKeyDown("p"))
        {
            timeToNextBeat = nextBeat - AudioSettings.dspTime;

            if (additionalLayerVolume > 0)
            {
                additionalLayerVolume = 0;
                StartCoroutine(FadeMixerGroupQuantized.StartFadeQuantized(MainMixer, exposedMixerParameter, 0.5f, additionalLayerVolume, (float)timeToNextBeat));
            }
            else
            {
                additionalLayerVolume = 1;
                StartCoroutine(FadeMixerGroupQuantized.StartFadeQuantized(MainMixer, exposedMixerParameter, 0.5f, additionalLayerVolume, (float)timeToNextBeat));
            }
        }
    }
}

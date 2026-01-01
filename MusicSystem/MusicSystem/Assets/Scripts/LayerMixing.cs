using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]

public class LayerMixing : MonoBehaviour
{
    public double bpm = 134.0F;
    public int signatureHi = 4;
    public int signatureLo = 4;

    public AudioSource[] baseLayers;
    public AudioSource[] additionalLayers;
    public AudioClip sourceClip;
    public float additionalLayerVolume;
    private int toggle = 0;

    private double nextTick = 0.0F;
    private double sampleRate = 0.0F;
    private int accent;
    private bool running = false;
    private double beatLength;
    private double barLength;
    private int barCount;

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
        Debug.Log("Clip Duration in Bars is " + clipDurationInBars);

        startTime = AudioSettings.dspTime + 0.5f;
        nextStartTime = startTime + clipDuration;
        for (int i = 0; i < baseLayers.Length / 2; i++)
        {
            baseLayers[i].PlayScheduled(startTime);
            Debug.Log("Play scheduled for track " + i);
        }
        for (int i = 0; i < additionalLayers.Length / 2; i++)
        {
            additionalLayers[i].PlayScheduled(startTime);
            additionalLayers[i].volume = 0;
            Debug.Log("Play scheduled for track " + i);
        }
        additionalLayerVolume = 0;
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
                if (++accent > signatureHi)
                {
                    accent = 1;
                    Debug.Log("Bar " + barCount);
                    barCount++;
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
                        Debug.Log("Play scheduled for track " + i);
                    }
                    //schedule additional layers and set volume
                    for (int i = 0; i < additionalLayers.Length / 2; i++)
                    {
                        additionalLayers[i].PlayScheduled(nextStartTime);
                        additionalLayers[i].volume = additionalLayerVolume;
                        Debug.Log("Play scheduled for track " + i);
                    }
                    break;
                case 1:
                    //schedule base layers
                    for (int i = baseLayers.Length / 2; i < baseLayers.Length; i++)
                    {
                        baseLayers[i].PlayScheduled(nextStartTime);
                        Debug.Log("Play scheduled for track " + i);
                    }
                    //schedule additional layers and set volume
                    for (int i = additionalLayers.Length / 2; i < additionalLayers.Length; i++)
                    {
                        additionalLayers[i].PlayScheduled(nextStartTime);
                        additionalLayers[i].volume = additionalLayerVolume;
                        Debug.Log("Play scheduled for track " + i);
                    }
                    break;
            }
            toggle = 1 - toggle;
            queueNextTrack = false;
        }

        if (Input.GetKeyDown("o"))
        {
            if (additionalLayerVolume > 0)
            {
                additionalLayerVolume = 0;
                for (int i = 0; i < additionalLayers.Length; i++)
                {
                    additionalLayers[i].volume = additionalLayerVolume;
                }
            }
            else
            {
                additionalLayerVolume = 1;
                for (int i = 0; i < additionalLayers.Length; i++)
                {
                    additionalLayers[i].volume = additionalLayerVolume;
                }
            }
        }
    }
}

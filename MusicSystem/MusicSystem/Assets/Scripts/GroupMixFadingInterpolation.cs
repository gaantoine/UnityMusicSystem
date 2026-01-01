using System;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]

public class GroupMixFadingInterpolation : MonoBehaviour
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
    private float additionalLayerVolumeTarget = 0.0f;
    private float currentHealth = 1.0f;
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

    UnityEvent healthEvent;

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
        //Debug.Log("Clip Duration in Bars is " + clipDurationInBars);

        startTime = AudioSettings.dspTime + 0.5f;
        nextStartTime = startTime + clipDuration;

        additionalLayerVolume = 0;
        MainMixer.SetFloat(exposedMixerParameter, -80.0f);

        for (int i = 0; i < baseLayers.Length / 2; i++)
        {
            baseLayers[i].PlayScheduled(startTime);
        }
        for (int i = 0; i < additionalLayers.Length / 2; i++)
        {
            additionalLayers[i].PlayScheduled(startTime);
        }

        toggle = 1 - toggle;

        if (healthEvent == null)
        {
            healthEvent = new UnityEvent();
        }
        healthEvent.AddListener(SetAudioLevelByHealthValue);
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
                    //Debug.Log("Bar " + barCount);
                    barCount++;
                    if (barCount > (int)clipDurationInBars)
                    {
                        nextStartTime = AudioSettings.dspTime + barLength;
                        queueNextTrack = true;
                        barCount = 1;
                    }
                }
                //Debug.Log("Tick: " + accent + "/" + signatureHi);
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
                    //schedule additional layers
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
                    //schedule additional layers
                    for (int i = additionalLayers.Length / 2; i < additionalLayers.Length; i++)
                    {
                        additionalLayers[i].PlayScheduled(nextStartTime);
                    }
                    break;
            }
            toggle = 1 - toggle;
            queueNextTrack = false;
        }

        /*every frame we'll interpolate the value of additionalLayervolume towards the value of additionalLayerVolumeTarget*/
        additionalLayerVolume = Mathf.Clamp(Mathf.MoveTowards(additionalLayerVolume, additionalLayerVolumeTarget, 0.35f * Time.deltaTime), 0.0001f, 1.0f);
        /*then we'll use the value of additionalLayerVolume in some maths to set the fader level for the additionalLayerMixerGroup*/
        MainMixer.SetFloat(exposedMixerParameter, Mathf.Log10(additionalLayerVolume) * 20);


    }

    //function to modify the value of currenthealth by an input change amount
    public void modifyHealth(float inputChangeAmount)
    {
        currentHealth += inputChangeAmount;
        //clamp the value of currentHealth between 0 and 1
        currentHealth = Mathf.Clamp01(currentHealth);
        /*an event is probably overkill here, but I coded it this way to better fall in line with
         perceived best practices*/
        healthEvent.Invoke();
    }

    private void SetAudioLevelByHealthValue()
    {
        if (currentHealth <= 0.2)
        {
            additionalLayerVolumeTarget = 1.0f;
        }
        else if (currentHealth <= 0.5f)
        {
            additionalLayerVolumeTarget = 0.5f;
        }
        else
        {
            additionalLayerVolumeTarget = 0.0f;
        }
    }
}

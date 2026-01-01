using System;
using System.Collections;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]

public class GroupMixFadingInterpolationImplementation : MonoBehaviour
{
    public double bpm = 95.0F;
    public int signatureHi = 4;
    public int signatureLo = 4;

    public GameObject interpText;
    public GameObject healthPlus;
    public GameObject healthMinus;
    public GameObject healthPlus1Parent;
    public GameObject healthPlus2Parent;
    public GameObject healthPlus3Parent;
    public GameObject healthMinus1Parent;
    public GameObject healthMinus2Parent;
    public GameObject healthMinus3Parent;
    public AudioSource[] baseLayers;
    public AudioSource[] additionalLayers;
    public AudioClip sourceClip;
    public AudioMixer MainMixer;
    private string exposedMixerParameter = "AdditionalLayersGroupVolume";
    private string interpGroupVolume = "InterpGroupVolume";
    public float additionalLayerVolume;
    private float additionalLayerVolumeTarget = 0.0f;
    private float currentHealth = 1.0f;
    private int healthPlusCount = 0;
    private int healthMinusCount = 0;
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

    UnityEvent<float> healthEvent;

    void Start()
    {
        accent = signatureHi;
        beatLength = 60d / bpm;
        barLength = beatLength * signatureLo;
        sampleRate = AudioSettings.outputSampleRate;

        clipDuration = (double)sourceClip.samples / sourceClip.frequency;
        clipDurationInBars = Mathf.RoundToInt((float)clipDuration / (float)barLength);

        additionalLayerVolume = 0;
        MainMixer.SetFloat(exposedMixerParameter, -80.0f);

        if (healthEvent == null)
        {
            healthEvent = new UnityEvent<float>();
        }
        healthEvent.AddListener(SetAudioLevelByHealthValue);
        healthEvent.AddListener(checkPickupInventories);
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
        healthEvent.Invoke(inputChangeAmount);
    }

    private void SetAudioLevelByHealthValue(float inputChangeAmount)
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

    private void checkPickupInventories(float inputChangeAmount)
    {
        //if we picked up a healthPlus object
        if (inputChangeAmount > 0)
        {
            healthPlusCount++;
            if (healthPlusCount % 3 == 0)
            {
                StartCoroutine(InstantiateHealthPlusPickups());
            }
        }//if we picked up a healthMinus object
        else if (inputChangeAmount < 0)
        {
            healthMinusCount++;
            if (healthMinusCount % 3 == 0)
            {
                StartCoroutine(InstantiateHealthMinusPickups());
            }
        }
    }

    private IEnumerator InstantiateHealthPlusPickups()
    {
        yield return new WaitForSeconds(3.0f);
        Instantiate(healthPlus, healthPlus1Parent.transform.position, healthPlus1Parent.transform.rotation);
        Instantiate(healthPlus, healthPlus2Parent.transform.position, healthPlus2Parent.transform.rotation);
        Instantiate(healthPlus, healthPlus3Parent.transform.position, healthPlus3Parent.transform.rotation);
    }

    private IEnumerator InstantiateHealthMinusPickups()
    {
        yield return new WaitForSeconds(3.0f);
        Instantiate(healthMinus, healthMinus1Parent.transform.position, healthMinus1Parent.transform.rotation);
        Instantiate(healthMinus, healthMinus2Parent.transform.position, healthMinus2Parent.transform.rotation);
        Instantiate(healthMinus, healthMinus3Parent.transform.position, healthMinus3Parent.transform.rotation);
    }

    private void OnTriggerEnter(Collider other)
    {
        interpText.SetActive(true);
        accent = signatureHi;
        barCount = 1;
        double startTick = AudioSettings.dspTime + 0.5;
        startTime = AudioSettings.dspTime + 0.5f;
        nextStartTime = startTime + clipDuration;
        nextTick = startTick * sampleRate;
        running = true;

        for (int i = 0; i < baseLayers.Length / 2; i++)
        {
            baseLayers[i].PlayScheduled(startTime);
        }
        for (int i = 0; i < additionalLayers.Length / 2; i++)
        {
            additionalLayers[i].PlayScheduled(startTime);
        }
        MainMixer.SetFloat(interpGroupVolume, 0.0f);
        toggle = 1 - toggle;
    }

    private void OnTriggerExit(Collider other)
    {
        StartCoroutine(FadeMixerGroup.StartFade(MainMixer, interpGroupVolume, 1.0f, 0.0f));
        for (int i = 0; i < baseLayers.Length; i++)
        {
            if (baseLayers[i].isPlaying)
            {
                StartCoroutine(StopSoundQuantized(1.0f, baseLayers[i]));
            }
        }
        for (int i = 0; i < additionalLayers.Length; i++)
        {
            if (additionalLayers[i].isPlaying)
            {
                StartCoroutine(StopSoundQuantized(1.0f, additionalLayers[i]));
            }
        }
        running = false;
        interpText.SetActive(false);
    }

    private static IEnumerator StopSoundQuantized(float timeToWait, AudioSource soundToStop)
    {
        yield return new WaitForSeconds(timeToWait);
        soundToStop.Stop();
    }
}

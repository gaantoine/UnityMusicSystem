using System;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]

public class GroupMixFadingVariablesImplementation : MonoBehaviour
{
    public double bpm = 134.0F;
    public int signatureHi = 4;
    public int signatureLo = 4;

    public AudioSource[] percussionLayer;
    public AudioSource[] woodwindLayer;
    public AudioSource[] brassLayer;
    public AudioSource[] stringLayer;
    public AudioClip sourceClip;
    public AudioMixer MainMixer;
    private string PercussionLayerGroupVolume = "PercussionLayerGroupVolume";
    private string WoodwindLayerGroupVolume = "WoodwindLayerGroupVolume";
    private string BrassLayerGroupVolume = "BrassLayerGroupVolume";
    private string StringLayerGroupVolume = "StringLayerGroupVolume";
    public float percussionLayerVolume;
    public float woodwindLayerVolume;
    public float brassLayerVolume;
    public float stringLayerVolume;
    private float distance;
    public GameObject distanceText;
    public GameObject player;
    public GameObject AOI;
    public float percMinDistance;
    public float percMaxDistance;
    public float wwMinDistance;
    public float wwMaxDistance;
    public float brassMinDistance;
    public float brassMaxDistance;
    public float stringMinDistance;
    public float stringMaxDistance;
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
    private bool trackingDistance = false;

    void Start()
    {
        beatLength = 60d / bpm;
        barLength = beatLength * signatureLo;
        sampleRate = AudioSettings.outputSampleRate;

        clipDuration = (double)sourceClip.samples / sourceClip.frequency;
        clipDurationInBars = Mathf.RoundToInt((float)clipDuration / (float)barLength);

        percussionLayerVolume = 0;
        woodwindLayerVolume = 0;
        brassLayerVolume = 0;
        stringLayerVolume = 0;
        MainMixer.SetFloat(WoodwindLayerGroupVolume, -80.0f);
        MainMixer.SetFloat(BrassLayerGroupVolume, -80.0f);
        MainMixer.SetFloat(StringLayerGroupVolume, -80.0f);

        distance = Vector3.Distance(player.transform.position, AOI.transform.position);
        percMinDistance = 37.5f;
        percMaxDistance = 47.5f;
        wwMinDistance = 27.5f;
        wwMaxDistance = 40.0f;
        brassMinDistance = 17.5f;
        brassMaxDistance = 30.0f;
        stringMinDistance = 10.0f;
        stringMaxDistance = 20.0f;
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
                    //schedule percussion layer
                    for (int i = 0; i < percussionLayer.Length / 2; i++)
                    {
                        percussionLayer[i].PlayScheduled(nextStartTime);
                    }
                    //schedule woodwind layer
                    for (int i = 0; i < woodwindLayer.Length / 2; i++)
                    {
                        woodwindLayer[i].PlayScheduled(nextStartTime);
                    }
                    //schedule brass layer
                    for (int i = 0; i < brassLayer.Length / 2; i++)
                    {
                        brassLayer[i].PlayScheduled(nextStartTime);
                    }
                    //schedule string layer
                    for (int i = 0; i < stringLayer.Length / 2; i++)
                    {
                        stringLayer[i].PlayScheduled(nextStartTime);
                    }
                    break;
                case 1:
                    //schedule percussion layer
                    for (int i = percussionLayer.Length / 2; i < percussionLayer.Length; i++)
                    {
                        percussionLayer[i].PlayScheduled(nextStartTime);
                    }
                    //schedule woodwind layer
                    for (int i = woodwindLayer.Length / 2; i < woodwindLayer.Length; i++)
                    {
                        woodwindLayer[i].PlayScheduled(nextStartTime);
                    }
                    //schedule brass layer
                    for (int i = brassLayer.Length / 2; i < brassLayer.Length; i++)
                    {
                        brassLayer[i].PlayScheduled(nextStartTime);
                    }
                    //schedule string layer
                    for (int i = stringLayer.Length / 2; i < stringLayer.Length; i++)
                    {
                        stringLayer[i].PlayScheduled(nextStartTime);
                    }
                    break;
            }
            toggle = 1 - toggle;
            queueNextTrack = false;
        }
        if (trackingDistance)
        {
            distance = Vector3.Distance(player.transform.position, AOI.transform.position);
            //Debug.Log("Distance is " + distance);
            SetAudioLevelByDistance(distance);
        }
    }

    private void SetAudioLevelByDistance(float inputdistance)
    {
        //first we take the current distance and convert it to a value between 0 and 1 for each layer
        float percCurrentDistance = Mathf.InverseLerp(percMinDistance, percMaxDistance, inputdistance);
        float wwCurrentDistance = Mathf.InverseLerp(wwMinDistance, wwMaxDistance, inputdistance);
        float brassCurrentDistance = Mathf.InverseLerp(brassMinDistance, brassMaxDistance, inputdistance);
        float stringCurrentDistance = Mathf.InverseLerp(stringMinDistance, stringMaxDistance, inputdistance);
        /*then we take the value between 0 and 1 and convert it to a value between 0 and -80
         and assign that value to each layer's volume variable*/
        percussionLayerVolume = Mathf.Lerp(0.0f, -80.0f, percCurrentDistance);
        woodwindLayerVolume = Mathf.Lerp(0.0f, -80.0f, wwCurrentDistance);
        brassLayerVolume = Mathf.Lerp(0.0f, -80.0f, brassCurrentDistance);
        stringLayerVolume = Mathf.Lerp(0.0f, -80.0f, stringCurrentDistance);
        //finally we set the level of each layer according to its layer volume variable
        MainMixer.SetFloat(PercussionLayerGroupVolume, percussionLayerVolume);
        MainMixer.SetFloat(WoodwindLayerGroupVolume, woodwindLayerVolume);
        MainMixer.SetFloat(BrassLayerGroupVolume, brassLayerVolume);
        MainMixer.SetFloat(StringLayerGroupVolume, stringLayerVolume);
    }

    private void OnTriggerEnter(Collider other)
    {
        distanceText.SetActive(true);
        trackingDistance = true;

        accent = signatureHi;
        barCount = 1;
        double startTick = AudioSettings.dspTime + 0.5;
        nextTick = startTick * sampleRate;
        running = true;

        startTime = AudioSettings.dspTime + 0.5f;
        nextStartTime = startTime + clipDuration;
        for (int i = 0; i < percussionLayer.Length / 2; i++)
        {
            percussionLayer[i].PlayScheduled(startTime);
        }
        for (int i = 0; i < woodwindLayer.Length / 2; i++)
        {
            woodwindLayer[i].PlayScheduled(startTime);
        }
        for (int i = 0; i < brassLayer.Length / 2; i++)
        {
            brassLayer[i].PlayScheduled(startTime);
        }
        for (int i = 0; i < stringLayer.Length / 2; i++)
        {
            stringLayer[i].PlayScheduled(startTime);
        }
        toggle = 1 - toggle;
    }

    private void OnTriggerExit(Collider other)
    {
        distanceText.SetActive(false);
        trackingDistance = false;
        running = false;
        percussionLayer[1 - toggle].Stop();
        woodwindLayer[1 - toggle].Stop();
        brassLayer[1 - toggle].Stop();
        stringLayer[1 - toggle].Stop();
    }
}

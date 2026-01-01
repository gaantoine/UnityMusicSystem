using UnityEngine;
using System.Collections;
using UnityEngine.Audio;
using UnityEngine.UIElements;
using System;

[RequireComponent(typeof(AudioSource))]

public class TransitionalRetriggeredLooping : MonoBehaviour
{
    public double bpm = 134.0F;
    public int signatureHi = 4;
    public int signatureLo = 4;

    //public AudioSource[] loopingTrack;
    public AudioSource[] trackOption0;
    public AudioSource[] trackOption1;
    public AudioClip sourceClip;
    public AudioMixer MainMixer;
    private string BaseLayers0GroupVolume = "BaseLayers0Volume";
    private string BaseLayers1GroupVolume = "BaseLayers1Volume";
    public float baseLayers0Volume;
    public float baseLayers1Volume;
    public int currentTrackOption = 0;
    private int toggle = 0;

    private double nextTick = 0.0F;
    private double sampleRate = 0.0F;
    private double samplesPerTick;
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
        trackOption0[toggle].PlayScheduled(startTime);
        baseLayers0Volume = 1;
        baseLayers1Volume = 0;
        MainMixer.SetFloat(BaseLayers0GroupVolume, 0.0f);
        MainMixer.SetFloat(BaseLayers1GroupVolume, -80.0f);
        toggle = 1 - toggle;
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!running)
            return;

        samplesPerTick = sampleRate * 60.0F / bpm * 4.0F / signatureLo;
        double sample = AudioSettings.dspTime * sampleRate;
        int dataLen = data.Length / channels;

        int n = 0;
        while (n < dataLen)
        {
            while (sample + n >= nextTick)
            {
                nextTick += samplesPerTick;
                nextBeat = nextTick / sampleRate;
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
            switch (currentTrackOption) 
            {
                case 0:
                    trackOption0[toggle].PlayScheduled(nextStartTime);
                    toggle = 1 - toggle;
                    queueNextTrack = false;
                    break;
                case 1:
                    trackOption1[toggle].PlayScheduled(nextStartTime);
                    toggle = 1 - toggle;
                    queueNextTrack = false;
                    break;
            }
        }

        //transition from option 0 to option 1 quantized on next bar with keyboard press "o"
        if (Input.GetKeyDown("o"))
        {
            //calculate the time to the next bar
            timeToNextBar = nextBar - AudioSettings.dspTime;
            //set base layer volume to use in quantized fading coroutine
            baseLayers0Volume = 0;
            //start fade coroutine on currently playing music
            StartCoroutine(FadeMixerGroupQuantized.StartFadeQuantized(MainMixer, BaseLayers0GroupVolume, 1.0f, baseLayers0Volume, (float)timeToNextBar));
            //set volume on the base layer 1 group to 0
            MainMixer.SetFloat(BaseLayers1GroupVolume, 0.0f);
            //schedule play for base layer 1 audio source
            trackOption1[toggle].PlayScheduled(nextBar);
            //reset transport using coroutine with callback to set bar count
            StartCoroutine(ResetTransportQuantized((float)timeToNextBar, (newTransportCount) =>
            {
                barCount = newTransportCount;
            }));
            //set track option for looping
            currentTrackOption = 1;
        }

        //transition from option 1 to option 0 quantized on next bar with keyboard press "p"
        if (Input.GetKeyDown("p"))
        {
            //calculate the time to the next bar
            timeToNextBar = nextBar - AudioSettings.dspTime;
            //set base layer volume to use in quantized fading coroutine
            baseLayers1Volume = 0;
            //start fade coroutine on currently playing music
            StartCoroutine(FadeMixerGroupQuantized.StartFadeQuantized(MainMixer, BaseLayers1GroupVolume, 1.0f, baseLayers1Volume, (float)timeToNextBar));
            //set volume on the base layer 1 group to 0
            MainMixer.SetFloat(BaseLayers0GroupVolume, 0.0f);
            //schedule play for base layer 1 audio source
            trackOption0[toggle].PlayScheduled(nextBar);
            //reset transport using coroutine with callback to set bar count
            StartCoroutine(ResetTransportQuantized((float)timeToNextBar, (newTransportCount) =>
            {
                barCount = newTransportCount;
            }));
            //set track option for looping
            currentTrackOption = 0;
        }


    }

    private static IEnumerator ResetTransportQuantized(float timeToWait, Action <int> callback)
    {
        yield return new WaitForSeconds(timeToWait);
        //pass value for bar/beat to set them to 1
        int newTransportCount = 1;
        //pass on values
        callback(newTransportCount);
    }
}

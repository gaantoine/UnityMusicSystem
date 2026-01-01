using UnityEngine;
using System.Collections;
using UnityEngine.Audio;
using UnityEngine.UIElements;
using System;
using System.Transactions;

[RequireComponent(typeof(AudioSource))]

public class TransitionalRetriggeredLoopingwTimeSigBPMChanges : MonoBehaviour
{
    public double bpm = 134.0F;
    public double tempo0 = 134.0f;
    public double tempo1 = 150.0f;
    public int signatureHi0 = 4;
    public int signatureLow0 = 4;
    public int signatureHi1 = 5;
    public int signatureLow1 = 4;
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
        /*if (Input.GetKeyDown("o"))
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
        }*/

        /*transition from option 0 (original tempo and time signature)
         * to option 1 (new tempo and time signature)
         * quantized on next bar with keyboard press "o"*/
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
            /*set new tempo and time signature quantized
             and perform recalculations of associated variables*/
            StartCoroutine(ChangeTempoTimeSignatureQuantized((float)timeToNextBar, tempo1, signatureHi1, signatureLow1, (newTempo, newSignatureHigh, newSignatureLow) =>
            {
                bpm = newTempo;
                signatureHi = newSignatureHigh;
                signatureLo = newSignatureLow;
                accent = signatureHi;
                beatLength = 60d / bpm;
                barLength = beatLength * signatureLo;
                sourceClip = trackOption1[toggle].clip;
                clipDuration = (double)sourceClip.samples / sourceClip.frequency;
                clipDurationInBars = clipDuration / barLength;
                samplesPerTick = sampleRate * 60.0F / bpm * 4.0F / signatureLo;
            }));
            //set track option for looping
            currentTrackOption = 1;
        }

        /*transition from option 1 (new tempo and time signature)
         * to option 0 (original tempo and time signature)
         * quantized on next bar with keyboard press "p"*/
        if (Input.GetKeyDown("p"))
        {
            //calculate the time to the next bar
            timeToNextBar = nextBar - AudioSettings.dspTime;
            //set base layer volume to use in quantized fading coroutine
            baseLayers1Volume = 0;
            //start fade coroutine on currently playing music
            StartCoroutine(FadeMixerGroupQuantized.StartFadeQuantized(MainMixer, BaseLayers0GroupVolume, 1.0f, baseLayers1Volume, (float)timeToNextBar));
            //set volume on the base layer 0 group to 0
            MainMixer.SetFloat(BaseLayers0GroupVolume, 0.0f);
            //schedule play for base layer 0 audio source
            trackOption0[toggle].PlayScheduled(nextBar);
            //reset transport using coroutine with callback to set bar count
            StartCoroutine(ResetTransportQuantized((float)timeToNextBar, (newTransportCount) =>
            {
                barCount = newTransportCount;
            }));
            /*set new tempo and time signature quantized
             and perform recalculations of associated variables*/
            StartCoroutine(ChangeTempoTimeSignatureQuantized((float)timeToNextBar, tempo0, signatureHi0, signatureLow0, (newTempo, newSignatureHigh, newSignatureLow) =>
            {
                bpm = newTempo;
                signatureHi = newSignatureHigh;
                signatureLo = newSignatureLow;
                accent = signatureHi;
                beatLength = 60d / bpm;
                barLength = beatLength * signatureLo;
                sourceClip = trackOption1[toggle].clip;
                clipDuration = (double)sourceClip.samples / sourceClip.frequency;
                clipDurationInBars = clipDuration / barLength;
                samplesPerTick = sampleRate * 60.0F / bpm * 4.0F / signatureLo;
            }));
            //set track option for looping
            currentTrackOption = 0;
        }

        //transition from option 1 to option 0 quantized on next bar with keyboard press "p"
        /*if (Input.GetKeyDown("p"))
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
        }*/


    }

    private static IEnumerator ResetTransportQuantized(float timeToWait, Action<int> callback)
    {
        yield return new WaitForSeconds(timeToWait);
        //pass value for bar/beat to set them to 1
        int newTransportCount = 1;
        //pass on values
        callback(newTransportCount);
    }

    //changes tempo after specified amount of delay and calls back new tempo
    private static IEnumerator ChangeTempoQuantized(float timeToWait, double inputTempo, Action<double> callback)
    {
        yield return new WaitForSeconds(timeToWait);
        //pass value for new tempo
        double newTempo = inputTempo;
        //pass on value in callback
        callback(newTempo);
    }

    //changes time signature after specified amount of delay and calls back new time signature
    private static IEnumerator ChangeTimeSignatureQuantized(float timeToWait, int inputSignatureHigh, int inputSignatureLow, Action<int, int> callback)
    {
        yield return new WaitForSeconds(timeToWait);
        //pass values for new time signature
        int newSignatureHigh = inputSignatureHigh;
        int newSignatureLow = inputSignatureLow;
        //pass on values in callback
        callback(newSignatureHigh, newSignatureLow);
    }

    //changes tempo and time signature together after specified amount of delay and calls back new tempo and time signature
    private static IEnumerator ChangeTempoTimeSignatureQuantized(float timeToWait, double inputTempo, int inputSignatureHigh, int inputSignatureLow, Action <double, int, int> callback)
    {
        yield return new WaitForSeconds(timeToWait);
        //pass value for new tempo
        double newTempo = inputTempo;
        //pass values for new time signature
        int newSignatureHigh = inputSignatureHigh;
        int newSignatureLow = inputSignatureLow;
        //pass on values in callback
        callback(newTempo, newSignatureHigh, newSignatureLow);
    }
}

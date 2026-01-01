using UnityEngine;
using System.Collections;
using UnityEngine.Audio;
using UnityEngine.UIElements;
using System;
using System.Transactions;

[RequireComponent(typeof(AudioSource))]

public class TransitionalRetriggeredLoopingwTimeSigBPMChangesImplementation : MonoBehaviour
{
    public double bpm = 134.0F;
    public double tempo0 = 134.0f;
    public double tempo1 = 88.0f;
    public int signatureHi0 = 4;
    public int signatureLow0 = 4;
    public int signatureHi1 = 3;
    public int signatureLow1 = 4;
    public int signatureHi = 4;
    public int signatureLo = 4;

    //public AudioSource[] loopingTrack;
    public GameObject arenaText;
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
    private bool transitionInProgress = false;

    void Start()
    {
        accent = signatureHi;
        beatLength = 60d / bpm;
        barLength = beatLength * signatureHi;
        barCount = 1;
        sampleRate = AudioSettings.outputSampleRate;

        clipDuration = (double)sourceClip.samples / sourceClip.frequency;
        clipDurationInBars = Mathf.RoundToInt((float)clipDuration / (float)barLength);
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
                    Debug.Log("Bar " + barCount + ", Time " + AudioSettings.dspTime);
                    barCount++;
                    nextBar = AudioSettings.dspTime + barLength;
                    if (barCount > (int)clipDurationInBars)
                    {
                        nextStartTime = AudioSettings.dspTime + barLength;
                        queueNextTrack = true;
                        barCount = 1;
                    }
                }
                Debug.Log("Tick: " + accent + "/" + signatureHi + " at " + AudioSettings.dspTime);
            }
            n++;
        }
    }

    private void Update()
    {
        if (queueNextTrack && !transitionInProgress)
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
        Debug.Log("Coroutine started at " + AudioSettings.dspTime);
        Debug.Log("Coroutine scheduled to execute at " + (AudioSettings.dspTime + timeToWait));
        yield return new WaitForSeconds(timeToWait);
        Debug.Log("Coroutine started actual execution at " + AudioSettings.dspTime);
        //pass value for new tempo
        double newTempo = inputTempo;
        //pass values for new time signature
        int newSignatureHigh = inputSignatureHigh;
        int newSignatureLow = inputSignatureLow;
        //pass on values in callback
        callback(newTempo, newSignatureHigh, newSignatureLow);
    }

    private static IEnumerator StopSoundQuantized(float timeToWait, AudioSource soundToStop)
    {
        yield return new WaitForSeconds(timeToWait);
        soundToStop.Stop();
    }

    /*transition from option 0 (original tempo and time signature)
    * to option 1 (new tempo and time signature)
    * quantized on next bar after entering combat area*/
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            Debug.Log("Trigger Enter Event on beat " + accent);
            transitionInProgress = true;
            //calculate the time to the next bar
            timeToNextBar = nextBar - AudioSettings.dspTime;
            Debug.Log("Next bar confirmed as " + (AudioSettings.dspTime + timeToNextBar));
            //set base layer volume to use in quantized fading coroutine
            baseLayers0Volume = 0;
            //start fade coroutine on currently playing music
            StartCoroutine(FadeMixerGroupQuantized.StartFadeQuantized(MainMixer, BaseLayers0GroupVolume, 1.0f, baseLayers0Volume, (float)timeToNextBar));
            //set volume on the base layer 1 group to 0
            MainMixer.SetFloat(BaseLayers1GroupVolume, 0.0f);
            //schedule play for base layer 1 audio source
            trackOption1[toggle].PlayScheduled(nextBar);
            Debug.Log("Play set for " + nextBar);
            if (trackOption1[1 - toggle].isPlaying)
            {
                trackOption1[1 - toggle].Stop();
            }
            //reset transport using coroutine with callback to set bar count
            StartCoroutine(ResetTransportQuantized((float)timeToNextBar, (newTransportCount) =>
            {
                barCount = newTransportCount;
            }));
            /*set new tempo and time signature quantized
             and perform recalculations of associated variables*/
            StartCoroutine(ChangeTempoTimeSignatureQuantized((float)timeToNextBar, tempo1, signatureHi1, signatureLow1, (newTempo, newSignatureHigh, newSignatureLow) =>
            {
                Debug.Log("Transport reset coroutine callback at " + AudioSettings.dspTime);
                bpm = newTempo;
                signatureHi = newSignatureHigh;
                signatureLo = newSignatureLow;
                accent = signatureHi;
                beatLength = 60d / bpm;
                barLength = beatLength * signatureHi;
                sourceClip = trackOption1[toggle].clip;
                clipDuration = (double)sourceClip.samples / sourceClip.frequency;
                clipDurationInBars = 4;  //Mathf.RoundToInt((float)clipDuration / (float)barLength);
                samplesPerTick = sampleRate * 60.0F / bpm * 4.0F / signatureLo;
                //stop currently playing Audio Source in trackOption0
                trackOption0[1 - toggle].Stop();
                nextStartTime = AudioSettings.dspTime + 5000;
                transitionInProgress = false;
            }));
            //set track option for looping
            currentTrackOption = 1;
            /*flip toggle variable to prevent scheduling of currently playing audio source*/
            toggle = 1 - toggle;
        }
    }

    /*transition from option 1 (new tempo and time signature)
    * to option 0 (original tempo and time signature)
    * quantized on next bar after exiting combat area*/
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            Debug.Log("Trigger Exit Event");
            transitionInProgress = true;
            //calculate the time to the next bar
            timeToNextBar = nextBar - AudioSettings.dspTime;
            Debug.Log("Next bar confirmed as " + (AudioSettings.dspTime + timeToNextBar));
            //set base layer volume to use in quantized fading coroutine
            baseLayers1Volume = 0;
            //start fade coroutine on currently playing music
            StartCoroutine(FadeMixerGroupQuantized.StartFadeQuantized(MainMixer, BaseLayers1GroupVolume, 1.0f, baseLayers1Volume, (float)timeToNextBar));
            //set volume on the base layer 0 group to 0
            MainMixer.SetFloat(BaseLayers0GroupVolume, 0.0f);
            //schedule play for base layer 0 audio source
            trackOption0[toggle].PlayScheduled(nextBar);
            Debug.Log("Play set for " + nextBar);
            if (trackOption0[1 - toggle].isPlaying)
            {
                trackOption0[1 - toggle].Stop();
            }
            //reset transport using coroutine with callback to set bar count
            StartCoroutine(ResetTransportQuantized((float)timeToNextBar, (newTransportCount) =>
            {
                barCount = newTransportCount;
            }));
            /*set new tempo and time signature quantized
             and perform recalculations of associated variables*/
            StartCoroutine(ChangeTempoTimeSignatureQuantized((float)timeToNextBar, tempo0, signatureHi0, signatureLow0, (newTempo, newSignatureHigh, newSignatureLow) =>
            {
                Debug.Log("Transport reset coroutine callback at " + AudioSettings.dspTime);
                bpm = newTempo;
                signatureHi = newSignatureHigh;
                signatureLo = newSignatureLow;
                accent = signatureHi;
                beatLength = 60d / bpm;
                barLength = beatLength * signatureHi;
                sourceClip = trackOption0[toggle].clip;
                clipDuration = (double)sourceClip.samples / sourceClip.frequency;
                clipDurationInBars = Mathf.RoundToInt((float)clipDuration / (float)barLength);
                samplesPerTick = sampleRate * 60.0F / bpm * 4.0F / signatureLo;
                //stop currently playing Audio Source in trackOption1
                trackOption1[1 - toggle].Stop();
                nextStartTime = AudioSettings.dspTime + 5000;
                transitionInProgress = false;
            }));
            //set track option for looping
            currentTrackOption = 0;
            /*flip toggle variable to prevent scheduling of currently playing audio source*/
            toggle = 1 - toggle;
        }
    }

    //function to call from external trigger to start transitional music system
    public void activateTransitionalSystem()
    {
        accent = signatureHi;
        barCount = 1;
        double startTick = AudioSettings.dspTime + 0.5;
        nextTick = startTick * sampleRate;
        running = true;
        startTime = AudioSettings.dspTime + 0.5f;
        nextStartTime = startTime + clipDuration;
        trackOption0[toggle].PlayScheduled(startTime);
        if (trackOption0[1 - toggle].isPlaying)
        {
            trackOption0[1 - toggle].Stop();
        }
        baseLayers0Volume = 1;
        baseLayers1Volume = 0;
        MainMixer.SetFloat(BaseLayers0GroupVolume, 0.0f);
        MainMixer.SetFloat(BaseLayers1GroupVolume, -80.0f);
        toggle = 1 - toggle;
        arenaText.SetActive(true);
    }

    //function to call from external trigger to stop transitional music system
    public void deactivateTransitionalSystem()
    {
        baseLayers0Volume = 0;
        baseLayers1Volume = 0;
        running = false;
        if (trackOption0[1 - toggle].isPlaying)
        {
            StartCoroutine(FadeMixerGroup.StartFade(MainMixer, BaseLayers0GroupVolume, 1.0f, baseLayers0Volume));
            StartCoroutine(StopSoundQuantized(1.0f, trackOption0[1 - toggle]));
        }
        else if (trackOption1[1 - toggle].isPlaying)
        {
            StartCoroutine(FadeMixerGroup.StartFade(MainMixer, BaseLayers1GroupVolume, 1.0f, baseLayers1Volume));
            StartCoroutine(StopSoundQuantized(1.0f, trackOption1[1 - toggle]));
        }

        arenaText.SetActive(false);
    }
}

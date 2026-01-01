using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine.UIElements;
using System;
using System.Linq;

[RequireComponent(typeof(AudioSource))]

public class TransitionalRetriggeredLoopingwConditionalStingers : MonoBehaviour
{
    public double bpm = 134.0F;
    public int signatureHi = 4;
    public int signatureLo = 4;

    //public AudioSource[] loopingTrack;
    public AudioSource[] trackOption0;
    public AudioSource[] trackOption1;
    public AudioSource[] beatSpecificStingers;
    public AudioSource[] harmonicStingers;
    public AudioSource unquantizedStinger;
    public AudioSource quantizedStinger;
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
    private int beatCount;
    private int currentBar;
    private double nextBar;
    private double nextBeat;
    private double timeToNextBar;
    private double timeToNextBeat;

    private double clipDuration;
    private double clipDurationInBars;
    private double startTime;
    private double nextStartTime;
    private bool queueNextTrack;
    public bool playConditionalStingers = true;
    public string[] harmonicContent;

    private enum QuantizationBoundary { Bar, Beat };

    void Start()
    {
        accent = signatureHi;
        beatLength = 60d / bpm;
        barLength = beatLength * signatureLo;
        barCount = 1;
        beatCount = 1;
        currentBar = 1;
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
                    beatCount++;
                    currentBar++;
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

        //play unquantized stinger on key press "L"
        if (Input.GetKeyDown("l"))
        {
            unquantizedStinger.Play();
        }

        //play quantized stinger on next beat after key press "k"
        if (Input.GetKeyDown("k"))
        {
            quantizedStinger.PlayScheduled(nextBeat);
        }

        /*play quantized stinger on the next occurrence of a specific beat in a bar
         in this case, I chose the first beat of the bar*/
        if (Input.GetKeyDown("j"))
        {
            PlayQuantizedBarRelative(quantizedStinger, 1, accent);
        }

        /*play quantized stinger on the next quantization boundary (bar or beat)
         that matches the quantization boundary
        e.g. if boundary is beat and multiplier is 2 then the stinger will start on beat 2 or 4
        In this case, I picked beat 3 since that only occurs once per bar in the current context
        and is easy to track*/
        if (Input.GetKeyDown("h"))
        {
            PlayQuantizedTransportRelative(quantizedStinger, 4, accent, QuantizationBoundary.Bar);
        }

        /* condtionally play quantized stinger on next beat after key press "i"
         based on state of playConditionalStingers boolean*/
        if (Input.GetKeyDown("i"))
        {
            if (playConditionalStingers)
            {
                quantizedStinger.PlayScheduled(nextBeat);
            }
        }

        /*play a beat-specific quantized stinger on the next beat after key press "u"*/
        if (Input.GetKeyDown("u"))
        {
            PlayQuantizedBeatSpecificStinger(beatSpecificStingers, accent);
        }

        /*play a quanitzed harmonic stinger on the next beat whose harmonic content matches
         that of the background music on the beat it will be played after key press "y"*/
        if (Input.GetKeyDown("y"))
        {
            PlayQuantizedHarmonicStinger(harmonicStingers, harmonicContent, accent);
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

    //this function will play an audio source on a specified beat, requiring the current beat as input
    private void PlayQuantizedBarRelative(AudioSource soundToPlay, int targetBeat, int currentBeat)
    {
        //calculate timeToNextBeat as we'll need this for calculations
        timeToNextBeat = nextBeat - AudioSettings.dspTime;
        /*if the target beat has already passed for the current bar,
         we need to do some maths to figure out when to schedule play for the audio*/
        if (targetBeat <= currentBeat)
        {
            double targetBeatTime = AudioSettings.dspTime + (((signatureHi - currentBeat + targetBeat) * beatLength) - timeToNextBeat);
            soundToPlay.PlayScheduled(targetBeatTime);
        }
        else
        {
            double targetBeatTime = AudioSettings.dspTime + ((targetBeat - currentBeat) * beatLength) - timeToNextBeat;
            soundToPlay.PlayScheduled(targetBeatTime);
        }
    }

    //this function will play an audio source at a specific point in time relative to the start of the metronome/transport
    private void PlayQuantizedTransportRelative(AudioSource soundToPlay, int targetMultiplier, int currentBeat, QuantizationBoundary inputBoundary)
    {
        if (inputBoundary == QuantizationBoundary.Bar)
        {
            //calculate time to next bar as we'll need this for calculations
            timeToNextBar = nextBar - AudioSettings.dspTime;
            //create variable to hold target bar number
            int targetBarCount = new int();
            //create bool variable for while loop and set to false
            for (int i = currentBar + 1; i < 1000; i++)
            {
                if (i % targetMultiplier == 0)
                {
                    targetBarCount = i;
                    break;
                }
            }
            double targetBarTime = AudioSettings.dspTime + ((targetBarCount - currentBar) * barLength) + timeToNextBar;
            soundToPlay.PlayScheduled(targetBarTime);
        }
        else if (inputBoundary == QuantizationBoundary.Beat)
        {
            //create new list to store potential start points
            List<int> potentialStartPoints = new List<int>();
            /*first we determine which beats satisfy the multiplier requirements
             e.g. if the mult is three, we have to find multiples of three
            within the number of beats contained in each bar*/
            for (int i = 1; i <= signatureHi; i++)
            {
                if (i % targetMultiplier == 0)
                {
                    potentialStartPoints.Add(i);
                    Debug.Log("potential starting point at beat " + i);
                }
            }
            /*re-order list based on which of the potential starting points
             is the nearest one that has not already occurred in the
            current bar; potentialStartPoints{0} should be the answer
            once this operation is finished*/
            int loopCount = potentialStartPoints.Count;
            for (int i = 0; i < loopCount; i++)
            {
                if (potentialStartPoints[i] <= currentBeat)
                {
                    potentialStartPoints.Add(potentialStartPoints[i]);
                    potentialStartPoints.RemoveAt(i);
                }
            }
            //set first item in potentialStartPoints as targetBeat
            int targetBeat = potentialStartPoints.First();
            Debug.Log("Target Beat is " + targetBeat);
            //calculate timeToNextBeat as we'll need this for calculations
            timeToNextBeat = nextBeat - AudioSettings.dspTime;
            /*if the target beat has already passed for the current bar,
             we need to do some maths to figure out when to schedule play for the audio*/
            if (targetBeat <= currentBeat)
            {
                double targetBeatTime = AudioSettings.dspTime + (((signatureHi - currentBeat + targetBeat) * beatLength) - timeToNextBeat);
                soundToPlay.PlayScheduled(targetBeatTime);
            }
            else
            {
                double targetBeatTime = AudioSettings.dspTime + ((targetBeat - currentBeat) * beatLength) - timeToNextBeat;
                soundToPlay.PlayScheduled(targetBeatTime);
            }
        }
    }

    private void PlayQuantizedBeatSpecificStinger(AudioSource[] soundsToPlay, int currentBeat)
    {
        /*use switch to determine which stinger to queue
         note that we are queueing for the beat the stinger
        will start on, which is 1 more than the beat it was
        triggered on*/
        switch ((currentBeat % 4) + 1)
        {
            case 1:
                soundsToPlay[1].PlayScheduled(nextBeat);
                break;
            case 2:
                soundsToPlay[2].PlayScheduled(nextBeat);
                break;
            case 3:
                soundsToPlay[3].PlayScheduled(nextBeat);
                break;
            case 4:
                soundsToPlay[4].PlayScheduled(nextBeat);
                break;
            default:
                break;
        }
    }

    /*function to play a quantized harmonic stinger on the next beat based on the harmonic content of the background
     music, which is represented by the harmonic content array accessed by this function*/
    private void PlayQuantizedHarmonicStinger(AudioSource[] soundsToPlay, string[] harmonicContent, int currentBeat)
    {
        //cycle through the array of audio sources
        foreach (AudioSource sound in soundsToPlay)
        {
            //store the name of the audio source as a string
            string soundName = sound.name.ToString();
            /*check to see if the last character in the name matches the value in harmnonicContent
             corresponding to the next beat*/
            if (soundName[soundName.Length - 1].ToString() == harmonicContent[(beatCount % 16) + 1])
            {
                //if it does, queue that sound and break out of the loop
                sound.PlayScheduled(nextBeat);
                break;
            }
        }
    }
}

using UnityEngine;
using System.Collections;
using UnityEngine.Audio;
using UnityEngine.UIElements;
using System;

[RequireComponent(typeof(AudioSource))]

public class TransitionalRetriggeredLoopingwPreSync : MonoBehaviour
{
    public double bpm = 134.0F;
    public int signatureHi = 4;
    public int signatureLo = 4;

    //public AudioSource[] loopingTrack;
    public AudioSource[] trackOption0;
    //public AudioSource[] trackOption1;
    public AudioSource[] preSync;
    public AudioSource[] endMusic;
    public AudioClip sourceClip;
    public AudioMixer MainMixer;
    private string BaseLayers0GroupVolume = "BaseLayers0Volume";
    //private string BaseLayers1GroupVolume = "BaseLayers1Volume";
    private string EndMusicGroupVolume = "EndMusicVolume";
    private string PreSyncGroupVolume = "PreSyncVolume";
    public float baseLayers0Volume;
    public float baseLayers1Volume;
    public float preSyncVolume;
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
    private bool endMusicTriggered = false;

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
        preSyncVolume = 0;
        MainMixer.SetFloat(BaseLayers0GroupVolume, 0.0f);
        MainMixer.SetFloat(EndMusicGroupVolume, -80.0f);
        MainMixer.SetFloat(PreSyncGroupVolume, -80.0f);
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
            // Schedule the next audio sources to play
            switch (toggle)
            {
                case 0:
                    //schedule base layers
                    for (int i = 0; i < trackOption0.Length / 2; i++)
                    {
                        trackOption0[i].PlayScheduled(nextStartTime);
                    }
                    //schedule additional layers and set volume
                    for (int i = 0; i < preSync.Length / 2; i++)
                    {
                        preSync[i].PlayScheduled(nextStartTime);

                    }
                    break;
                case 1:
                    //schedule base layers
                    for (int i = trackOption0.Length / 2; i < trackOption0.Length; i++)
                    {
                        trackOption0[i].PlayScheduled(nextStartTime);
                    }
                    //schedule additional layers and set volume
                    for (int i = preSync.Length / 2; i < preSync.Length; i++)
                    {
                        preSync[i].PlayScheduled(nextStartTime);
                    }
                    break;
            }
            toggle = 1 - toggle;
            queueNextTrack = false;
        }

        //transition from option 0 to end music quantized on next bar with keyboard press "o"
        if (Input.GetKeyDown("o"))
        {
            if (!endMusicTriggered)
            {
                //calculate the time to the next bar
                timeToNextBar = nextBar - AudioSettings.dspTime;
                //calculate the time to the next beat
                timeToNextBeat = nextBeat - AudioSettings.dspTime;
                //set base layer volume to use in quantized fading coroutine
                baseLayers0Volume = 0;
                //start fade coroutine on currently playing music
                StartCoroutine(FadeMixerGroupQuantized.StartFadeQuantized(MainMixer, BaseLayers0GroupVolume, 1.0f, baseLayers0Volume, (float)timeToNextBar));
                //set volume on the end music group to 0
                MainMixer.SetFloat(EndMusicGroupVolume, 0.0f);
                //schedule play for end music audio source
                for (int i = 0; i < endMusic.Length; i++)
                {
                    endMusic[i].PlayScheduled(nextBar);
                }
                //set pre-sync layer volume to use in quantized fading routine
                preSyncVolume = 1;
                //start fade coroutine on pre-sync layer to start on next beat
                StartCoroutine(FadeMixerGroupQuantized.StartFadeQuantized(MainMixer, PreSyncGroupVolume, 0.5f, preSyncVolume, (float)timeToNextBeat));
                //stop transport using coroutine with callback to set running boolean
                StartCoroutine(StopTransportQuantized((float)timeToNextBar, (isRunning) =>
                {
                    running = isRunning;
                }));
                endMusicTriggered = true;
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

    private static IEnumerator StopTransportQuantized(float timeToWait, Action<bool> callback)
    {
        yield return new WaitForSeconds(timeToWait);
        //pass value for running variable of transport to set it to false
        bool isRunning = false;
        //pass on value
        callback(isRunning);
    }
}

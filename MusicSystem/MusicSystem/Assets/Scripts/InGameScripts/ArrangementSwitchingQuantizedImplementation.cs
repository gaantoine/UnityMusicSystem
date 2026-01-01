using System;
using System.Collections;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]

public class ArrangementSwitchingQuantizedImplementation : MonoBehaviour
{
    public double bpm = 134.0F;
    public int signatureHi = 4;
    public int signatureLo = 4;

    public GameObject mainRoomText;
    public AudioSource[] arrangementA;
    public AudioSource[] arrangementB;
    public AudioSource[] beatSpecificStingers;
    public AudioSource[] harmonicStingers;
    public AudioClip sourceClip;
    public AudioMixer MainMixer;
    private string ArrangementAGroupVolume = "ArrangementAGroupVolume";
    private string ArrangementBGroupVolume = "ArrangementBGroupVolume";
    private string MainRoomGroupVolume = "MainRoomGroupVolume";
    public float arrangementAVolume;
    public float arrangementBVolume;
    private int toggle = 0;

    private double nextTick = 0.0F;
    private double sampleRate = 0.0F;
    private int accent;
    private bool running = false;
    private double beatLength;
    private double barLength;
    private int barCount;
    private int beatCount;
    private double nextBar;
    private double nextBeat;
    private double timeToNextBar;
    private double timeToNextBeat;

    private double clipDuration;
    private double clipDurationInBars;
    private double startTime;
    private double nextStartTime;
    private bool queueNextTrack;
    private bool resettingSystem = false;
    private bool inMainRoom = true;
    public string[] harmonicContent;

    void Start()
    {
        mainRoomText.SetActive(true);
        accent = signatureHi;
        beatLength = 60d / bpm;
        barLength = beatLength * signatureLo;
        barCount = 1;
        beatCount = 1;
        double startTick = AudioSettings.dspTime + 0.5;
        sampleRate = AudioSettings.outputSampleRate;
        nextTick = startTick * sampleRate;
        running = true;

        clipDuration = (double)sourceClip.samples / sourceClip.frequency;
        clipDurationInBars = Mathf.RoundToInt((float)clipDuration / (float)barLength);

        startTime = AudioSettings.dspTime + 0.5f;
        nextStartTime = startTime + clipDuration;
        for (int i = 0; i < arrangementA.Length / 2; i++)
        {
            arrangementA[i].PlayScheduled(startTime);
        }
        for (int i = 0; i < arrangementB.Length / 2; i++)
        {
            arrangementB[i].PlayScheduled(startTime);
        }

        arrangementAVolume = 1;
        arrangementBVolume = 0;
        MainMixer.SetFloat(ArrangementAGroupVolume, 0.0f);
        MainMixer.SetFloat(ArrangementBGroupVolume, -80.0f);
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
                beatCount++;
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
                    for (int i = 0; i < arrangementA.Length / 2; i++)
                    {
                        arrangementA[i].PlayScheduled(nextStartTime);
                        //Debug.Log("Play scheduled for track " + i);
                    }
                    //schedule additional layers and set volume
                    for (int i = 0; i < arrangementB.Length / 2; i++)
                    {
                        arrangementB[i].PlayScheduled(nextStartTime);
                        //Debug.Log("Play scheduled for track " + i);
                    }
                    break;
                case 1:
                    //schedule base layers
                    for (int i = arrangementA.Length / 2; i < arrangementA.Length; i++)
                    {
                        arrangementA[i].PlayScheduled(nextStartTime);
                        //Debug.Log("Play scheduled for track " + i);
                    }
                    //schedule additional layers and set volume
                    for (int i = arrangementB.Length / 2; i < arrangementB.Length; i++)
                    {
                        arrangementB[i].PlayScheduled(nextStartTime);
                        //Debug.Log("Play scheduled for track " + i);
                    }
                    break;
            }
            toggle = 1 - toggle;
            queueNextTrack = false;
        }

        //quantized arrangement switching to next bar using o key
        if (Input.GetKeyDown("o"))
        {

            timeToNextBar = nextBar - AudioSettings.dspTime;

            if (arrangementBVolume > 0)
            {
                arrangementAVolume = 1;
                arrangementBVolume = 0;
                StartCoroutine(FadeMixerGroupQuantized.StartFadeQuantized(MainMixer, ArrangementAGroupVolume, 1.5f, arrangementAVolume, (float)timeToNextBar));
                StartCoroutine(FadeMixerGroupQuantized.StartFadeQuantized(MainMixer, ArrangementBGroupVolume, 1.5f, arrangementBVolume, (float)timeToNextBar));
            }
            else
            {
                arrangementAVolume = 0;
                arrangementBVolume = 1;
                StartCoroutine(FadeMixerGroupQuantized.StartFadeQuantized(MainMixer, ArrangementAGroupVolume, 1.5f, arrangementAVolume, (float)timeToNextBar));
                StartCoroutine(FadeMixerGroupQuantized.StartFadeQuantized(MainMixer, ArrangementBGroupVolume, 1.5f, arrangementBVolume, (float)timeToNextBar));
            }
        }

        //quantized arrangement switching to next beat using p key
        if (Input.GetKeyDown("p"))
        {

            timeToNextBeat = nextBeat - AudioSettings.dspTime;

            if (arrangementBVolume > 0)
            {
                arrangementAVolume = 1;
                arrangementBVolume = 0;
                StartCoroutine(FadeMixerGroupQuantized.StartFadeQuantized(MainMixer, ArrangementAGroupVolume, 1.0f, arrangementAVolume, (float)timeToNextBeat));
                StartCoroutine(FadeMixerGroupQuantized.StartFadeQuantized(MainMixer, ArrangementBGroupVolume, 1.0f, arrangementBVolume, (float)timeToNextBeat));
            }
            else
            {
                arrangementAVolume = 0;
                arrangementBVolume = 1;
                StartCoroutine(FadeMixerGroupQuantized.StartFadeQuantized(MainMixer, ArrangementAGroupVolume, 0.5f, arrangementAVolume, (float)timeToNextBeat));
                StartCoroutine(FadeMixerGroupQuantized.StartFadeQuantized(MainMixer, ArrangementBGroupVolume, 0.5f, arrangementBVolume, (float)timeToNextBeat));
            }
        }

        /*play a beat-specific quantized stinger on the next beat after key press "u"*/
        if (Input.GetKeyDown("u"))
        {
            if (inMainRoom)
            {
                PlayQuantizedBeatSpecificStinger(beatSpecificStingers, accent);
            }
        }

        /*play a quanitzed harmonic stinger on the next beat whose harmonic content matches
         that of the background music on the beat it will be played after key press "y"*/
        if (Input.GetKeyDown("y"))
        {
            if (inMainRoom)
            {
                PlayQuantizedHarmonicStinger(harmonicStingers, harmonicContent, accent);
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
    
    private void OnTriggerEnter(Collider other)
    {
        if (resettingSystem)
        {
            inMainRoom = true;
            mainRoomText.SetActive(true);
            MainMixer.SetFloat(MainRoomGroupVolume, 0.0f);
            accent = signatureHi;
            barCount = 1;
            beatCount = 1;
            double startTick = AudioSettings.dspTime + 0.5;
            nextTick = startTick * sampleRate;
            running = true;

            startTime = AudioSettings.dspTime + 0.5f;
            nextStartTime = startTime + clipDuration;
            for (int i = 0; i < arrangementA.Length / 2; i++)
            {
                arrangementA[i].PlayScheduled(startTime);
            }
            for (int i = 0; i < arrangementB.Length / 2; i++)
            {
                arrangementB[i].PlayScheduled(startTime);
            }
            toggle = 1 - toggle;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        StartCoroutine(FadeMixerGroup.StartFade(MainMixer, MainRoomGroupVolume, 1.0f, 0.0f));
        for (int i = 0; i < arrangementA.Length; i++)
        {
            if (arrangementA[i].isPlaying)
            {
                StartCoroutine(StopSoundQuantized(1.0f, arrangementA[i]));
            }
        }
        for (int i = 0; i < arrangementB.Length; i++)
        {
            if (arrangementB[i].isPlaying)
            {
                StartCoroutine(StopSoundQuantized(1.0f, arrangementB[i]));
            }
        }
        running = false;
        inMainRoom = false;
        mainRoomText.SetActive(false);
        resettingSystem = true;
    }

    private static IEnumerator StopSoundQuantized(float timeToWait, AudioSource soundToStop)
    {
        yield return new WaitForSeconds(timeToWait);
        soundToStop.Stop();
    }
}

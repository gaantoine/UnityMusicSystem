using System;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]

public class ArrangementSwitching : MonoBehaviour
{
    public double bpm = 134.0F;
    public int signatureHi = 4;
    public int signatureLo = 4;

    public AudioSource[] arrangementA;
    public AudioSource[] arrangementB;
    public AudioClip sourceClip;
    public AudioMixer MainMixer;
    private string ArrangementAGroupVolume = "ArrangementAGroupVolume";
    private string ArrangementBGroupVolume = "ArrangementBGroupVolume";
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
        for (int i = 0; i < arrangementA.Length / 2; i++)
        {
            arrangementA[i].PlayScheduled(startTime);
            Debug.Log("Play scheduled for track " + i);
        }
        for (int i = 0; i < arrangementB.Length / 2; i++)
        {
            arrangementB[i].PlayScheduled(startTime);
            Debug.Log("Play scheduled for track " + i);
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
                    for (int i = 0; i < arrangementA.Length / 2; i++)
                    {
                        arrangementA[i].PlayScheduled(nextStartTime);
                        Debug.Log("Play scheduled for track " + i);
                    }
                    //schedule additional layers and set volume
                    for (int i = 0; i < arrangementB.Length / 2; i++)
                    {
                        arrangementB[i].PlayScheduled(nextStartTime);
                        Debug.Log("Play scheduled for track " + i);
                    }
                    break;
                case 1:
                    //schedule base layers
                    for (int i = arrangementA.Length / 2; i < arrangementA.Length; i++)
                    {
                        arrangementA[i].PlayScheduled(nextStartTime);
                        Debug.Log("Play scheduled for track " + i);
                    }
                    //schedule additional layers and set volume
                    for (int i = arrangementB.Length / 2; i < arrangementB.Length; i++)
                    {
                        arrangementB[i].PlayScheduled(nextStartTime);
                        Debug.Log("Play scheduled for track " + i);
                    }
                    break;
            }
            toggle = 1 - toggle;
            queueNextTrack = false;
        }

        if (Input.GetKeyDown("o"))
        {
            if (arrangementBVolume > 0)
            {
                arrangementAVolume = 1;
                arrangementBVolume = 0;
                StartCoroutine(FadeMixerGroup.StartFade(MainMixer, ArrangementAGroupVolume, 1.5f, arrangementAVolume));
                StartCoroutine(FadeMixerGroup.StartFade(MainMixer, ArrangementBGroupVolume, 1.5f, arrangementBVolume));
            }
            else
            {
                arrangementAVolume = 0;
                arrangementBVolume = 1;
                StartCoroutine(FadeMixerGroup.StartFade(MainMixer, ArrangementAGroupVolume, 1.5f, arrangementAVolume));
                StartCoroutine(FadeMixerGroup.StartFade(MainMixer, ArrangementBGroupVolume, 1.5f, arrangementBVolume));
            }
        }
    }
}

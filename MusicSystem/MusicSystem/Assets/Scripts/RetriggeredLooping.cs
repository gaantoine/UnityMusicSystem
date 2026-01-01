using UnityEngine;

[RequireComponent(typeof(AudioSource))]

public class RetriggeredLooping : MonoBehaviour
{
    public double bpm = 134.0F;
    //public float gain = 0.5F;
    public int signatureHi = 4;
    public int signatureLo = 4;

    public AudioSource[] loopingTrack;
    public AudioClip sourceClip;
    private int toggle = 0;

    private double nextTick = 0.0F;
    //private float amp = 0.0F;
    //private float phase = 0.0F;
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
        loopingTrack[toggle].PlayScheduled(startTime);
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
            loopingTrack[toggle].PlayScheduled(nextStartTime);
            toggle = 1 - toggle;
            queueNextTrack = false;
        }
    }
}

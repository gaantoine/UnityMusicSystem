using UnityEngine;

// The code example shows how to implement a metronome that procedurally
// generates the click sounds via the OnAudioFilterRead callback.
// While the game is paused or suspended, this time will not be updated and sounds
// playing will be paused. Therefore developers of music scheduling routines do not have
// to do any rescheduling after the app is unpaused

/*An audio source component is required for the metronome to do anything
 Without it, the OnAudioFilterRead function doesn't have anything to write to*/
[RequireComponent(typeof(AudioSource))]
public class TestMetronome : MonoBehaviour
{
    public double bpm = 140.0F;
    public float gain = 0.5F;
    public int signatureHi = 4;
    public int signatureLo = 4;

    private double nextTick = 0.0F;
    private float amp = 0.0F;
    private float phase = 0.0F;
    private double sampleRate = 0.0F;
    private int accent;
    private bool running = false;
    private double beatLength;
    private double barLength;
    private int barCount;

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
            /*This first section writes values to the audio filter
             The data is created by taking a sine wave function and multiplying it by the gain and amp values*/
            float x = gain * amp * Mathf.Sin(phase);
            int i = 0;
            while (i < channels)
            {
                data[n * channels + i] += x;
                i++;
            }
            /*This section handles the act of tracking the current beat and bar counts
             It also sets a louder volume for the first beat in each bar*/
            while (sample + n >= nextTick)
            {
                nextTick += samplesPerTick;
                amp = 1.0F;
                if (++accent > signatureHi)
                {
                    accent = 1;
                    amp *= 2.0F;
                    Debug.Log("Bar " + barCount);
                    barCount++;
                }
                Debug.Log("Tick: " + accent + "/" + signatureHi);
            }
            phase += amp * 0.3F;
            amp *= 0.993F;
            n++;
        }
    }
}
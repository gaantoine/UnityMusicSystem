using System.Collections;
using UnityEngine;

public class OneTrackLoopingAudioSource : MonoBehaviour
{

    public AudioSource[] loopingTrack;
    public AudioClip sourceClip;
    private int toggle = 0;

    public double bpm = 134.0F;
    public int signatureHi = 4;
    public int signatureLo = 4;

    private double beatLength;
    private double barLength;
    private int barCount;

    private double clipDuration;
    private double startTime;
    private double nextStartTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        beatLength = 60d / bpm;
        barLength = beatLength * signatureLo;
        barCount = 1;
        clipDuration = (double)sourceClip.samples / sourceClip.frequency;

        startTime = AudioSettings.dspTime + 0.5f;
        nextStartTime = startTime + clipDuration;
        loopingTrack[toggle].PlayScheduled(startTime);
        toggle = 1 - toggle;
    }

    // Update is called once per frame
    void Update()
    {
        if (AudioSettings.dspTime > nextStartTime - 1)
        {
            // Schedule the next audio source to play
            loopingTrack[toggle].PlayScheduled(nextStartTime);
            // Update the Next Start Time with a new value
            nextStartTime = nextStartTime + clipDuration;
            // Switches the toggle to use the other Audio Source next
            toggle = 1 - toggle;
            Debug.Log("Queued audio source " + toggle);
        }
    }
}

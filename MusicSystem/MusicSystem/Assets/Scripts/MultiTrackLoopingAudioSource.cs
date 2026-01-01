using System.Collections;
using UnityEngine;

public class MultiTrackLoopingAudioSource : MonoBehaviour
{

    public AudioSource[] loopingMultiTracks;
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
        for (int i = 0; i < loopingMultiTracks.Length / 2; i++)
        {
            loopingMultiTracks[i].PlayScheduled(startTime);
            Debug.Log("Play scheduled for track " + i);
        }
        toggle = 1 - toggle;
    }

    // Update is called once per frame
    void Update()
    {
        if (AudioSettings.dspTime > nextStartTime - 1)
        {
            // Schedule the next audio sources to play
            switch (toggle)
            {
                case 0:
                    for (int i = 0; i < loopingMultiTracks.Length / 2; i++)
                    {
                        loopingMultiTracks[i].PlayScheduled(nextStartTime);
                        Debug.Log("Play scheduled for track " + i);
                    }
                    break;
                case 1:
                    for (int i = loopingMultiTracks.Length / 2; i < loopingMultiTracks.Length; i++)
                    {
                        loopingMultiTracks[i].PlayScheduled(nextStartTime);
                        Debug.Log("Play scheduled for track " + i);
                    }
                    break;
            }
            // Update the Next Start Time with a new value
            nextStartTime = nextStartTime + clipDuration;
            // Switches the toggle to use the other Audio Sources next
            toggle = 1 - toggle;
        }
    }
}

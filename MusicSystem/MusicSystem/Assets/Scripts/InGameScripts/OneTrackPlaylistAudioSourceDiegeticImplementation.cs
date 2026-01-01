using UnityEngine;
using System.Collections;

public class OneTrackPlaylistAudioSourceDiegeticImplementation : MonoBehaviour
{

    public AudioSource[] musicPlaylist;
    public AudioSource[] jukeBoxSounds;
    public float fadeLength = 1.5f;
    private AudioClip currentClip;
    private AudioSource currentTrack;
    private AudioSource previousTrack;
    private AudioSource changeOverSound;
    private AudioClip changeOverClip;
    private double currentClipDuration;
    private double changeOverClipDuration;
    private double startTime;
    private double nextStartTime;
    private double nextChangeOverTime;
    private bool canInteract = false;
    private bool jukeboxActive = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        /*I don't want the jukebox starting when the game starts, so commenting this out
         * for this implementation
        //randomly select an audio source to play and store it as currentTrack
        currentTrack = musicPlaylist[Random.Range(0, musicPlaylist.Length)];
        //store the clip associated with that audio source as currentClip
        currentClip = currentTrack.clip;
        //calculate the duration of currentClip
        currentClipDuration = (double)currentClip.samples / currentClip.frequency;
        //set start time for the first audio source
        startTime = AudioSettings.dspTime + 0.5f;
        //set the start time for the first change over
        nextChangeOverTime = startTime + currentClipDuration;
        //play the current track
        currentTrack.PlayScheduled(startTime);
        //set nextStartTime sufficiently in the future such that it won't trigger before the next changeOver
        nextStartTime = startTime + 5000;
        //Debug.Log("Current Track is " + currentTrack);
        //Debug.Log("Current Clip is " + currentClip);
        //Debug.Log("Current Clip Duration is " + currentClipDuration);
        //Debug.Log("Started jukebox with " + currentTrack);
        */
    }

    // Update is called once per frame
    void Update()
    {
        if (jukeboxActive)
        {
            //check to see if we are close to the next change over time
            if (AudioSettings.dspTime > nextChangeOverTime - 1)
            {
                queueChangeOver(); //if we are then schedule the next change over to play
            }
            //otherwise check to see if we are close to the start of the next track
            else if (AudioSettings.dspTime > nextStartTime - 1)
            {
                queueNextTrack(); //if we are then schedule the next track to play
            }
        }

        if (canInteract && Input.GetKeyDown("e"))
        {
            StartCoroutine(switchTrack());
        }
    }

    void queueChangeOver()
    {
        //randomly select an audio source to store as changeOverSound
        changeOverSound = jukeBoxSounds[Random.Range(0, jukeBoxSounds.Length)];
        //store the clip for that audio source as changeOverClip
        changeOverClip = changeOverSound.clip;
        //calculate the duration of the changeOverClip and store it as changeOverClipDuration
        changeOverClipDuration = (double)changeOverClip.samples / changeOverClip.frequency;
        if (changeOverSound.volume < 1)
        {
            changeOverSound.volume = 1;
        }
        //schedule the changeOverSound to play
        changeOverSound.PlayScheduled(nextChangeOverTime);
        //set the start time for the next music track
        nextStartTime = nextChangeOverTime + changeOverClipDuration;
        //queue nextChangeOver time sufficiently in the future that it won't trigger prematurely
        nextChangeOverTime = AudioSettings.dspTime + 5000;
        //Debug.Log("Queued changeOverSound " + changeOverSound);
    }

    void queueNextTrack()
    {
        //store the track that is close to finishing as previousTrack
        previousTrack = currentTrack;
        //randomly select a track in musicPlaylist to play next
        currentTrack = musicPlaylist[Random.Range(0, musicPlaylist.Length)];
        //if it's the same as the last track, randomly select another track until we are not repeating tracks
        while (currentTrack == previousTrack)
        {
            currentTrack = musicPlaylist[Random.Range(0, musicPlaylist.Length)];
        }
        //if the track was previously faded out by the player manually switching tracks, reset its volume to 1
        if (currentTrack.volume == 0)
        {
            currentTrack.volume = 1;
        }
        //store the audio clip associated with that audio source as current Clip
        currentClip = currentTrack.clip;
        //caluclate the duration of currentClip
        currentClipDuration = (double)currentClip.samples / currentClip.frequency;
        //schedule the next track to play
        currentTrack.PlayScheduled(nextStartTime);
        //set the start time for the next change over sound
        nextChangeOverTime = nextStartTime + currentClipDuration;
        //set nextStartTime sufficiently in the future that it does not trigger again prematurely
        nextStartTime = AudioSettings.dspTime + 5000;
        //Debug.Log("Queued next track: " + currentTrack);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            canInteract = true;
            //Debug.Log("canInteract is true");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            canInteract = false;
            //Debug.Log("canInteract is false");
        }
    }

    IEnumerator switchTrack()
    {
        //Debug.Log("Switching Track...");
        //if the jukebox is not active then activate it
        if (!jukeboxActive)
        {
            jukeboxActive = true;
            nextChangeOverTime = AudioSettings.dspTime;
        }
        else
        {
            //fade out the current track
            float elapsedTime = 0.0f;
            while (currentTrack.volume > 0)
            {
                currentTrack.volume = Mathf.Lerp(1, 0, elapsedTime / fadeLength);
                elapsedTime += Time.deltaTime;
            }
            //once it's been faded out, stop it
            currentTrack.Stop();
            //set nextChangeOverTime to present time to trigger queueChangeOver function
            nextChangeOverTime = AudioSettings.dspTime;
        }
        yield return null;
    }

    IEnumerator fadeAndStop(AudioSource soundToFade)
    {
        //fade the input sound
        float elapsedTime = 0.0f;
        while (soundToFade.volume > 0)
        {
            soundToFade.volume = Mathf.Lerp(1, 0, elapsedTime / fadeLength);
            elapsedTime += Time.deltaTime;
        }
        //once it's been faded out, stop it
        soundToFade.Stop();
        yield return null;
    }

    public void testFunction()
    {
        Debug.Log("Activated the test function");
    }

    public void deactivateJukebox()
    {
        if (currentTrack.isPlaying)
        {
            StartCoroutine(fadeAndStop(currentTrack));
        }
        if (changeOverSound.isPlaying)
        {
            StartCoroutine(fadeAndStop(changeOverSound));
        }
        jukeboxActive = false;
    }
}

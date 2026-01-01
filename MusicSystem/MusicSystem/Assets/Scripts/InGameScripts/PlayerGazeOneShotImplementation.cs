using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]

public class PlayerGazeOneShotImplementation : MonoBehaviour
{
    public GameObject player;
    public GameObject ClueRoomText;
    public AudioSource[] poiMusicCues;
    public AudioSource[] bgMusic;
    public GameObject door;
    public bool debuggingRaysOn = false;
    private bool raycastsEnabled = false;
    private AudioSource currentCue;
    private AudioSource nextCue;
    private AudioClip currentCueClip;
    private double currentCueClipDuration;
    private Camera playerCamera;
    private List<GameObject> poiLookedAt = new List<GameObject>();
    private int poiMax = 3;
    LayerMask layerMask;

    public double bpm = 100.0F;
    public int signatureHi = 4;
    public int signatureLo = 4;
    public AudioClip sourceClip;
    public AudioMixer MainMixer;
    private string exposedMixerParameter = "ClueRoomGroupVolume";
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
    private bool firstActivation = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerCamera = player.GetComponent<Camera>();
        layerMask = LayerMask.GetMask("Default");
        currentCue = poiMusicCues[Random.Range(0, poiMusicCues.Length)];
        currentCueClip = currentCue.clip;
        currentCueClipDuration = (double)currentCueClip.samples / currentCueClip.frequency;

        accent = signatureHi;
        beatLength = 60d / bpm;
        barLength = beatLength * signatureLo;
        barCount = 1;
        sampleRate = AudioSettings.outputSampleRate;

        clipDuration = (double)sourceClip.samples / sourceClip.frequency;
        clipDurationInBars = clipDuration / barLength;
    }

    //use OnAudioFilterRead to track metronome status and update it accordingly
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
                    //Debug.Log("Bar " + barCount);
                    barCount++;
                    if (barCount > (int)clipDurationInBars)
                    {
                        nextStartTime = AudioSettings.dspTime + barLength;
                        queueNextTrack = true;
                        barCount = 1;
                    }
                }
                //Debug.Log("Tick: " + accent + "/" + signatureHi);
            }
            n++;
        }
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 line = new Vector3(
                playerCamera.transform.position.x + Random.Range(-0.5f, 0.5f),
                playerCamera.transform.position.y + Random.Range(-0.5f, 0.5f),
                playerCamera.transform.position.z);

        if (raycastsEnabled)
        {
            if (Physics.Raycast(
                line,
                playerCamera.transform.forward,
                out RaycastHit hitInfo,
                5000,
                layerMask,
                QueryTriggerInteraction.Collide))
            {
                if (hitInfo.collider.gameObject.name.Contains("POI"))
                {
                    GameObject hitObject = hitInfo.collider.gameObject;
                    if (!poiLookedAt.Contains(hitObject))
                    {
                        poiLookedAt.Add(hitObject);
                        currentCue.Play();
                        setNextCue();
                        StartCoroutine(PauseRaycastsDuringAudio(currentCueClipDuration));
                        Debug.Log("Found " +  poiLookedAt.Count + " out of " + poiMax);
                        if (poiLookedAt.Count == poiMax)
                        {
                            door.SetActive(false);
                            raycastsEnabled = false;
                            debuggingRaysOn = false;
                        }
                    }
                }
            }

            if (debuggingRaysOn)
            {
                Debug.DrawRay(
                line,
                playerCamera.transform.forward * 5000,
                Color.red,
                1.0f,
                false);
            }
        }
        //toggle debugging rays on/off
        if (Input.GetKeyDown("r"))
        {
            if (raycastsEnabled)
            {
                switch (debuggingRaysOn)
                {
                    case true:
                        debuggingRaysOn = false;
                        break;
                    case false:
                        debuggingRaysOn = true;
                        break;
                }
            }
        }

        if (queueNextTrack)
        {
            // Schedule the next audio sources to play
            switch (toggle)
            {
                case 0:
                    //schedule bg music
                    for (int i = 0; i < bgMusic.Length / 2; i++)
                    {
                        bgMusic[i].PlayScheduled(nextStartTime);
                    }
                    break;
                case 1:
                    //schedule bg music
                    for (int i = bgMusic.Length / 2; i < bgMusic.Length; i++)
                    {
                        bgMusic[i].PlayScheduled(nextStartTime);
                    }
                    break;
            }
            toggle = 1 - toggle;
            queueNextTrack = false;
        }
    }

    IEnumerator PauseRaycastsDuringAudio(double inputTime)
    {
        raycastsEnabled = false;
        yield return new WaitForSeconds((float)inputTime);
        raycastsEnabled = true;
    }

    private void setNextCue()
    {
        nextCue = poiMusicCues[Random.Range(0, poiMusicCues.Length)];
        while (nextCue == currentCue)
        {
            nextCue = poiMusicCues[Random.Range(0, poiMusicCues.Length)];
        }
        currentCue = nextCue;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (firstActivation)
        {
            //enable player instructions
            ClueRoomText.SetActive(true);
            //enabled raycasting system
            raycastsEnabled = true;
            //activate door to lock player inside room
            door.SetActive(true);
            //start the metronome
            double startTick = AudioSettings.dspTime + 0.5;
            nextTick = startTick * sampleRate;
            running = true;
            //schedule background music
            startTime = AudioSettings.dspTime + 0.5f;
            nextStartTime = startTime + clipDuration;
            for (int i = 0; i < bgMusic.Length / 2; i++)
            {
                bgMusic[i].PlayScheduled(startTime);
            }
            toggle = 1 - toggle;
            firstActivation = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        //disable player instructions
        ClueRoomText.SetActive(false);
        //disable raycasting system
        raycastsEnabled = false;
        //fade out currently playing music
        StartCoroutine(FadeMixerGroup.StartFade(MainMixer, exposedMixerParameter, 1.0f, 0));
        //turn off metronome
        running = false;
    }
}

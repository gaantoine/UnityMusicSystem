using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerGazeOneShot : MonoBehaviour
{
    //public GameObject player;
    public AudioSource[] poiMusicCues;
    public bool debuggingRaysOn = false;
    private bool raycastsEnabled = true;
    private AudioSource currentCue;
    private AudioSource nextCue;
    private AudioClip currentClip;
    private double currentClipDuration;
    private Camera playerCamera;
    private List<GameObject> poiLookedAt = new List<GameObject>();
    LayerMask layerMask;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        layerMask = LayerMask.GetMask("Default");
        currentCue = poiMusicCues[Random.Range(0, poiMusicCues.Length)];
        currentClip = currentCue.clip;
        currentClipDuration = (double)currentClip.samples / currentClip.frequency;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 line = new Vector3(
                playerCamera.transform.position.x + Random.Range(-0.5f, 0.5f),
                playerCamera.transform.position.y + Random.Range(-0.5f, 0.5f),
                0);

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
                    Debug.Log("POI was " + hitObject);
                    if (!poiLookedAt.Contains(hitObject))
                    {
                        poiLookedAt.Add(hitObject);
                        currentCue.Play();
                        setNextCue();
                        StartCoroutine(PauseRaycastsDuringAudio(currentClipDuration));
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
}

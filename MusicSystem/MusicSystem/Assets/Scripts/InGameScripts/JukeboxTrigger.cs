using UnityEngine;

public class JukeboxTrigger : MonoBehaviour
{
    OneTrackPlaylistAudioSourceDiegeticImplementation jukebox;

    public GameObject jukeboxText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        jukebox = GameObject.FindGameObjectWithTag("Jukebox").
            GetComponentInChildren<OneTrackPlaylistAudioSourceDiegeticImplementation>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        jukeboxText.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        jukebox.deactivateJukebox();
        jukeboxText.SetActive(false);
    }
}

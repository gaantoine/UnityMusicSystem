using UnityEngine;

public class OverlapOneShot : MonoBehaviour
{

    public AudioSource oneShotSound;
    private BoxCollider trigger;
    private bool firstTime = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        trigger = gameObject.GetComponent<BoxCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player" && firstTime)
        {
            oneShotSound.Play();
            firstTime = false;
        }
    }
}

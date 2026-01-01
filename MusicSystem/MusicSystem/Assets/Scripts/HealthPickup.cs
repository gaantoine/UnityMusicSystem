using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    public float healthChangeAmount;
    public GroupMixFadingInterpolationImplementation MusicManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MusicManager = GameObject.FindGameObjectWithTag("MusicManager").
            GetComponent<GroupMixFadingInterpolationImplementation>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            MusicManager.modifyHealth(healthChangeAmount);
        }
        Destroy(gameObject);
    }
}

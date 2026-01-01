using Unity.VisualScripting;
using UnityEngine;

public class TransitionalExampleTrigger : MonoBehaviour
{
    TransitionalRetriggeredLoopingwTimeSigBPMChangesImplementation arenaExample;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        arenaExample = GameObject.FindGameObjectWithTag("Arena").
            GetComponent<TransitionalRetriggeredLoopingwTimeSigBPMChangesImplementation>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        arenaExample.activateTransitionalSystem();
    }

    private void OnTriggerExit(Collider other)
    {
        arenaExample.deactivateTransitionalSystem();
    }
}

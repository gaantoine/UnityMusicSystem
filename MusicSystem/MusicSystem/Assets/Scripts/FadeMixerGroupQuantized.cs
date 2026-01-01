using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine;

/*This is a modification of John Leonard French's original code
 * for fading a mixer group that allows for a delayed start of the 
 * fade using a float; This means it's not sample-accurate, but for
 * fade-ins and fade-outs it's probably close enough
~G. Allen Antoine*/
public static class FadeMixerGroupQuantized
{
    /*Coroutine takes in the following as parameters:
     the audio mixer containing the group to be faded
    the name of the exposed volume parameter for that group (exposing that parameter is done in the inspector)
    the duration of the fade
    the target volume for the mixer group
    a time to wait before starting the fade*/
    public static IEnumerator StartFadeQuantized(AudioMixer audioMixer, string exposedParam, float duration, float targetVolume, float timeToWait)
    {
        float currentTime = 0;
        float currentVol;
        //first we get the current level of the audio group mixer fader, which should be somewhere between -80 and 20
        audioMixer.GetFloat(exposedParam, out currentVol);
        //then we need to do some maths to convert that to a number between 0 and 1 to use in our functions
        currentVol = Mathf.Pow(10, currentVol / 20);
        /*clamp the currentVol if it is somehow outside our expected range
         a value of 0 appears to do strange things with faders, hence the min of 0.0001f*/
        float targetValue = Mathf.Clamp(targetVolume, 0.0001f, 1);
        yield return new WaitForSeconds(timeToWait);
        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            //use a lerp function to scale the currentVol between 0 and 1 as desired
            float newVol = Mathf.Lerp(currentVol, targetValue, currentTime / duration);
            //then we have to use maths to convert that number back to the logarithmic range between -80 and 20
            audioMixer.SetFloat(exposedParam, Mathf.Log10(newVol) * 20);
            yield return null;
        }
        yield break;
    }
}
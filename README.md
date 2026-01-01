# UnityMusicSystem

This is a Unity game project I created to showcase a music system with various mechanics related to player location, distance from an area of interest, player gaze, player health, and diegetic music.



The six implemented systems are:



A system that dynamically mixes layers of music in and out as you approach or leave an area of interest.



A system that triggers one-shot music cues when the player looks at particular points of interest.



A system that allows for arrangement switching of a piece of music along with harmonic and non-harmonic stingers that are quantized to the next musical beat.



A system that mixes an additional layer of music into the music mix based on the current state of the player's health.



A system that transitions between two different pieces of music based on player location.





This project was originally created as part of an assessment for a games programming module taken for my master's in computer games development at Manchester Metropolitan University.



I am currently troubleshooting an issue with the transitional system-since I am tracking bars and beats from the audio thread and executing other logic on the game thread, they don't always synchronize, resulting in gaps when transitioning between one piece of music and another in that specific area.





This project was created in Unity version 6000.0.51f1, which has a known security issue.  I will attempt to update the project to a secure version in the future but for now, please update to a newer version if you plan to incorporate this into a published game.


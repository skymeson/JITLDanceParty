using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playSongOnActivate : MonoBehaviour {
    AudioSource audioSource;
    public float startTime;
    float val;
	// Use this for initialization
	void Start () {
    }

    void OnEnable()
    {
        if(audioSource == null)
            audioSource = GetComponent<AudioSource>();

        audioSource.Play();
        audioSource.time = startTime;
    }

    
    void OnDisable()
    {
        val = audioSource.time;
        audioSource.Pause();
    }

    // Update is called once per frame
    void Update () {
        startTime = audioSource.time;
	}
}

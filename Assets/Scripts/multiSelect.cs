using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class multiSelect : MonoBehaviour {
    public KeyCode advance=KeyCode.Space;
    public GameObject[] objects;
    public int index;
    public bool autoAdvance;
    public float advanceTime = 10;  //seconds
    float lastAdvance;
    // Use this for initialization
    void Start() {
        objects = new GameObject[transform.childCount];
        int inc = 0;
        foreach (Transform child in transform)
        {
            objects[inc] = child.gameObject;
            inc++;
        }
        for (int i = 0; i < objects.Length; i++)
            objects[i].SetActive(false);
        objects[index].SetActive(true);
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(advance) || (Time.time-lastAdvance>advanceTime))
        {
            lastAdvance = Time.time;
            objects[index].SetActive(false);
            index = (index + 1) % objects.Length;
            objects[index].SetActive(true);
        }
	}
}

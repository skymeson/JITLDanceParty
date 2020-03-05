/*
 //  disabling because I'm having an issue getting it to work in builds

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class multiSelectWithAnimationAndMusic : MonoBehaviour {
    public KeyCode advance=KeyCode.Space;
    public GameObject[] objects;
    public selectedAnimation[] animations;
    public int index;
    int animationIndex = 0;
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
        if (Input.GetKeyDown(advance))
        {
            objects[index].SetActive(false);
            index = (index + 1) % objects.Length;
            objects[index].SetActive(true);
        }
	}
}

[System.Serializable]
public class selectedAnimation : MonoBehaviour
{
    public AudioClip music;
    public AnimatorController animController;
    public float audioPlayhead;
}

*/
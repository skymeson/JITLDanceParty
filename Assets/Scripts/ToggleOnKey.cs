using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleOnKey : MonoBehaviour
{
    public KeyCode toggle;
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(toggle))
            foreach (Transform child in transform)
                child.gameObject.SetActive(!child.gameObject.activeInHierarchy);

    }
}

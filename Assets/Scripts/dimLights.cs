using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class dimLights : MonoBehaviour {
    public float dimmer = 1f;  //this is a multiplier for the light brightness of all children
    public bool rampBrightness=false;
    public float startBrightness = 0.5f;
    public float targetBrightness = 1f;
    public float rampTime = 1f;  //time, in seconds, for the brightness to ramp from the original to the target
    float lastDimmer;
    public List<Light> lights;
    public float[] originalIntensity;
    float startTime;
	// Use this for initialization


	void Awake () {
        lights = new List<Light>();
        foreach (Transform child in transform)
        {
            Light light = child.GetComponent<Light>();
            if (light != null)
                lights.Add(light);
        
        }
        originalIntensity = new float[lights.Count];
        int index = 0;
        foreach (Light light in lights)
        {
            originalIntensity[index] = light.intensity;
            index++;
        }
	}

    private void OnEnable()
    {
        if (rampBrightness)
        {
            dimmer = startBrightness;
            foreach (Light light in lights)
                light.intensity = startBrightness;
        }
        startTime = Time.time;

    }


    float clampMap(float s, float a1, float a2, float b1, float b2)
    {
        float maxClamp, minClamp;
        if(b1>b2)
        {
            maxClamp = b1;
            minClamp = b2;
        }
        else
        {
            maxClamp = b2;
            minClamp = b1;
        }
        return Mathf.Clamp(b1 + (s - a1) * (b2 - b1) / (a2 - a1), minClamp, maxClamp);
    }

    // Update is called once per frame
    void Update () {
        if (rampBrightness)
        {
            if ((Time.time - startTime) < rampTime)
                dimmer = clampMap(Time.time - startTime, 0, rampTime, startBrightness, targetBrightness);
        }

        if (dimmer!=lastDimmer)  //did we change the dimmer value?
        {
            int index = 0;
            foreach (Transform child in transform)
            {
                Light light = child.GetComponent<Light>();
                if (light != null)
                {
                    light.intensity = originalIntensity[index] * dimmer;
                    index++;
                }
            }
        }

        lastDimmer = dimmer;

	}
}

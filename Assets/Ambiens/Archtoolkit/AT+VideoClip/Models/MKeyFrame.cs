using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ambiens.archtoolkit.atvideo.models
{
    [Serializable]
    public class MKeyFrame 
    {
        
        public Vector3 CameraPosition;
        public Vector3 CameraForward;
        //Used Only in Editor to move the camera around
        public Vector3 CameraPivot;
        public Texture2D Image;
        public string ID;
        public float duration;

        [NonSerialized]
        public MVideoClip clipRef;

        public int GetCurrentIndex()
        {
            return this.clipRef.KeyFrames.IndexOf(this);
        }
    }
}
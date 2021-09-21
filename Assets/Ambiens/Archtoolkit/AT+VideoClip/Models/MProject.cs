using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ambiens.archtoolkit.atvideo.models
{
    [Serializable]
    public class MProject :ScriptableObject
    {
        public string ProjectName;

        public int ResolutionIndex = 0;
        public int OutputWidth=1920;
        public int OutputHeight=1080;

        public bool is360=false;
        public bool is360Stereo=false;

        public int FrameRateIndex = 0;
        public float FrameRate = 30;

        public bool TakeScreenShots = false;
        public float ScreenShotsInterval = 1;

        public int GetDuration()
        {
            int t = 0;
            foreach (var c in clips) t +=(int) c.Duration;
            return t;
        }
        public List<MVideoClip> clips=new List<MVideoClip>();

        public int GetClipIndex(MVideoClip clip)
        {
            for (int i = 0; i < clips.Count; i++) if (clips[i].ID == clip.ID) return i;
            return -1;
        }
    }
}
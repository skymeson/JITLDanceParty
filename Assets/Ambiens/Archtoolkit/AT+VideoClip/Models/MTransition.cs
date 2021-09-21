using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ambiens.archtoolkit.atvideo.models
{
    [Serializable]
    public class MTransition
    {
        public enum TransitionType
        {
            none = 0,
            dipToBlack = 1,
            dipToWhite = 2,

        }
        public bool isLeftTransition = false;
        public TransitionType Type= TransitionType.dipToBlack;
        public float duration = 1f;
    }
}
using ambiens.archtoolkit.atvideo.models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ambiens.archtoolkit.atvideo
{
    public class ATTransitionPlayer : MonoBehaviour
    {
        public static string TransitionGOPrefab = "TransitionGO";
        public Material transitionColorMaterial;
        public Color blackColorReset;
        public Color WhiteColorReset;

        private MTransition mTransition;
        private float currentTime = 0;
        public bool isPlaying { get; private set; }

        public void ResetAll()
        {
            transitionColorMaterial.SetColor("_Color",blackColorReset) ;
            
        }

        public void PlayTransition(MVideoClip clip, int clipIndex, MTransition transition)
        {
            
            if(transition.Type == MTransition.TransitionType.dipToBlack || transition.Type == MTransition.TransitionType.dipToWhite)
            {
                this.currentTime = 0;
                this.isPlaying = true;
                this.mTransition = transition;
                if (this.mTransition.isLeftTransition)
                {
                    var color = this.getColor();

                    this.transitionColorMaterial.SetColor("_Color",new Color(
                        color.r,
                        color.g,
                        color.b, 
                        1)) ;
                }
            }
            else
            {
                this.ResetAll();
            }
        }
        public void ManagedUpdate()
        {
            if (this.mTransition == null) return;
            
            if (this.mTransition.Type == MTransition.TransitionType.dipToBlack || this.mTransition.Type == MTransition.TransitionType.dipToWhite)
            {

                var value = this.currentTime / this.mTransition.duration;
                if (value <= 1)
                {
                    if (this.mTransition.isLeftTransition)
                        value = 1 - value;

                    var color = this.getColor();

                    this.transitionColorMaterial.SetColor("_Color",new Color(
                        color.r,
                        color.g,
                        color.b, 
                        value)) ;
                }
                else
                {
                    this.isPlaying = false;
                    this.mTransition = null;
                    //this.ResetAll();
                    StartCoroutine(DelayedReset());
                }
                this.currentTime += Time.deltaTime;
            }
        }

        Color getColor()
        {
            return (mTransition.Type == MTransition.TransitionType.dipToBlack) ? this.blackColorReset : this.WhiteColorReset;
        }
        IEnumerator DelayedReset()
        {
            yield return new WaitForEndOfFrame();
            this.ResetAll();
        }
    }
}
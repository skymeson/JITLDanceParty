using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class animateAllChildren : MonoBehaviour {
    Animator animator;
    RuntimeAnimatorController lastRuntimeAnimatorController;
    void Start () {
        animator = gameObject.GetComponent<Animator>();
	}
	
	// Update is called once per frame
	void Update () {
		if(animator.runtimeAnimatorController!=lastRuntimeAnimatorController)  //only update animations if we change the animator controller
        {
            lastRuntimeAnimatorController = animator.runtimeAnimatorController;
            Animator[] dancerAnimators = gameObject.GetComponentsInChildren<Animator>();
            foreach (Animator danceAnimator in dancerAnimators)
                danceAnimator.runtimeAnimatorController = animator.runtimeAnimatorController;
        }
	}
}

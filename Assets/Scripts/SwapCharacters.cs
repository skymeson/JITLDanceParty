using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SwapCharacters : MonoBehaviour
{
    // Use this for initialization
    public GameObject[] puppets;
    public GameObject[] originalPositions;

    public GameObject musicAndAnimations;

    public AudioSource[] audios;
    float[] playhead;
    public AnimationClip[] clips;
    public Transform[] animationTransform;
    public float audioFadeTime = 2;
    public float groundLevel = -1.31f;  //ground level (y) in world positions
    private int currentPuppetInd = 0;

    public AudioSource src;

    public int lastAnimIndex = 0;

    public float swapTimer = 0f;

    public bool autoSwapTimer = false;
    public float timeUntilCharacterSwap = 45f;

    private float animationSwapTimer;

    public float timeUntilAnimationSwap = 8f;

    public int animationIndex = 0;
    public int puppetIndex = 0;

    void Start()
    {
        src = gameObject.GetComponent<AudioSource>();
        originalPositions = new GameObject[transform.childCount];
        puppets=new GameObject[transform.childCount];
 
        audios = new AudioSource[musicAndAnimations.transform.childCount];
        playhead = new float[audios.Length];
        clips = new AnimationClip[musicAndAnimations.transform.childCount];
        //because some animations want different orientations/scales than others
        animationTransform = new Transform[musicAndAnimations.transform.childCount];

        //build up our array of audiosource and animation clips from the children of a 
        //musicAndAnimation game object
        //every child of the musicAndAnimation game object is its own gameObject with
        //one audio source and one animatorController on that object
        //you can put as many music and animation elements there as you like, and they 
        //all get loaded in on start
        int index = 0;
        foreach(Transform child in musicAndAnimations.transform)
        {
            audios[index] = child.gameObject.GetComponent<AudioSource>();
            animationTransform[index] = child;
            try
            {
                Animator animator = child.gameObject.GetComponent<Animator>();
                clips[index] = animator.runtimeAnimatorController.animationClips[0];
            }
            catch
            {
                clips[index] = null;
            }
            index++;
        }

        index = 0;
        foreach(Transform child in transform)
        {
            puppets[index] = child.gameObject;
            originalPositions[index] = new GameObject();
            originalPositions[index].transform.localPosition = child.localPosition;
            originalPositions[index].transform.localRotation = child.localRotation;

            //turn off all characters except the first one
            if (index != 0)
                child.gameObject.SetActive(false);
            index++;
        }

#if UNITY_EDITOR
        
        foreach (AudioSource audioSource in audios)
        {
           // audioSource.volume = 0f;
        }
#endif
        SwapPuppet(puppetIndex);
        SwapAnim(animationIndex);
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            puppetIndex++;
            if (puppetIndex >= puppets.Length)
                puppetIndex = 0;
            SwapPuppet(puppetIndex);
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            puppetIndex--;
            if (puppetIndex <0)
                puppetIndex = puppets.Length-1;
            SwapPuppet(puppetIndex);
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            animationIndex++;
            if (animationIndex >= clips.Length)
                animationIndex = 0;
            SwapAnim(animationIndex);
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            animationIndex--;
            if (animationIndex < 0)
                animationIndex = clips.Length - 1;
            SwapAnim(animationIndex);
        }
        if (autoSwapTimer)
        {
            swapTimer += Time.deltaTime;
            animationSwapTimer += Time.deltaTime;
        }

        if (swapTimer > timeUntilCharacterSwap)
        {
            SwapAnim(Random.Range(0, clips.Length));
            SwapPuppet(Random.Range(0, clips.Length));
            swapTimer = 0f;
        }

        if(animationSwapTimer > timeUntilAnimationSwap)
        {
            SwapAnim(Random.Range(0, puppets.Length));
            animationSwapTimer = 0f;
        }
        

        playhead[animationIndex] = audios[animationIndex].time;
    }

    public void SwapMusic(int ind)
    {
#if UNITY_EDITOR
        Debug.Log("Skipping the music in editor");
#endif
#if UNITY_STANDALONE_OSX
        audios[lastAnimIndex].DOFade(0f, audioFadeTime);
        audios[ind].Play();
        audios[ind].DOFade(1f, audioFadeTime);
#endif
#if UNITY_STANDALONE_WIN
        audios[lastAnimIndex].DOFade(0f, audioFadeTime);
        audios[ind].time = playhead[ind];
        audios[ind].Play();
        audios[ind].DOFade(1f, audioFadeTime);
#endif
    }

    public void SwapAnim(int index)
    {
        /*
        puppets[puppetIndex].transform.localPosition = originalPositions[puppetIndex].transform.localPosition+animationTransform[index].localPosition;
        puppets[puppetIndex].transform.localRotation = originalPositions[puppetIndex].transform.localRotation*animationTransform[index].localRotation;
        puppets[puppetIndex].transform.localScale = animationTransform[index].localScale;
        */
        src.Play();

        SwapMusic(index);
        Animator animator = puppets[currentPuppetInd].GetComponent<Animator>();

        AnimatorOverrideController aoc = new AnimatorOverrideController(animator.runtimeAnimatorController);
        var anims = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        AnimationClip newClip = clips[index];

        foreach (var a in aoc.animationClips)
            anims.Add(new KeyValuePair<AnimationClip, AnimationClip>(a, newClip));
        aoc.ApplyOverrides(anims);

        animator.runtimeAnimatorController = aoc;

        lastAnimIndex = index;

    }

    public void SwapPuppet(int index)
    {
        src.Play();
        puppets[currentPuppetInd].SetActive(false);

        currentPuppetInd = index;
        Animator animator = puppets[currentPuppetInd].GetComponentInChildren<Animator>();

        animator.transform.localPosition = Vector3.zero;
        animator.transform.localRotation = Quaternion.Euler(Vector3.back);

        puppets[currentPuppetInd].SetActive(true);

        SwapAnim(animationIndex);

    }
}
using ambiens.archtoolkit.atvideo.models;
using System;
using System.Collections;
using System.Collections.Generic;
#if AT_VIDEO_INIT && UNITY_EDITOR
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
#endif
using UnityEngine;

namespace ambiens.archtoolkit.atvideo
{
    public class ATVideoClipPlayer : MonoBehaviour
    {

        public MProject mProject;

        public bool RecordVideo;
        
        public int ForcedClipIndex=-1;

        private float currentScreenShotsElapsedTime = 0;

        public bool IsPlaying { get; private set; } = true;

        private float elapsedTime;
        public float currentPercentage { get; private set; }
        public int currentVideoClipIndex { get; private set; }
        private MVideoClip currentClip;

        private Transform myt;
        private Camera cam;

        public Action<MVideoClip, float, float > OnUpdate;
        public Action<MVideoClip> OnChangeClip;

        ATTransitionPlayer transitionsPlayer;
#if AT_VIDEO_INIT && UNITY_EDITOR
        private RecorderController mRecorderController;
#endif

        private void Awake()
        {
            this.myt = this.transform;
            this.cam = this.GetComponent<Camera>();

            this.cam.nearClipPlane = 0.1f;

            if (ForcedClipIndex != -1)
            {
                this.currentVideoClipIndex = ForcedClipIndex;
            }
            else
            {
                this.currentVideoClipIndex = 0;
            }
            
            this.StartClip(currentVideoClipIndex);

            bool needTransitions = false;
            foreach (var c in mProject.clips)
            {
                c.RefreshCurves();
                if (c.LeftTransition.Type != MTransition.TransitionType.none)
                {
                    needTransitions = true;
                }
                if (c.RightTransition.Type != MTransition.TransitionType.none)
                {
                    needTransitions = true;
                }
            }

            if (needTransitions)
            {
                transitionsPlayer=GameObject.Instantiate(Resources.Load<ATTransitionPlayer>(ATTransitionPlayer.TransitionGOPrefab),this.transform);
                transitionsPlayer.ResetAll();
                //transitionsPlayer.PlayTransition(currentClip, currentVideoClipIndex, currentClip.LeftTransition);
            }

            if (RecordVideo)
            {
                StartVideoRecording();
            }
        }

        private void Update()
        {
            if (IsPlaying)
            {
                elapsedTime += Time.deltaTime;
                currentPercentage = elapsedTime / currentClip.Duration;

                this.ApplyClip(currentClip.GetPosition(currentPercentage), currentClip.GetForward(currentPercentage));

                if (currentClip.LeftTransition.Type != MTransition.TransitionType.none)
                {
                    if (elapsedTime < currentClip.LeftTransition.duration && !transitionsPlayer.isPlaying)
                    {
                        transitionsPlayer.PlayTransition(currentClip, currentVideoClipIndex, currentClip.LeftTransition);
                    }
                }

                if (currentClip.RightTransition.Type != MTransition.TransitionType.none)
                {
                    if (currentClip.Duration - elapsedTime > 0)
                    {
                        //Debug.Log((currentClip.Duration - elapsedTime) + " - " + currentClip.RightTransition.duration);
                        if (currentClip.Duration - elapsedTime <= currentClip.RightTransition.duration && !transitionsPlayer.isPlaying)
                        {
                            transitionsPlayer.PlayTransition(currentClip, currentVideoClipIndex, currentClip.RightTransition);
                        }
                    }
                }

                if (currentPercentage >= 1f)
                {
                    if (this.ForcedClipIndex != -1)
                    {
                        StartCoroutine(this.Stop());
                    }
                    else
                    {
                        currentVideoClipIndex++;
                        if (currentVideoClipIndex < mProject.clips.Count)
                        {
                            this.StartClip(currentVideoClipIndex);
                        }
                        else
                        {
                            StartCoroutine(this.Stop());
                        }
                    }
                    
                }

                if (transitionsPlayer != null) transitionsPlayer.ManagedUpdate();
                if (OnUpdate != null) OnUpdate(this.currentClip, elapsedTime, currentPercentage);
            }
        }
        private void LateUpdate()
        {
            if(IsPlaying && RecordVideo && this.mProject.TakeScreenShots)
            {
                currentScreenShotsElapsedTime += Time.deltaTime;
                if(currentScreenShotsElapsedTime>= this.mProject.ScreenShotsInterval)
                {
                    currentScreenShotsElapsedTime = 0;

                    TakeScreen();

                }
            }
        }

        private void TakeScreen()
        {
            RenderTexture rt = new RenderTexture(this.mProject.OutputWidth, this.mProject.OutputHeight, 24);
            this.cam.targetTexture = rt;
            Texture2D screenShot = new Texture2D(this.mProject.OutputWidth, this.mProject.OutputHeight, TextureFormat.RGB24, false);
            this.cam.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, this.mProject.OutputWidth, this.mProject.OutputHeight), 0, 0);
            this.cam.targetTexture = null;
            RenderTexture.active = null; 
            Destroy(rt);
            byte[] bytes = screenShot.EncodeToPNG();
            string filename = ScreenShotName(this.mProject.OutputWidth, this.mProject.OutputHeight);
            System.IO.File.WriteAllBytes(filename, bytes);
            
        }
        private static string ScreenShotName(int width, int height)
        {
            return string.Format("{0}/Recordings/screen_{1}x{2}_{3}.png",
                                 Application.dataPath.Replace("Assets",""),
                                 width, height,
                                 System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
        }

        public virtual void StartClip(int clipToApply)
        {

            elapsedTime = 0;
            currentPercentage = 0;
            currentClip = mProject.clips[clipToApply];
            if (OnChangeClip != null)
                OnChangeClip(currentClip);
        }

        public virtual void ApplyClip(Vector3 position, Vector3 forward)
        {
            myt.position = position;
            if(!this.mProject.is360)
                myt.forward = forward;
            else {
                myt.forward = Vector3.ProjectOnPlane(forward,Vector3.up);
            }
                
        }
        public void Play()
        {
            this.IsPlaying = true;
        }
        public void Pause()
        {
            this.IsPlaying = false;
        }
        private IEnumerator Stop()
        {
#if UNITY_EDITOR
#if AT_VIDEO_INIT
            if (mRecorderController != null)
                mRecorderController.StopRecording();
#endif
            yield return new WaitForSeconds(1);

            UnityEditor.EditorApplication.isPlaying = false;
#else
            yield return null;
#endif

        }

        void StartVideoRecording()
        {
#if UNITY_EDITOR && AT_VIDEO_INIT
            RecorderOptions.VerboseMode = false;

            var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            this.mRecorderController = new RecorderController(controllerSettings);

            var videoRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            videoRecorder.name = this.mProject.ProjectName;
            videoRecorder.Enabled = true;
            videoRecorder.OutputFile = "Recordings/" +GetVideoName();
            videoRecorder.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
            videoRecorder.VideoBitRateMode = UnityEditor.VideoBitrateMode.High;

            if(this.mProject.is360)
            {
                videoRecorder.ImageInputSettings = new Camera360InputSettings()
                {
                    OutputWidth = this.mProject.OutputWidth,
                    OutputHeight = this.mProject.OutputHeight,
                    RenderStereo = this.mProject.is360Stereo
                };
            }
            else{
                videoRecorder.ImageInputSettings = new GameViewInputSettings()
                {
                    OutputWidth = this.mProject.OutputWidth,
                    OutputHeight = this.mProject.OutputHeight,
                };
            }
            

            videoRecorder.AudioInputSettings.PreserveAudio = true;

            controllerSettings.AddRecorderSettings(videoRecorder);

            controllerSettings.SetRecordModeToManual();
            //controllerSettings.SetRecordModeToFrameInterval(0, this.mProject.GetDuration() * this.FrameRate); // 2s @ 30 FPS
            controllerSettings.FrameRate = this.mProject.FrameRate;
            controllerSettings.FrameRatePlayback = FrameRatePlayback.Constant;
            controllerSettings.CapFrameRate = false;

            mRecorderController.PrepareRecording();
            mRecorderController.StartRecording();

#endif
        }

        public string GetVideoName()
        {

            return string.Format(this.mProject.ProjectName + "_{0}x{1}_{2}",
                          this.mProject.OutputWidth, this.mProject.OutputHeight,
                          System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
        }
    }
}
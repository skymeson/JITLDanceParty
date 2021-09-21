using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using ambiens.archtoolkit.atvideo.models;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using System.Collections.Generic;

namespace ambiens.archtoolkit.atvideo
{
    public class ATVideoWindow : EditorWindow
    {
        public MProjectSceneContainer sceneContainer;
        public static string UXMLRootFolder = "Assets/Ambiens/Archtoolkit/AT+VideoClip/Editor/UXML_uss/";
        public static string LayoutRootFolder = "Assets/Ambiens/Archtoolkit/AT+VideoClip/Editor/";
        public static string PreviousLayoutRootFolder = "Assets/Ambiens/Archtoolkit/";


        //Projects Elements
        public ListView projectList;
        public Button addProjectButton;
        //public Button deleteProjectButton;
        public Button playButton;
        public Button recordButton;
        //public Button settingsButton;
        public Button openFolderButton;
        public Button setLayoutButton;

        public static string V = "1.2";

        //VideoClips
        public ScrollView VideoClipList;

        private MProject currentSelectedProject = null;

        //Managers
        private KeyFramesWindowManager keyFrameManager;
        private ModalWindowsManager modalManager;

        public List<string> PackageDependencies = new List<string>() {"com.unity.recorder" };


        [MenuItem("Tools/Ambiens/ArchToolkit/AT+VideoClip")]
        public static void ShowDefaultWindow()
        {
            var wnd = GetWindow<ATVideoWindow>();
            wnd.titleContent = new GUIContent("AT+VideoClip (v"+V+")");

        }

        public void OnEnable()
        {
            var root = this.rootVisualElement;
            
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXMLRootFolder+"ATVideoClipWindow.uxml");
            visualTree.CloneTree(root);
#if !AT_VIDEO_INIT
            DependenciesManager d = new DependenciesManager();
            if (d.NamespaceExists("UnityEditor.Recorder"))
            {
                d.AddScriptingSymbol(new List<string>(){ "AT_VIDEO_INIT" });
                this.InitAll();
            }
            else
            {
                Debug.Log("Installing Dependencies");
                d.ApplyDependencies(PackageDependencies,
                () => {
                    InitAll();
                },
                (float f) => {
                    
                });
            }

#else
            this.InitAll();
#endif

        }
        private void InitAll()
        {
            InitData();
            InitUI();
            InitManagers();
        }
        private void InitManagers()
        {
            this.keyFrameManager = new KeyFramesWindowManager(this.rootVisualElement, this);
            this.modalManager = new ModalWindowsManager(this.rootVisualElement, this);

            EditorApplication.playModeStateChanged += this.onPlayModeStateChanged;
        }

        private void onPlayModeStateChanged(PlayModeStateChange playState)
        {
            if(playState == PlayModeStateChange.EnteredPlayMode)
            {
                this.RefreshVideoClips();
                var player=GameObject.FindObjectOfType<ATVideoClipPlayer>();

                player.OnChangeClip = null;
                player.OnChangeClip += this.OnRuntimeChangeClip;
                this.OnRuntimeChangeClip(player.mProject.clips[player.currentVideoClipIndex]);

                player.OnUpdate = null;
                player.OnUpdate += this.OnRuntimeUpdate;
            }
            else
            {
                var prevClip = rootVisualElement.Q<VisualElement>(null, "ActiveClip");
                if (prevClip != null)
                    prevClip.RemoveFromClassList("ActiveClip");
            }
            
        }
        
        private void OnRuntimeChangeClip(MVideoClip clip)
        {
            var prevClip=rootVisualElement.Q<VisualElement>(null, "ActiveClip");
            if(prevClip!=null)
                prevClip.RemoveFromClassList("ActiveClip");

            var clipEle = rootVisualElement.Q<VisualElement>(clip.ID);
            clipEle.AddToClassList("ActiveClip");
        }

        private void OnRuntimeUpdate(MVideoClip clip, float time, float percentage)
        {
            var clipEle=rootVisualElement.Q<VisualElement>(clip.ID);
            
            var pLine=clipEle.Q<VisualElement>(null, "PlayLine");

            var width = clipEle.contentRect.width;
            percentage = Math.Min(1f, percentage);
            pLine.style.left = width * percentage;
        }

        public void InitData()
        {
            /*REMOVED IN 1.2 */
            /*var scene=EditorSceneManager.GetActiveScene();
            var sPath = Path.GetDirectoryName(scene.path);
            var assetPath = Path.Combine(sPath, "atv_" + scene.name + ".asset");
            var asset=AssetDatabase.LoadAssetAtPath<MProjectSceneContainer>(assetPath);
            */

            MProjectSceneContainer asset=null;
            var assets=AssetDatabase.FindAssets("t:MProjectSceneContainer");
            if(assets.Length>0) asset=AssetDatabase.LoadAssetAtPath<MProjectSceneContainer>(AssetDatabase.GUIDToAssetPath(assets[0]));

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<MProjectSceneContainer>();

                AssetDatabase.CreateAsset(asset, "Assets/Ambiens/Archtoolkit/AT+VideoClip/ATV_data.asset");
                AssetDatabase.SaveAssets();
            }

            this.sceneContainer = asset;
            if (this.sceneContainer.projects == null) this.sceneContainer.projects = new List<MProject>();

            this.RefreshKeyFrameToClipReference();
        }
        private void RefreshKeyFrameToClipReference()
        {
            foreach (var p in this.sceneContainer.projects)
            {
                foreach (var c in p.clips)
                {

                    foreach (var k in c.KeyFrames)
                    {
                        k.clipRef = c;
                    }
                }
            }
        }

        Transform ___atvpivot;
        public Transform ATVPivot {
            get{
                if (___atvpivot == null) {
                    var go = GameObject.Find("ATV_Pivot");
                    if (go != null) ___atvpivot = go.transform;
                }
                if (___atvpivot == null)
                {
                    var go = new GameObject("ATV_Pivot");
                    ___atvpivot = go.transform;
                }
                return ___atvpivot;
            }
            
        }

        private float scrollerMovementHack=3;
        public void InitUI()
        {
            //Bind Project List
            SerializedObject so = new SerializedObject(sceneContainer);
            
            projectList = rootVisualElement.Q<ListView>("ProjectList");
            projectList.makeItem = this.MakeProjectEle;
            projectList.bindingPath ="projects";
            projectList.bindItem = this.BindProjectList;
            projectList.itemHeight = 50;
            projectList.Bind(so);
            projectList.onSelectionChanged += this.OnProjectListItemSelectionChange;
            projectList.Refresh();

            addProjectButton = rootVisualElement.Q<Button>("addProjectButton");
            addProjectButton.clicked += this.OnAddProjectButtonClick;

            //deleteProjectButton = rootVisualElement.Q<Button>("DeleteProjectButton");
            //deleteProjectButton.clicked += this.OnDeleteProjectButtonClick;

            playButton = rootVisualElement.Q<Button>("PlayButton");
            playButton.clicked += ()=> { this.StartVideo(false); };

            recordButton = rootVisualElement.Q<Button>("RecordButton");
            recordButton.clicked += () => { this.StartVideo(true); };// this.OnRecordButtonClicked;

            openFolderButton = rootVisualElement.Q<Button>("openFolderButton");
            openFolderButton.clicked += this.OnOpenFolderButtonClicked;

            setLayoutButton = rootVisualElement.Q<Button>("setLayoutButton");
            setLayoutButton.clicked += this.OnSetLayoutButtonClicked;

            var VideoClipScrollView = rootVisualElement.Q<ScrollView>("VideoClipScrollView");

            var rightScroller = rootVisualElement.Q<VisualElement>("ScrollScrollerRight");
            
            rightScroller.RegisterCallback<MouseMoveEvent>((MouseMoveEvent evt) => {
                VideoClipScrollView.scrollOffset = new Vector2(VideoClipScrollView.scrollOffset.x + 10f, VideoClipScrollView.scrollOffset.y);
            });
            var leftScroller = rootVisualElement.Q<VisualElement>("ScrollScrollerLeft");
            leftScroller.RegisterCallback<MouseMoveEvent>((MouseMoveEvent evt) => {
                VideoClipScrollView.scrollOffset = new Vector2(VideoClipScrollView.scrollOffset.x - 10f, VideoClipScrollView.scrollOffset.y);
            });
        }

        public void StartVideo(bool record=false, int ForceClipIndex=-1)
        {
            var mainCamera=GameObject.FindObjectOfType<Camera>();
            if (mainCamera == null)
            {
                Debug.LogError("Please add a Camera in the scene!");
                return;
            }
            if (this.currentSelectedProject == null)
            {
                Debug.LogError("Please Select a Project first!");
                return;
            }
            var player=mainCamera.GetComponent<ATVideoClipPlayer>();
            if(player==null) player = mainCamera.gameObject.AddComponent<ATVideoClipPlayer>();
            
            player.ForcedClipIndex = ForceClipIndex;
            player.mProject = this.currentSelectedProject;
            player.RecordVideo = record;

            EditorApplication.EnterPlaymode();
        }
       
        void OnOpenFolderButtonClicked()
        {
            EditorUtility.RevealInFinder(Application.dataPath.Replace("Assets", "Recordings"));
        }
        public static bool isOldLayout = false;
        void OnSetLayoutButtonClicked()
        {
            
            var prevPathFull=Path.Combine(Directory.GetCurrentDirectory(), Path.Combine(PreviousLayoutRootFolder, "PreviousLayout.wlt"));
            var prevPath = Path.Combine(PreviousLayoutRootFolder, "PreviousLayout.wlt");
           
            if (!File.Exists(prevPathFull))
            {
                isOldLayout = true;
                LayoutUtility.SaveLayout(prevPath);
            }
            
            if (isOldLayout)
            {
                string path = Path.Combine(LayoutRootFolder, "ATVideoClip.wlt");
                LayoutUtility.LoadLayoutFromAsset(path);
                isOldLayout = false;
            }
            else
            {
                isOldLayout = true;
                LayoutUtility.LoadLayoutFromAsset(prevPath);
            }
            

        }
        

#region Projects_Managment
        VisualTreeAsset ProjectItemListTemplate = null;
        private void BindProjectList(VisualElement element, int index)
        {
            if (index < this.sceneContainer.projects.Count)
            {
                SerializedObject so = new SerializedObject(this.sceneContainer.projects[index]);

                element.Q<Label>(null,"NameLabel").bindingPath = "ProjectName";
                element.Q<Label>(null, "WidthLabel").bindingPath = "OutputWidth";
                element.Q<Label>(null, "HeightLabel").bindingPath = "OutputHeight";
                element.Q<Button>(null, "SettingsButton").clicked +=()=> this.modalManager.OpenProjectSettings(this.sceneContainer.projects[index]);

                if (this.sceneContainer.projects[index].clips.Count > 0)
                {
                    var f = this.sceneContainer.projects[index].clips[0].KeyFrames[0];
                    f.Image = this.keyFrameManager.GetTextureForKeyFrame(f);
                    element.Q<Image>(null, "Image").style.backgroundImage = new StyleBackground(f.Image);
                }
                
                element.Bind(so);
            }
            else
            {
                element.visible = false;
            }
        }

        private VisualElement MakeProjectEle()
        {
            if (ProjectItemListTemplate == null)
                ProjectItemListTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXMLRootFolder + "MProjectListItemTemplate.uxml");

            return ProjectItemListTemplate.CloneTree();
        }
        public void DeleteProject(MProject project)
        {
            if (project != null)
            {
                if(currentSelectedProject==project) projectList.selectedIndex=0;

                this.sceneContainer.projects.Remove(project);
                EditorUtility.SetDirty(this.sceneContainer);

                UnityEngine.Object.DestroyImmediate(project, true);
                
                AssetDatabase.SaveAssets();
                
                projectList.selectedIndex = 0;
                
                this.modalManager.CloseModal();
            }
        }

        private void OnAddProjectButtonClick()
        {

            var p = MProject.CreateInstance<MProject>();
            p.name = Guid.NewGuid().ToString();
            p.ProjectName = "Project "+(this.sceneContainer.projects.Count+1);

            AssetDatabase.AddObjectToAsset(p, this.sceneContainer);
            this.sceneContainer.projects.Add(p);
            EditorUtility.SetDirty(this.sceneContainer);

            AssetDatabase.SaveAssets();

            projectList.selectedIndex = this.sceneContainer.projects.Count-1;
        }

        private void OnProjectListItemSelectionChange(List<object> obj)
        {
            if (projectList.selectedIndex < this.sceneContainer.projects.Count)
            {
                currentSelectedProject = (MProject)this.sceneContainer.projects[projectList.selectedIndex];
            }
            this.RefreshVideoClips();
        }


#endregion

#region VideoClip_Managment
        //TODO: need polishing
        VisualTreeAsset VideoClipItemListTemplate = null;

        public void RefreshVideoClips()
        {

            if (VideoClipList == null)
            {
                VideoClipList=rootVisualElement.Q<ScrollView>("VideoClipScrollView");
            }
            if (currentSelectedProject != null)
            {
                VideoClipList.Clear();
                for(int i=0; i<this.currentSelectedProject.clips.Count+1; i++)
                {
                    var ele=MakeVideoClipEle();
                    BindVideoClipList(ele, i);
                    VideoClipList.Add(ele);
                }
                rootVisualElement.UnregisterCallback<MouseUpEvent>(DeactivateMovementAllTimelineCallBack);
                rootVisualElement.RegisterCallback<MouseUpEvent>(DeactivateMovementAllTimelineCallBack);
            }
        }

        private void BindVideoClipList(VisualElement element, int index)
        {
            if (this.currentSelectedProject == null) return;

            if (index < this.currentSelectedProject.clips.Count)
            {
                element.AddToClassList("VideoClip_"+index);
                if(index==this.currentSelectedProject.clips.Count-1)
                    element.AddToClassList("VideoClip_Last");

                element.Q<VisualElement>(null,"AddVideoClipContainer").style.display = DisplayStyle.None;

                element.name = this.currentSelectedProject.clips[index].ID;

                this.keyFrameManager.RefreshKeyFramesForClip(this.currentSelectedProject.clips[index], element);
                InitPreviewTimeline(this.currentSelectedProject.clips[index], element);
                InitNameAndTimeBinding(this.currentSelectedProject.clips[index], element);
                InitTransitionsBinding(this.currentSelectedProject.clips[index], element);
                InitMoveButtonsBinding(this.currentSelectedProject.clips[index], element);
            }
            else
            {
                element.Q<VisualElement>(null,"VideoClipContainer").style.display = DisplayStyle.None;
                element.Q<Button>("addVideoClipButton").clicked += OnAddVideoClipButtonClick;
            }
        }
        private VisualElement MakeVideoClipEle()
        {
            if (VideoClipItemListTemplate == null)
                VideoClipItemListTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXMLRootFolder + "MVideoClipItemTemplate.uxml");

            return VideoClipItemListTemplate.CloneTree();
        }
        private void OnAddVideoClipButtonClick()
        {

            var clip = new MVideoClip();
            clip.ID = GUID.Generate().ToString();
            var kf=this.keyFrameManager.CreateKeyFrame();
            kf.clipRef = clip;
            clip.KeyFrames.Add(kf);
            this.currentSelectedProject.clips.Add(clip);
            kf.clipRef.RefreshCurves();

            EditorUtility.SetDirty(this.sceneContainer);
            AssetDatabase.SaveAssets();

            RefreshVideoClips();

        }
        private void InitMoveButtonsBinding(MVideoClip clip, VisualElement element)
        {
            var leftMove = element.Q<Button>(null, "MoveLeftButton");
            var rightMove = element.Q<Button>(null, "MoveRightButton");

            leftMove.clicked += () => MoveClip(clip, true);
            rightMove.clicked += () => MoveClip(clip, false);
        }
        private void InitTransitionsBinding(MVideoClip clip, VisualElement element)
        {
            var leftTransition = element.Q<Button>(null, "LeftTransitionButton");
            var rightTransition = element.Q<Button>(null, "RightTransitionButton");

            leftTransition.clicked += () => this.modalManager.OpenClipsTransitionsModal( clip, clip.LeftTransition);
            rightTransition.clicked += () => this.modalManager.OpenClipsTransitionsModal( clip, clip.RightTransition);
        }
        public void DeleteClip( MVideoClip clip)
        {
            if (clip != null)
            {
                this.currentSelectedProject.clips.Remove(clip);
                RefreshVideoClips();
            }
        }
        private void MoveClip(MVideoClip clip, bool left)
        {

            int index=this.currentSelectedProject.clips.LastIndexOf(clip);

            int newIndex = index;

            if ( left && index > 0)
            {
                newIndex = index - 1;
            }
            else if(!left && index < this.currentSelectedProject.clips.Count - 1)
            {
                newIndex = index + 1;
            }
            if(newIndex != index)
            {
                MVideoClip oldClip = this.currentSelectedProject.clips[newIndex];
                this.currentSelectedProject.clips[newIndex] = clip;
                this.currentSelectedProject.clips[index] = oldClip;

                RefreshVideoClips();

            }

        }

        
        private void InitNameAndTimeBinding(MVideoClip clip, VisualElement element)
        {
           
            var timeField = element.Q<IntegerField>(null, "TimeField");
            var secondsTickContainer = element.Q<VisualElement>(null, "KeyFrameSecondsTickContainer");

            timeField.value = (int) clip.Duration;
            InitSecondsUI(secondsTickContainer, clip);
            
            timeField.RegisterValueChangedCallback<int>((ChangeEvent<int> evt) => {
                clip.Duration = evt.newValue;
                InitSecondsUI(secondsTickContainer, clip);
            });

            var settingsButton = element.Q<Button>(null, "ClipSettingsButton");
            settingsButton.clicked +=()=> this.modalManager.OpenClipSettingsModal(clip);

            var playButton = element.Q<Button>(null, "ClipPreviewButton");
            playButton.clicked += () => this.StartVideo(false, this.currentSelectedProject.GetClipIndex(clip));
        }

        

        private void InitSecondsUI(VisualElement DraggerContainer, MVideoClip clip)
        {
            DraggerContainer.Clear();
            for (int i = 0; i < clip.Duration; i++)
            {
                var cm = new VisualElement();
                cm.AddToClassList("SecondsTick");
                DraggerContainer.Add(cm);
            }
        }
        Dictionary<string, bool> MVideoClipDragController = new Dictionary<string, bool>();
        bool IsTimelineActive(string id)
        {
            if (!MVideoClipDragController.ContainsKey(id)) return false;
            else return MVideoClipDragController[id];
        }
        void ToggleTimeline(string id, bool active)
        {
            if (!MVideoClipDragController.ContainsKey(id))
                MVideoClipDragController.Add(id, active);
            else MVideoClipDragController[id]=active;
        }
        void DeactivateMovementAllTimelineCallBack(MouseUpEvent evt)
        {
            MVideoClipDragController.Clear();
        }
        private void InitPreviewTimeline(MVideoClip clip, VisualElement element)
        {
            var previewTimeline = element.Q<VisualElement>(null, "KeyFrameDraggerElement");
            
            previewTimeline.RegisterCallback<MouseDownEvent>((MouseDownEvent evt) => {
                ToggleTimeline(clip.ID, true);
            });
            element.RegisterCallback<MouseMoveEvent>((MouseMoveEvent evt) => {
                
                if (IsTimelineActive(clip.ID))
                {
                    float time = (evt.localMousePosition.x / previewTimeline.contentRect.width);

                    this.ATVPivot.position = clip.GetPosition(time);
                    this.ATVPivot.forward = clip.GetForward(time);

                    this.keyFrameManager.UpdateSceneViewCameraTransform(this.ATVPivot);
                    this.keyFrameManager.UpdateMainCameraTransform(clip.GetPosition(time),clip.GetForward(time));
                }
            });
            element.RegisterCallback<MouseUpEvent>((MouseUpEvent evt) => {
                ToggleTimeline(clip.ID, false);
            });
            /*element.RegisterCallback<MouseOutEvent>((MouseOutEvent evt) => {
                Debug.Log("MouseOut");
                ToggleTimeline(clip.ID, false);
            },TrickleDown.TrickleDown);
            */
        }
        
#endregion

    }
}
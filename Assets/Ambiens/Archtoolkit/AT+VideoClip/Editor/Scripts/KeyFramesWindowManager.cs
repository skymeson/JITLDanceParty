using ambiens.archtoolkit.atvideo.models;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace ambiens.archtoolkit.atvideo
{
    public class KeyFramesWindowManager
    {
        VisualElement rootVisualElement;
        ATVideoWindow m_MainWindow;
        public KeyFramesWindowManager(VisualElement root, ATVideoWindow mainWindow)
        {
            this.rootVisualElement = root;
            this.m_MainWindow = mainWindow;
        }
        
        public void RefreshKeyFramesForClip(MVideoClip clip, VisualElement element)
        {

            var keyFramesContainer = element.Q<VisualElement>(null, "KeyFrameContainer");
            keyFramesContainer.ClearClassList();
            keyFramesContainer.AddToClassList("KeyFrameContainer");
            keyFramesContainer.AddToClassList("KeyFrames_" + clip.KeyFrames.Count);

            keyFramesContainer.Clear();
            foreach (var f in clip.KeyFrames)
            {
                var ele = MakeKeyFrameEle();
                BindKeyFrameElement(f, ele);
                keyFramesContainer.Add(ele);
            }
            clip.RefreshCurves();

            EditorUtility.SetDirty(this.m_MainWindow.sceneContainer);
            AssetDatabase.SaveAssets();

        }

        VisualTreeAsset KeyFrameItemListTemplate = null;
        private void BindKeyFrameElement(MKeyFrame frame, VisualElement ele)
        {

            var kfImage = ele.Q<Image>(null, "KeyFrameImage");
            var kfImageButton = ele.Q<Button>(null, "ImageContainer");

            var lAddButton = ele.Q<Button>(null, "LeftButton");
            var rAddButton = ele.Q<Button>(null, "RightButton");
            var delButton = ele.Q<Button>(null, "DeleteKeyFrameButton");
            var refreshButton = ele.Q<Button>(null, "refreshKeyFramePosition");

            kfImage.name = "IMG_" + frame.ID;

            SetImageForKeyFrame(kfImage, frame.ID);

            lAddButton.clicked += () => this.AddKeyFrameSibling(frame, false);
            rAddButton.clicked += () => this.AddKeyFrameSibling(frame, true);
            delButton.clicked += () =>
            {
                frame.clipRef.KeyFrames.Remove(frame);
                var element = this.rootVisualElement.Q<VisualElement>(frame.clipRef.ID);
                RefreshKeyFramesForClip(frame.clipRef, element);
            };
            refreshButton.clicked += () =>
            {
                SetKeyFrameCameraData( frame);
            };

            kfImageButton.clicked += () =>
            {
                UpdateCameraTransform(frame);
            };

        }

        Transform CameraTransform = null;
        public MKeyFrame CreateKeyFrame()
        {
            var keyFrame = new MKeyFrame();
            keyFrame.ID = GUID.Generate().ToString();

            SetKeyFrameCameraData( keyFrame);

            return keyFrame;
        }
        private void SetKeyFrameCameraData( MKeyFrame keyFrame)
        {

            var sv = (SceneView)SceneView.sceneViews[0];
            CameraTransform = sv.camera.transform;

            keyFrame.CameraPosition = this.CameraTransform.position;
            keyFrame.CameraForward = this.CameraTransform.forward;

            //We record also the pivot point of the scene view, this is used only during the editing mode
            keyFrame.CameraPivot = sv.pivot;

            //Pick Image for keyframe and save it
            this.SaveImageFromSceneView(keyFrame);

            //Refresh Clip curves
            if (keyFrame.clipRef != null)
                keyFrame.clipRef.RefreshCurves();
        }
        public void UpdateCameraTransform(MKeyFrame keyFrame)
        {
            m_MainWindow.ATVPivot.position = keyFrame.CameraPosition;
            m_MainWindow.ATVPivot.forward = keyFrame.CameraForward;

            this.UpdateSceneViewCameraTransform(m_MainWindow.ATVPivot);
            this.UpdateMainCameraTransform(keyFrame.CameraPosition, keyFrame.CameraForward);
        }
        public void UpdateSceneViewCameraTransform(Vector3 pivot, Vector3 forward)
        {
            var sv = (SceneView)SceneView.sceneViews[0];

            sv.pivot = pivot;
            sv.rotation = Quaternion.LookRotation(forward);
            sv.Repaint();
        }
        public void UpdateSceneViewCameraTransform(Transform t)
        {
            var sv = (SceneView)SceneView.sceneViews[0];
            //sv.LookAtDirect(t.position, t.localRotation);
            //sv.LookAtDirect(t.position + sv.camera.transform.forward * sv.cameraDistance, t.localRotation);
            sv.AlignViewToObject(t);
            sv.Repaint();
        }
        public void UpdateMainCameraTransform(Vector3 position, Vector3 forward)
        {
            if(Camera.main){
                var cm = Camera.main.transform;

                cm.position = position;
                cm.forward = forward;
            }
        }
        static string KeyFrameImagePath = "/ATVideoClip/Thumbs/";
        private void SaveImageFromSceneView(MKeyFrame frame)
        {
            var path = Application.dataPath + KeyFrameImagePath;

            Utils.CaptureSceneViewScreenshot(frame.ID, path, (string fPath, Texture2D txt) =>
            {
                frame.Image = txt;
                this.rootVisualElement.Q<Image>("IMG_" + frame.ID).style.backgroundImage = new StyleBackground(txt);
            });
        }
        public Texture2D GetTextureForKeyFrame(MKeyFrame frame)
        {
            var path = "Assets" + KeyFrameImagePath + frame.ID + ".png";
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        }
        private void SetImageForKeyFrame(Image image = null, string id = null)
        {
            if (image == null)
            {
                image = this.rootVisualElement.Q<Image>("IMG_" + id);
            }
            if (image == null) return;

            var localPath = "Assets" + KeyFrameImagePath + id + ".png";

            if (AssetDatabase.LoadAssetAtPath<Texture2D>(localPath) != null)
                image.style.backgroundImage = new StyleBackground(AssetDatabase.LoadAssetAtPath<Texture2D>(localPath));
        }

        private void AddKeyFrameSibling(MKeyFrame frame, bool right)
        {

            var frameIndex = frame.GetCurrentIndex();
            if (right) frameIndex++;

            var newKeyFrame = this.CreateKeyFrame();
            newKeyFrame.clipRef = frame.clipRef;

            frame.clipRef.KeyFrames.Insert(frameIndex, newKeyFrame);

            var element = this.rootVisualElement.Q<VisualElement>(frame.clipRef.ID);

            this.RefreshKeyFramesForClip(frame.clipRef, element);

            EditorUtility.SetDirty(this.m_MainWindow.sceneContainer);
            AssetDatabase.SaveAssets();

        }
        private VisualElement MakeKeyFrameEle()
        {
            if (KeyFrameItemListTemplate == null)
                KeyFrameItemListTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ATVideoWindow.UXMLRootFolder + "MKeyFrameItemTemplate.uxml");

            return KeyFrameItemListTemplate.CloneTree();
        }
    }
}
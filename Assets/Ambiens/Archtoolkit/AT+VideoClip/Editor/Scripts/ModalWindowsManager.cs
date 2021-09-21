using ambiens.archtoolkit.atvideo.models;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ambiens.archtoolkit.atvideo
{
    public class ModalWindowsManager
    {
        VisualElement rootVisualElement;
        ATVideoWindow m_MainWindow;
        //Modal
        public VisualElement Modal;

        public ModalWindowsManager(VisualElement root, ATVideoWindow mainWindow)
        {
            this.rootVisualElement = root;
            this.m_MainWindow = mainWindow;
        }

        //PROJECT SETTINGS MODAL
        List<string> ResNames = new List<string> { "FullHD", "4K", "Instagram Square", "Instagram Story", "360 Mono", "360 Stereo", "Custom" };
        public int CustomResIndex = 6;
        public int _360StereoIndex = 5;
        public List<int> _360Indices = new List<int> { 4, 5 };

        List<Vector2> ResSizes = new List<Vector2>
        {
            new Vector2(1920,1080),//FullHD
            new Vector2(3840,2160),//4K
            new Vector2(1080,1080),//IG Square
            new Vector2(1080,1920),//IG Story

            new Vector2(4096,2048),//360 Mono
            new Vector2(4096,4096),//360 Stereo

            new Vector2(1920,1080),

        };
        List<string> FPSNames = new List<string> { "30FPS", "60FPS" };
        List<float> FPSValues = new List<float>
        {
            30,
            60
        };

        public void OpenProjectSettings(MProject project)
        {
            var ModalContent = this.ShowModalWindow("PROJECT SETTINGS");

            var NameField = new TextField("Project Name: ");
            NameField.value = project.ProjectName;
            ModalContent.Add(NameField);
            NameField.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                project.ProjectName = evt.newValue;
            });
            // Resolution Field.
            var resolutionField = new PopupField<string>("Resolution: ", ResNames, project.ResolutionIndex);
            //normalField.value = ResNames[0];
            ModalContent.Add(resolutionField);

            // Create a new field and assign it its value.
            var framerateField = new PopupField<string>("FrameRate: ", FPSNames, project.FrameRateIndex);
            ModalContent.Add(framerateField);

            var customResField = new Vector2IntField("Custom:");
            customResField.value = new Vector2Int(project.OutputWidth, project.OutputHeight);
            customResField.style.display = (project.ResolutionIndex == CustomResIndex) ? DisplayStyle.Flex : DisplayStyle.None;

            ModalContent.Add(customResField);
            // Mirror value of uxml field into the C# field.
            resolutionField.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                project.ResolutionIndex = ResNames.IndexOf(evt.newValue);

                var size = this.ResSizes[project.ResolutionIndex];
                project.OutputWidth = (int)size.x;
                project.OutputHeight = (int)size.y;

                customResField.style.display = (project.ResolutionIndex == CustomResIndex)
                    ? DisplayStyle.Flex : DisplayStyle.None;

                project.is360 = _360Indices.Contains(project.ResolutionIndex);
                project.is360Stereo = (project.ResolutionIndex == _360StereoIndex);

            });
            framerateField.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                project.FrameRateIndex = FPSNames.IndexOf(evt.newValue);
                project.FrameRate = this.FPSValues[project.FrameRateIndex];

            });

            customResField.RegisterCallback<ChangeEvent<Vector2Int>>((evt) =>
            {
                project.OutputWidth = evt.newValue.x;
                project.OutputHeight = evt.newValue.y;
            });

            var toggleScreenShots = new Toggle("Capture ScreenShots:");
            toggleScreenShots.value = project.TakeScreenShots;
            ModalContent.Add(toggleScreenShots);

            var ScreenShotsInterval = new FloatField("ScreenShots Interval (sec): ");
            ScreenShotsInterval.value = project.ScreenShotsInterval;
            ScreenShotsInterval.style.display = (project.TakeScreenShots) ? DisplayStyle.Flex : DisplayStyle.None;
            ModalContent.Add(ScreenShotsInterval);

            toggleScreenShots.RegisterCallback<ChangeEvent<bool>>((evt) =>
            {
                project.TakeScreenShots = evt.newValue;
                ScreenShotsInterval.style.display = (project.TakeScreenShots) ? DisplayStyle.Flex : DisplayStyle.None;
            });

            ScreenShotsInterval.RegisterCallback<ChangeEvent<float>>((evt) =>
            {
                project.ScreenShotsInterval = evt.newValue;
            });

            var DeleteButton = new Button();
            DeleteButton.text = "DELETE PROJECT";
            DeleteButton.clicked += () => {
                this.ShowConfirmDialog("Delete Project", "Are you sure?", 
                    (bool confirm) => {
                        if (confirm)
                            this.m_MainWindow.DeleteProject(project);
                        else
                            this.OpenProjectSettings(project);
                    });
            };
            ModalContent.Add(DeleteButton);

        }
        public void OpenClipSettingsModal(MVideoClip clip)
        {
            var ModalContent = this.ShowModalWindow("Clip Settings");

            var NameField = new TextField("Clip Name: ");
            NameField.value = clip.Name;
            ModalContent.Add(NameField);
            NameField.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                clip.Name = evt.newValue;
            });

            var DeleteButton = new Button();
            DeleteButton.text = "DELETE CLIP";
            DeleteButton.clicked += () => {DeleteClipWithConfirm(clip);};
            ModalContent.Add(DeleteButton);

        }
        public void DeleteClipWithConfirm(MVideoClip clip)
        {
            this.ShowConfirmDialog("Delete Clip", "Are you sure?",
                    (bool confirm) => {
                        if (confirm)
                        {
                            this.m_MainWindow.DeleteClip(clip);
                        }
                        else
                        {
                            this.OpenClipSettingsModal(clip);
                        }
                    });
        }

        public void OpenClipsTransitionsModal(MVideoClip clip, MTransition transition)
        {
            var modalContent = this.ShowModalWindow("Transition");

            // Create a new field and assign it its value.
            var TypeField = new EnumField("Type:", transition.Type);

            modalContent.Add(TypeField);

            var durationField = new FloatField("Duration:");
            durationField.value = transition.duration;

            modalContent.Add(durationField);

            TypeField.RegisterCallback<ChangeEvent<System.Enum>>((evt) =>
            {
                transition.Type = (MTransition.TransitionType)evt.newValue;

            });

            durationField.RegisterCallback<ChangeEvent<float>>((evt) =>
            {
                transition.duration = evt.newValue;
            });

        }

        public VisualElement ShowConfirmDialog(string title, string message, Action<bool> OnComplete, bool closeOnComplete=true)
        {
            var c=this.ShowModalWindow(title);
            Modal.AddToClassList("ConfirmDialog");

            c.Add(new Label(message));

            this.Modal.Q<Button>("CloseModalButton").clickable = null;
            this.Modal.Q<Button>("CloseModalButton").clicked += () =>{
                if(closeOnComplete)this.CloseModal();
                OnComplete(false);
            };
            this.Modal.Q<Button>("ConfirmModalButton").clickable = null;
            this.Modal.Q<Button>("ConfirmModalButton").clicked += () => {
                
                if (closeOnComplete) this.CloseModal();
                OnComplete(true);
            };

            return c;
        }

        public VisualElement ShowModalWindow(string title, Action OnClose=null)
        {
            if (this.Modal == null)
            {
                this.Modal = this.rootVisualElement.Q<VisualElement>("ModalWindow");
            }
            this.Modal.Q<Button>("CloseModalButton").clickable = null;
            if (OnClose == null)
            {
                this.Modal.Q<Button>("CloseModalButton").clicked+= CloseModal;
            }
            else
            {
                this.Modal.Q<Button>("CloseModalButton").clicked += OnClose;
            }
            

            this.Modal.RemoveFromClassList("ConfirmDialog");
            this.Modal.style.display = DisplayStyle.Flex;
            this.Modal.Q<Label>("ModalWindowTitle").text = title;
            var content = this.Modal.Q<VisualElement>("ModalWindowContent");
            content.Clear();
            return content;
        }
        public void CloseModal()
        {
            this.Modal.style.display = DisplayStyle.None;
        }
    }
}
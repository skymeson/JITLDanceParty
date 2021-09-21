using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ambiens.archtoolkit.atvideo
{
    public class DependenciesManager
    {
        public bool isCheckingDependencies { get; private set; }
        public List<string> dependencies;
        private int CurrentCheckingDependency = 0;
        private AddRequest addRequest;

        private Action OnComplete;
        private Action<float> OnProgress;

        public void ApplyDependencies(List<string> dPackages, Action onComplete, Action<float> onProgress)
        {
            CurrentCheckingDependency = 0;
            this.dependencies = dPackages;
            this.OnComplete = onComplete;
            this.OnProgress = onProgress;

            this.ApplyDependencies();
        }
        private void ApplyDependencies()
        {
            this.isCheckingDependencies = true;

            this.OnProgress((float)CurrentCheckingDependency/(float)this.dependencies.Count);

            if (this.dependencies.Count > CurrentCheckingDependency)
            {
                this.addRequest = Client.Add(this.dependencies[CurrentCheckingDependency]);
                EditorApplication.update += DependenciesProgress;
            }
            else
            {
                this.isCheckingDependencies = false;
                EditorApplication.update -= DependenciesProgress;
                this.OnComplete();
            }
        }

        void DependenciesProgress()
        {
            if (this.addRequest.IsCompleted)
            {
                if (this.addRequest.Status == StatusCode.Success)
                {
                    Debug.Log("Installed: " + this.addRequest.Result.packageId);
                }
                else if (this.addRequest.Status >= StatusCode.Failure)
                    Debug.Log(this.addRequest.Error.message);

                
                CurrentCheckingDependency++;
                this.ApplyDependencies();
                EditorApplication.update -= DependenciesProgress;
            }
        }

        public bool NamespaceExists(string desiredNamespace)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.Namespace == desiredNamespace)
                        return true;
                }
            }
            return false;
        }
        public void AddScriptingSymbol(List<string> symbols)
        {

            string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            List<string> allDefines = definesString.Split(';').ToList();
            allDefines.AddRange(symbols.Except(allDefines));
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", allDefines.ToArray()));
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ambiens.archtoolkit.atvideo
{
    public class Utils
    {
        public static void CaptureSceneViewScreenshot(string name = null, string path = "Assets/", System.Action<string, Texture2D> onComplete = null)
        {
            int width = 256;
            int height = 256;

            GameObject go = new GameObject("fCam");
            Camera cam = go.AddComponent<Camera>();
            cam.farClipPlane = 150;
            var lastSelection = Selection.objects;
            Selection.objects = new GameObject[] { cam.gameObject };
            EditorApplication.delayCall += () =>
            {
                SceneView.lastActiveSceneView.AlignWithView();
                EditorApplication.delayCall += () =>
                {
                    RenderTexture rt = new RenderTexture(width, height, 24);
                    cam.targetTexture = rt;
                    EditorApplication.delayCall += () =>
                    {
                        Texture2D dest = new Texture2D(width, height, TextureFormat.RGB24, true);
                        cam.Render();
                        RenderTexture.active = rt;

                        dest.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                        cam.targetTexture = null;
                        RenderTexture.active = null;
                        GameObject.DestroyImmediate(rt);
                        if (name == null)
                            name = string.Format("img_{0}x{1}_{2}",
                                    width, height,
                                    System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));

                        string fLoc = path + "/" + name + ".png";
                        Directory.CreateDirectory(path);
                        File.WriteAllBytes(fLoc, dest.EncodeToPNG());
                        AssetDatabase.Refresh();

                        EditorApplication.delayCall += () =>
                        {
                            GameObject.DestroyImmediate(go);
                            if (lastSelection != null)
                                Selection.objects = lastSelection;
                            if (onComplete != null)
                            {
                                var localPath = "Assets" + path.Replace(Application.dataPath, "") + name + ".png";
                                onComplete(path + name + ".png", AssetDatabase.LoadAssetAtPath<Texture2D>(localPath));
                            }
                        };
                    };

                };
            };
        }



    }
}
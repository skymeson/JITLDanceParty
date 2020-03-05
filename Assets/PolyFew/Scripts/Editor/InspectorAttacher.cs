/* 
 * PolyFew is built on top of 
 * Unity Mesh Simplifier project by Mattias Edlund  
 * https://github.com/Whinarn/UnityMeshSimplifier
*/


using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityMeshSimplifier;




namespace PolyFew
{
    //[CustomEditor(typeof(GameObject))]
    public class InspectorAttacher //: DecoratorEditor
    {
        //public InspectorAttacher() : base("GameObjectInspector") { }

#pragma warning disable

        private bool resetFlags = true;
        private static HideFlags oldFlags;

        
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmoForMyScript(Transform scr, GizmoType gizmoType)
        {
            //Debug.Log("OnEnable on InspectorAttacher on PolyFew");
            if (Selection.activeGameObject == null) { return; }
            if (Selection.activeTransform == null || Selection.activeTransform is RectTransform) { return; }
            if (Selection.activeGameObject.GetComponent<PolyFewHost>() != null) { return; }
            if (!UtilityServices.CheckIfFeasible(Selection.activeTransform)) { return; }
            if (Application.isPlaying) { return; }


            PrefabType prefabType = PrefabUtility.GetPrefabType(Selection.activeGameObject);

            /*
            if (prefabType != PrefabType.None && prefabType != PrefabType.DisconnectedModelPrefabInstance && prefabType != PrefabType.DisconnectedPrefabInstance && prefabType != PrefabType.MissingPrefabInstance)
            { 
                bool positive = EditorUtility.DisplayDialog("Prefab Instance Detected",
                    "Poly Few doesn't show up for connected prefab instances until you disconnect them. Press \"Disconnect\" to proceed with the prefab disconnection.",
                    "Disconnect",
                    "Cancel");

                if(positive) { PrefabUtility.DisconnectPrefabInstance(Selection.activeGameObject); }
            }
            */

            prefabType = PrefabUtility.GetPrefabType(Selection.activeGameObject);

            if (prefabType != PrefabType.None && prefabType != PrefabType.DisconnectedModelPrefabInstance && prefabType != PrefabType.DisconnectedPrefabInstance && prefabType != PrefabType.MissingPrefabInstance)
            { return; }


            // Attach the inspector hosting script
            if (Selection.activeGameObject != null)
            {
                //Debug.Log("Adding hosting script to gameobject  " +Selection.activeGameObject.name);
                PolyFewHost host = Selection.activeGameObject.AddComponent(typeof(PolyFewHost)) as PolyFewHost;
                
                
                oldFlags = Selection.activeGameObject.hideFlags;
                Selection.activeGameObject.hideFlags = HideFlags.DontSave;
                host.hideFlags = HideFlags.DontSave;


#pragma warning disable

                int moveUp = Selection.activeGameObject.GetComponents<Component>().Length - 2;


                /*
                for (int a = 0; a < moveUp; a++)
                {
                    UnityEditorInternal.ComponentUtility.MoveComponentUp(host);
                }
                */
              

                Selection.activeGameObject.hideFlags = oldFlags;

                var backup = Selection.activeGameObject.GetComponent<LODBackupComponent>();

                if (backup) { backup.hideFlags = HideFlags.HideInInspector; }
                
            }

            else
            {
                Debug.Log("ActiveSelection is null");
            }
        }
    


        void OnEnable()
        {
        }

        void OnDisable()
        {
        }


        /*
        public override void OnInspectorGUI()
        {
            Debug.Log("10");
            if (resetFlags)
            {
                resetFlags = false;

                if (Selection.activeGameObject && Selection.activeGameObject.GetComponent<PolyFewHost>())
                {
                    Selection.activeGameObject.hideFlags = oldFlags;

                    Selection.activeGameObject.GetComponent<PolyFewHost>().hideFlags = HideFlags.DontSave;

                    var backup = Selection.activeGameObject.GetComponent<LODBackupComponent>();

                    if (backup) { backup.hideFlags = HideFlags.HideInInspector; }
                }

            }
        }
        */
    }


}


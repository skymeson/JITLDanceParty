/* 
 * PolyFew is built on top of 
 * Unity Mesh Simplifier project by Mattias Edlund  
 * https://github.com/Whinarn/UnityMeshSimplifier
*/


using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using UnityEditor.Callbacks;
using static PolyFew.UtilityServices;
using UnityEditor.SceneManagement;
using System.Reflection;
using UnityMeshSimplifier;


namespace PolyFew
{
    

    [CustomEditor(typeof(PolyFewHost))]
    public class InspectorDrawer: Editor
    {


        private static bool PreserveBorders { get { return dataContainer.preserveBorders; } set { dataContainer.preserveBorders = value; } }
        private static bool PreserveUVSeams { get { return dataContainer.preserveUVSeams; } set { dataContainer.preserveUVSeams = value; } }
        private static bool PreserveUVFoldover { get { return dataContainer.preserveUVFoldover; } set { dataContainer.preserveUVFoldover = value; } }
        private static bool SmartLinking { get { return dataContainer.smartLinking; } set { dataContainer.smartLinking = value; } }
        private static int MaxIterations { get { return dataContainer.maxIterations; } set { dataContainer.maxIterations = value; } }
        private static float Aggressiveness { get { return dataContainer.aggressiveness; } set { dataContainer.aggressiveness = value; } }
        private static bool ReduceDeep { get { return dataContainer.reduceDeep; } set { dataContainer.reduceDeep = value; } }


        private static int TriangleCount { get { return dataContainer.triangleCount; } set { dataContainer.triangleCount = value; } }
        private static float ReductionStrength { get { return dataContainer.reductionStrength; } set { dataContainer.reductionStrength = value; } }
        private static bool FoldoutAutoLOD { get { return dataContainer.foldoutAutoLOD; } set { dataContainer.foldoutAutoLOD = value; } }
        private static bool IsPreservationActive { get { return dataContainer.isPreservationActive; } set { dataContainer.isPreservationActive = value; } }
        private static Vector3 spherePos;
        private static float SphereDiameter { get { return dataContainer.sphereDiameter; } set { dataContainer.sphereDiameter = value; } }
        private static Transform PrevSelection { get { return dataContainer.prevSelection; } set { dataContainer.prevSelection = value; } }
        private static bool isFeasibleTarget;
        private static string sphereColHex = "#FBFF00C8";
        private static Color sphereColor = UtilityServices.HexToColor(sphereColHex);
        private static Material sphereMat;

        private static Texture icon;
        private static bool toolMainFoldout = true;
        private const string ICON_PATH = "Assets/PolyFew/icons/";
        private const string PREFAB_PATH = "Assets/PolyFew/prefab/sphere.prefab";
#pragma warning disable
        private bool isVersionOk = true;
        private GameObject thisGameObject;
        private static UnityEngine.Object LastDrawer { get { if (dataContainer == null) { return null; }; return dataContainer.lastDrawer; } set { dataContainer.lastDrawer = value; } }
        private Vector3 OldSphereScale { get { return dataContainer.oldSphereScale; } set { dataContainer.oldSphereScale = value; } }
        private bool areAllMeshesSaved;
        private bool applyForOptionsChange;
        private bool ReductionPending { get { return dataContainer.reductionPending; } set { dataContainer.reductionPending = value; } }
        private GameObject PrevFeasibleTarget { get { return dataContainer.prevFeasibleTarget; } set { dataContainer.prevFeasibleTarget = value; } }
        private static bool RunOnThreads { get { return dataContainer.runOnThreads; } set { dataContainer.runOnThreads = value; } }
#pragma warning disable
        private readonly System.Object threadLock1 = new System.Object();
#pragma warning disable
        private readonly System.Object threadLock2 = new System.Object();
        private Func<bool> CheckOnThreads = new Func<bool>(() => { return (TriangleCount >= 1500 && dataContainer.objectMeshPairs.Count >= 2); });



        void OnEnable()
        {

            isFeasibleTarget = UtilityServices.CheckIfFeasible(Selection.activeTransform);


            #region Getting persistent data 

            UtilityServices.savePath = EditorPrefs.HasKey("saveLODPath") ? EditorPrefs.GetString("saveLODPath") : SetAndReturnStringPref("saveLODPath", UtilityServices.savePath);
            string hex = EditorPrefs.HasKey("sphereColHex") ? EditorPrefs.GetString("sphereColHex") : SetAndReturnStringPref("sphereColHex", sphereColHex);
            sphereColor = UtilityServices.HexToColor(hex);

            #endregion Getting persistent data 


            string version = Application.unityVersion.Trim();

            isVersionOk = version.Contains("2017.1") || version.Contains("2017.2") ? false : true;
            if (version.Contains("2015")) { isVersionOk = false; }

            Selection.selectionChanged -= SelectionChanged;
            Selection.selectionChanged += SelectionChanged;


            thisGameObject = Selection.activeGameObject;


            containerObject = UtilityServices.FindObjectFromTags("fcfab0d594624ba9acfb3155f4bd061b", "EditorOnly");
            preservationSphere = UtilityServices.FindObjectFromTags("4bbe6110e6faf2b499fcb86cd896c082", "EditorOnly");

            //prefabPath

            if (!preservationSphere)
            {
                //Debug.Log("Creating sphere object");
                preservationSphere = Instantiate(EditorGUIUtility.Load(PREFAB_PATH) as GameObject);
                preservationSphere.gameObject.GetComponent<MeshRenderer>().enabled = false;
                preservationSphere.tag = "EditorOnly";
                preservationSphere.name = "4bbe6110e6faf2b499fcb86cd896c082";
                preservationSphere.hideFlags = HideFlags.HideAndDontSave;
            }


            sphereMat = preservationSphere.GetComponent<MeshRenderer>().sharedMaterial;


            if (!containerObject)
            {
                //Debug.Log("Creating container object");
                containerObject = new GameObject
                {
                    name = "fcfab0d594624ba9acfb3155f4bd061b",
                    tag = "EditorOnly",
                    hideFlags = HideFlags.HideAndDontSave
                };
                containerObject.AddComponent<DataContainer>();
            }


            UtilityServices.dataContainer = containerObject.GetComponent<DataContainer>();

            if (dataContainer.objectsHistory == null)
            {
                dataContainer.objectsHistory = new DataContainer.ObjectsHistory();
            }

            if (dataContainer.historyAddedObjects == null)
            {
                dataContainer.historyAddedObjects = new List<GameObject>();
            }


            //RestoreVarsFromDataContainer();

            if (preservationSphere)
            {
                preservationSphere.gameObject.GetComponent<MeshRenderer>().enabled = IsPreservationActive;
            }


            LastDrawer = this;


            SelectionChanged();
        }




        void OnDisable()
        {
            //Debug.Log("OnDisable called on InspectorDrawer");

            Selection.selectionChanged -= SelectionChanged;

            if (preservationSphere)
            {
                preservationSphere.gameObject.GetComponent<MeshRenderer>().enabled = false;
            }


        }



        public void OnDestroy()
        {
            //Debug.Log("OnDestroy called on InspectorDrawer");

            if(thisGameObject != null)
            {
                PolyFewHost host = thisGameObject.GetComponent<PolyFewHost>();
                if(host != null) { DestroyImmediate(host); }
            }


            if (Application.isEditor && thisGameObject == null)
            {
                //Debug.Log("Getting into history");
                GameObject key = thisGameObject;

                try
                {
                    if (dataContainer.objectsHistory.ContainsKey(key))
                    {
                        //Debug.Log("Destroyed Undo Histroy for object");
                        DataContainer.UndoRedoOps ops = dataContainer.objectsHistory[key];

                        ops.Destruct();
                        ops = null;
                        dataContainer.objectsHistory.Remove(key);
                    }
                }

                catch(Exception ex)
                {

                }

            }


            if (preservationSphere)
            {
                preservationSphere.gameObject.GetComponent<MeshRenderer>().enabled = false;
            }
        }




        void OnSceneGUI()
        {
            if (isFeasibleTarget && Selection.activeTransform != null)
            {
                PrevFeasibleTarget = Selection.activeTransform.gameObject;
                if ((Tools.current == Tool.Move || Tools.current == Tool.Scale) && IsPreservationActive && !Mathf.Approximately(SphereDiameter, 0))
                {

                    #region Draw custom handles for preservation sphere


                    sphereMat.color = sphereColor;
                    //sphereObject.transform.localScale = new Vector3(sphereRadius, sphereRadius, sphereRadius);
                    //Handles.color = sphereColor;
                    //Handles.SphereHandleCap(0, spherePos, Quaternion.identity, sphereRadius, EventType.Repaint);

                    if (Tools.current == Tool.Move)
                    {
                        Vector3 prevPos = spherePos;

                        spherePos = Handles.DoPositionHandle(preservationSphere.transform.position, Quaternion.identity);
                        preservationSphere.transform.position = spherePos;
                        if (prevPos != spherePos) { Repaint(); }
                    }

                    else
                    {

                        float oldDiameter = SphereDiameter;
                        Vector3 prevScale = new Vector3(SphereDiameter, SphereDiameter, SphereDiameter);

                        Vector3 newScale = Handles.DoScaleHandle(prevScale, spherePos, Quaternion.identity, HandleUtility.GetHandleSize(spherePos));

                        // Scaled with the sphere scaling handle
                        SphereDiameter = UtilityServices.Average(newScale.x, newScale.y, newScale.z);

                        preservationSphere.transform.localScale = new Vector3(SphereDiameter, SphereDiameter, SphereDiameter);
                        spherePos = preservationSphere.transform.position;


                        Vector3 lossyScale = preservationSphere.transform.lossyScale;

                        if (!(Mathf.Approximately(lossyScale.x, lossyScale.y) && Mathf.Approximately(lossyScale.y, lossyScale.z)))
                        {
                            GameObject prevParent = preservationSphere.transform.parent.gameObject;
                            preservationSphere.transform.parent = null;
                            float avg = UtilityServices.Average(lossyScale.x, lossyScale.y, lossyScale.z);
                            preservationSphere.transform.localScale = new Vector3(avg, avg, avg);
                            //sphereObject.transform.localScale = oldSphereScale;  
                            preservationSphere.transform.parent = prevParent.transform;
                        }


                        if (Mathf.Approximately(oldDiameter, SphereDiameter)) { Repaint(); }
                    }

                    #endregion Draw custom handles for preservation sphere


                }
            }

            else if (preservationSphere)
            {
                preservationSphere.gameObject.GetComponent<MeshRenderer>().enabled = false;
            }

            OldSphereScale = preservationSphere.transform.lossyScale;

        }



        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            if (Selection.activeGameObject == null) { return; }

            if(!Selection.activeGameObject.activeSelf) { return; }

            if (!isFeasibleTarget)
            {
                return;
            }

            //SaveVarsToDataContainer();


            toolMainFoldout = EditorGUILayout.Foldout(toolMainFoldout, "");


            EditorGUILayout.BeginVertical("GroupBox");


            #region Title Header

            EditorGUILayout.BeginHorizontal();
            GUIContent content = new GUIContent();

            string saveMeshPath = ICON_PATH + "icon.png";
            icon = EditorGUIUtility.Load(saveMeshPath) as Texture;
            if (icon) GUILayout.Label(icon, GUILayout.Width(30), GUILayout.MaxHeight(30));
            GUILayout.Space(6);

            EditorGUILayout.BeginVertical();
            GUILayout.Space(7);
            var style = GUI.skin.label;
            style.richText = true;  // #FF6347ff4

            //EditorGUILayout.LabelField("<size=13><b><color=#A52A2AFF>POLY FEW</color></b></size>", style);
            if (GUILayout.Button("<size=13><b><color=#A52A2AFF>POLY FEW</color></b></size>", style)) { toolMainFoldout = !toolMainFoldout; }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            #endregion Title Header


            if (toolMainFoldout)
            {

                UtilityServices.DrawHorizontalLine(Color.black, 1, 8);


                #region Section Header


                GUILayout.Space(10);

                EditorGUILayout.BeginHorizontal();


                #region Go Deep

                content = new GUIContent();
                style = GUI.skin.textField;
                style.richText = true;
                RectOffset oldPadding = new RectOffset(style.padding.left, style.padding.right, style.padding.top, style.padding.bottom);

                style.padding = new RectOffset(18, style.padding.right, 3, style.padding.bottom);
                content.text = "<b>Reduce Deep</b>";
                content.tooltip = "Check this option to apply reduction with the current settings to this mesh and all of the children meshes. If this option is unchecked the reduction is only applied to the currently selected object, if it has a mesh.This might be slow for complex object hierarchies containing lots of meshes.";

                EditorGUILayout.LabelField(content, style, GUILayout.Width(120), GUILayout.Height(20));
                GUILayout.Space(10);

                bool prevValue = ReduceDeep;
                ReduceDeep = EditorGUILayout.Toggle(ReduceDeep, GUILayout.Width(28), GUILayout.ExpandWidth(false));
                style.padding = oldPadding;

                if (prevValue != ReduceDeep == true)
                {
                    TriangleCount = UtilityServices.CountTriangles(ReduceDeep, dataContainer.objectMeshPairs, Selection.activeGameObject);
                    RunOnThreads = CheckOnThreads();
                    applyForOptionsChange = true;
                }


                #endregion Go Deep


                GUILayout.Space(14);


                #region Undo / Redo buttons

                content = new GUIContent();

                //GUILayout.FlexibleSpace();
                content.tooltip = "Undo the last reduction operation. Please note that you will have to save the scene to keep these changes persistent";


                GameObject kee = Selection.activeGameObject;

                bool flag1 = true;


                flag1 = !dataContainer.objectsHistory.ContainsKey(kee)
                        || dataContainer.objectsHistory[kee] == null
                        || dataContainer.objectsHistory[kee].undoOperations == null
                        || dataContainer.objectsHistory[kee].undoOperations.Count == 0;


                EditorGUI.BeginDisabledGroup(flag1);
                bool hasLods = false;

                if (!flag1)
                {
                    hasLods = UtilityServices.HasLODs(Selection.activeGameObject);
                }

                content.text = "";
                content.image = EditorGUIUtility.Load(ICON_PATH + "undo.png") as Texture;
                style = GUI.skin.button;

                if (GUILayout.Button(content, style, GUILayout.Width(20), GUILayout.MaxHeight(24), GUILayout.ExpandWidth(true)))
                {
                    if (hasLods)
                    {
                        EditorUtility.DisplayDialog("LODs found under this object", "This object appears to have an LOD group or LOD assets generated. Please remove them first before trying to undo the last reduction operation", "Ok");
                    }
                    else
                    {
                        // undo
                        UtilityServices.ApplyUndoRedoOperation(kee, true);
                        TriangleCount = UtilityServices.CountTriangles(ReduceDeep, dataContainer.objectMeshPairs, Selection.activeGameObject);
                        EditorSceneManager.MarkSceneDirty(Selection.activeGameObject.scene);
                        //EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                        GUIUtility.ExitGUI();
                    }

                }

                EditorGUI.EndDisabledGroup();



                GUILayout.Space(1);

                content.tooltip = "Redo the last undo operation. Please note that you will have to save the scene to keep these changes persistent";
                content.image = EditorGUIUtility.Load(ICON_PATH + "redo.png") as Texture;



                flag1 = !dataContainer.objectsHistory.ContainsKey(kee)
                        || dataContainer.objectsHistory[kee] == null
                        || dataContainer.objectsHistory[kee].redoOperations == null
                        || dataContainer.objectsHistory[kee].redoOperations.Count == 0;


                EditorGUI.BeginDisabledGroup(flag1);

                if (!flag1)
                {
                    hasLods = UtilityServices.HasLODs(Selection.activeGameObject);
                }


                if (GUILayout.Button(content, style, GUILayout.Width(20), GUILayout.MaxHeight(24), GUILayout.ExpandWidth(true)))
                {
                    if (hasLods)
                    {
                        EditorUtility.DisplayDialog("LODs found under this object", "This object appears to have an LOD group or LOD assets generated. Please remove them first before trying to redo the last undo operation", "Ok");
                    }
                    else
                    {
                        //redo
                        UtilityServices.ApplyUndoRedoOperation(kee, false);
                        TriangleCount = UtilityServices.CountTriangles(ReduceDeep, dataContainer.objectMeshPairs, Selection.activeGameObject);
                        //EditorSceneManager.MarkSceneDirty(Selection.activeGameObject.scene); //baw did
                        //EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                        GUIUtility.ExitGUI();
                    }

                }

                EditorGUI.EndDisabledGroup();



                #endregion Undo / Redo buttons



                GUILayout.Space(10);


                #region Apply Changes here



                style = GUI.skin.button;
                style.richText = true;

                Color originalColor = new Color(GUI.backgroundColor.r, GUI.backgroundColor.g, GUI.backgroundColor.b);
                //# ffc14d   60%
                //# F0FFFF   73%
                //# F5F5DC   75%
                GUI.backgroundColor = UtilityServices.HexToColor("#F5F5DC");

                content = new GUIContent();
                content.text = "<size=11> <b><color=#000000>Reduce</color></b> </size>";
                content.tooltip = "Apply reduction to this object with the current settings. If you don't reduce the object the changes will be lost when this object gets out of focus. Please note that you must save this scene after reducing the object otherwise the reduce operation will be reset on Editor restart.";

                EditorGUI.BeginDisabledGroup(!ReductionPending || Mathf.Approximately(ReductionStrength, 0));

                bool didPress = GUILayout.Button(content, style, GUILayout.Width(92), GUILayout.Height(24), GUILayout.ExpandWidth(true));

                EditorGUI.EndDisabledGroup();

                GUI.backgroundColor = originalColor;



                if (didPress)
                {

                    bool prevRedPendingVal = ReductionPending;

                    // Must save the meshes as assets before applying reduction operations
                    if (ReduceDeep)
                    {

                        List<Mesh> originalMeshes = UtilityServices.GetMeshesFromPairs(dataContainer.objectMeshPairs);

                        // The unsaved reduced meshes are the those which have their original meshes in (dataContainer.objectMeshPairs) unsaved as .mesh file
                        HashSet<Mesh> unsavedReducedMeshes = UtilityServices.GetUnsavedReducedMeshes(dataContainer.objectMeshPairs);

                        // Contains copies of the original meshes that will be saved in the undo operations for this object
                        DataContainer.ObjectMeshPairs originalMeshesClones = new DataContainer.ObjectMeshPairs();


                        bool areMeshesSaved = UtilityServices.AreMeshesSavedAsAssets(originalMeshes);


                        // Indicates if the reduction operation is successfully applied to all the target meshes.
                        // If this value is true we can then add this reduce operation to the list of undo operations for this object.
                        bool fullySucceeded = true;


                        try
                        {

                            bool savedJustNow = false;

                            if (!areMeshesSaved)
                            {
                                //Debug.Log("Saving meshes as the meshes weren't saved");

                                int option = EditorUtility.DisplayDialogComplex("Unsaved Meshes",
                                            "The reduce operation won't be applied unless you save the modified meshes under this object. This is also required for keeping the changes persistent and for making prefabs workable for the modified objects. You only have to save the meshes once for an object. You must save this scene after saving the object.",
                                            "Save",
                                            "Cancel",
                                            "Don't Save");

                                List<Mesh> tempUnsavedMeshes = unsavedReducedMeshes.ToList();

                                switch (option)
                                {

                                    case 0:

                                        bool passed = UtilityServices.SaveAllMeshes(tempUnsavedMeshes, savePath, true, (error) =>
                                        {
                                            EditorUtility.DisplayDialog("Cannot Save Meshes", error, "Ok");
                                            areMeshesSaved = false;
                                        });

                                        if (passed)
                                        {
                                            areMeshesSaved = true;
                                        }

                                        break;

                                    case 1:
                                    case 2:
                                        areMeshesSaved = false;
                                        savedJustNow = false;
                                        break;
                                }


                                if (UtilityServices.AreMeshesSavedAsAssets(tempUnsavedMeshes))
                                {
                                    areMeshesSaved = true;
                                    savedJustNow = true;
                                    ReductionPending = false;
                                    ReductionStrength = 0;
                                }

                                else
                                {
                                    areMeshesSaved = false;
                                    savedJustNow = false;
                                }

                            }


                            fullySucceeded = areMeshesSaved;

                            // After successfully saving the original meshes copy the modified properties from the modded meshes to the original meshes in the dataContainer list and add the meshes to the objects
                            if (areMeshesSaved)
                            {

                                //Debug.Log("Original mesh are saved so copying properties");
                                foreach (var kvp in dataContainer.objectMeshPairs)
                                {

                                    GameObject gameObject = kvp.Key;

                                    if (gameObject == null) { continue; }

                                    DataContainer.MeshRendererPair mRendererPair = kvp.Value;

                                    if (mRendererPair.mesh == null) { continue; }


                                    if (mRendererPair.attachedToMeshFilter)
                                    {
                                        MeshFilter filter = gameObject.GetComponent<MeshFilter>();


                                        if (filter != null)
                                        {
                                            Mesh moddedMesh = filter.sharedMesh;

                                            // Do this for those meshes that are just saved and exclude those that aren't saved now and had their original meshes saved before
                                            if (savedJustNow && unsavedReducedMeshes.Contains(moddedMesh))
                                            {
                                                //Debug.Log("Mesh was SavedJustNow  " + moddedMesh.name);

                                                Mesh prevMeshCopy = Instantiate(mRendererPair.mesh);
                                                prevMeshCopy.name = mRendererPair.mesh.name;
                                                DataContainer.MeshRendererPair mRenderPair = new DataContainer.MeshRendererPair(true, prevMeshCopy);
                                                originalMeshesClones.Add(gameObject, mRenderPair);

                                                mRendererPair.mesh = moddedMesh;
                                            }
                                            else
                                            {
                                                Mesh prevMeshCopy = Instantiate(mRendererPair.mesh);
                                                prevMeshCopy.name = mRendererPair.mesh.name;
                                                DataContainer.MeshRendererPair mRenderPair = new DataContainer.MeshRendererPair(true, prevMeshCopy);
                                                originalMeshesClones.Add(gameObject, mRenderPair);

                                                mRendererPair.mesh.MakeSimilarToOtherMesh(moddedMesh);
                                                DestroyImmediate(moddedMesh);
                                            }

                                            filter.sharedMesh = mRendererPair.mesh;

                                        }
                                    }

                                    else
                                    {
                                        SkinnedMeshRenderer sRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();

                                        if (sRenderer != null)
                                        {
                                            Mesh moddedMesh = sRenderer.sharedMesh;

                                            // Do this for those meshes that are just saved and exclude those that aren't saved now and had their original meshes saved before
                                            if (savedJustNow && unsavedReducedMeshes.Contains(moddedMesh))
                                            {
                                                //Debug.Log("Mesh was SavedJustNow  " + moddedMesh.name);

                                                Mesh prevMeshCopy = Instantiate(mRendererPair.mesh);
                                                prevMeshCopy.name = mRendererPair.mesh.name;
                                                DataContainer.MeshRendererPair mRenderPair = new DataContainer.MeshRendererPair(false, prevMeshCopy);
                                                originalMeshesClones.Add(gameObject, mRenderPair);

                                                mRendererPair.mesh = moddedMesh;
                                            }
                                            else
                                            {
                                                Mesh prevMeshCopy = Instantiate(mRendererPair.mesh);
                                                prevMeshCopy.name = mRendererPair.mesh.name;
                                                DataContainer.MeshRendererPair mRenderPair = new DataContainer.MeshRendererPair(false, prevMeshCopy);
                                                originalMeshesClones.Add(gameObject, mRenderPair);

                                                mRendererPair.mesh.MakeSimilarToOtherMesh(moddedMesh);
                                                DestroyImmediate(moddedMesh);
                                            }

                                            sRenderer.sharedMesh = mRendererPair.mesh;
                                        }

                                    }


                                }

                                EditorSceneManager.MarkSceneDirty(Selection.activeGameObject.scene);
                                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                                ReductionPending = false;
                                ReductionStrength = 0;
                            }


                        }

#pragma warning disable

                        catch (Exception ex)
                        {
                            ReductionPending = prevRedPendingVal;
                            fullySucceeded = false;
                        }


                        // Add this operation to the undo ops for this object if reduction operation succeeded          
                        if (fullySucceeded)
                        {
                            // Add here the undo record
                            UtilityServices.SaveRecord(Selection.activeGameObject, true, true, originalMeshesClones);
                        }

                        // Destroy the mesh copies if failed to reduce.
                        else if (originalMeshesClones.Count > 0)
                        {
                            foreach (var item in originalMeshesClones)
                            {
                                item.Value.Destruct();
                            }

                            originalMeshesClones = null;
                        }
                    }

                    else
                    {

                        GameObject gameObject = Selection.activeGameObject;
                        // Contains the copy of the original mesh that will be saved in the undo operations for this object
                        DataContainer.ObjectMeshPairs originalMeshClone = new DataContainer.ObjectMeshPairs();

                        DataContainer.MeshRendererPair mRendererPair = dataContainer.objectMeshPairs[gameObject];

                        Mesh moddedMesh = UtilityServices.GetReducedMesh(gameObject, mRendererPair);
                        bool isMeshPresent = mRendererPair.mesh == null ? false : true;
                        bool isMeshSaved = UtilityServices.IsMeshSavedAsAsset(mRendererPair.mesh);

                        bool savedJustNow = false;

                        // Indicates if the reduction operation is successfully applied to the target mesh.
                        // If this value is true we can then add this reduce operation to the list of undo operations for this object.
                        bool fullySucceeded = true;


                        if (isMeshPresent)
                        {

                            try
                            {
                                if (!isMeshSaved)
                                {
                                    //Debug.Log("Saving mesh as the mesh wasn't saved");
                                    int option = EditorUtility.DisplayDialogComplex("Unsaved Mesh",
                                                "The reduce operation won't be applied unless you save the modified mesh of this object. This is also required for keeping the changes persistent and for making prefabs workable for the modified object. You only have to save the mesh once for an object. You must save this scene after saving the object.",
                                                "Save",
                                                "Cancel",
                                                "Don't Save");


                                    switch (option)
                                    {
                                        case 0:

                                            bool isSuccess = SaveMesh(moddedMesh, savePath, true, (error) =>
                                            {
                                                EditorUtility.DisplayDialog("Cannot Save Mesh", error, "Ok");
                                                isMeshSaved = false;
                                            });

                                            if (isSuccess)
                                            {
                                                isMeshSaved = true;
                                            }

                                            break;

                                        case 1:
                                        case 2:
                                            isMeshSaved = false;
                                            break;
                                    }

                                    if (UtilityServices.IsMeshSavedAsAsset(moddedMesh))
                                    {
                                        isMeshSaved = true;
                                        savedJustNow = true;
                                        ReductionPending = false;
                                        ReductionStrength = 0;
                                    }

                                    else
                                    {
                                        isMeshSaved = false;
                                        savedJustNow = false;
                                    }


                                }

                                fullySucceeded = isMeshSaved;

                                // After successfully saving the modded mesh copy the modified properties from the modded mesh to the original mesh in the dataContainer list and add the mesh to the object
                                if (isMeshSaved)
                                {
                                    //Debug.Log("Object saved so now applying to original mesh");
                                    if (mRendererPair.attachedToMeshFilter)
                                    {
                                        MeshFilter filter = gameObject.GetComponent<MeshFilter>();

                                        if (filter != null)
                                        {
                                            if (savedJustNow)
                                            {
                                                //Debug.Log("SavedJustNow");

                                                Mesh prevMeshCopy = Instantiate(mRendererPair.mesh);
                                                prevMeshCopy.name = mRendererPair.mesh.name;
                                                DataContainer.MeshRendererPair mRenderPair = new DataContainer.MeshRendererPair(true, prevMeshCopy);
                                                originalMeshClone.Add(gameObject, mRenderPair);
                                                //Debug.Log("Created mesh copy for undo  Triangles count  " + mRenderPair.mesh.triangles.Length / 3 + "  modded tris length  " + moddedMesh.triangles.Length / 3 + "  moddedMesh.HashCode  " + moddedMesh.GetHashCode() + "  created undo mesh hashcode   " + mRenderPair.mesh.GetHashCode());

                                                mRendererPair.mesh = moddedMesh;
                                            }
                                            else
                                            {
                                                Mesh prevMeshCopy = Instantiate(mRendererPair.mesh);
                                                prevMeshCopy.name = mRendererPair.mesh.name;
                                                DataContainer.MeshRendererPair mRenderPair = new DataContainer.MeshRendererPair(true, prevMeshCopy);
                                                originalMeshClone.Add(gameObject, mRenderPair);

                                                mRendererPair.mesh.MakeSimilarToOtherMesh(moddedMesh);
                                                DestroyImmediate(moddedMesh);
                                            }

                                            filter.sharedMesh = mRendererPair.mesh;

                                        }
                                    }

                                    else
                                    {
                                        SkinnedMeshRenderer sRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();

                                        if (sRenderer != null)
                                        {
                                            if (savedJustNow)
                                            {
                                                Mesh prevMeshCopy = Instantiate(mRendererPair.mesh);
                                                prevMeshCopy.name = mRendererPair.mesh.name;
                                                DataContainer.MeshRendererPair mRenderPair = new DataContainer.MeshRendererPair(false, prevMeshCopy);
                                                originalMeshClone.Add(gameObject, mRenderPair);

                                                mRendererPair.mesh = moddedMesh;
                                            }
                                            else
                                            {
                                                Mesh prevMeshCopy = Instantiate(mRendererPair.mesh);
                                                prevMeshCopy.name = mRendererPair.mesh.name;
                                                DataContainer.MeshRendererPair mRenderPair = new DataContainer.MeshRendererPair(false, prevMeshCopy);
                                                originalMeshClone.Add(gameObject, mRenderPair);

                                                mRendererPair.mesh.MakeSimilarToOtherMesh(moddedMesh);
                                            }

                                            sRenderer.sharedMesh = mRendererPair.mesh;
                                        }

                                    }

                                    EditorSceneManager.MarkSceneDirty(Selection.activeGameObject.scene);
                                    EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                                    ReductionPending = false;
                                    ReductionStrength = 0;

                                }


                            }
#pragma warning disable

                            catch (Exception ex)
                            {
                                ReductionPending = prevRedPendingVal;
                                fullySucceeded = false;
                            }



                            // Add this operation to the undo ops for this object if reduction operation succeeded          
                            if (fullySucceeded)
                            {
                                // Add here the undo record
                                UtilityServices.SaveRecord(Selection.activeGameObject, false, true, originalMeshClone);
                            }

                            // Destroy the mesh copies if failed to reduce. This might fail as DestroyImmediate() might not be allowed from a secondary thread.
                            else if (originalMeshClone.Count > 0)
                            {
                                // There will be just one value in this case because it's not a reduceDeep operation
                                originalMeshClone[gameObject].Destruct();
                                originalMeshClone = null;
                            }

                        }

                    }

                }


                #endregion Apply Changes here



                EditorGUILayout.EndHorizontal();

                GUILayout.Space(4);

                EditorGUILayout.BeginHorizontal();


                /*
            #region Save Object mesh/es as an asset


            EditorGUI.BeginDisabledGroup(areAllMeshesSaved);

            style = GUI.skin.button;
            style.richText = true;


            content = new GUIContent();
            content.text = "<b><size=11><color=#006699>Save Object</color></size></b>";        
            content.tooltip = "Save this object's mesh and all the modified meshes under this object as assets. This is required if you want to save this object as a prefab. If you create a prefab without saving the meshes as assets, the prefab and all of its children objects will save without referencing any mesh. You only have to save the object once. You must save this scene after saving the object.";


            if(GUILayout.Button(content, style, GUILayout.Width(120), GUILayout.Height(20), GUILayout.ExpandWidth(false)))
            {            

                bool isSuccess = SaveAllMeshesUnderObject((string error)=> 
                {
                    EditorUtility.DisplayDialog("Cannot Save Object", error, "Ok");
                });

            }


            EditorGUI.EndDisabledGroup();

            #endregion Save Object mesh/es as an asset
                 */


                GUILayout.Space(180);

                /*
                #region Reduce Lossless


                style = GUI.skin.button;
                style.richText = true;


                content = new GUIContent();
                content.text = "<b><size=11><color=#000000>Reduce Lossless</color></size></b>";        
                content.tooltip = "Reduce the polygon count of this mesh without loosing too much quality.";


                if(GUILayout.Button(content, style, GUILayout.Width(50), GUILayout.Height(20), GUILayout.ExpandWidth(true)))
                {
                    // Add for recurse children
                    areAllMeshesSaved = AreAllMeshesSavedAsAssets(Selection.activeGameObject, true);
                }



                #endregion Reduce Lossless
                */

                EditorGUILayout.EndHorizontal();


                #endregion Section Header


                #region Section body

                GUILayout.Space(6);
                UtilityServices.DrawHorizontalLine(new Color(105 / 255f, 105 / 255f, 105 / 255f), 1, 5);

                #region  Reduction options

                GUILayout.Space(8);
                content = new GUIContent();
                style = GUI.skin.label;
                style.richText = true;




                EditorGUILayout.BeginHorizontal();

                bool previousValue;

                content.text = "Preserve UV Foldover";
                content.tooltip = "Check this option to preserve UV foldover areas. Usually these are the areas where sharp edges, corners or dents are formed in the mesh or simply the areas where the mesh folds over.";

                EditorGUILayout.LabelField(content, style, GUILayout.Width(130));
                previousValue = PreserveUVFoldover;
                PreserveUVFoldover = EditorGUILayout.Toggle(PreserveUVFoldover, GUILayout.Width(28), GUILayout.ExpandWidth(false));

                if(previousValue != PreserveUVFoldover && !applyForOptionsChange)
                {
                    RunOnThreads = CheckOnThreads();
                    applyForOptionsChange = true;
                }

                GUILayout.Space(12);

                content.text = "Preserve UV Seams";
                content.tooltip = "Preserve the mesh areas where the UV seams are made.These are the areas where different UV islands are formed (usually the shallow polygon conjested areas).";

                EditorGUILayout.LabelField(content, style, GUILayout.Width(120));
                previousValue = PreserveUVSeams;
                PreserveUVSeams = EditorGUILayout.Toggle(PreserveUVSeams, GUILayout.Width(20), GUILayout.ExpandWidth(false));

                if (previousValue != PreserveUVSeams && !applyForOptionsChange)
                {
                    RunOnThreads = CheckOnThreads();
                    applyForOptionsChange = true;
                }

                EditorGUILayout.EndHorizontal();



                GUILayout.Space(2);
                EditorGUILayout.BeginHorizontal();


                content.text = "Preserve Borders";
                content.tooltip = "Check this option to preserve border edges of the mesh. Border edges are the edges that are unconnected and open. Preserving border edges might lead to lesser polygon reduction but can be helpful where you see serious mesh and texture distortions.";

                EditorGUILayout.LabelField(content, style, GUILayout.Width(130));
                previousValue = PreserveBorders;
                PreserveBorders = EditorGUILayout.Toggle(PreserveBorders, GUILayout.Width(28), GUILayout.ExpandWidth(false));

                if (previousValue != PreserveBorders && !applyForOptionsChange)
                {
                    RunOnThreads = CheckOnThreads();
                    applyForOptionsChange = true;
                }

                GUILayout.Space(12);

                content.text = "Smart Linking";
                content.tooltip = "Smart linking links vertices that are very close to each other. This helps in the mesh simplification process where holes or other serious issues could arise. Disabling this (where not needed) can cause a minor performance gain.";

                EditorGUILayout.LabelField(content, style, GUILayout.Width(120));

                previousValue = SmartLinking;
                SmartLinking = EditorGUILayout.Toggle(SmartLinking, GUILayout.Width(20), GUILayout.ExpandWidth(false));

                if (previousValue != SmartLinking && !applyForOptionsChange)
                {
                    RunOnThreads = CheckOnThreads();
                    applyForOptionsChange = true;
                }

                EditorGUILayout.EndHorizontal();


                GUILayout.Space(10);

                GUILayout.BeginHorizontal();


                content.text = "Aggressiveness";
                content.tooltip = "The aggressiveness of the reduction algorithm. Higher number equals higher quality, but more expensive to run. Lowest value is 7.";

                EditorGUILayout.LabelField(content, GUILayout.Width(131));

                content.text = "";
                //aggressiveness = Mathf.Abs(EditorGUILayout.FloatField(content, aggressiveness, GUILayout.Width(168), GUILayout.ExpandWidth(false)));
                float previous = Aggressiveness;
                Aggressiveness = Mathf.Abs(EditorGUILayout.FloatField(content, Aggressiveness, GUILayout.Width(168), GUILayout.ExpandWidth(true)));

                if (Aggressiveness < 7) { Aggressiveness = 7; }

                if (!Mathf.Approximately(previous, Aggressiveness) && !applyForOptionsChange)
                {
                    RunOnThreads = CheckOnThreads();
                    applyForOptionsChange = true;
                }
                
                GUILayout.EndHorizontal();


                GUILayout.Space(2);


                GUILayout.BeginHorizontal();


                content.text = "Max Iterations";
                content.tooltip = "The maximum passes the reduction algorithm does. Higher number is more expensive but can bring you closer to your target quality. 100 is the lowest allowed value.";

                EditorGUILayout.LabelField(content, GUILayout.Width(131));

                content.text = "";
                //maxIterations = Mathf.Abs(EditorGUILayout.IntField(content, maxIterations, GUILayout.Width(168), GUILayout.ExpandWidth(false)));
                int temp = MaxIterations;
                MaxIterations = Mathf.Abs(EditorGUILayout.IntField(content, MaxIterations, GUILayout.Width(168), GUILayout.ExpandWidth(true)));

                if (MaxIterations < 100) { MaxIterations = 100; }

                if (!Mathf.Approximately(temp, MaxIterations) && !applyForOptionsChange)
                {
                    RunOnThreads = CheckOnThreads();
                    applyForOptionsChange = true;
                }

                GUILayout.EndHorizontal();


                GUILayout.Space(10);


                #region Preservation Sphere

                EditorGUILayout.BeginHorizontal();

                content.text = "Tolerance Sphere";
                content.tooltip = "Check this option to enable the tolerance sphere. The tolerance sphere allows you to encompass specific areas of the Mesh that you want to preserve polygons of during the reduction process. This can leave certain areas of the mesh with the original quality by ignoring them during the reduction process. Please note that reduction with preservation sphere might get slow.";

                EditorGUILayout.LabelField(content, style, GUILayout.Width(130));

                previousValue = IsPreservationActive;

                IsPreservationActive = EditorGUILayout.Toggle(IsPreservationActive, GUILayout.Width(28), GUILayout.ExpandWidth(false));

                if (preservationSphere)
                {
                    preservationSphere.gameObject.GetComponent<MeshRenderer>().enabled = IsPreservationActive;
                }

                if (previousValue != IsPreservationActive && !applyForOptionsChange)
                {
                    RunOnThreads = CheckOnThreads();
                    applyForOptionsChange = true;
                }

                GUILayout.Space(13);


                EditorGUI.BeginDisabledGroup(!IsPreservationActive);


                GUILayout.BeginHorizontal();

                style = GUI.skin.label;
                style.richText = true;
                content.text = "Colour";
                content.tooltip = "Change the color of the tolerance sphere.";
                EditorGUILayout.LabelField(content, style, GUILayout.Width(50));
                sphereColor = EditorGUILayout.ColorField(sphereColor, GUILayout.Width(110), GUILayout.ExpandWidth(true));
                sphereColHex = ColorUtility.ToHtmlStringRGBA(sphereColor);
                EditorPrefs.SetString("sphereColHex", sphereColHex);

                if (sphereMat) { sphereMat.color = sphereColor; }

                GUILayout.EndHorizontal();

                EditorGUILayout.EndHorizontal();

                GUILayout.Space(2);


                GUILayout.BeginHorizontal();

                content.text = "Relative Size";
                content.tooltip = "The diameter of the tolerance sphere. Used for controlling the size. The diameter is relative to the selected object's scale. Please note that this cannot take into account the scale factor set in the file itself.";

                EditorGUILayout.LabelField(content, GUILayout.Width(131));


                content.text = "";
                float oldDiameter = SphereDiameter;
                SphereDiameter = Mathf.Abs(EditorGUILayout.FloatField(content, SphereDiameter, GUILayout.Width(100), GUILayout.ExpandWidth(true)));

                if (Mathf.Approximately(oldDiameter, SphereDiameter))
                {
                    preservationSphere.transform.localScale = new Vector3(SphereDiameter, SphereDiameter, SphereDiameter);
                }

                GUILayout.EndHorizontal();


                GUILayout.Space(2);

                GUILayout.BeginHorizontal();

                style = GUI.skin.label;
                style.richText = true;
                content.text = "Position";
                content.tooltip = "The current position values of the preservation sphere in world space.";
                EditorGUILayout.LabelField(content, style, GUILayout.Width(129));
                //EditorGUILayout.LabelField(content, style, GUILayout.Width(118));

                spherePos = EditorGUILayout.Vector3Field("", spherePos, GUILayout.Width(140), GUILayout.ExpandWidth(true));
                preservationSphere.transform.position = spherePos;

                GUILayout.EndHorizontal();

                EditorGUI.EndDisabledGroup();


                #endregion Preservation Sphere


                #endregion Reduction options


                #region Reduction section


                GUILayout.Space(8);
                UtilityServices.DrawHorizontalLine(new Color(105 / 255f, 105 / 255f, 105 / 255f), 1, 5);
                GUILayout.Space(8);


                GUILayout.BeginHorizontal();

                content = new GUIContent();
                style = GUI.skin.label;
                style.richText = true;
                RectOffset prevPadding = new RectOffset(style.padding.left, style.padding.right, style.padding.top, style.padding.bottom);

                style.padding.left = -2;

                content.text = "Reduction Strength";
                content.tooltip = "The intensity of the reduction process. This is the amount in percentage to reduce the model by.";


                GUILayout.Space(4);

                EditorGUILayout.LabelField(content, style, GUILayout.Width(127));
                style.padding = prevPadding;


                if (Mathf.Approximately(ReductionStrength, 0)) { applyForOptionsChange = false; }

                float oldStrength = ReductionStrength;
                bool isMeshless = ReduceDeep ? false : UtilityServices.IsMeshless(Selection.activeTransform);
                hasLods = UtilityServices.HasLODs(Selection.activeGameObject);




                ReductionStrength = Mathf.Abs(GUILayout.HorizontalSlider(ReductionStrength, 0, 100, GUILayout.Width(138), GUILayout.ExpandWidth(true)));


                float quality = 1f - (ReductionStrength / 100f);
                bool isFeasible1 = !Mathf.Approximately(oldStrength, ReductionStrength) && (!isMeshless && !hasLods);
                bool isFeasible2 = applyForOptionsChange && (!isMeshless && !hasLods);


                //Debug.Log("IsFeasible1?   "  +isFeasible1 + " !Mathf.Approximately(oldStrength, reductionStrength)?  " + !Mathf.Approximately(oldStrength, reductionStrength) + "  !Mathf.Approximately(reductionStrength, 0)?  " + !Mathf.Approximately(reductionStrength, 0) + "  Flag?  " + flag + "  ReductionStrenght is  " +reductionStrength);
                //Debug.Log("IsFeasibl2?   "  +isFeasible2 + " applyForReduceDeep?  " + applyForReduceDeep);

                if (!Mathf.Approximately(oldStrength, ReductionStrength))
                {

                    if (isMeshless)
                    {
                        EditorUtility.DisplayDialog("Meshless Object", "This object appears to have no feasible mesh for reduction. You might want to enable \"Reduce Deep\" to consider the nested children for reduction.", "Ok");
                        ReductionStrength = oldStrength;
                    }
                    else if (hasLods)
                    {
                        EditorUtility.DisplayDialog("LODs found under this object", "This object appears to have an LOD group or LOD assets generated. Please remove them first before trying to simplify the mesh for this object", "Ok");
                        ReductionStrength = oldStrength;
                    }
                }

                if (isFeasible1 || isFeasible2)
                {
                    ReductionPending = true;


                    try
                    {
                        // PRESERVATION SPHERE CAUSING SLOW BEHAVIOUR ON REDUCTION
                        if (ReduceDeep)
                        {
                            int prevTriangleCount = TriangleCount;

                            TriangleCount = UtilityServices.SimplifyObjectDeep(dataContainer.objectMeshPairs, RunOnThreads, IsPreservationActive, quality, (string err) =>
                             {
                                 applyForOptionsChange = false;
                                 Debug.LogError(err);
                                 TriangleCount = prevTriangleCount;
                             });
                        }


                        else
                        {
                            DataContainer.MeshRendererPair meshRendererPair;
                            GameObject selectedObject = Selection.activeGameObject;

                            //EditorUtility.DisplayProgressBar("Reducing Mesh", "Simplifying selected object's mesh. Depending on the mesh complexity this might take some time.", 0);

                            if (dataContainer.objectMeshPairs.TryGetValue(selectedObject, out meshRendererPair))
                            {
                                TriangleCount = SimplifyObjectShallow(meshRendererPair, selectedObject, IsPreservationActive, quality);
                            }


                            if (applyForOptionsChange)
                            {
                                //Debug.Log("reduce deep was unchecked so restoring other meshes quality is:   " +quality + "  ISFeasible1?  " + isFeasible1 +  "IsFeasible2  " + isFeasible2 + " !Mathf.Approximately(quality, 0)?  " + !Mathf.Approximately(quality, 0));
                                UtilityServices.RestoreMeshesFromPairs(dataContainer.objectMeshPairs, Selection.activeGameObject);
                            }

                        }


                    }

                    catch (Exception ex)
                    {
                        //EditorUtility.ClearProgressBar();
                        applyForOptionsChange = false;
                    }

                    //areAllMeshesSaved = AreAllMeshesSaved(Selection.activeGameObject, true); Might not need this

                    applyForOptionsChange = false;
                    //EditorUtility.ClearProgressBar();
                }


                style = GUI.skin.textField;

                GUILayout.Space(5);

                content.text = "";

                oldStrength = ReductionStrength;

                ReductionStrength = Mathf.Abs(EditorGUILayout.FloatField(content, ReductionStrength, style, GUILayout.Width(10), GUILayout.ExpandWidth(true)));
                
                if((int)ReductionStrength > 100)
                {
                    ReductionStrength = GetFirstNDigits((int)ReductionStrength, 2);
                }

                if(!Mathf.Approximately(oldStrength, ReductionStrength))
                {
                    applyForOptionsChange = true;
                }
                //GUILayout.Space(2);

                style = GUI.skin.label;
                content.text = "<b><size=13>%</size></b>";
                EditorGUILayout.LabelField(content, style, GUILayout.Width(20));



                GUILayout.EndHorizontal();

                GUILayout.Space(2);

                GUILayout.BeginHorizontal();

                content = new GUIContent();
                style = GUI.skin.label;
                style.richText = true;
                prevPadding = new RectOffset(style.padding.left, style.padding.right, style.padding.top, style.padding.bottom);

                style.padding.left = -2;

                content.text = "Triangles Count";
                content.tooltip = "The current number of triangles in the selected mesh.";

                if (ReduceDeep)
                {
                    content.tooltip = "The total number of triangles in the selected object. Includes the triangles of this mesh as well as all of its children meshes.";
                }

                GUILayout.Space(4);

                EditorGUILayout.LabelField(content, style, GUILayout.Width(127));
                style.padding = prevPadding;

                style = GUI.skin.textField;
                content.text = TriangleCount.ToString();


                //trianglesCount = Mathf.Abs(EditorGUILayout.IntField(content, trianglesCount, style, GUILayout.Width(50), GUILayout.ExpandWidth(true)));
                EditorGUILayout.LabelField(content, style, GUILayout.Width(50), GUILayout.ExpandWidth(true));


                GUILayout.EndHorizontal();


                #endregion Reduction section


                #endregion Section body


                GUILayout.Space(12);

                UtilityServices.DrawHorizontalLine(Color.black, 1, 8);

                #region AUTO LOD


                #region TITLE HEADER

                GUILayout.Space(4);

                EditorGUILayout.BeginHorizontal();

                content = new GUIContent();
                content.text = "<size=13><b><color=#A52A2AFF>AUTOMATIC LOD</color></b></size>";
                content.tooltip = "Expand this section to see options for automatic LOD generation.";

                style = EditorStyles.foldout;
                style.richText = true;  // #FF6347ff  //A52A2AFF
                prevPadding = new RectOffset(style.padding.left, style.padding.right, style.padding.top, style.padding.bottom);
                style.padding = new RectOffset(20, 0, -1, 0);

                GUILayout.Space(19);
                FoldoutAutoLOD = EditorGUILayout.Foldout(FoldoutAutoLOD, content, true, style);

                style.padding = prevPadding;

                style = new GUIStyle();
                style.richText = true;

                EditorGUILayout.EndHorizontal();


                #endregion TITLE HEADER



                if (FoldoutAutoLOD)
                {

                    UtilityServices.DrawHorizontalLine(Color.black, 1, 8);
                    GUILayout.Space(6);

                    #region Section Header


                    GUILayout.Space(6);

                    EditorGUILayout.BeginHorizontal();


                    #region Change Save Path


                    style = GUI.skin.button;
                    style.richText = true;


                    content = new GUIContent();
                    content.text = "<b><size=11><color=#006699>Change Save Path</color></size></b>";
                    content.tooltip = "Change the path where the generated LODs mesh assets will be saved. If you don't select a path the default path will be used.";

                    if (GUILayout.Button(content, style, GUILayout.Width(134), GUILayout.Height(20), GUILayout.ExpandWidth(false)))
                    {
                        string path = EditorUtility.OpenFolderPanel("Choose LOD Assets Save path", savePath, "");

                        //Validate the save path. It might be outside the assets folder   

                        // User pressed the cancel button
                        if (string.IsNullOrWhiteSpace(path)) { }

                        else if (!UtilityServices.IsPathInAssetsDir(path))
                        {
                            EditorUtility.DisplayDialog("Invalid Path", "The path you chose is not valid.Please choose a path that points to a directory that exists in the project's Assets folder.", "Ok");
                        }

                        path = UtilityServices.GetValidFolderPath(path);
                        UtilityServices.savePath = UtilityServices.SetAndReturnStringPref("saveLODPath", path);

                    }

                    EditorGUI.EndDisabledGroup();

                    #endregion Change Save Path


                    GUILayout.Space(40);

                    #region Add LOD Level

                    content = new GUIContent();

                    //GUILayout.FlexibleSpace();
                    content.tooltip = "Add an LOD level.";

                    content.text = "<b>Add</b>";

                    if (GUILayout.Button(content, style, GUILayout.Width(20), GUILayout.MaxHeight(24), GUILayout.ExpandWidth(true)))
                    {
                        //if (dataContainer.currentLodLevelSettings.Count < UtilityServices.MAX_LOD_COUNT)
                        //{
                            dataContainer.currentLodLevelSettings.Add(new DataContainer.LODLevelSettings(0, 0, false, false, false, true, 7, 100, false));
                        //}
                    }


                    #endregion Add LOD Level

                    GUILayout.Space(2);

                    #region Generate LODs


                    style = GUI.skin.button;
                    style.richText = true;

                    originalColor = new Color(GUI.backgroundColor.r, GUI.backgroundColor.g, GUI.backgroundColor.b);
                    //# ffc14d   60%
                    //# F0FFFF   73%
                    //# F5F5DC   75%
                    GUI.backgroundColor = UtilityServices.HexToColor("#F5F5DC");

                    content = new GUIContent();
                    content.text = "<size=11> <b><color=#000000>Generate LODS</color></b> </size>";
                    content.tooltip = "Generate LODs for this mesh with the settings specified. Please note that you must save the scene after successfull generation of LODs.";

                    didPress = GUILayout.Button(content, style, GUILayout.Width(120), GUILayout.Height(24), GUILayout.ExpandWidth(true));

                    GUI.backgroundColor = originalColor;


                    if (didPress)
                    {

                        UtilityServices.RestoreMeshesFromPairs(dataContainer.objectMeshPairs);
                        dataContainer.objectMeshPairs = UtilityServices.GetObjectMeshPairs(Selection.activeGameObject, true, true);
                        ReductionPending = false;
                        ReductionStrength = 0;

                        try
                        {

                            // Delete LOD levels that have 0 screen relative height and 0 reduction strength(Excluding the 1st one)



                            if(dataContainer.currentLodLevelSettings.Count > 1)
                            {
                            
                                for(int a = 1; a < dataContainer.currentLodLevelSettings.Count; a++)
                                {
                                    var lodLevel = dataContainer.currentLodLevelSettings[a];

                                    if(Mathf.Approximately(lodLevel.transitionHeight, 0))
                                    {
                                        dataContainer.currentLodLevelSettings.RemoveAt(a);
                                        a--;
                                    }

                                    if (Mathf.Approximately(lodLevel.reductionStrength, 0))
                                    {
                                        dataContainer.currentLodLevelSettings.RemoveAt(a);
                                        a--;
                                    }
                                }
                            }



                            bool isSuccess = UtilityServices.GenerateLODS(Selection.activeGameObject, dataContainer.currentLodLevelSettings, UtilityServices.savePath); ;
                            EditorUtility.ClearProgressBar();

                            if (isSuccess)
                            {
                                EditorSceneManager.MarkSceneDirty(Selection.activeGameObject.scene);
                                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                            }

                            else
                            {
                                EditorUtility.DisplayDialog("Failed", "Failed to generate LODs", "Ok");
                            }

                            
                        }

                        catch (Exception error)
                        {
                            EditorUtility.ClearProgressBar();
                            EditorUtility.DisplayDialog("Failed to generate LODs. The LODs might be partially generated", error.ToString(), "Ok");
                        }

                    }


                    #endregion Generate LODs

                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(2);

                    EditorGUILayout.BeginHorizontal();


                    string version = Application.unityVersion.Trim();

                    if (version.Contains("2019"))
                    {
                        GUILayout.Space(174);
                    }

                    else
                    {
                        GUILayout.Space(178);
                    }
                    

                    //GUILayout.Space(221);

                    style = GUI.skin.button;
                    style.richText = true;

                    originalColor = new Color(GUI.backgroundColor.r, GUI.backgroundColor.g, GUI.backgroundColor.b);
                    //# ffc14d   60%
                    //# F0FFFF   73%
                    //# F5F5DC   75%
                    //GUI.backgroundColor = UtilityServices.HexToColor("#f9f5f5");

                    content = new GUIContent();
                    content.text = "<size=11><color=#006699><b>Destroy LODs</b></color></size>";
                    content.tooltip = $"Destroy the generated LODs for this mesh. This will also delete the \".mesh\" files in the folder \"{UtilityServices.LOD_ASSETS_PARENT_PATH}\" that were created for this object during the LOD generation process. Please note that you will have to delete the empty folders manually.";



                    bool hasLODs = UtilityServices.HasLODs(Selection.activeGameObject);

                    EditorGUI.BeginDisabledGroup(!hasLODs);

                    didPress = GUILayout.Button(content, style, GUILayout.Height(17), GUILayout.ExpandWidth(true));

                    EditorGUI.EndDisabledGroup();
                    
                    GUI.backgroundColor = originalColor;


                    if (didPress)
                    {
                        bool didSucceed = UtilityServices.DestroyLODs(Selection.activeGameObject);

                        if (didSucceed)
                        {
                            EditorUtility.DisplayDialog("Success", $"Successfully destroyed the LODS and deleted the associated mesh assets. Please note that you must delete the empty folders in the path {LOD_ASSETS_PARENT_PATH} manually.", "Ok");
                            EditorSceneManager.MarkSceneDirty(Selection.activeGameObject.scene);
                            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                        }
                    }


                    EditorGUILayout.EndHorizontal();



                    #endregion Section Header


                    GUILayout.Space(4);


                    #region Draw LOD Level


                    for (int a = 0; a < dataContainer.currentLodLevelSettings.Count; a++)
                    {

                        var lodLevel = dataContainer.currentLodLevelSettings[a];

                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                        content = new GUIContent();  //FF6347ff  //006699
                        content.text = String.Format("<b><color=#3e2723>Level {0}</color></b>", a + 1);

                        if(a == 0)
                        {
                            content.text = String.Format("<b><color=#3e2723>Level {0} (Base)</color></b>", a + 1);
                        }

                        style = GUI.skin.label;
                        style.richText = true;

                        GUILayout.Label(content, style);

                        var previousBackgroundColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(Color.gray.r, Color.gray.g, Color.gray.b, 0.8f);
                        GUIContent deleteLevelButtonContent = new GUIContent("<b><color=#FFFFFFD2>X</color></b>", "Delete this LOD level.");
                        style = GUI.skin.button;
                        style.richText = true;

                        if (GUILayout.Button(deleteLevelButtonContent, GUILayout.Width(20)))
                        {
                            if (dataContainer.currentLodLevelSettings.Count > 2)
                            {
                                dataContainer.currentLodLevelSettings.RemoveAt(a);
                                a--;
                            }
                        }

                        GUI.backgroundColor = previousBackgroundColor;

                        EditorGUILayout.EndHorizontal();


                        GUILayout.Space(6);


                        #region Reduction Strength Slider

                        GUILayout.BeginHorizontal();

                        content = new GUIContent();
                        style = GUI.skin.label;
                        style.richText = true;
                        prevPadding = new RectOffset(style.padding.left, style.padding.right, style.padding.top, style.padding.bottom);

                        content.text = "Reduction Strength";
                        content.tooltip = "The intensity of the reduction process. This is the amount in percentage to reduce the model by in this LOD level. The lower this value the higher will be the quality of this LOD level. For the base level or level 1 you should keep this to 0.";


                        GUILayout.Space(16);

                        EditorGUILayout.LabelField(content, style, GUILayout.Width(115));


                        lodLevel.reductionStrength = Mathf.Abs(GUILayout.HorizontalSlider(lodLevel.reductionStrength, 0, 100, GUILayout.Width(130), GUILayout.ExpandWidth(true)));
                        style = GUI.skin.textField;

                        GUILayout.Space(5);

                        content.text = "";

                        lodLevel.reductionStrength = Mathf.Abs(EditorGUILayout.FloatField(content, lodLevel.reductionStrength, style, GUILayout.Width(10), GUILayout.ExpandWidth(true)));


                        if ((int)lodLevel.reductionStrength > 100)
                        {
                            lodLevel.reductionStrength = GetFirstNDigits((int)lodLevel.reductionStrength, 2);
                        }

                        style = GUI.skin.label;
                        content.text = "<b><size=13>%</size></b>";
                        EditorGUILayout.LabelField(content, style, GUILayout.Width(20));



                        GUILayout.EndHorizontal();

                        #endregion   Reduction Strength Slider


                        #region Screen relative transition height


                        GUILayout.BeginHorizontal();

                        content = new GUIContent();
                        style = GUI.skin.label;
                        style.richText = true;
                        prevPadding = new RectOffset(style.padding.left, style.padding.right, style.padding.top, style.padding.bottom);

                        content.text = "Transition Height";
                        content.tooltip = "The screen relative height controls how far the viewing camera must be from the object before a transition to the next LOD level is made.";


                        GUILayout.Space(16);

                        EditorGUILayout.LabelField(content, style, GUILayout.Width(115));

                        float oldHeight = lodLevel.transitionHeight;
                        lodLevel.transitionHeight = Mathf.Abs(GUILayout.HorizontalSlider(lodLevel.transitionHeight, 0, 1, GUILayout.Width(130), GUILayout.ExpandWidth(true)));
                        style = GUI.skin.textField;

                        GUILayout.Space(5);

                        content.text = "";

                        lodLevel.transitionHeight = Mathf.Abs(EditorGUILayout.FloatField(content, lodLevel.transitionHeight, style, GUILayout.Width(10), GUILayout.ExpandWidth(true)));
                        lodLevel.transitionHeight = Mathf.Clamp01(lodLevel.transitionHeight);
                        
                        if(!Mathf.Approximately(oldHeight, lodLevel.transitionHeight) && a != 0)
                        {
                            float lastLevelHeight = dataContainer.currentLodLevelSettings[a-1].transitionHeight;
                            float currentLevelHeight = lodLevel.transitionHeight;

                            if ((lastLevelHeight - currentLevelHeight) <= 0.05f)
                            {
                                //Debug.Log($"Last level height  {lastLevelHeight}  currentLevelHeight = {currentLevelHeight}  Mathf.Abs(lastLevelHeight - currentLevelHeight)   " +(Mathf.Abs(lastLevelHeight - currentLevelHeight) + "  a is  " + a));
                                lodLevel.transitionHeight = lastLevelHeight - 0.05f;
                                lodLevel.transitionHeight = Mathf.Clamp01(lodLevel.transitionHeight);
                            }
                        }


                        if (!Mathf.Approximately(oldHeight, lodLevel.transitionHeight) && a != (dataContainer.currentLodLevelSettings.Count - 1))
                        {
                            float nextLevelHeight = dataContainer.currentLodLevelSettings[a + 1].transitionHeight;
                            float currentLevelHeight = lodLevel.transitionHeight;

                            if ((currentLevelHeight - nextLevelHeight) <= 0.05f)
                            {
                                //Debug.Log($"Next level height  {nextLevelHeight}  currentLevelHeight = {currentLevelHeight}  Mathf.Abs(lastLevelHeight - currentLevelHeight)   " +(Mathf.Abs(lastLevelHeight - currentLevelHeight) + "  a is  " + a));
                                lodLevel.transitionHeight = nextLevelHeight + 0.05f;
                                lodLevel.transitionHeight = Mathf.Clamp01(lodLevel.transitionHeight);
                            }
                        }



                        GUILayout.Space(24);


                        GUILayout.EndHorizontal();


                        #endregion   Screen relative transition height


                        #region Reduction extra options


                        EditorGUILayout.BeginHorizontal();

                        GUILayout.Space(16);
                        content = new GUIContent();
                        content.text = "Reduction Options";
                        content.tooltip = "Expand this section to see options for mesh simplification for this LOD level.";

                        lodLevel.simplificationOptionsFoldout = EditorGUILayout.Foldout(lodLevel.simplificationOptionsFoldout, content, true);

                        EditorGUILayout.EndHorizontal();

                        if (lodLevel.simplificationOptionsFoldout)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(6);
                            UtilityServices.DrawHorizontalLine(Color.black, 1, 8, 14);
                            EditorGUILayout.EndHorizontal();
                        }

                        EditorGUILayout.BeginHorizontal();

                        GUILayout.Space(16);


                        if (lodLevel.simplificationOptionsFoldout)
                        {

                            style = GUI.skin.label;

                            content.text = "Preserve Borders";
                            content.tooltip = "Check this option to preserve border edges for this LOD level. Border edges are the edges that are unconnected and open. Preserving border edges might lead to lesser polygon reduction but can be helpful where you see serious mesh and texture distortions.";

                            EditorGUILayout.LabelField(content, style, GUILayout.Width(114));
                            lodLevel.preserveBorders = EditorGUILayout.Toggle(lodLevel.preserveBorders, GUILayout.Width(15), GUILayout.ExpandWidth(false));

                            GUILayout.Space(8);

                            content.text = "Preserve UV Foldover";
                            content.tooltip = "Check this option to preserve UV foldover for this LOD level.";

                            EditorGUILayout.LabelField(content, style, GUILayout.Width(135));
                            lodLevel.preserveUVFoldover = EditorGUILayout.Toggle(lodLevel.preserveUVFoldover, GUILayout.Width(18), GUILayout.ExpandWidth(false));


                            EditorGUILayout.EndHorizontal();



                            EditorGUILayout.BeginHorizontal();


                            GUILayout.Space(16);

                            content.text = "Smart Linking";
                            content.tooltip = "Smart linking links vertices that are very close to each other. This helps in the mesh simplification process where holes or other serious issues could arise. Disabling this (where not needed) can cause a minor performance gain.";

                            EditorGUILayout.LabelField(content, style, GUILayout.Width(114));
                            lodLevel.smartLinking = EditorGUILayout.Toggle(lodLevel.smartLinking, GUILayout.Width(10), GUILayout.ExpandWidth(false));

                            GUILayout.Space(13);

                            content.text = "Preserve UV Seams";
                            content.tooltip = "Preserve the mesh areas where the UV seams are made.These are the areas where different UV islands are formed (usually the shallow polygon conjested areas).";

                            EditorGUILayout.LabelField(content, style, GUILayout.Width(135));
                            lodLevel.preserveUVSeams = EditorGUILayout.Toggle(lodLevel.preserveUVSeams, GUILayout.Width(20), GUILayout.ExpandWidth(false));


                            EditorGUILayout.EndHorizontal();


                            GUILayout.BeginHorizontal();


                            GUILayout.Space(16);


                            content.text = "Aggressiveness";
                            content.tooltip = "The agressiveness of the reduction algorithm to use for this LOD level. Higher number equals higher quality, but more expensive to run. Lowest value is 7.";

                            EditorGUILayout.LabelField(content, GUILayout.Width(115));

                            content.text = "";

                            lodLevel.aggressiveness = Mathf.Abs(EditorGUILayout.FloatField(content, lodLevel.aggressiveness, GUILayout.Width(168), GUILayout.ExpandWidth(true)));

                            if (lodLevel.aggressiveness < 7) { lodLevel.aggressiveness = 7; }


                            GUILayout.EndHorizontal();


                            GUILayout.Space(2);


                            GUILayout.BeginHorizontal();

                            GUILayout.Space(16);

                            content.text = "Max Iterations";
                            content.tooltip = "The maximum passes the reduction algorithm does for this LOD level. Higher number is more expensive but can bring you closer to your target quality. 100 is the lowest allowed value.";

                            EditorGUILayout.LabelField(content, GUILayout.Width(115));

                            content.text = "";

                            lodLevel.maxIterations = Mathf.Abs(EditorGUILayout.IntField(content, lodLevel.maxIterations, GUILayout.Width(168), GUILayout.ExpandWidth(true)));

                            if (lodLevel.maxIterations < 100) { lodLevel.maxIterations = 100; }

                        }


                        GUILayout.EndHorizontal();


                        #endregion Reduction extra options


                        #region Regard Tolerance Sphere

                        if (lodLevel.simplificationOptionsFoldout)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(6);
                            UtilityServices.DrawHorizontalLine(Color.black, 1, 8, 14);
                            EditorGUILayout.EndHorizontal();
                        }


                        EditorGUILayout.BeginHorizontal();

                        content.text = "Regard Tolerance";
                        content.tooltip = "Check this option if you want this LOD level to regard the tolerance sphere and retain the original quality of the mesh area enclosed within the tolerance sphere. Please note that the LOD generation for this level with preservation sphere might get slow.";
                        style = GUI.skin.label;

                        GUILayout.Space(16);


                        EditorGUILayout.LabelField(content, style, GUILayout.Width(114));

                        lodLevel.regardTolerance = EditorGUILayout.Toggle(lodLevel.regardTolerance, GUILayout.Width(28), GUILayout.ExpandWidth(false));


                        EditorGUILayout.EndHorizontal();


                        #endregion Regard Tolerance Sphere


                        EditorGUILayout.EndVertical();


                    }



                    #endregion Draw LOD Level


                }


                #endregion AUTO LOD

            }


            EditorGUILayout.EndVertical();


        }





        private void SelectionChanged()
        {

            //Debug.Log("SelectionChanged called on InspectorDrawer");

            if (Selection.activeTransform != null && isFeasibleTarget)
            {
                //Debug.Log("Selection Changed and the target is Feasible  reductionPending?  " + reductionPending);

                bool sameTarget = PrevSelection && Selection.activeTransform.GetHashCode().Equals(PrevSelection.GetHashCode());

                if (ReductionPending && PrevFeasibleTarget != null && !sameTarget)
                {
                    //Debug.Log("Restoring the original meshes as the reductions weren't applied");
                    UtilityServices.RestoreMeshesFromPairs(dataContainer.objectMeshPairs);
                    dataContainer.objectMeshPairs = UtilityServices.GetObjectMeshPairs(Selection.activeGameObject, true, true);
                    ReductionPending = false;
                }

                if (!sameTarget)
                {
                    //Debug.Log("Constructing Object Mesh Pairs");
                    dataContainer.objectMeshPairs = UtilityServices.GetObjectMeshPairs(Selection.activeGameObject, true, true);
                    //foreach(var item in dataContainer.objectMeshPairs) { Debug.Log(item.Key.name); }
                    //Debug.Log("");
                }


                //areAllMeshesSaved = AreAllMeshesSavedAsAssets(Selection.activeGameObject, true);

                //if (prevSelection && !Selection.activeTransform.GetHashCode().Equals(prevSelection.GetHashCode()))
                if (!sameTarget)
                {
                    // reset all params

                    ReductionPending = false;
                    ReductionStrength = 0;

                    dataContainer.currentLodLevelSettings = new List<DataContainer.LODLevelSettings>();

                    dataContainer.currentLodLevelSettings.Add(new DataContainer.LODLevelSettings(0, 0.6f, false, false, false, true, 7, 100, false));
                    dataContainer.currentLodLevelSettings.Add(new DataContainer.LODLevelSettings(30, 0.4f, false, false, false, true, 7, 100, false));
                    dataContainer.currentLodLevelSettings.Add(new DataContainer.LODLevelSettings(60, 0.15f, false, false, false, true, 7, 100, false));
                    FoldoutAutoLOD = false;

                    preservationSphere.transform.position = Selection.activeTransform.position;
                    preservationSphere.transform.parent = Selection.activeTransform;

                    preservationSphere.transform.localScale = new Vector3(SphereDiameter, SphereDiameter, SphereDiameter);


                    Vector3 lossyScale = preservationSphere.transform.lossyScale;

                    if (!(Mathf.Approximately(lossyScale.x, lossyScale.y) && Mathf.Approximately(lossyScale.y, lossyScale.z)))
                    {
                        GameObject prevParent = preservationSphere.transform.parent.gameObject;
                        preservationSphere.transform.parent = null;
                        float avg = UtilityServices.Average(lossyScale.x, lossyScale.y, lossyScale.z);
                        preservationSphere.transform.localScale = new Vector3(avg, avg, avg);
                        //sphereObject.transform.localScale = oldScale;
                        OldSphereScale = preservationSphere.transform.localScale;

                        preservationSphere.transform.parent = prevParent.transform;
                    }

                    preservationSphere.transform.localPosition = new Vector3(preservationSphere.transform.localScale.x + (SphereDiameter / 2), 0, 0);
                    spherePos = preservationSphere.transform.position;

                    TriangleCount = UtilityServices.CountTriangles(ReduceDeep, dataContainer.objectMeshPairs, Selection.activeGameObject);
                    RunOnThreads = CheckOnThreads();
                }

                else if (PrevSelection == null)
                {
                    spherePos = Selection.activeTransform.position;
                    preservationSphere.transform.position = spherePos;
                    preservationSphere.transform.parent = Selection.activeTransform;
                    preservationSphere.transform.localPosition = new Vector3(0.2f, 0, 0);
                }


                PrevSelection = Selection.activeTransform;

            }


        }

        [DidReloadScripts]
        public static void ScriptReloaded()
        {
            // Reset all defaults
        }




        public class FileModificationWarning : UnityEditor.AssetModificationProcessor
        {
            static string[] OnWillSaveAssets(string[] paths)
            {
                int a = 0;
                //Debug.Log("OnWillSaveAssets");

                foreach (string path in paths)
                {
                    if (a == 0 && LastDrawer != null)
                    {
                        UtilityServices.RestoreMeshesFromPairs(dataContainer.objectMeshPairs);
                        dataContainer.objectMeshPairs = UtilityServices.GetObjectMeshPairs(Selection.activeGameObject, true, true);
                        ((InspectorDrawer)LastDrawer).ReductionPending = false;
                        ReductionStrength = 0;
                    }
                    a++;
                    //Debug.Log(path);
                }

                return paths;
            }


            public static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
            {
                //Debug.Log("Deleted   "+ path + "  " + options);
                return AssetDeleteResult.DidNotDelete;
            }

        }
     
    }



}
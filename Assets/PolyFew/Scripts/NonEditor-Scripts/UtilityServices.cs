/* 
 * PolyFew is built on top of 
 * Unity Mesh Simplifier project by Mattias Edlund  
 * https://github.com/Whinarn/UnityMeshSimplifier
*/

#if UNITY_EDITOR



using AsImpL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityMeshSimplifier;
using static PolyFew.DataContainer;



namespace PolyFew
{


    public class UtilityServices : EditorWindow
    {


        private const int OBJECT_HISTORY_LIMIT = 5;
        private const int TOTAL_HISTORY_LIMIT = 50;
        public const int MAX_LOD_COUNT = 8;

#pragma warning disable
        public static int maxConcurrentThreads = SystemInfo.processorCount * 2;

        public const string LOD_PARENT_OBJECT_NAME = "(POLY FEW)_LODS-DON'T-DELETE-MANUALLY";
        public const string LOD_ASSETS_PARENT_PATH = "Assets/POLYFEW_LODs";   // Might delete it as we will use the path specified by the user.
        public static string savePath = "Assets/";




        public enum HandleOrientation
        {
            localAligned,
            globalAligned
        }



        [System.Serializable]
        public struct ChildStateTuple
        {
            public Transform transform;
            public Vector3 position;
            public Quaternion rotation;

            public ChildStateTuple(Transform transform, Vector3 position, Quaternion rotation)
            {
                this.transform = transform;
                this.position = position;
                this.rotation = rotation;
            }
        }


        [System.Serializable]
        public struct ColliderState
        {
            public ColliderType type;
            public Vector3 center;
            public Quaternion rotation;
        }





        public static GameObject preservationSphere;
        public static GameObject containerObject;
        public static DataContainer dataContainer;







        public static void SaveRecord(GameObject forObject, bool isReduceDeep, bool isUndo, DataContainer.ObjectMeshPairs originalMeshesClones)
        {

            if (!dataContainer.objectsHistory.ContainsKey(forObject))
            {

                List<ObjectHistory> undoRecord = new List<ObjectHistory>();
                List<ObjectHistory> redoRecord = new List<ObjectHistory>();


                if (dataContainer.objectsHistory.Count == TOTAL_HISTORY_LIMIT)
                {
                    for (int a = 0; a < dataContainer.historyAddedObjects.Count; a++)
                    {
                        GameObject addedObject = dataContainer.historyAddedObjects[a];

                        if (addedObject == null || !dataContainer.objectsHistory.ContainsKey(addedObject))
                        {
                            dataContainer.historyAddedObjects.RemoveAt(a);
                            a--;
                            continue;
                        }

                        dataContainer.objectsHistory[addedObject].Destruct();
                        dataContainer.objectsHistory.Remove(addedObject);
                        dataContainer.historyAddedObjects.RemoveAt(a);
                        break;
                    }
                }


                if (isUndo)
                {
                    // fill the incoming undo record
                    if (isReduceDeep) { }

                    else { }

                    ObjectHistory undoOperation = new ObjectHistory(isReduceDeep, originalMeshesClones);
                    undoRecord.Add(undoOperation);
                }

                else
                {
                    // fill the incoming redo record
                    if (isReduceDeep) { }

                    else { }

                    ObjectHistory redoOperation = new ObjectHistory(isReduceDeep, originalMeshesClones);
                    redoRecord.Add(redoOperation);
                }

                UndoRedoOps undoRedoOps = new UndoRedoOps(forObject, undoRecord, redoRecord);
                dataContainer.objectsHistory.Add(forObject, undoRedoOps);
                dataContainer.historyAddedObjects.Add(forObject);
            }


            else
            {

                UndoRedoOps undoRedoOps = dataContainer.objectsHistory[forObject];

                if (isUndo)
                {
                    if (isReduceDeep) { }

                    else { }


                    List<ObjectHistory> undoOperations = undoRedoOps.undoOperations;

                    if (undoOperations.Count == OBJECT_HISTORY_LIMIT)
                    {
                        undoOperations[0].Destruct();
                        undoOperations[0] = null;
                        undoOperations.RemoveAt(0);
                    }

                    ObjectHistory undoOperation = new ObjectHistory(isReduceDeep, originalMeshesClones);

                    undoOperations.Add(undoOperation);

                }

                else
                {
                    if (isReduceDeep) { }

                    else { }


                    List<ObjectHistory> redoOperations = undoRedoOps.redoOperations;

                    if (redoOperations.Count == OBJECT_HISTORY_LIMIT)
                    {
                        redoOperations[0].Destruct();
                        redoOperations[0] = null;
                        redoOperations.RemoveAt(0);
                    }

                    ObjectHistory redoOperation = new ObjectHistory(isReduceDeep, originalMeshesClones);

                    redoOperations.Add(redoOperation);
                }

            }

        }




        public static void ApplyUndoRedoOperation(GameObject forObject, bool isUndo)
        {

            if (!dataContainer.objectsHistory.ContainsKey(forObject)) { return; }

            UndoRedoOps undoRedoOps = dataContainer.objectsHistory[forObject];

            if (isUndo)
            {
                if (undoRedoOps == null || undoRedoOps.undoOperations == null || undoRedoOps.undoOperations.Count == 0)
                {
                    return;
                }
            }

            else
            {
                if (undoRedoOps == null || undoRedoOps.redoOperations == null || undoRedoOps.redoOperations.Count == 0)
                {
                    return;
                }
            }



            List<ObjectHistory> operations = isUndo ? undoRedoOps.undoOperations : undoRedoOps.redoOperations;
            ObjectHistory lastOp = operations[operations.Count - 1];


            if (lastOp.isReduceDeep)
            {
                if (isUndo)
                {
                    //Debug.Log("Last undo operation was reduce deep   ObjectMeshPair count  " + lastOp.objectMeshPairs.Count);
                }
                else
                {
                    //Debug.Log("Last redo operation was reduce deep   ObjectMeshPair count  " + lastOp.objectMeshPairs.Count);
                }
            }

            else
            {
                if (isUndo)
                {
                    Debug.Log("Last undo operation was NOT reduce deep   ObjectMeshPair count  " + lastOp.objectMeshPairs.Count);
                }
                else
                {
                    Debug.Log("Last redo operation was NOT reduce deep   ObjectMeshPair count  " + lastOp.objectMeshPairs.Count);
                }
            }



            DataContainer.ObjectMeshPairs originalMeshesClones = new ObjectMeshPairs();

            foreach (var kvp in lastOp.objectMeshPairs)
            {

                MeshRendererPair meshRendererPair = kvp.Value;
                GameObject gameObject = kvp.Key;

                if (gameObject == null) { continue; }
                if (meshRendererPair == null) { continue; }
                if (meshRendererPair.mesh == null) { continue; }


                if (meshRendererPair.attachedToMeshFilter)
                {
                    MeshFilter filter = gameObject.GetComponent<MeshFilter>();

                    if (filter != null)
                    {
                        Mesh origMesh = Instantiate(filter.sharedMesh);
                        MeshRendererPair originalPair = new MeshRendererPair(true, origMesh);
                        originalMeshesClones.Add(gameObject, originalPair);

                        filter.sharedMesh.MakeSimilarToOtherMesh(meshRendererPair.mesh);

                        meshRendererPair.Destruct();
                    }
                }

                else
                {
                    SkinnedMeshRenderer sRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();

                    if (sRenderer != null)
                    {
                        Mesh origMesh = Instantiate(sRenderer.sharedMesh);
                        MeshRendererPair originalPair = new MeshRendererPair(false, origMesh);
                        originalMeshesClones.Add(gameObject, originalPair);

                        sRenderer.sharedMesh.MakeSimilarToOtherMesh(meshRendererPair.mesh);

                        meshRendererPair.Destruct();
                    }
                }
            }


            SaveRecord(forObject, lastOp.isReduceDeep, !isUndo, originalMeshesClones);

            lastOp.objectMeshPairs = null;
            lastOp = null;
            operations.RemoveAt(operations.Count - 1);

        }





        public static int SimplifyObjectDeep(ObjectMeshPairs objectMeshPairs, bool runOnThreads, bool IsPreservationActive, float quality, Action<string> OnError, Action<string, GameObject, MeshRendererPair> OnEachSimplificationError = null, Action<GameObject, MeshRendererPair> OnEachMeshSimplified = null)
        {


            // PRESERVATION SPHERE CAUSING SLOW BEHAVIOUR ON REDUCTION

            int totalMeshCount = objectMeshPairs.Count;
            int meshesHandled = 0;
            int threadsRunning = 0;
            bool isError = false;
            string error = "";
            int triangleCount = 0;

            object threadLock1 = new object();
            object threadLock2 = new object();

            //if(applyForReduceDeep)
            //Debug.Log("reduce deep was checked so executing like slider changed val");


            if (runOnThreads)
            {

                List<CustomMeshActionStructure> meshAssignments = new List<CustomMeshActionStructure>();
                //System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                //watch.Start();

                foreach (var kvp in objectMeshPairs)
                {

                    GameObject gameObject = kvp.Key;

                    if (gameObject == null) { meshesHandled++; continue; }

                    DataContainer.MeshRendererPair meshRendererPair = kvp.Value;

                    if (meshRendererPair.mesh == null) { meshesHandled++; continue; }

                    var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();

                    SetParametersForSimplifier(meshSimplifier);

                    meshSimplifier.Initialize(meshRendererPair.mesh);



                    //while (threadsRunning == maxConcurrentThreads) { } // Don't create another thread if the max limit is reached wait for existing threads to clear


                    threadsRunning++;


                    Vector3 ignoreSphereCenterLocal = gameObject.transform.InverseTransformPoint(preservationSphere.transform.position);
                    float sphereRadius = preservationSphere.transform.lossyScale.x / 2f;

                    Task.Factory.StartNew(() =>
                    {


                        CustomMeshActionStructure structure = new CustomMeshActionStructure
                        (

                            meshRendererPair,

                            gameObject,

                            () =>
                            {
                                var reducedMesh = meshSimplifier.ToMesh();

                                AssignReducedMesh(gameObject, meshRendererPair.mesh, reducedMesh, meshRendererPair.attachedToMeshFilter, true);

                                triangleCount += reducedMesh.triangles.Length / 3;
                            }


                        );


                        try
                        {
                            if (IsPreservationActive)
                            {
                                meshSimplifier.SimplifyMesh(quality, ignoreSphereCenterLocal, sphereRadius);
                            }

                            else
                            {
                                meshSimplifier.SimplifyMesh(quality);
                            }


                        // Create cannot be called from a background thread
                        lock (threadLock1)
                            {
                                meshAssignments.Add(structure);
                            /*
                            meshAssignments.Add(() =>
                            {
                                //Debug.Log("reduced for  " + gameObject.name);

                                var reducedMesh = meshSimplifier.ToMesh();

                                UtilityServices.AssignReducedMesh(gameObject, meshRendererPair.mesh, reducedMesh, meshRendererPair.attachedToMeshFilter, true);

                                triangleCount += reducedMesh.triangles.Length / 3;
                            });
                            */

                                threadsRunning--;
                                meshesHandled++;
                            }
                        }

                        catch (Exception ex)
                        {
                            lock (threadLock2)
                            {
                                threadsRunning--;
                                meshesHandled++;
                                isError = true;
                                error = ex.ToString();
                                //structure?.action();
                                OnEachSimplificationError?.Invoke(error, structure?.gameObject, structure?.meshRendererPair);
                            }
                        }

                    }, CancellationToken.None, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current);


                }

                //Wait for all threads to complete
                //Not reliable sometimes gets stuck
                //System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

                while (meshesHandled < totalMeshCount && !isError) { }


                if (!isError)
                {
                    foreach (CustomMeshActionStructure structure in meshAssignments)
                    {
                        structure?.action();
                        OnEachMeshSimplified?.Invoke(structure?.gameObject, structure?.meshRendererPair);
                    }
                }

                else
                {
                    OnError?.Invoke(error);
                }

                //watch.Stop();
                //Debug.Log("Elapsed Time   " + watch.Elapsed.TotalSeconds + "  isPreservationActive?  " +isPreservationActive + "  reductionStrength   " + reductionStrength);
                //Debug.Log("MESHESHANDLED  " + meshesHandled + "  Threads Allowed?  " + maxConcurrentThreads + "   Elapsed Time   "  +watch.Elapsed.TotalSeconds);

            }

            else
            {
                foreach (var kvp in objectMeshPairs)
                {

                    GameObject gameObject = kvp.Key;

                    if (gameObject == null) { continue; }

                    DataContainer.MeshRendererPair meshRendererPair = kvp.Value;

                    if (meshRendererPair.mesh == null) { continue; }


                    var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();

                    SetParametersForSimplifier(meshSimplifier);

                    //meshSimplifier.VertexLinkDistance = meshSimplifier.VertexLinkDistance / 10f;
                    meshSimplifier.Initialize(meshRendererPair.mesh);

                    Vector3 ignoreSphereCenterLocal = gameObject.transform.InverseTransformPoint(preservationSphere.transform.position);
                    float sphereRadius = preservationSphere.transform.lossyScale.x / 2f;


                    if (IsPreservationActive)
                    {
                        meshSimplifier.SimplifyMesh(quality, ignoreSphereCenterLocal, sphereRadius);
                    }

                    else
                    {
                        meshSimplifier.SimplifyMesh(quality);
                    }

                    var reducedMesh = meshSimplifier.ToMesh();

                    reducedMesh.bindposes = meshRendererPair.mesh.bindposes;   // Might cause issues

                    reducedMesh.name = meshRendererPair.mesh.name.Replace("-POLY_REDUCED", "") + "-POLY_REDUCED";

                    if (meshRendererPair.attachedToMeshFilter)
                    {
                        MeshFilter filter = gameObject.GetComponent<MeshFilter>();

                        if (filter != null)
                        {
                            filter.sharedMesh = reducedMesh;
                        }
                    }

                    else
                    {
                        SkinnedMeshRenderer sRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();

                        if (sRenderer != null)
                        {
                            sRenderer.sharedMesh = reducedMesh;
                        }
                    }

                    triangleCount += reducedMesh.triangles.Length / 3;
                }
            }

            return triangleCount;

        }




        public static int SimplifyObjectShallow(MeshRendererPair meshRendererPair, GameObject selectedObject, bool isPreservationActive, float quality)
        {

            var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();

            UtilityServices.SetParametersForSimplifier(meshSimplifier);

            meshSimplifier.Initialize(meshRendererPair.mesh);

            Vector3 ignoreSphereCenterLocal = Selection.activeTransform.InverseTransformPoint(preservationSphere.transform.position);
            float sphereRadius = preservationSphere.transform.lossyScale.x / 2f;


            if (isPreservationActive)
            {
                meshSimplifier.SimplifyMesh(quality, ignoreSphereCenterLocal, sphereRadius);
            }

            else
            {
                meshSimplifier.SimplifyMesh(quality);
            }

            var reducedMesh = meshSimplifier.ToMesh();

            reducedMesh.bindposes = meshRendererPair.mesh.bindposes;   // Might cause issues

            reducedMesh.name = meshRendererPair.mesh.name.Replace("-POLY_REDUCED", "") + "-POLY_REDUCED";

            if (meshRendererPair.attachedToMeshFilter)
            {
                MeshFilter filter = selectedObject.GetComponent<MeshFilter>();

                if (filter != null)
                {
                    filter.sharedMesh = reducedMesh;
                }
            }

            else
            {
                SkinnedMeshRenderer sRenderer = selectedObject.GetComponent<SkinnedMeshRenderer>();

                if (sRenderer != null)
                {
                    sRenderer.sharedMesh = reducedMesh;
                }
            }

            return reducedMesh.triangles.Length / 3;

        }



        // Returns Tuple<StaticReducedMesh[]. SkinnedReducedMesh[]>[] -- Each Tuple corresponds to a LOD level in order
        public static Tuple<Mesh[], Mesh[]>[] GetReducedMeshes(ref LODGenerator.StaticRenderer[] staticRenderers, ref LODGenerator.SkinnedRenderer[] skinnedRenderers, GameObject forObject, LODLevelSettings[] lodSettings)
        {


            if ((staticRenderers == null || staticRenderers.Length == 0) && (skinnedRenderers == null || skinnedRenderers.Length == 0)) { return null; }

            if (lodSettings == null || lodSettings.Length == 0) { return null; }



            int totalStaticMeshes = staticRenderers == null ? 0 : staticRenderers.Length;
            int totalSkinnedMeshes = skinnedRenderers == null ? 0 : skinnedRenderers.Length;

            int lodLevelsToReduce = 0;

            foreach (var settings in lodSettings)
            {
                if (!Mathf.Approximately(settings.reductionStrength, 0))
                {
                    lodLevelsToReduce++;
                }
            }

            // The total mehses to reduce also includes the meshes for which the LOD level settings say quality = 1 (means don't reduce)
            int totalMeshesToReduce = lodLevelsToReduce * (totalStaticMeshes + totalSkinnedMeshes);
            int meshesHandled = 0;
            int threadsRunning = 0;

#pragma warning disable

            string error = "";

            object threadLock1 = new object();
            object threadLock2 = new object();

            //if(applyForReduceDeep)
            //Debug.Log("reduce deep was checked so executing like slider changed val");


            Tuple<Mesh[], Mesh[]>[] allLevelsReducedMeshes = new Tuple<Mesh[], Mesh[]>[lodSettings.Length];
            List<Action> meshActions = new List<Action>(totalMeshesToReduce);

            for (int a = 0; a < lodSettings.Length; a++)
            {
                Tuple<Mesh[], Mesh[]> lodLevelMeshes = Tuple.Create(new Mesh[totalStaticMeshes], new Mesh[totalSkinnedMeshes]);
                allLevelsReducedMeshes[a] = lodLevelMeshes;
            }



            for (int a = 0; a < lodSettings.Length; a++)
            {

                var lodLevelSettings = lodSettings[a];

                Tuple<Mesh[], Mesh[]> lodLevelMeshes = allLevelsReducedMeshes[a];


                if (staticRenderers != null && staticRenderers.Length > 0)
                {

                    Mesh[] reducedStaticMeshes = lodLevelMeshes.Item1;

                    for (int b = 0; b < staticRenderers.Length; b++)
                    {

                        var renderer = staticRenderers[b];
                        var meshToReduce = renderer.mesh;
                        float quality = (1f - (lodLevelSettings.reductionStrength / 100f));


                        // Simplify the mesh if necessary
                        if (!Mathf.Approximately(quality, 1))
                        {

                            MeshSimplifier meshSimplifier = new MeshSimplifier();
                            SetParametersForSimplifier(meshSimplifier, lodLevelSettings);

                            meshSimplifier.Initialize(meshToReduce);


                            //while (threadsRunning == maxConcurrentThreads) { } // Don't create another thread if the max limit is reached wait for existing threads to clear

                            threadsRunning++;

                            // IF USER HAS DISABLED TOLERANCE SPHERE THE SPHERE SIZE MIGHT BE INACCURATE 
                            Vector3 ignoreSphereCenterLocal = renderer.transform.InverseTransformPoint(preservationSphere.transform.position);
                            float sphereRadius = preservationSphere.transform.lossyScale.x / 2f;

#pragma warning disable

                            int meshToReduceIndex = b;
                            int levelSettings = a;


                            Task.Factory.StartNew(() =>
                            {

                                try
                                {

                                    if (lodLevelSettings.regardTolerance)
                                    {
                                        meshSimplifier.SimplifyMesh(quality, ignoreSphereCenterLocal, sphereRadius);
                                    }

                                    else
                                    {
                                        meshSimplifier.SimplifyMesh(quality);
                                    }


                                // Create cannot be called from a background thread
                                lock (threadLock1)
                                    {

                                        meshActions.Add(() =>
                                        {
                                            Mesh reducedMesh = meshSimplifier.ToMesh();
                                            reducedMesh.bindposes = meshToReduce.bindposes;
                                            reducedMesh.name = meshToReduce.name.Replace("-POLY_REDUCED", "") + "-POLY_REDUCED";

                                            reducedStaticMeshes[meshToReduceIndex] = reducedMesh;
                                        });

                                        threadsRunning--;
                                        meshesHandled++;
                                    }

                                }

                                catch (Exception ex)
                                {  
                                    lock (threadLock2)
                                    {
                                        threadsRunning--;
                                        meshesHandled++;
                                        error = ex.ToString();
                                    }
                                }

                            }, CancellationToken.None, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current);


                        }

                    }
                }


                if (skinnedRenderers != null && skinnedRenderers.Length > 0)
                {

                    Mesh[] reducedSkinnedMeshes = lodLevelMeshes.Item2;

                    for (int b = 0; b < skinnedRenderers.Length; b++)
                    {

                        var renderer = skinnedRenderers[b];
                        var meshToReduce = renderer.mesh;
                        float quality = (1f - (lodLevelSettings.reductionStrength / 100f));

                        // Simplify the mesh if necessary
                        if (!Mathf.Approximately(quality, 1))
                        {

                            MeshSimplifier meshSimplifier = new MeshSimplifier();
                            SetParametersForSimplifier(meshSimplifier, lodLevelSettings);

                            meshSimplifier.Initialize(meshToReduce);


                            //while (threadsRunning == maxConcurrentThreads) { } // Don't create another thread if the max limit is reached wait for existing threads to clear

                            threadsRunning++;

                            // IF USER HAS DISABLED TOLERANCE SPHERE THE SPHERE SIZE MIGHT BE INACCURATE 
                            Vector3 ignoreSphereCenterLocal = renderer.transform.InverseTransformPoint(preservationSphere.transform.position);
                            float sphereRadius = preservationSphere.transform.lossyScale.x / 2f;


                            int meshToReduceIndex = b;


                            Task.Factory.StartNew(() =>
                            {

                                try
                                {

                                    if (lodLevelSettings.regardTolerance)
                                    {
                                        meshSimplifier.SimplifyMesh(quality, ignoreSphereCenterLocal, sphereRadius);
                                    }

                                    else
                                    {
                                        meshSimplifier.SimplifyMesh(quality);
                                    }


                                // Create cannot be called from a background thread
                                lock (threadLock1)
                                    {

                                        meshActions.Add(() =>
                                        {
                                            Mesh reducedMesh = meshSimplifier.ToMesh();
                                            reducedMesh.bindposes = meshToReduce.bindposes;
                                            reducedMesh.name = meshToReduce.name.Replace("-POLY_REDUCED", "") + "-POLY_REDUCED";

                                            reducedSkinnedMeshes[meshToReduceIndex] = reducedMesh;
                                        });

                                        threadsRunning--;
                                        meshesHandled++;
                                    }
                                }

                                catch (Exception ex)
                                {
                                    lock (threadLock2)
                                    {
                                        threadsRunning--;
                                        meshesHandled++;
                                        error = ex.ToString();
                                    }
                                }

                            }, CancellationToken.None, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current);


                        }

                    }

                }


            }




            //Wait for all threads to complete
            //Not reliable sometimes gets stuck

            while (meshesHandled < totalMeshesToReduce)
            {
                EditorUtility.DisplayProgressBar("Generating LODs", $"Reducing Meshes {meshesHandled + 1}/{totalMeshesToReduce}", (float)meshesHandled / totalMeshesToReduce);
            }

            EditorUtility.ClearProgressBar();


            foreach (Action meshAction in meshActions)
            {
                meshAction?.Invoke();
            }



            return allLevelsReducedMeshes;

        }





        /// <summary>
        /// Generates the LODs and sets up a LOD Group for the specified game object.
        /// </summary>
        /// <param name="gameObject">The game object to set up.</param>
        /// <param name="lodLevelSettings">The LOD levels to set up.</param>
        /// <param name="autoCollectRenderers">If the renderers under the game object and any children should be automatically collected.
        /// Enabling this will ignore any renderers defined under each LOD level.</param>
        /// <param name="simplificationOptions">The mesh simplification options.</param>
        /// <param name="saveAssetsPath">The path to where the generated assets should be saved. Can be null or empty to use the default path.</param>
        /// <returns>The generated LOD Group.</returns>
        public static bool GenerateLODS(GameObject forObject, List<LODLevelSettings> lodLevelSettings, string saveAssetsPath)
        {


            #region Pre Checks


            if (forObject == null)
            {
                string error = new System.ArgumentNullException(nameof(forObject)).Message;
                EditorUtility.DisplayDialog("Failed to generate LODs", error, "Ok");
                return false;
            }

            else if (lodLevelSettings == null)
            {
                string error = new System.ArgumentNullException(nameof(lodLevelSettings)).Message;
                EditorUtility.DisplayDialog("Failed to generate LODs", error, "Ok");
                return false;
            }

            var transform = forObject.transform;

            var existingLodParent = transform.Find(LOD_PARENT_OBJECT_NAME);

            if (existingLodParent != null)
            {
                string error = "The game object already appears to have LODs. Please remove them first.";
                EditorUtility.DisplayDialog("Failed to generate LODs", error, "Ok");
                return false;
            }

            var existingLodGroup = forObject.GetComponent<LODGroup>();

            if (existingLodGroup != null)
            {
                string error = "The game object already appears to have an LOD Group. Please remove it first.";
                EditorUtility.DisplayDialog("Failed to generate LODs", error, "Ok");
                return false;
            }

            // Collect all enabled renderers under the game object
            Renderer[] renderersToReduce = GetChildRenderersForLOD(forObject);


            if (renderersToReduce == null || renderersToReduce.Length == 0)
            {
                string error = "No valid renderers found under this object";
                EditorUtility.DisplayDialog("Failed to generate LODs", error, "Ok");
                return false;
            }

            #endregion Pre Checks


            var lodParentGameObject = new GameObject(LOD_PARENT_OBJECT_NAME);
            var lodParent = lodParentGameObject.transform;
            ParentAndResetTransform(lodParent, transform);

            var lodGroup = forObject.AddComponent<LODGroup>();

            var renderersToDisable = new List<Renderer>(renderersToReduce.Length);
            var lodLevels = new LOD[lodLevelSettings.Count];

            string rootPath;
            string uniqueParentPath;

            if (!string.IsNullOrWhiteSpace(saveAssetsPath))
            {
                if (saveAssetsPath != "Assets/")
                {
                    if (saveAssetsPath.EndsWith("/"))
                    {
                        rootPath = LOD_ASSETS_PARENT_PATH + forObject.name + "_LOD_Meshes";
                    }

                    else
                    {
                        rootPath = LOD_ASSETS_PARENT_PATH + "/" + forObject.name + "_LOD_Meshes";
                    }
                }

                else
                {
                    rootPath = LOD_ASSETS_PARENT_PATH + "/" + forObject.name + "_LOD_Meshes";
                }

            }

            else
            {
                rootPath = LOD_ASSETS_PARENT_PATH + "/" + forObject.name + "_LOD_Meshes";
            }


            uniqueParentPath = AssetDatabase.GenerateUniqueAssetPath(rootPath);

            if (!String.IsNullOrWhiteSpace(uniqueParentPath))
            {
                rootPath = uniqueParentPath;
            }


            MeshRenderer[] meshRenderers = null;
            SkinnedMeshRenderer[] skinnedMeshRenderers = null;
            LODGenerator.StaticRenderer[] staticRenderers = null;
            LODGenerator.SkinnedRenderer[] skinnedRenderers = null;


            meshRenderers =
                (from renderer in renderersToReduce
                 where renderer.enabled && renderer as MeshRenderer != null
                 select renderer as MeshRenderer).ToArray();

            skinnedMeshRenderers =
                (from renderer in renderersToReduce
                 where renderer.enabled && renderer as SkinnedMeshRenderer != null
                 select renderer as SkinnedMeshRenderer).ToArray();


            staticRenderers = LODGenerator.GetStaticRenderers(meshRenderers);
            skinnedRenderers = LODGenerator.GetSkinnedRenderers(skinnedMeshRenderers);


            int totalStaticMeshes = staticRenderers == null ? 0 : staticRenderers.Length;
            int totalSkinnedMeshes = skinnedRenderers == null ? 0 : skinnedRenderers.Length;


            int lodLevelsToReduce = 0;

            foreach (var settings in lodLevelSettings)
            {
                if (!Mathf.Approximately(settings.reductionStrength, 0))
                {
                    lodLevelsToReduce++;
                }
            }

            // The total meshes to reduce doesn't include the meshes for which the LOD level settings say quality = 1 (means don't reduce)
            int totalMeshesToReduce = lodLevelsToReduce * (totalStaticMeshes + totalSkinnedMeshes);
            int meshesHandled = 0;


            foreach (var renderer in renderersToReduce)
            {
                renderersToDisable.Add(renderer);
            }




            Tuple<Mesh[], Mesh[]>[] allLevelsReducedMeshes = GetReducedMeshes(ref staticRenderers, ref skinnedRenderers, forObject, lodLevelSettings.ToArray());


            for (int levelIndex = 0; levelIndex < lodLevelSettings.Count; levelIndex++)
            {
                var levelSettings = lodLevelSettings[levelIndex];
                var levelGameObject = new GameObject(string.Format("Level{0:00}", levelIndex + 1));  // Making levels start from 1 not 0(index based)
                var levelTransform = levelGameObject.transform;

                Mesh[] reducedStaticMeshes = allLevelsReducedMeshes[levelIndex].Item1;
                Mesh[] reducedSkinnedMeshes = allLevelsReducedMeshes[levelIndex].Item2;

                ParentAndResetTransform(levelTransform, lodParent);


                var levelRenderers = new List<Renderer>((renderersToReduce != null ? renderersToReduce.Length : 0));
                float quality = (1f - (levelSettings.reductionStrength / 100f));

                #region Reduction Here

                if (staticRenderers != null)
                {
                    for (int rendererIndex = 0; rendererIndex < staticRenderers.Length; rendererIndex++)
                    {
                        LODGenerator.StaticRenderer renderer = staticRenderers[rendererIndex];
                        Mesh mesh = renderer.mesh;
                        Mesh reducedMesh = reducedStaticMeshes[rendererIndex];

                        // Simplify the mesh if necessary
                        if (!Mathf.Approximately(quality, 1))
                        {

                            reducedMesh.bindposes = mesh.bindposes;
                            mesh = reducedMesh;

                            EditorUtility.DisplayProgressBar("Generating LODs", $"Saving Mesh Assets {++meshesHandled}/{totalMeshesToReduce}", (float)meshesHandled / totalMeshesToReduce);

                            SaveLODMeshAsset(mesh, forObject.name, renderer.name, levelIndex, renderer.mesh.name, rootPath);

                            if (renderer.isNewMesh)
                            {
                                DestroyObject(renderer.mesh);
                                renderer.mesh = null;
                            }
                        }

                        string rendererName = string.Format("{0:000}_static_{1}", rendererIndex, renderer.name);
                        var levelRenderer = CreateStaticLevelRenderer(rendererName, levelTransform, renderer.transform, mesh, renderer.materials);
                        levelRenderers.Add(levelRenderer);
                    }
                }

                if (skinnedRenderers != null)
                {
                    for (int rendererIndex = 0; rendererIndex < skinnedRenderers.Length; rendererIndex++)
                    {
                        var renderer = skinnedRenderers[rendererIndex];
                        var mesh = renderer.mesh;
                        Mesh reducedMesh = reducedSkinnedMeshes[rendererIndex];

                        // Simplify the mesh if necessary
                        if (!Mathf.Approximately(quality, 1))
                        {
                            /*
                            MeshSimplifier meshSimplifier = new MeshSimplifier();
                            SetParametersForSimplifier(meshSimplifier, levelSettings);

                            meshSimplifier.Initialize(mesh);
                            meshSimplifier.SimplifyMesh(quality);

                            var simplifiedMesh = meshSimplifier.ToMesh();
                            simplifiedMesh.bindposes = mesh.bindposes;
                            mesh = simplifiedMesh;
                            */

                            reducedMesh.bindposes = mesh.bindposes;
                            mesh = reducedMesh;

                            EditorUtility.DisplayProgressBar("Generating LODs", $"Saving Mesh Assets {++meshesHandled}/{totalMeshesToReduce}", (float)meshesHandled / totalMeshesToReduce);

                            SaveLODMeshAsset(mesh, forObject.name, renderer.name, levelIndex, renderer.mesh.name, rootPath);

                            if (renderer.isNewMesh)
                            {
                                DestroyObject(renderer.mesh);
                                renderer.mesh = null;
                            }
                        }

                        string rendererName = string.Format("{0:000}_skinned_{1}", rendererIndex, renderer.name);
                        var levelRenderer = CreateSkinnedLevelRenderer(rendererName, levelTransform, renderer.transform, mesh, renderer.materials, renderer.rootBone, renderer.bones);
                        levelRenderers.Add(levelRenderer);
                    }
                }

                #endregion Reduction Here
                
                lodLevels[levelIndex] = new LOD(levelSettings.transitionHeight, levelRenderers.ToArray());
            }


            CreateBackup(forObject, lodParent.gameObject, renderersToDisable.ToArray());

            foreach (var renderer in renderersToDisable)
            {
                renderer.enabled = false;
            }
            
            lodGroup.animateCrossFading = false;
            lodGroup.SetLODs(lodLevels);


            EditorUtility.ClearProgressBar();

            return true;
        }



        public static bool HasLODs(GameObject checkFor)
        {
            if (checkFor == null) { return false; }

            GameObject existingLodParent = checkFor.transform.Find(LOD_PARENT_OBJECT_NAME)?.gameObject;

            if (existingLodParent == null)
            {
                existingLodParent = checkFor.GetComponent<LODBackupComponent>()?.lodParentObject;
            }

            var existingLodGroup = checkFor.transform.GetComponent<LODGroup>();

            return (existingLodParent != null || existingLodGroup != null);
        }







        #region From MeshSimplifier


        public static bool DestroyLODs(GameObject gameObject)
        {
            if (gameObject == null)
            {
                string error = new System.ArgumentNullException(nameof(gameObject)).Message;
                EditorUtility.DisplayDialog("Failed to delete LODs", error, "Ok");
                return false;
            }

            RestoreBackup(gameObject);

            var transform = gameObject.transform;
            var lodParent = transform.Find(LOD_PARENT_OBJECT_NAME);

            if (lodParent == null)
            {
                lodParent = gameObject.GetComponent<LODBackupComponent>()?.lodParentObject?.transform;

                if (lodParent == null)
                {
                    EditorUtility.DisplayDialog("Failed to delete LODs", $"Found no LOD parent nested under this Game Object. Did you modify in any way the child object named  \"{LOD_PARENT_OBJECT_NAME}\". If so then you must delete the LODs manually.", "Ok");
                    return false;
                }
            }

            var backUpComponent = gameObject.GetComponent<LODBackupComponent>();

            if (backUpComponent)
            {
                DestroyImmediate(backUpComponent);
            }

            try
            {
                // Destroy LOD assets
                DestroyLODAssets(lodParent);
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
            }


            // Destroy the LOD parent
            DestroyImmediate(lodParent.gameObject);

            // Destroy the LOD Group if there is one
            var lodGroup = gameObject.GetComponent<LODGroup>();

            if (lodGroup != null)
            {
                DestroyImmediate(lodGroup);
            }

            return true;
        }

        private static Renderer[] GetChildRenderersForLOD(GameObject forObject)
        {
            var resultRenderers = new List<Renderer>();
            CollectChildRenderersForLOD(forObject.transform, resultRenderers);
            return resultRenderers.ToArray();
        }

        private static void CollectChildRenderersForLOD(Transform transform, List<Renderer> resultRenderers)
        {

            // Collect the renderers of this transform
            var childRenderers = transform.GetComponents<Renderer>();

            resultRenderers.AddRange(childRenderers);

            int childCount = transform.childCount;

            for (int a = 0; a < childCount; a++)
            {

                // Skip children that are not active
                var childTransform = transform.GetChild(a);

                if (!childTransform.gameObject.activeSelf)
                {
                    continue;
                }


                // If the transform have the identical name as to our LOD Parent GO name, then we also skip it
                if (string.Equals(childTransform.name, LOD_PARENT_OBJECT_NAME))
                {
                    continue;
                }


                // Skip children that has a LOD Group or a LOD Generator Helper component
                if (childTransform.GetComponent<LODGroup>() != null)
                {
                    continue;
                }

                /*
                else if (childTransform.GetComponent<LODGeneratorHelper>() != null)
                {
                    continue;
                }
                */

                // Skip the preservation sphere object
                if (childTransform.hideFlags == HideFlags.HideAndDontSave && childTransform.name == "4bbe6110e6faf2b499fcb86cd896c082") 
                {
                    continue;
                }

                // Continue recursively through the children of this transform
                CollectChildRenderersForLOD(childTransform, resultRenderers);
            }
        }

        private static void ParentAndResetTransform(Transform transform, Transform parentTransform)
        {
            transform.SetParent(parentTransform);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        private static void SaveLODMeshAsset(UnityEngine.Object asset, string gameObjectName, string rendererName, int levelIndex, string meshName, string rootFolderPath)
        {
            gameObjectName = MakePathSafe(gameObjectName);
            rendererName = MakePathSafe(rendererName);
            meshName = MakePathSafe(meshName);
            meshName = string.Format("{0:00}_{1}", levelIndex + 1, meshName);   // Level indices are 0 based

            string path;

            //path = $"{rootFolderPath}/LEVEL_{levelIndex + 1}/{rendererName}/{meshName}.mesh";  // Creates folders for each individual mesh
            path = $"{rootFolderPath}/LEVEL_{levelIndex + 1}/{meshName}.mesh";    // No folders for individual meshes
                                                                                  //Task.Run(()=> { SaveAsset(asset, path); });
            SaveAsset(asset, path);
        }

        private static void CreateBackup(GameObject gameObject, GameObject lodParentObject, Renderer[] originalRenderers)
        {
            var backupComponent = gameObject.AddComponent<LODBackupComponent>();
            backupComponent.hideFlags = HideFlags.HideInInspector;
            backupComponent.OriginalRenderers = originalRenderers;
            backupComponent.lodParentObject = lodParentObject;
        }

        private static void SaveAsset(UnityEngine.Object asset, string path)
        {
#if UNITY_EDITOR
            CreateParentDirectory(path);


            // Make sure that there is no asset with the same path already
            path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(path);

            UnityEditor.AssetDatabase.CreateAsset(asset, path);
#endif
        }

        private static void CreateParentDirectory(string path)
        {

#if UNITY_EDITOR
            int lastSlashIndex = path.LastIndexOf('/');
            if (lastSlashIndex != -1)
            {

                string parentPath = path.Substring(0, lastSlashIndex);
                if (!UnityEditor.AssetDatabase.IsValidFolder(parentPath))
                {
                    lastSlashIndex = parentPath.LastIndexOf('/');
                    if (lastSlashIndex != -1)
                    {
                        string folderName = parentPath.Substring(lastSlashIndex + 1);
                        string folderParentPath = parentPath.Substring(0, lastSlashIndex);
                        CreateParentDirectory(parentPath);

                        UnityEditor.AssetDatabase.CreateFolder(folderParentPath, folderName);
                    }
                    else
                    {
                        UnityEditor.AssetDatabase.CreateFolder(string.Empty, parentPath);
                    }
                }
            }
#endif
        }

        private static string MakePathSafe(string name)
        {
            var sb = new System.Text.StringBuilder(name.Length);
            bool lastWasSeparator = false;
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                {
                    lastWasSeparator = false;
                    sb.Append(c);
                }
                else if (c == '_' || c == '-')
                {
                    if (!lastWasSeparator)
                    {
                        lastWasSeparator = true;
                        sb.Append(c);
                    }
                }
                else
                {
                    if (!lastWasSeparator)
                    {
                        lastWasSeparator = true;
                        sb.Append('_');
                    }
                }
            }
            return sb.ToString();
        }

        private static string ValidateSaveAssetsPath(string saveAssetsPath)
        {
            if (string.IsNullOrEmpty(saveAssetsPath))
                return null;

            saveAssetsPath = saveAssetsPath.Replace('\\', '/');
            saveAssetsPath = saveAssetsPath.Trim('/');

            if (System.IO.Path.IsPathRooted(saveAssetsPath))
                throw new System.InvalidOperationException("The save assets path cannot be rooted.");
            else if (saveAssetsPath.Length > 100)
                throw new System.InvalidOperationException("The save assets path cannot be more than 100 characters long to avoid I/O issues.");

            // Make the path safe
            var pathParts = saveAssetsPath.Split('/');
            for (int i = 0; i < pathParts.Length; i++)
            {
                pathParts[i] = MakePathSafe(pathParts[i]);
            }
            saveAssetsPath = string.Join("/", pathParts);

            return saveAssetsPath;
        }

        private static bool DeleteEmptyDirectory(string path)
        {
#if UNITY_EDITOR
            bool deletedAllSubFolders = true;
            var subFolders = UnityEditor.AssetDatabase.GetSubFolders(path);
            for (int i = 0; i < subFolders.Length; i++)
            {
                if (!DeleteEmptyDirectory(subFolders[i]))
                {
                    deletedAllSubFolders = false;
                }
            }

            if (!deletedAllSubFolders)
                return false;

            string[] assetGuids = UnityEditor.AssetDatabase.FindAssets(string.Empty, new string[] { path });
            if (assetGuids.Length > 0)
                return false;

            return UnityEditor.AssetDatabase.DeleteAsset(path);
#else
            return false;
#endif
        }

        private static MeshRenderer CreateStaticLevelRenderer(string name, Transform parentTransform, Transform originalTransform, Mesh mesh, Material[] materials)
        {
            var levelGameObject = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            var levelTransform = levelGameObject.transform;
            if (originalTransform != null)
            {
                ParentAndOffsetTransform(levelTransform, parentTransform, originalTransform);
            }
            else
            {
                ParentAndResetTransform(levelTransform, parentTransform);
            }

            var meshFilter = levelGameObject.GetComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = levelGameObject.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = materials;
            //SetupLevelRenderer(meshRenderer, ref level);
            return meshRenderer;
        }

        private static SkinnedMeshRenderer CreateSkinnedLevelRenderer(string name, Transform parentTransform, Transform originalTransform, Mesh mesh, Material[] materials, Transform rootBone, Transform[] bones)
        {
            var levelGameObject = new GameObject(name, typeof(SkinnedMeshRenderer));
            var levelTransform = levelGameObject.transform;
            if (originalTransform != null)
            {
                ParentAndOffsetTransform(levelTransform, parentTransform, originalTransform);
            }
            else
            {
                ParentAndResetTransform(levelTransform, parentTransform);
            }

            var skinnedMeshRenderer = levelGameObject.GetComponent<SkinnedMeshRenderer>();
            skinnedMeshRenderer.sharedMesh = mesh;
            skinnedMeshRenderer.sharedMaterials = materials;
            skinnedMeshRenderer.rootBone = rootBone;
            skinnedMeshRenderer.bones = bones;

            return skinnedMeshRenderer;
        }

        private static void DestroyLODAssets(Transform transform)
        {
#if UNITY_EDITOR
            var renderers = transform.GetComponentsInChildren<Renderer>(true);

            if (renderers == null || renderers.Length == 0) { return; }
            int a = 0;

            foreach (var renderer in renderers)
            {

                var meshRenderer = renderer as MeshRenderer;
                var skinnedMeshRenderer = renderer as SkinnedMeshRenderer;

                //Debug.Log($"Deleting LOD Asset {a + 1}/{renderers.Length}   Progress   " + (float)a / renderers.Length);
                EditorUtility.DisplayProgressBar("Destroying LODS", $"Deleting LOD Asset {a + 1}/{renderers.Length}", (float)a / renderers.Length);
                a++;

                if (meshRenderer != null)
                {
                    var meshFilter = meshRenderer.GetComponent<MeshFilter>();
                    if (meshFilter != null)
                    {
                        DestroyLODAsset(meshFilter.sharedMesh);
                    }
                }
                else if (skinnedMeshRenderer != null)
                {
                    DestroyLODAsset(skinnedMeshRenderer.sharedMesh);
                }

                foreach (var material in renderer.sharedMaterials)
                {
                    DestroyLODMaterialAsset(material);
                }

            }

            EditorUtility.DisplayProgressBar("Destroying LODS", $"Deleting LOD Asset {a}/{renderers.Length}", (float)a / renderers.Length);
            EditorUtility.ClearProgressBar();


            // Delete any empty LOD asset directories
            //DeleteEmptyDirectory(LODAssetParentPath.TrimEnd('/'));
#endif
        }

        private static void DestroyLODMaterialAsset(Material material)
        {
            if (material == null)
                return;

#if UNITY_EDITOR
            var shader = material.shader;
            if (shader == null)
                return;

            // We find all texture properties of materials and delete those assets also
            int propertyCount = UnityEditor.ShaderUtil.GetPropertyCount(shader);
            for (int propertyIndex = 0; propertyIndex < propertyCount; propertyIndex++)
            {
                var propertyType = UnityEditor.ShaderUtil.GetPropertyType(shader, propertyIndex);
                if (propertyType == UnityEditor.ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    string propertyName = UnityEditor.ShaderUtil.GetPropertyName(shader, propertyIndex);
                    var texture = material.GetTexture(propertyName);
                    DestroyLODAsset(texture);
                }
            }

            DestroyLODAsset(material);
#endif
        }

        private static void DestroyLODAsset(UnityEngine.Object asset)
        {
            if (asset == null)
                return;

#if UNITY_EDITOR
            // We only delete assets that we have automatically generated
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(asset);
            if (assetPath.StartsWith(LOD_ASSETS_PARENT_PATH))
            {
                UnityEditor.AssetDatabase.DeleteAsset(assetPath);
            }
#endif
        }

        private static void RestoreBackup(GameObject gameObject)
        {
            /*
            var backupComponents = gameObject.GetComponents<LODBackupComponent>();
            foreach (var backupComponent in backupComponents)
            {
                var originalRenderers = backupComponent.OriginalRenderers;
                if (originalRenderers != null)
                {
                    foreach (var renderer in originalRenderers)
                    {
                        if(renderer == null) { continue; }
                        renderer.enabled = true;
                    }
                }
            }

            */
            var backupComponent = gameObject.GetComponent<LODBackupComponent>();

            if (backupComponent == null) { return; }

            var originalRenderers = backupComponent.OriginalRenderers;

            if (originalRenderers != null)
            {
                foreach (var renderer in originalRenderers)
                {
                    if (renderer == null) { continue; }

                    renderer.enabled = true;
                }
            }
        }

        #endregion From MeshSimplifier



        public static void ParentAndOffsetTransform(Transform transform, Transform parentTransform, Transform originalTransform)
        {
            transform.position = originalTransform.position;
            transform.rotation = originalTransform.rotation;
            transform.localScale = originalTransform.lossyScale;
            transform.SetParent(parentTransform, true);
        }




        public static void SetParametersForSimplifier(UnityMeshSimplifier.MeshSimplifier meshSimplifier)
        {
            meshSimplifier.EnableSmartLink = dataContainer.smartLinking;
            meshSimplifier.PreserveUVSeamEdges = dataContainer.preserveUVSeams;
            meshSimplifier.PreserveUVFoldoverEdges = dataContainer.preserveUVFoldover;
            meshSimplifier.PreserveBorderEdges = dataContainer.preserveBorders;
            meshSimplifier.Aggressiveness = dataContainer.aggressiveness;
            meshSimplifier.MaxIterationCount = dataContainer.maxIterations;
        }



        public static void SetParametersForSimplifier(MeshSimplifier meshSimplifier, LODLevelSettings levelSettings)
        {
            meshSimplifier.EnableSmartLink = levelSettings.smartLinking;
            meshSimplifier.PreserveUVSeamEdges = levelSettings.preserveUVSeams;
            meshSimplifier.PreserveUVFoldoverEdges = levelSettings.preserveUVFoldover;
            meshSimplifier.PreserveBorderEdges = levelSettings.preserveBorders;
            meshSimplifier.Aggressiveness = levelSettings.aggressiveness;
            meshSimplifier.MaxIterationCount = levelSettings.maxIterations;
        }



        public static void DrawHorizontalLine(Color color, int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));

            r.height = thickness;
            r.y += padding / 2;
            r.x -= 10;
            r.width += 20;
            EditorGUI.DrawRect(r, color);
        }



        public static void DrawHorizontalLine(Color color, int thickness = 2, int padding = 10, int widthAdder = 20)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));

            r.height = thickness;
            r.y += padding / 2;
            r.x -= 10;
            r.width += widthAdder;
            EditorGUI.DrawRect(r, color);
        }


        public static void DrawVerticalLine(Color color, int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Width(padding + thickness));

            r.width = thickness;
            r.x += padding / 2;
            r.y -= 10;
            r.height += 20;
            EditorGUI.DrawRect(r, color);
        }



        public static Texture2D MakeColoredTexture(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }



        public static Texture2D CreateTexture2DCopy(Texture2D original)
        {

            Texture2D result = new Texture2D(original.width, original.height);
            result.SetPixels(original.GetPixels());
            result.Apply();
            return result;

        }




        public static Color HexToColor(string hex)
        {
            hex = hex.Replace("0x", "");//in case the string is formatted 0xFFFFFF
            hex = hex.Replace("#", "");//in case the string is formatted #FFFFFF
            byte a = 255;//assume fully visible unless specified in hex
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            //Only use alpha if the string has enough characters
            if (hex.Length == 8)
            {
                a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
            }

            return new Color32(r, g, b, a);

        }







        public static bool IsMeshless(Transform transform)
        {
            if (transform == null) { return true; }

            MeshFilter meshFilter = transform.GetComponent<MeshFilter>();
            SkinnedMeshRenderer sRenderer = transform.GetComponent<SkinnedMeshRenderer>();


            if (meshFilter)
            {
                if (meshFilter.sharedMesh != null) { return false; }
            }

            if (sRenderer && sRenderer.enabled)
            {
                if (sRenderer.sharedMesh != null) { return false; }
            }

            return true;
        }




        public static bool CheckIfFeasible(Transform transform)
        {
            if (transform == null || !transform.gameObject.activeInHierarchy) { return false; }

            MeshRenderer[] meshrenderers = transform.GetComponentsInChildren<MeshRenderer>(true);
            SkinnedMeshRenderer[] sMeshRenderers = null;

            if (meshrenderers == null || meshrenderers.Length == 0)
            {
                sMeshRenderers = transform.GetComponentsInChildren<SkinnedMeshRenderer>(true);

                if (sMeshRenderers == null || sMeshRenderers.Length == 0)
                { return false; }
            }

            MeshFilter[] filters = transform.GetComponentsInChildren<MeshFilter>(true);

            if (filters != null)
            {
                foreach (var filter in filters)
                {
                    if (filter.sharedMesh != null) { return true; }
                }
            }

            if (sMeshRenderers == null)
            {
                sMeshRenderers = transform.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            }

            foreach (var skinnedRenderer in sMeshRenderers)
            {
                if (skinnedRenderer.sharedMesh != null) { return true; }
            }


            return false;

        }



        public static bool IsInbuiltAsset(UnityEngine.Object asset)
        {
            return (AssetDatabase.Contains(asset) && (!AssetDatabase.IsSubAsset(asset) && AssetDatabase.GetAssetPath(asset).ToLower().StartsWith("library")));
        }



        public static List<Mesh> GetAllStaticMeshesUnderObject(GameObject go, bool includeInactive, bool includeInbuilt)
        {
            MeshFilter[] meshFilters = go.GetComponentsInChildren<MeshFilter>(includeInactive);
            List<Mesh> meshes = new List<Mesh>();

            if (meshFilters == null || meshFilters.Length == 0) { return null; }


            foreach (var filter in meshFilters)
            {
                // Skip the sphere object
                if (filter.gameObject.hideFlags == HideFlags.HideAndDontSave && filter.gameObject.name == "4bbe6110e6faf2b499fcb86cd896c082")
                {
                    continue;
                }

                if (filter.sharedMesh)
                {
                    if (includeInbuilt)
                    {
                        meshes.Add(filter.sharedMesh);
                    }

                    else if (!IsInbuiltAsset(filter.sharedMesh))
                    {
                        meshes.Add(filter.sharedMesh);
                    }
                }

            }

            if (meshes.Count == 0) { return null; }

            return meshes;
        }



        public static List<Mesh> GetAllSkinnedMeshesUnderObject(GameObject go, bool includeInactive, bool includeInbuilt)
        {
            SkinnedMeshRenderer[] sMeshRenderers = go.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive);
            List<Mesh> meshes = new List<Mesh>();

            if (sMeshRenderers == null || sMeshRenderers.Length == 0) { return null; }


            foreach (var renderer in sMeshRenderers)
            {

                // Skip the sphere object
                if (renderer.gameObject.hideFlags == HideFlags.HideAndDontSave && renderer.gameObject.name == "4bbe6110e6faf2b499fcb86cd896c082")
                {
                    continue;
                }

                if (renderer.sharedMesh)
                {
                    if (includeInbuilt)
                    {
                        meshes.Add(renderer.sharedMesh);
                    }

                    else if (!IsInbuiltAsset(renderer.sharedMesh))
                    {
                        meshes.Add(renderer.sharedMesh);
                    }
                }
            }

            if (meshes.Count == 0) { return null; }

            return meshes;
        }



        public static bool SaveAllMeshesUnderObject(GameObject go, Action<Mesh> SavedMeshCallback, string folderPath, bool includeInactive, bool includeInbuilt, bool optimizeMeshes)
        {
            // This excludes the Tolerance sphere
            List<Mesh> staticMeshes = GetAllStaticMeshesUnderObject(go, includeInactive, includeInbuilt);
            List<Mesh> skinnedMeshes = GetAllSkinnedMeshesUnderObject(go, includeInactive, includeInbuilt);

#pragma warning disable

            string actual = folderPath;
            folderPath = FileUtil.GetProjectRelativePath(folderPath);
            if (!folderPath.EndsWith("/")) { folderPath += "/"; }

            int totalIterations = 0;
            int currentIteration = 0;
            string filePath = "";

            if (staticMeshes != null) { totalIterations = staticMeshes.Count; }
            if (skinnedMeshes != null) { totalIterations += skinnedMeshes.Count; }



            if (staticMeshes != null)
            {
                foreach (var mesh in staticMeshes)
                {

                    EditorUtility.DisplayProgressBar("Saving Object", $"Writing static mesh assets to disk {currentIteration + 1}/{totalIterations}", (float)currentIteration / totalIterations);

                    currentIteration++;

                    bool createdAsset = false;

                    try
                    {
                        if (!IsMeshSavedAsAsset(mesh))
                        {
                            if (optimizeMeshes) { MeshUtility.Optimize(mesh); }

                            //filePath = folderPath + mesh.name + ".asset"; //baw did
                            filePath = folderPath + mesh.name + ".mesh";
                            filePath = AssetDatabase.GenerateUniqueAssetPath(filePath);
                            AssetDatabase.CreateAsset(mesh, filePath);

                            createdAsset = true;
                        }

                        else { createdAsset = false; }
                    }

                    catch (Exception ex)
                    {
                        EditorUtility.ClearProgressBar();
                        Debug.LogError(ex);
                        return false;
                    }

                    EditorUtility.DisplayProgressBar("Saving Object", $"Writing static mesh assets to disk {currentIteration}/{totalIterations}", (float)(currentIteration) / totalIterations);

                    if (createdAsset) { SavedMeshCallback(mesh); }

                }

            }

            if (skinnedMeshes != null)
            {
                foreach (var mesh in skinnedMeshes)
                {

                    EditorUtility.DisplayProgressBar("Saving Object", $"Writing skinned mesh assets to disk {currentIteration + 1}/{totalIterations}", (float)currentIteration / totalIterations);

                    currentIteration++;

                    bool createdAsset = false;

                    try
                    {
                        if (!IsMeshSavedAsAsset(mesh))
                        {
                            if (optimizeMeshes) { MeshUtility.Optimize(mesh); }

                            //filePath = folderPath + "static_REDUCED.asset";  //baw did
                            filePath = folderPath + "static_REDUCED.mesh";
                            filePath = AssetDatabase.GenerateUniqueAssetPath(filePath);
                            AssetDatabase.CreateAsset(mesh, filePath);
                            createdAsset = true;
                        }

                        else { createdAsset = false; }
                    }

                    catch (Exception ex)
                    {
                        EditorUtility.ClearProgressBar();
                        Debug.LogError(ex);
                        return false;
                    }

                    if (createdAsset) { SavedMeshCallback(mesh); }

                }

                EditorUtility.DisplayProgressBar("Saving Object", $"Writing skinned mesh assets to disk {currentIteration}/{totalIterations}", (float)(currentIteration) / totalIterations);

            }

            EditorUtility.ClearProgressBar();

            if (staticMeshes == null && skinnedMeshes == null) { return false; }
            else { return true; }

        }





        public static bool SaveAllMeshes(List<Mesh> meshes, string defaultSavePath, bool optimizeMeshes, Action<string> ErrorCallback)
        {

            string folderPath = EditorUtility.OpenFolderPanel("Save the mesh assets", defaultSavePath, "");


            if (String.IsNullOrWhiteSpace(folderPath))
            {
                ErrorCallback("Failed to save mesh because no path was chosen.");
                return false;
            }

            if (!UtilityServices.IsPathInAssetsDir(folderPath))
            {
                ErrorCallback("Failed to save mesh because the chosen path is not valid.The path must point to a directory that exists in the project's Assets folder.");
                return false;
            }

#pragma warning disable

            string actual = folderPath;
            folderPath = FileUtil.GetProjectRelativePath(folderPath);


            if (!folderPath.EndsWith("/")) { folderPath += "/"; }


            if (meshes == null)
            {
                ErrorCallback("Failed to save meshes because the provided list is empty.");
                return false;
            }


            string filePath = "";
            int totalIterations = meshes.Count;
            int currentIteration = 0;




            foreach (var mesh in meshes)
            {
                currentIteration++;

                if (mesh == null) { continue; }

                EditorUtility.DisplayProgressBar("Saving Object", $"Writing mesh assets to disk {currentIteration}/{meshes.Count}", (float)(currentIteration - 1) / totalIterations);

                try
                {
                    if (!IsMeshSavedAsAsset(mesh))
                    {
                        if (optimizeMeshes) { MeshUtility.Optimize(mesh); }

                        //filePath = folderPath + mesh.name + ".asset";  //baw did
                        filePath = folderPath + mesh.name + ".mesh";
                        filePath = AssetDatabase.GenerateUniqueAssetPath(filePath);
                        AssetDatabase.CreateAsset(mesh, filePath);
                    }

                    else { }

                }

                catch (Exception ex)
                {
                    EditorUtility.ClearProgressBar();
                    Debug.LogError(ex);
                    return false;
                }

            }

            EditorUtility.DisplayProgressBar("Saving Object", $"Writing mesh assets to disk {currentIteration}/{meshes.Count}", (float)(currentIteration) / totalIterations);

            EditorUtility.ClearProgressBar();

            return true;

        }



        public static bool SaveMesh(Mesh mesh, string defaultSavePath, bool optimizeMesh, Action<string> ErrorCallback)
        {

            string folderPath = EditorUtility.OpenFolderPanel("Save the mesh asset", defaultSavePath, "");


            if (String.IsNullOrWhiteSpace(folderPath))
            {
                ErrorCallback("Failed to save mesh because no path was chosen.");
                return false;
            }

            if (!UtilityServices.IsPathInAssetsDir(folderPath))
            {
                ErrorCallback("Failed to save mesh because the chosen path is not valid.The path must point to a directory that exists in the project's Assets folder.");
                return false;
            }


            string actual = folderPath;
            folderPath = FileUtil.GetProjectRelativePath(folderPath);


            if (!folderPath.EndsWith("/")) { folderPath += "/"; }

            string filePath = "";

            if (mesh == null)
            {
                ErrorCallback("Failed to save mesh because the mesh specified is null.");
                return false;
            }

            EditorUtility.DisplayProgressBar("Saving Object", "Writing mesh asset to disk", 0);

            try
            {
                if (!IsMeshSavedAsAsset(mesh))
                {
                    if (optimizeMesh) { MeshUtility.Optimize(mesh); }

                    //filePath = folderPath + mesh.name + ".asset"; //baw did
                    filePath = folderPath + mesh.name + ".mesh";
                    filePath = AssetDatabase.GenerateUniqueAssetPath(filePath);
                    AssetDatabase.CreateAsset(mesh, filePath);
                }
            }

            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                ErrorCallback("Failed to save mesh: " + ex.ToString());
                return false;
            }

            EditorUtility.DisplayProgressBar("Saving Object", "Writing mesh asset to disk", 1);


            EditorUtility.ClearProgressBar();

            return true;

        }




        public static bool AreAllMeshesSavedAsAssets(GameObject go, bool includeInactive)
        {
            Debug.Log("Checking if all meshes are saved");

            // Theses exclude the tolerance sphere
            List<Mesh> staticMeshes = GetAllStaticMeshesUnderObject(go, includeInactive, true);
            List<Mesh> skinnedMeshes = GetAllSkinnedMeshesUnderObject(go, includeInactive, true);


            if (staticMeshes != null)
            {
                foreach (var mesh in staticMeshes)
                {
                    //Debug.Log("Static Mesh Path  " + AssetDatabase.GetAssetPath(mesh));

                    if (!IsMeshSavedAsAsset(mesh)) { return false; }
                }
            }

            if (skinnedMeshes != null)
            {
                foreach (var mesh in skinnedMeshes)
                {
                    //Debug.Log("Skinned Mesh Path  " + AssetDatabase.GetAssetPath(mesh));

                    if (!IsMeshSavedAsAsset(mesh)) { return false; }
                }
            }


            return true;
        }




        public static bool AreMeshesSavedAsAssets(List<Mesh> meshes)
        {

            if (meshes == null || meshes.Count == 0) { return false; }

            foreach (var mesh in meshes)
            {
                if (!IsMeshSavedAsAsset(mesh)) { return false; }
            }

            return true;
        }




        public static bool AreMeshesSavedAsAssets(DataContainer.ObjectMeshPairs objMeshPairs)
        {

            if (objMeshPairs == null || objMeshPairs.Count == 0) { return false; }

            foreach (var kvp in objMeshPairs)
            {
                if (kvp.Key == null || kvp.Value.mesh == null) { continue; }

                if (!IsMeshSavedAsAsset(kvp.Value.mesh)) { return false; }
            }

            return true;
        }




        public static bool IsMeshSavedAsAsset(GameObject go)
        {
            Mesh staticMesh = go.GetComponent<MeshFilter>().sharedMesh;
            Mesh skinnedMesh = go.GetComponent<SkinnedMeshRenderer>().sharedMesh;


            if (staticMesh == null && skinnedMesh == null) { return true; }

            if (staticMesh != null)
            {
                if (!IsMeshSavedAsAsset(staticMesh)) { return false; }
            }

            if (skinnedMesh != null)
            {
                if (!IsMeshSavedAsAsset(skinnedMesh)) { return false; }
            }


            return true;
        }





        public static bool IsMeshSavedAsAsset(Mesh mesh)
        {
            if (mesh == null) { return true; }

            string path = AssetDatabase.GetAssetPath(mesh);
            //return (AssetDatabase.Contains(mesh) && path.ToLower().EndsWith(".asset")); baw did
            return (AssetDatabase.Contains(mesh) && path.ToLower().EndsWith(".mesh"));
        }



        public static Mesh GetReducedMesh(GameObject gameObject, DataContainer.MeshRendererPair mRendererPair)
        {

            if (gameObject == null) { return null; }


            if (mRendererPair.attachedToMeshFilter)
            {
                MeshFilter filter = gameObject.GetComponent<MeshFilter>();

                if (filter != null)
                {
                    return filter.sharedMesh;
                }
            }

            else
            {
                SkinnedMeshRenderer sRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();

                if (sRenderer != null)
                {
                    return sRenderer.sharedMesh;
                }
            }

            return null;
        }




        public static List<Mesh> GetAllReducedMeshes(DataContainer.ObjectMeshPairs objMeshPairs)
        {

            if (objMeshPairs == null || objMeshPairs.Count == 0) { return null; }

            List<Mesh> reducedMeshes = new List<Mesh>();


            foreach (var kvp in objMeshPairs)
            {

                if (kvp.Key == null || kvp.Value.mesh == null) { continue; }

                DataContainer.MeshRendererPair meshRendererPair = kvp.Value;
                GameObject gameObject = kvp.Key;

                if (meshRendererPair.attachedToMeshFilter)
                {
                    MeshFilter filter = gameObject.GetComponent<MeshFilter>();

                    if (filter != null)
                    {
                        Mesh mesh = filter.sharedMesh;
                        if (mesh != null) { reducedMeshes.Add(mesh); }
                    }
                }

                else
                {
                    SkinnedMeshRenderer sRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();

                    if (sRenderer != null)
                    {
                        Mesh mesh = sRenderer.sharedMesh;
                        if (mesh != null) { reducedMeshes.Add(mesh); }
                    }
                }
            }


            if (reducedMeshes.Count == 0) { return null; }

            return reducedMeshes;
        }





        public static List<Mesh> GetMeshesFromPairs(DataContainer.ObjectMeshPairs objMeshPairs)
        {

            if (objMeshPairs == null || objMeshPairs.Count == 0) { return null; }


            List<Mesh> originalMeshes = new List<Mesh>();


            foreach (var kvp in objMeshPairs)
            {

                if (kvp.Key == null || kvp.Value.mesh == null) { continue; }

                DataContainer.MeshRendererPair meshRendererPair = kvp.Value;
                GameObject gameObject = kvp.Key;


                if (kvp.Value.mesh != null)
                {
                    originalMeshes.Add(kvp.Value.mesh);
                }
            }


            return originalMeshes;
        }




        public static HashSet<Mesh> GetUnsavedReducedMeshes(DataContainer.ObjectMeshPairs objMeshPairs)
        {

            if (objMeshPairs == null || objMeshPairs.Count == 0) { return null; }


            HashSet<Mesh> unsavedReducedMeshes = new HashSet<Mesh>();


            foreach (var kvp in objMeshPairs)
            {

                if (kvp.Key == null || kvp.Value.mesh == null) { continue; }

                DataContainer.MeshRendererPair meshRendererPair = kvp.Value;
                GameObject gameObject = kvp.Key;

                if (meshRendererPair.attachedToMeshFilter)
                {
                    MeshFilter filter = gameObject.GetComponent<MeshFilter>();

                    if (filter != null)
                    {
                        Mesh reducedMesh = filter.sharedMesh;
                        Mesh originalMesh = kvp.Value.mesh;

                        if (!IsMeshSavedAsAsset(originalMesh))
                        {
                            unsavedReducedMeshes.Add(reducedMesh);
                        }
                    }
                }

                else
                {
                    SkinnedMeshRenderer sRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();

                    if (sRenderer != null)
                    {
                        Mesh reducedMesh = sRenderer.sharedMesh;
                        Mesh originalMesh = kvp.Value.mesh;

                        if (!IsMeshSavedAsAsset(originalMesh))
                        {
                            unsavedReducedMeshes.Add(reducedMesh);
                        }
                    }
                }
            }


            if (unsavedReducedMeshes == null || unsavedReducedMeshes.Count == 0) { return null; }

            return unsavedReducedMeshes;

        }




        public static void GetAllReducedAndOriginalMeshes(DataContainer.ObjectMeshPairs objMeshPairs, Action<List<Mesh>, List<Mesh>> MeshesCallback)
        {

            if (objMeshPairs == null || objMeshPairs.Count == 0) { MeshesCallback(null, null); return; }


            List<Mesh> originalMeshes = new List<Mesh>();
            List<Mesh> reducedMeshes = new List<Mesh>();


            foreach (var kvp in objMeshPairs)
            {

                if (kvp.Key == null || kvp.Value.mesh == null) { continue; }

                DataContainer.MeshRendererPair meshRendererPair = kvp.Value;
                GameObject gameObject = kvp.Key;

                if (meshRendererPair.attachedToMeshFilter)
                {
                    MeshFilter filter = gameObject.GetComponent<MeshFilter>();

                    if (filter != null)
                    {
                        Mesh reducedMesh = filter.sharedMesh;
                        Mesh originalMesh = kvp.Value.mesh;

                        if (reducedMesh != null)
                        {
                            reducedMeshes.Add(reducedMesh);
                        }

                        if (originalMesh != null)
                        {
                            originalMeshes.Add(originalMesh);
                        }
                    }
                }

                else
                {
                    SkinnedMeshRenderer sRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();

                    if (sRenderer != null)
                    {
                        Mesh reducedMesh = sRenderer.sharedMesh;
                        Mesh originalMesh = kvp.Value.mesh;

                        if (reducedMesh != null)
                        {
                            reducedMeshes.Add(reducedMesh);
                        }

                        if (originalMesh != null)
                        {
                            originalMeshes.Add(originalMesh);
                        }
                    }
                }
            }


            MeshesCallback(originalMeshes, reducedMeshes);
        }




        public static DataContainer.ObjectMeshPairs GetObjectMeshPairs(GameObject forObject, bool includeInactive, bool includeInbuilt)
        {

            DataContainer.ObjectMeshPairs objectMeshPairs = new DataContainer.ObjectMeshPairs();


            MeshFilter[] meshFilters = forObject.GetComponentsInChildren<MeshFilter>(includeInactive);

            if (meshFilters != null && meshFilters.Length != 0)
            {
                foreach (var filter in meshFilters)
                {
                    if (filter.gameObject.hideFlags == HideFlags.HideAndDontSave && filter.gameObject.name == "4bbe6110e6faf2b499fcb86cd896c082")
                    { continue; }  // Don't save for the tolerance sphere

                    if (filter.sharedMesh)
                    {
                        if (includeInbuilt)
                        {
                            //Debug.Log("Adding From Mesh Filter   "+ filter.sharedMesh.name + "  for gameobject  "+ filter.gameObject.name);
                            DataContainer.MeshRendererPair meshRendererPair = new DataContainer.MeshRendererPair(true, filter.sharedMesh);
                            objectMeshPairs.Add(filter.gameObject, meshRendererPair);
                        }

                        else if (!IsInbuiltAsset(filter.sharedMesh))
                        {
                            DataContainer.MeshRendererPair meshRendererPair = new DataContainer.MeshRendererPair(true, filter.sharedMesh);
                            objectMeshPairs.Add(filter.gameObject, meshRendererPair);
                        }
                    }
                }
            }


            SkinnedMeshRenderer[] sMeshRenderers = forObject.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive);

            if (sMeshRenderers != null && sMeshRenderers.Length != 0)
            {
                foreach (var renderer in sMeshRenderers)
                {
                    // Don't save for the tolerance sphere
                    if (renderer.gameObject.hideFlags == HideFlags.HideAndDontSave && renderer.gameObject.name == "4bbe6110e6faf2b499fcb86cd896c082")
                    { continue; }  

                    if (renderer.sharedMesh)
                    {
                        if (includeInbuilt)
                        {
                            DataContainer.MeshRendererPair meshRendererPair = new DataContainer.MeshRendererPair(false, renderer.sharedMesh);
                            objectMeshPairs.Add(renderer.gameObject, meshRendererPair);
                        }

                        else if (!IsInbuiltAsset(renderer.sharedMesh))
                        {
                            DataContainer.MeshRendererPair meshRendererPair = new DataContainer.MeshRendererPair(false, renderer.sharedMesh);
                            objectMeshPairs.Add(renderer.gameObject, meshRendererPair);
                        }
                    }
                }

            }


            return objectMeshPairs;

        }








        public static void RestoreMeshesFromPairs(DataContainer.ObjectMeshPairs objectMeshPairs, GameObject exclude = null)
        {
            if (objectMeshPairs != null)
            {
                foreach (GameObject gameObject in objectMeshPairs.Keys)
                {
                    if (gameObject != null)
                    {
                        if (gameObject == exclude) { continue; }

                        DataContainer.MeshRendererPair meshRendererPair = objectMeshPairs[gameObject];

                        if (meshRendererPair.mesh == null) { continue; }

                        if (meshRendererPair.attachedToMeshFilter)
                        {
                            MeshFilter filter = gameObject.GetComponent<MeshFilter>();

                            if (filter == null) { continue; }

                            //Debug.Log("Is attached to meshfilter  GAMOBJECT:   " + gameObject.name + "  CurrentMesh name:  " + filter.sharedMesh.name + "  set sharedMesh to  " + meshRendererPair.mesh.name);

                            filter.sharedMesh = meshRendererPair.mesh;
                        }

                        else if (!meshRendererPair.attachedToMeshFilter)
                        {
                            SkinnedMeshRenderer sRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();

                            if (sRenderer == null) { continue; }

                            //Debug.Log("Is attached to SkinnedMeshRendere  GAMOBJECT:   " + gameObject.name + "  CurrentMesh name:  " + sRenderer.sharedMesh.name + "  set sharedMesh to  " + meshRendererPair.mesh.name);

                            sRenderer.sharedMesh = meshRendererPair.mesh;
                        }
                    }

                }
            }

            else { Debug.Log("Object mesh pair is null"); }

        }




        public static Mesh GetObjectMesh(GameObject go)
        {

            if (go == null) { return null; }

            Mesh mesh = null;

            if (go.GetComponent<MeshFilter>())
            {
                mesh = go.GetComponent<MeshFilter>().sharedMesh;
            }

            else if (go.GetComponent<SkinnedMeshRenderer>())
            {
                mesh = go.GetComponent<SkinnedMeshRenderer>().sharedMesh;
            }

            return mesh;
        }




        public static void AssignReducedMesh(GameObject gameObject, Mesh originalMesh, Mesh reducedMesh, bool attachedToMeshfilter, bool assignBindposes)
        {
            if (assignBindposes)
            {
                reducedMesh.bindposes = originalMesh.bindposes;   // Might cause issues
            }

            reducedMesh.name = originalMesh.name.Replace("-POLY_REDUCED", "") + "-POLY_REDUCED";

            if (attachedToMeshfilter)
            {
                MeshFilter filter = gameObject.GetComponent<MeshFilter>();

                if (filter != null)
                {
                    filter.sharedMesh = reducedMesh;
                }
            }

            else
            {
                SkinnedMeshRenderer sRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();

                if (sRenderer != null)
                {
                    sRenderer.sharedMesh = reducedMesh;
                }
            }
        }





        public static bool IsNonuniformScale(Vector3 prevScale, Vector3 currScale)
        {
            float s1 = Mathf.Abs(prevScale.x - currScale.x);
            float s2 = Mathf.Abs(prevScale.y - currScale.y);
            float s3 = Mathf.Abs(prevScale.z - currScale.z);


            if (Mathf.Approximately(s1, s2) && Mathf.Approximately(s2, s3)) { return false; }

            return true;
        }



        public static int CountTriangles(bool countDeep, DataContainer.ObjectMeshPairs objectMeshPairs, GameObject forObject)
        {
            int triangleCount = 0;

            if (objectMeshPairs == null) { return 0; }

            if(forObject == null) { return 0; }

            if (countDeep)
            {
                foreach (var item in objectMeshPairs)
                {
                    if (item.Key == null || item.Value == null || item.Value.mesh == null)
                    { continue; }

                    triangleCount += (item.Value.mesh.triangles.Length) / 3;
                }
            }
            else
            {
                if (objectMeshPairs.ContainsKey(forObject))
                {
                    MeshRendererPair mRendererPair = objectMeshPairs[forObject];

                    if(mRendererPair == null || mRendererPair.mesh == null)
                    {
                        return 0;
                    }

                    triangleCount = (mRendererPair.mesh.triangles.Length / 3);
                }
            }

            return triangleCount;
        }


        public static Vector3 GetClosestVertex(Vector3 point, Mesh mesh, Transform obj)
        {

            if (mesh == null) { return Vector3.zero; }


            Vector3 closestVertex = Vector3.zero;
            float minDist = Mathf.Infinity;
            point = obj.InverseTransformPoint(point);

            for (int a = 0; a < mesh.vertexCount; a++)
            {
                Vector3 vertexPos = mesh.vertices[a];
                float distance = Vector3.Distance(vertexPos, point);

                if (distance < minDist)
                {
                    minDist = distance;
                    closestVertex = vertexPos;
                }
            }

            return obj.TransformPoint(closestVertex);

        }




        public static PointState GetPointSphereRelation(Vector3 sphereCenter, float sphereRadius, Vector3 point)
        {
            int x1 = (int)Math.Pow((point.x - sphereCenter.x), 2);
            int y1 = (int)Math.Pow((point.y - sphereCenter.y), 2);
            int z1 = (int)Math.Pow((point.z - sphereCenter.z), 2);

            float dist = (x1 + y1 + z1);


            // distance btw centre 
            // and point is less  
            // than radius 

            if (dist < (sphereRadius * sphereRadius)) { return PointState.LIES_INSIDE; }

            // distance btw centre 
            // and point is  
            // equal to radius 
            else if (dist == (sphereRadius * sphereRadius)) { return PointState.LIES_OVER; }

            // distance btw center  
            // and point is greater 
            // than radius 
            else { return PointState.LIES_OUTSIDE; }

        }



        public enum PointState
        {
            LIES_OUTSIDE,
            LIES_INSIDE,
            LIES_OVER
        }



        public static Vector3 GetSnapPoint(Vector3 position, Quaternion rotation, Vector3 snapVector, Vector3 dragDirection, HandleOrientation handlesOrientation)
        {

            var selectedControl = HandleControlsUtility.handleControls.GetCurrentSelectedControl();
            Vector3 result = Vector3.zero;

            if (handlesOrientation == HandleOrientation.globalAligned)
            {
                rotation = Quaternion.identity;
            }


            if (selectedControl == HandleControlsUtility.HandleControls.xAxisMoveHandle)
            {
                result = GetXSnappedPos(position, rotation, dragDirection, snapVector, selectedControl);
            }

            else if (selectedControl == HandleControlsUtility.HandleControls.yAxisMoveHandle)
            {
                result = GetYSnappedPos(position, rotation, dragDirection, snapVector, selectedControl);
            }

            else if (selectedControl == HandleControlsUtility.HandleControls.zAxisMoveHandle)
            {
                result = GetZSnappedPos(position, rotation, dragDirection, snapVector, selectedControl);
            }

            else if (selectedControl == HandleControlsUtility.HandleControls.xyAxisMoveHandle)
            {
                Vector3 localAxisDir = GetXaxisinWorld(rotation);
                selectedControl = HandleControlsUtility.HandleControls.xAxisMoveHandle;
                result = GetXSnappedPos(position, rotation, dragDirection, snapVector, selectedControl);


                localAxisDir = GetYaxisinWorld(rotation);
                selectedControl = HandleControlsUtility.HandleControls.yAxisMoveHandle;
                result = GetYSnappedPos(result, rotation, dragDirection, snapVector, selectedControl);
            }

            else if (selectedControl == HandleControlsUtility.HandleControls.yzAxisMoveHandle)
            {
                Vector3 localAxisDir = GetYaxisinWorld(rotation);
                selectedControl = HandleControlsUtility.HandleControls.yAxisMoveHandle;
                result = GetYSnappedPos(position, rotation, dragDirection, snapVector, selectedControl);

                localAxisDir = GetZaxisinWorld(rotation);
                selectedControl = HandleControlsUtility.HandleControls.zAxisMoveHandle;
                result = GetZSnappedPos(result, rotation, dragDirection, snapVector, selectedControl);
            }

            else if (selectedControl == HandleControlsUtility.HandleControls.xzAxisMoveHandle)
            {
                Vector3 localAxisDir = GetXaxisinWorld(rotation);
                selectedControl = HandleControlsUtility.HandleControls.xAxisMoveHandle;
                result = GetXSnappedPos(position, rotation, dragDirection, snapVector, selectedControl);

                localAxisDir = GetZaxisinWorld(rotation);
                selectedControl = HandleControlsUtility.HandleControls.zAxisMoveHandle;
                result = GetZSnappedPos(result, rotation, dragDirection, snapVector, selectedControl);
            }

            else if (selectedControl == HandleControlsUtility.HandleControls.allAxisMoveHandle)
            {
                Vector3 localAxisDir = GetXaxisinWorld(rotation);
                selectedControl = HandleControlsUtility.HandleControls.xAxisMoveHandle;
                result = GetXSnappedPos(position, rotation, dragDirection, snapVector, selectedControl);

                localAxisDir = GetYaxisinWorld(rotation);
                selectedControl = HandleControlsUtility.HandleControls.yAxisMoveHandle;
                result = GetXSnappedPos(result, rotation, dragDirection, snapVector, selectedControl);

                localAxisDir = GetZaxisinWorld(rotation);
                selectedControl = HandleControlsUtility.HandleControls.zAxisMoveHandle;
                result = GetXSnappedPos(result, rotation, dragDirection, snapVector, selectedControl);
            }

            return (result);

        }



        private static Vector3 GetXSnappedPos(Vector3 position, Quaternion rotation, Vector3 dragDirection, Vector3 snapVector, HandleControlsUtility.HandleControls selectedControl)
        {


            Vector3 result = Vector3.zero;
            Vector3 localAxisDir = GetXaxisinWorld(rotation);
            float dot = Vector3.Dot(localAxisDir, dragDirection);
            float angle = Vector3.Angle(dragDirection, localAxisDir);
            if (dot < 0) { localAxisDir *= -1; }

            result = position + (snapVector.x * localAxisDir);

            if (dot >= 0 && dot <= 1) { result = position; }

            return result;
        }


        private static Vector3 GetYSnappedPos(Vector3 position, Quaternion rotation, Vector3 dragDirection, Vector3 snapVector, HandleControlsUtility.HandleControls selectedControl)
        {
            Vector3 result = Vector3.zero;
            Vector3 localAxisDir = GetYaxisinWorld(rotation);
            float dot = Vector3.Dot(localAxisDir, dragDirection);

            if (dot < 0) { localAxisDir *= -1; }

            result = position + (snapVector.y * localAxisDir);

            if (dot >= 0 && dot <= 1f) { result = position; }

            return result;
        }


        private static Vector3 GetZSnappedPos(Vector3 position, Quaternion rotation, Vector3 dragDirection, Vector3 snapVector, HandleControlsUtility.HandleControls selectedControl)
        {
            Vector3 result = Vector3.zero;
            Vector3 localAxisDir = GetZaxisinWorld(rotation);
            float dot = Vector3.Dot(localAxisDir, dragDirection);

            if (dot < 0) { localAxisDir *= -1; }

            result = position + (snapVector.z * localAxisDir);

            if (dot >= 0 && dot <= 1f) { result = position; }

            return result;
        }




        public static Vector3? CorrectHandleValues(Vector3 pointToCorrect, Vector3 oldValueOfPoint)
        {

            if (HandleControlsUtility.handleControls == null) { return null; }


            Vector3 corrected = Vector3.zero;

            using (HandleControlsUtility handleControls = HandleControlsUtility.handleControls)
            {
                switch (handleControls.GetCurrentSelectedControl())
                {
                    case HandleControlsUtility.HandleControls.xAxisMoveHandle:
                        corrected = new Vector3(pointToCorrect.x, oldValueOfPoint.y, oldValueOfPoint.z);
                        break;

                    case HandleControlsUtility.HandleControls.yAxisMoveHandle:
                        corrected = new Vector3(oldValueOfPoint.x, pointToCorrect.y, oldValueOfPoint.z);
                        break;

                    case HandleControlsUtility.HandleControls.zAxisMoveHandle:
                        corrected = new Vector3(oldValueOfPoint.x, oldValueOfPoint.y, pointToCorrect.z);
                        break;

                    case HandleControlsUtility.HandleControls.xyAxisMoveHandle:
                        corrected = new Vector3(pointToCorrect.x, pointToCorrect.y, oldValueOfPoint.z);
                        break;

                    case HandleControlsUtility.HandleControls.xzAxisMoveHandle:
                        corrected = new Vector3(pointToCorrect.x, oldValueOfPoint.y, pointToCorrect.z);
                        break;

                    case HandleControlsUtility.HandleControls.yzAxisMoveHandle:
                        corrected = new Vector3(oldValueOfPoint.x, pointToCorrect.y, pointToCorrect.z);
                        break;
                }
            }

            return corrected;

        }




        public void RunAfter(Action command, YieldInstruction yieldInstruction)
        {
            this.StartCoroutine(CommandEnumerator(command, yieldInstruction));
        }



        public static Vector3 GetXaxisinWorld(Quaternion rotation)
        {
            return rotation * Vector3.right;
        }


        public static Vector3 GetYaxisinWorld(Quaternion rotation)
        {
            return rotation * Vector3.up;
        }


        public static Vector3 GetZaxisinWorld(Quaternion rotation)
        {
            return rotation * Vector3.forward;
        }



        public static float CalcEulerSafeAngle(float angle)
        {
            if (angle >= -90 && angle <= 90)
                return angle;
            angle = angle % 180;
            if (angle > 0)
                angle -= 180;
            else
                angle += 180;
            return angle;
        }





        private static IEnumerator CommandEnumerator(Action command, YieldInstruction yieldInstruction)
        {
            yield return yieldInstruction;
            command();
        }



        public static string GetValidFolderPath(string folderPath)
        {
            string path = "";

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return "Assets/";
            }

            path = FileUtil.GetProjectRelativePath(folderPath);


            if (!AssetDatabase.IsValidFolder(path))
            {
                return "Assets/";
            }

            return path;
        }




        public static bool IsPathInAssetsDir(string folderPath)
        {

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return false;
            }

            folderPath = FileUtil.GetProjectRelativePath(folderPath);

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                return false;
            }

            return true;
        }



        public static GameObject DuplicateGameObject(GameObject toDuplicate, string newName, bool duplicateFromRoot, bool duplicateChildren)
        {
            if (toDuplicate == null) { return null; }

            GameObject selectedObject = Selection.activeGameObject;
            GameObject duplicate = null;


            if (!selectedObject.GetHashCode().Equals(toDuplicate.GetHashCode()))
            {
                Selection.activeGameObject = toDuplicate;
            }


            GameObject rootParent = (GameObject)PrefabUtility.GetPrefabParent(toDuplicate);
            if (duplicateFromRoot && rootParent) { Selection.activeGameObject = rootParent; }


            SceneView.lastActiveSceneView.Focus();
            EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent("Duplicate"));

            duplicate = Selection.activeGameObject;
            Selection.activeGameObject.name = newName;
            Selection.activeGameObject = selectedObject;

            if (!duplicateChildren)
            {

                while (duplicate.transform.childCount > 0)
                {
                    DestroyImmediate(duplicate.transform.GetChild(0).gameObject);
                }

            }

            duplicate.transform.parent = null;

            return duplicate;
        }





        public static ChildStateTuple[] SaveChildrenStates(GameObject forObject)
        {

            var children = GetTopLevelChildren(forObject.transform);

            ChildStateTuple[] childrenStates = new ChildStateTuple[children.Length];

            for (int a = 0; a < children.Length; a++)
            {
                childrenStates[a].transform = children[a];
                childrenStates[a].position = children[a].position;
                childrenStates[a].rotation = children[a].rotation;
            }

            return childrenStates;

        }




        /// <summary> Restores the children states to the ones before pivot modification. </summary>

        public static void RestoreChildrenStates(ChildStateTuple[] childStates)
        {

            if (childStates == null) { return; }

            for (int a = 0; a < childStates.Length; a++)
            {
                if (childStates[a].transform == null) { continue; }

                childStates[a].transform.position = childStates[a].position;
                childStates[a].transform.rotation = childStates[a].rotation;
            }

        }




        public static ColliderState SaveColliderState(GameObject forObject)
        {

            Collider selectedObjectCollider = forObject.GetComponent<Collider>();
            Transform selectedTransform = forObject.transform;
            ColliderState colliderState = new ColliderState();

            if (selectedObjectCollider)
            {
                if (selectedObjectCollider is BoxCollider)
                {
                    colliderState.center = selectedTransform.TransformPoint(((BoxCollider)selectedObjectCollider).center);
                    colliderState.type = ColliderType.BoxCollider;
                }
                else if (selectedObjectCollider is CapsuleCollider)
                {
                    colliderState.center = selectedTransform.TransformPoint(((CapsuleCollider)selectedObjectCollider).center);
                    colliderState.type = ColliderType.CapsuleCollider;
                }
                else if (selectedObjectCollider is SphereCollider)
                {
                    colliderState.center = selectedTransform.TransformPoint(((SphereCollider)selectedObjectCollider).center);
                    colliderState.type = ColliderType.SphereCollider;
                }
                else if (selectedObjectCollider is MeshCollider)
                {
                    colliderState.type = ColliderType.MeshCollider;
                    //colliderState.center = selectedTransform.TransformPoint(((MeshCollider)selectedObjectCollider).bounds.center);
                }
            }

            return colliderState;

        }





        /// <summary> Restore the collider orientation.</summary>
        public static void RestoreColliderState(GameObject forObject, ColliderState colliderState)
        {


            Collider selectedObjectCollider = forObject.GetComponent<Collider>();
            Transform selectedTransform = forObject.transform;


            if (selectedObjectCollider)
            {
                if (selectedObjectCollider is BoxCollider)
                {
                    if (colliderState.type == ColliderType.BoxCollider)
                    {
                        ((BoxCollider)selectedObjectCollider).center = selectedTransform.InverseTransformPoint(colliderState.center);
                    }
                }
                else if (selectedObjectCollider is CapsuleCollider)
                {
                    if (colliderState.type == ColliderType.CapsuleCollider)
                    {
                        ((CapsuleCollider)selectedObjectCollider).center = selectedTransform.InverseTransformPoint(colliderState.center);
                    }
                }
                else if (selectedObjectCollider is SphereCollider)
                {
                    if (colliderState.type == ColliderType.SphereCollider)
                    {
                        ((SphereCollider)selectedObjectCollider).center = selectedTransform.InverseTransformPoint(colliderState.center);
                    }
                }
                else if (selectedObjectCollider is MeshCollider)
                {

                    /*
                    MeshCollider meshColl = (MeshCollider)selectedObjectCollider;

                    bool isConvex = meshColl.convex;

                    meshColl.convex = false;

                    meshColl.sharedMesh = selectedObjectMesh;

                    if (isConvex)
                    {

                        if (selectedObjectMesh.vertexCount >= 2000)
                        {

                            Debug.Log("<b><i><color=#008000ff> PLEASE WAIT... while the convex property on the mesh collider does some calculations.The editor won't be usable until the MeshCollider finishes its calculations.</color></i></b>");
                            new UtilityServices().RunAfter(() => { meshColl.convex = true; }, new WaitForSeconds(0.2f));
                        }

                        else { meshColl.convex = true; } 

                    }
                    */
                }
            }

        }





        public static Transform[] GetTopLevelChildren(Transform Parent)
        {
            Transform[] Children = new Transform[Parent.childCount];
            for (int a = 0; a < Parent.childCount; a++)
            {
                Children[a] = Parent.GetChild(a);
            }
            return Children;
        }




        public static GameObject CreateTestObj(PrimitiveType type, Vector3 position, Vector3 scale, string name = "")
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.localScale = scale;
            go.transform.position = position;

            if (name != "") { go.name = name; }

            return go;
        }






        private static Vector3 SubtractAngles(Vector3 rotation1, Vector3 rotation2)
        {

            float xDif = 0;
            float yDif = 0;
            float zDif = 0;

            if (AreAnglesSame(rotation1.x, rotation2.x)) { xDif = 0; }
            else { xDif = rotation1.x - rotation2.x; }

            if (AreAnglesSame(rotation1.y, rotation2.y)) { yDif = 0; }
            else { yDif = rotation1.y - rotation2.y; }

            if (AreAnglesSame(rotation1.z, rotation2.z)) { zDif = 0; }
            else { zDif = rotation1.z - rotation2.z; }

            return new Vector3(xDif, yDif, zDif);
        }



        private static bool AreAnglesSame(float angle1, float angle2)
        {

            if (Mathf.Approximately((Mathf.Cos(angle1) * Mathf.Deg2Rad), (Mathf.Cos(angle2) * Mathf.Deg2Rad)))
            {
                if (Mathf.Approximately((Mathf.Sin(angle1) * Mathf.Deg2Rad), (Mathf.Sin(angle2) * Mathf.Deg2Rad)))
                {
                    //Debug.Log("equal");
                    return true;
                }
            }

            return false;
        }


        public static Vector3 NormalizeAngles(Vector3 angles)
        {
            angles.x = NormalizeAngle(angles.x);
            angles.y = NormalizeAngle(angles.y);
            angles.z = NormalizeAngle(angles.z);
            return angles;
        }


        static float NormalizeAngle(float angle)
        {
            while (angle > 180)
                angle -= 360;

            return angle;
        }


        public static Vector3 Absolute(Vector3 vector)
        {
            return new Vector3(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));
        }



        /*
        public static GameObject CreateChildCollider(GameObject forObject, bool forPosition, Vector3? newPivotPos, Quaternion? newPivotRot)
        {   
            string colliderName = "";

            var colliders = forObject.GetComponents<Collider>();

            if (colliders == null || colliders.Length == 0) { return null; }


            #region Setting name of the child collider and disabling original colliders

            ColliderType lastType = ColliderType.None;

            foreach (Collider collider in colliders)
            {

                ColliderType currentType;
                //collider.enabled = false;


                if (collider is BoxCollider)
                {
                    currentType = ColliderType.BoxCollider;
                    colliderName = forObject.name + "_" + Enum.GetName(typeof(ColliderType), currentType);

                    if (lastType != ColliderType.None)
                    {
                        if (lastType != currentType)
                        {
                            colliderName = forObject.name + "_MixedColliders";
                            break;
                        }

                        else
                        {
                            colliderName = forObject.name + "_" + Enum.GetName(typeof(ColliderType), currentType) + "s";
                        }
                    }

                    lastType = currentType;
                }

                else if (collider is CapsuleCollider)
                {
                    currentType = ColliderType.CapsuleCollider;
                    colliderName = forObject.name + "_" + Enum.GetName(typeof(ColliderType), currentType);

                    if (lastType != ColliderType.None)
                    {
                        if (lastType != currentType)
                        {
                            colliderName = forObject.name + "_MixedColliders";
                            break;
                        }

                        else
                        {
                            colliderName = forObject.name + "_" + Enum.GetName(typeof(ColliderType), currentType) + "s";
                        }
                    }

                    lastType = currentType;
                }

                else if (collider is SphereCollider)
                {
                    currentType = ColliderType.SphereCollider;
                    colliderName = forObject.name + "_" + Enum.GetName(typeof(ColliderType), currentType);

                    if (lastType != ColliderType.None)
                    {
                        if (lastType != currentType)
                        {
                            colliderName = forObject.name + "_MixedColliders";
                            break;
                        }

                        else
                        {
                            colliderName = forObject.name + "_" + Enum.GetName(typeof(ColliderType), currentType) + "s";
                        }
                    }

                    lastType = currentType;
                }

                else if (collider is MeshCollider)
                {
                    currentType = ColliderType.MeshCollider;
                    colliderName = forObject.name + "_" + Enum.GetName(typeof(ColliderType), currentType);

                    if (lastType != ColliderType.None)
                    {
                        if (lastType != currentType)
                        {
                            colliderName = forObject.name + "_MixedColliders";
                            break;
                        }

                        else
                        {
                            colliderName = forObject.name + "_" + Enum.GetName(typeof(ColliderType), currentType) + "s";
                        }
                    }

                    lastType = currentType;
                }
            }

            #endregion Setting name of the child collider and disabling original colliders


            colliderName = "**" + colliderName + "_DON'T DELETE**";
            GameObject childCollider = UtilityServices.DuplicateGameObject(forObject, colliderName, false, false);

            foreach (Component component in childCollider.GetComponents<Component>())
            {
                if (component is Transform || component is Collider) { continue; }

                DestroyImmediate(component);
            }



            foreach (Collider collider in forObject.GetComponents<Collider>())
            {
                if(collider.enabled && forPosition) { collider.enabled = true; }

                else { collider.enabled = false;  }
            }

            childCollider.transform.parent = forObject.transform;
            childCollider.AddComponent<CollRecognize>();

            Collider coll  = forObject.GetComponent<Collider>();

            if(coll && coll is MeshCollider)
            {
                if(newPivotPos != null)
                {
                    //childCollider.transform.position = (Vector3)newPivotPos;
                }

                else if(newPivotRot != null)
                {
                    //childCollider.transform.rotation = (Quaternion)newPivotRot;
                }

            }

            return childCollider;


        }
        */



        /*
        public static GameObject CreateChildNavMeshObs(GameObject forObject, bool forPosition)
        {


            string navMeshObsName = "";

            var navMeshObstacle = forObject.GetComponent<NavMeshObstacle>();

            if (navMeshObstacle == null) { return null; }


            navMeshObsName = forObject.name + "_NavMeshObstacle";
            navMeshObsName = "**" + navMeshObsName + "_DON'T DELETE**";

            GameObject childNavMeshObs = UtilityServices.DuplicateGameObject(forObject, navMeshObsName, false, false);

            foreach (Component component in childNavMeshObs.GetComponents<Component>())
            {
                if (component is Transform || component is NavMeshObstacle) { continue; }

                DestroyImmediate(component);
            }

            navMeshObstacle.enabled = false;

            childNavMeshObs.transform.position = forObject.transform.position; 
            childNavMeshObs.transform.rotation = forObject.transform.rotation; 

            childNavMeshObs.transform.parent = forObject.transform;
            childNavMeshObs.AddComponent<NavObsRecognize>();


            return childNavMeshObs;

        }
        */



        public static float SetAndReturnFloatPref(string name, float val)
        {
            EditorPrefs.SetFloat(name, val);
            return val;
        }


        public static int SetAndReturnIntPref(string name, int val)
        {
            EditorPrefs.SetInt(name, val);
            return val;
        }


        public static bool SetAndReturnBoolPref(string name, bool val)
        {
            EditorPrefs.SetBool(name, val);
            return val;
        }


        public static string SetAndReturnStringPref(string name, string val)
        {
            EditorPrefs.SetString(name, val);
            return val;
        }


        public static Vector3 SetAndReturnVectorPref(string nameX, string nameY, string nameZ, Vector3 value)
        {
            EditorPrefs.SetFloat(nameX, value.x);
            EditorPrefs.SetFloat(nameY, value.y);
            EditorPrefs.SetFloat(nameZ, value.z);

            return value;
        }




        public enum ColliderType
        {
            BoxCollider,
            SphereCollider,
            CapsuleCollider,
            MeshCollider,
            None
        }




        public static float Average(params float[] list)
        {

            if (list == null || list.Length == 0) { return 0; }

            float sum = 0;
            float count = list.Length;

            foreach (var num in list) { sum += num; }

            return sum / count;
        }




        public static GameObject FindObjectFromTags(string goName, string tag)
        {
            var list = GameObject.FindGameObjectsWithTag(tag);

            foreach (var item in list)
            {
                if (item.name.Equals(goName)) { return item; }
            }
            
            return null;
        }



        public static int GetFirstNDigits(int number, int N)
        {
            // this is for handling negative numbers, we are only insterested in postitve number 
            number = Math.Abs(number);

            // special case for 0 as Log of 0 would be infinity 
            if (number == 0) { return number; }
                
            int numberOfDigits = (int)Math.Floor(Math.Log10(number) + 1);

            if (numberOfDigits >= N)
            {
                return (int)Math.Truncate((number / Math.Pow(10, numberOfDigits - N)));
            }

            else { return number; }
                
        }


        /*
        public static GameObject DuplicateGameObject(string newName, bool duplicateFromRoot, bool duplicateChildren)
        {
            if (Selection.activeGameObject == null) { return null; }

            GameObject selectedObject = Selection.activeGameObject;
            GameObject duplicate = null;

            string name = Selection.activeGameObject.name;

            GameObject rootParent = (GameObject)PrefabUtility.GetPrefabParent(Selection.activeGameObject);
            if (duplicateFromRoot) { Selection.activeGameObject = rootParent; }


            SceneView.lastActiveSceneView.Focus();
            EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent("Duplicate"));

            duplicate = Selection.activeGameObject;
            Selection.activeGameObject.name = newName;
            Selection.activeGameObject = selectedObject;

            if (!duplicateChildren)
            {
                foreach (Transform child in duplicate.transform) { DestroyImmediate(child.gameObject); }
            }

            return duplicate;
        }
        */



        public static Texture2D DuplicateTexture(Texture2D source)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary
            (
                source.width,
                source.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear
            );

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);

            return readableText;
        }



        #region OBJ_EXPORT_IMPORT


/*
 ======================================================================================
 |	    Special thanks to aaro4130 for the Unity3D Scene OBJ Exporter
 |      This section would not have been made possible or would have been partial 
 |      without his works.
 |
 |      Do check out: 
 |      https://assetstore.unity.com/packages/tools/utilities/scene-obj-exporter-22250
 |  
 ======================================================================================
*/


        public class OBJExporterImporter
        {

            #region OBJ_EXPORT

            private bool applyPosition = true;
            private bool applyRotation = true;
            private bool applyScale = true;
            private bool generateMaterials = true;
            private bool exportTextures = true;
            private string exportPath;
            private MeshFilter meshFilter;

            private Mesh meshToExport;
            private MeshRenderer meshRenderer;



            public OBJExporterImporter() { }

            

            public class OBJExportOptions
            {

                public readonly bool applyPosition = true;
                public readonly bool applyRotation = true;
                public readonly bool applyScale = true;
                public readonly bool generateMaterials = true;
                public readonly bool exportTextures = true;


                public OBJExportOptions(bool applyPosition, bool applyRotation, bool applyScale, bool generateMaterials, bool exportTextures)
                {
                    this.applyPosition = applyPosition;
                    this.applyRotation = applyRotation;
                    this.applyScale = applyScale;
                    this.generateMaterials = generateMaterials;
                    this.exportTextures = exportTextures;
                }
            }



            private void InitializeExporter(GameObject toExport, string exportPath, OBJExportOptions exportOptions)
            {
                this.exportPath = exportPath;


                if (string.IsNullOrWhiteSpace(exportPath))
                {
                    throw new DirectoryNotFoundException("The path provided is non-existant.");
                }

                else
                {
                    exportPath = Path.GetFullPath(exportPath);
                    if (exportPath[exportPath.Length - 1] == '\\') { exportPath = exportPath.Remove(exportPath.Length - 1); }
                    else if (exportPath[exportPath.Length - 1] == '/') { exportPath = exportPath.Remove(exportPath.Length - 1); }
                }

                if (!System.IO.Directory.Exists(exportPath))
                {
                    throw new DirectoryNotFoundException("The path provided is non-existant.");
                }

                if (toExport == null)
                {
                    throw new ArgumentNullException("toExport", "Please provide a GameObject to export as OBJ file.");
                }


                meshRenderer = toExport.GetComponent<MeshRenderer>();
                meshFilter = toExport.GetComponent<MeshFilter>();

                if (meshRenderer == null)
                {

                }

                else
                {
                    if (meshRenderer.isPartOfStaticBatch)
                    {
                        throw new InvalidOperationException("The provided object is static batched. Static batched object cannot be exported. Please disable it before trying to export the object.");
                    }
                }

                if (meshFilter == null)
                {
                    throw new InvalidOperationException("There is no MeshFilter attached to the provided GameObject.");
                }

                else
                {
                    meshToExport = meshFilter.sharedMesh;

                    if (meshToExport == null || meshToExport.triangles == null || meshToExport.triangles.Length == 0)
                    {
                        throw new InvalidOperationException("The MeshFilter on the provided GameObject has invalid or no mesh at all.");
                    }
                }


                if (exportOptions != null)
                {
                    applyPosition     = exportOptions.applyPosition;
                    applyRotation     = exportOptions.applyRotation;
                    applyScale        = exportOptions.applyScale;
                    generateMaterials = exportOptions.generateMaterials;
                    exportTextures    = exportOptions.exportTextures;
                }

            }


            private void InitializeExporter(Mesh toExport, string exportPath)
            {
                this.exportPath = exportPath;

                if (string.IsNullOrWhiteSpace(exportPath))
                {
                    throw new DirectoryNotFoundException("The path provided is non-existant.");
                }


                if (!System.IO.Directory.Exists(exportPath))
                {
                    throw new DirectoryNotFoundException("The path provided is non-existant.");
                }


                if (toExport == null)
                {
                    throw new ArgumentNullException("toExport", "Please provide a Mesh to export as OBJ file.");
                }


                meshToExport = toExport;


                if (meshToExport == null || meshToExport.triangles == null || meshToExport.triangles.Length == 0)
                {
                    throw new InvalidOperationException("The MeshFilter on the provided GameObject has invalid or no mesh at all.");
                }
                
            }



            Vector3 RotateAroundPoint(Vector3 point, Vector3 pivot, Quaternion angle)
            {
                return angle * (point - pivot) + pivot;
            }

            Vector3 MultiplyVec3s(Vector3 v1, Vector3 v2)
            {
                return new Vector3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
            }



            public void ExportGameObjectToOBJ(GameObject toExport, string exportPath, OBJExportOptions exportOptions = null, Action OnSuccess = null)
            {
                //init stuff
                Dictionary<string, bool> materialCache = new Dictionary<string, bool>();

                //Debug.Log("Exporting OBJ. Please wait.. Starting to export.");


                InitializeExporter(toExport, exportPath, exportOptions);


                //get list of required export things


                string objectName = toExport.gameObject.name;


                //work on export
                StringBuilder sb = new StringBuilder();
                StringBuilder sbMaterials = new StringBuilder();


                if (generateMaterials)
                {
                    sb.AppendLine("mtllib " + objectName + ".mtl");
                }

                int lastIndex = 0;


                if (meshRenderer != null && generateMaterials)
                {
                    Material[] mats = meshRenderer.sharedMaterials;
                    for (int j = 0; j < mats.Length; j++)
                    {
                        Material m = mats[j];
                        if (!materialCache.ContainsKey(m.name))
                        {
                            materialCache[m.name] = true;
                            sbMaterials.Append(MaterialToString(m));
                            sbMaterials.AppendLine();
                        }
                    }
                }

                //export the meshhh :3
                
                int faceOrder = (int)Mathf.Clamp((toExport.gameObject.transform.lossyScale.x * toExport.gameObject.transform.lossyScale.z), -1, 1);

                //export vector data (FUN :D)!
                foreach (Vector3 vx in meshToExport.vertices)
                {
                    Vector3 v = vx;
                    if (applyScale)
                    {
                        v = MultiplyVec3s(v, toExport.gameObject.transform.lossyScale);
                    }

                    if (applyRotation)
                    {
                        v = RotateAroundPoint(v, Vector3.zero, toExport.gameObject.transform.rotation);
                    }

                    if (applyPosition)
                    {
                        v += toExport.gameObject.transform.position;
                    }

                    v.x *= -1;
                    sb.AppendLine("v " + v.x + " " + v.y + " " + v.z);

                }

                foreach (Vector3 vx in meshToExport.normals)
                {
                    Vector3 v = vx;

                    if (applyScale)
                    {
                        v = MultiplyVec3s(v, toExport.gameObject.transform.lossyScale.normalized);
                    }
                    if (applyRotation)
                    {
                        v = RotateAroundPoint(v, Vector3.zero, toExport.gameObject.transform.rotation);
                    }

                    v.x *= -1;
                    sb.AppendLine("vn " + v.x + " " + v.y + " " + v.z);

                }

                foreach (Vector2 v in meshToExport.uv)
                {
                    sb.AppendLine("vt " + v.x + " " + v.y);
                }

                for (int j = 0; j < meshToExport.subMeshCount; j++)
                {
                    if (meshRenderer != null && j < meshRenderer.sharedMaterials.Length)
                    {
                        string matName = meshRenderer.sharedMaterials[j].name;
                        sb.AppendLine("usemtl " + matName);
                    }
                    else
                    {
                        sb.AppendLine("usemtl " + objectName + "_sm" + j);
                    }

                    int[] tris = meshToExport.GetTriangles(j);

                    for (int t = 0; t < tris.Length; t += 3)
                    {
                        int idx2 = tris[t] + 1 + lastIndex;
                        int idx1 = tris[t + 1] + 1 + lastIndex;
                        int idx0 = tris[t + 2] + 1 + lastIndex;

                        if (faceOrder < 0)
                        {
                            sb.AppendLine("f " + ConstructOBJString(idx2) + " " + ConstructOBJString(idx1) + " " + ConstructOBJString(idx0));
                        }
                        else
                        {
                            sb.AppendLine("f " + ConstructOBJString(idx0) + " " + ConstructOBJString(idx1) + " " + ConstructOBJString(idx2));
                        }

                    }
                }

                lastIndex += meshToExport.vertices.Length;


                //write to disk

                string writePath = System.IO.Path.Combine(exportPath, objectName + ".obj");

                System.IO.File.WriteAllText(writePath, sb.ToString());

                if (generateMaterials)
                {
                    writePath = System.IO.Path.Combine(exportPath, objectName + ".mtl");
                    System.IO.File.WriteAllText(writePath, sbMaterials.ToString());
                }

                //export complete, close progress dialog
                OnSuccess?.Invoke();
            }



            public void ExportMeshToOBJ(Mesh mesh, string exportPath)
            {

                InitializeExporter(mesh, exportPath);

                string objectName = meshToExport.name;
                StringBuilder sb = new StringBuilder();
                int lastIndex = 0;
                int faceOrder = 1;

                //export vector data (FUN :D)!
                foreach (Vector3 vx in meshToExport.vertices)
                {
                    Vector3 v = vx;

                    v.x *= -1;

                    sb.AppendLine("v " + v.x + " " + v.y + " " + v.z);

                }

                foreach (Vector3 vx in meshToExport.normals)
                {
                    Vector3 v = vx;

                    v.x *= -1;
                    sb.AppendLine("vn " + v.x + " " + v.y + " " + v.z);

                }

                foreach (Vector2 v in meshToExport.uv)
                {
                    sb.AppendLine("vt " + v.x + " " + v.y);
                }

                for (int j = 0; j < meshToExport.subMeshCount; j++)
                {

                    sb.AppendLine("usemtl " + objectName + "_sm" + j);
                    
                    int[] tris = meshToExport.GetTriangles(j);

                    for (int t = 0; t < tris.Length; t += 3)
                    {
                        int idx2 = tris[t] + 1 + lastIndex;
                        int idx1 = tris[t + 1] + 1 + lastIndex;
                        int idx0 = tris[t + 2] + 1 + lastIndex;

                        if (faceOrder < 0)
                        {
                            sb.AppendLine("f " + ConstructOBJString(idx2) + " " + ConstructOBJString(idx1) + " " + ConstructOBJString(idx0));
                        }
                        else
                        {
                            sb.AppendLine("f " + ConstructOBJString(idx0) + " " + ConstructOBJString(idx1) + " " + ConstructOBJString(idx2));
                        }

                    }
                }

                lastIndex += meshToExport.vertices.Length;


                //write to disk

                string writePath = System.IO.Path.Combine(exportPath, objectName + ".obj");

                System.IO.File.WriteAllText(writePath, sb.ToString());

            }



            string TryExportTexture(string propertyName, Material m, string exportPath)
            {
                if (m.HasProperty(propertyName))
                {
                    Texture t = m.GetTexture(propertyName);

                    if (t != null)
                    {
                        return ExportTexture((Texture2D)t, exportPath);
                    }
                }

                return "false";
            }


            string ExportTexture(Texture2D t, string exportPath)
            {
                //Debug.Log($"Exporting texture:  {t.name} to path: {exportPath}");

                string textureName = t.name;

                try
                {
                    Color32[] pixels32 = null;

                    try
                    {
                        pixels32 = t.GetPixels32();
                    }

                    catch (UnityException ex)
                    {
                        t = UtilityServices.DuplicateTexture(t);
                        pixels32 = t.GetPixels32();
                    }

                    string qualifiedPath = System.IO.Path.Combine(exportPath, textureName + ".png");
                    Texture2D exTexture = new Texture2D(t.width, t.height, TextureFormat.ARGB32, false);
                    exTexture.SetPixels32(pixels32);

                    System.IO.File.WriteAllBytes(qualifiedPath, exTexture.EncodeToPNG());

                    return qualifiedPath;
                }

                catch (System.Exception ex)
                {
                    Debug.Log("Could not export texture : " + t.name + ". is it readable?");
                    return "null";
                }

            }


            private string ConstructOBJString(int index)
            {
                string idxString = index.ToString();
                return idxString + "/" + idxString + "/" + idxString;
            }


            string MaterialToString(Material m)
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine("newmtl " + m.name);


                //add properties
                if (m.HasProperty("_Color"))
                {
                    sb.AppendLine("Kd " + m.color.r.ToString() + " " + m.color.g.ToString() + " " + m.color.b.ToString());
                    if (m.color.a < 1.0f)
                    {
                        //use both implementations of OBJ transparency
                        sb.AppendLine("Tr " + (1f - m.color.a).ToString());
                        sb.AppendLine("d " + m.color.a.ToString());
                    }
                }
                if (m.HasProperty("_SpecColor"))
                {
                    Color sc = m.GetColor("_SpecColor");
                    sb.AppendLine("Ks " + sc.r.ToString() + " " + sc.g.ToString() + " " + sc.b.ToString());
                }
                if (exportTextures)
                {
                    //diffuse
                    string exResult = TryExportTexture("_MainTex", m, exportPath);
                    if (exResult != "false")
                    {
                        sb.AppendLine("map_Kd " + exResult);
                    }
                    //spec map
                    exResult = TryExportTexture("_SpecMap", m, exportPath);
                    if (exResult != "false")
                    {
                        sb.AppendLine("map_Ks " + exResult);
                    }
                    //bump map
                    exResult = TryExportTexture("_BumpMap", m, exportPath);
                    if (exResult != "false")
                    {
                        sb.AppendLine("map_Bump " + exResult);
                    }

                }
                sb.AppendLine("illum 2");
                return sb.ToString();
            }


            #endregion OBJ_EXPORT


            #region OBJ_IMPORT


            [System.Serializable]
            /// <summary>
            /// Options to define how the model will be loaded and imported.
            /// </summary>
            public class OBJImportOptions : AsImpL.ImportOptions
            {

            }



            public async void Import(string objPath, string texturesFolderPath, string materialsFolderPath, Action<GameObject> Callback, OBJImportOptions importOptions = null)
            {


                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    Debug.LogWarning("The fuction cannot run on WebGL player. As web apps cannot read from or write to local file system.");
                    return;
                }

                if(!String.IsNullOrWhiteSpace(objPath))
                {
                    objPath = Path.GetFullPath(objPath);
                    if(objPath[objPath.Length - 1] == '\\') { objPath = objPath.Remove(objPath.Length - 1); }
                    else if(objPath[objPath.Length - 1] == '/') { objPath = objPath.Remove(objPath.Length - 1); }
                }
                if(!String.IsNullOrWhiteSpace(texturesFolderPath))
                {
                    texturesFolderPath = Path.GetFullPath(texturesFolderPath);
                    if (texturesFolderPath[texturesFolderPath.Length - 1] == '\\') { texturesFolderPath = texturesFolderPath.Remove(texturesFolderPath.Length - 1); }
                    else if (texturesFolderPath[texturesFolderPath.Length - 1] == '/') { texturesFolderPath = texturesFolderPath.Remove(texturesFolderPath.Length - 1); }
                }
                if(!String.IsNullOrWhiteSpace(materialsFolderPath))
                {
                    materialsFolderPath = Path.GetFullPath(materialsFolderPath);
                    if (materialsFolderPath[materialsFolderPath.Length - 1] == '\\') { materialsFolderPath = materialsFolderPath.Remove(materialsFolderPath.Length - 1); }
                    else if (materialsFolderPath[materialsFolderPath.Length - 1] == '/') { materialsFolderPath = materialsFolderPath.Remove(materialsFolderPath.Length - 1); }
                }


                if (!System.IO.File.Exists(objPath))
                {
                    throw new FileNotFoundException("The path provided doesn't point to a file. The path might be invalid or the file is non-existant.");
                }

                if (!System.IO.Directory.Exists(texturesFolderPath))
                {
                    throw new DirectoryNotFoundException("The directory pointed to by the given path for textures is non-existant.");
                }

                if (!System.IO.Directory.Exists(materialsFolderPath))
                {
                    throw new DirectoryNotFoundException("The directory pointed to by the given path for materials is non-existant.");
                }


                string fileNameWithExt = System.IO.Path.GetFileName(objPath);
                string dirPath = System.IO.Path.GetDirectoryName(objPath);
                string objName = fileNameWithExt.Split('.')[0];
                bool didFail = false;

                GameObject objectToPopulate = new GameObject();
                objectToPopulate.AddComponent<ObjectImporter>();
                ObjectImporter objImporter = objectToPopulate.GetComponent<ObjectImporter>();

                if (dirPath.Contains("/") && !dirPath.EndsWith("/")) { dirPath += "/"; }
                else if (!dirPath.EndsWith("\\")) { dirPath += "\\"; }

                var split = fileNameWithExt.Split('.');


                if (split[1].ToLower() != "obj")
                {
                    DestroyImmediate(objectToPopulate);
                    throw new System.InvalidOperationException("The path provided must point to a wavefront obj file.");
                }


                if(importOptions == null)
                {
                    importOptions = new OBJImportOptions();
                }


                try
                {
                    GameObject toReturn = await objImporter.ImportModelAsync(objName, objPath, null, importOptions, texturesFolderPath, materialsFolderPath);
                    Callback(toReturn);
                }

                catch (Exception ex)
                {
                    DestroyImmediate(objectToPopulate);
                    throw ex;
                }

            }

            #endregion OBJ_IMPORT


        }


        #endregion OBJ_EXPORT_IMPORT


    }

}


#endif
/*
 * The credit for OBJ import functionality goes to Marc Kusters. 
 * Check out FastObjImporter.cs
 * https://wiki.unity3d.com/index.php/FastObjImporter
 * 
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PolyFewRuntime
{

    using static PolyFewRuntime.UtilityServicesRuntime.OBJExporterImporter;
    

    public class PolyfewRuntime : MonoBehaviour
    {
        

#region DATA_STRUCTURES


        private const int MAX_LOD_COUNT = 8;
        //public const int MAX_CONCURRENT_THREADS = 16;
#pragma warning disable
        private static int maxConcurrentThreads = SystemInfo.processorCount * 2;


        /// <summary>
        /// A Dictionary that holds a GameObject as key and the associated MeshRendererPair as value
        /// </summary>
        [System.Serializable]
        public class ObjectMeshPairs : Dictionary<GameObject, MeshRendererPair> { }


        /// <summary>
        /// This class represents a simple data structure that holds reference to a mesh and whether that mesh is part of a MeshRenderer (Attached to MeshFilter) or SkinnedMeshRenderer. This structure is used thoroughly in various mesh simplification operations. 
        /// </summary>
        [System.Serializable]
        public class MeshRendererPair
        {
            /// <summary>
            /// Whether mesh is part of a MeshRenderer (Attached to MeshFilter) or SkinnedMeshRenderer.
            /// </summary>
            public bool attachedToMeshFilter;
            /// <summary>
            ///  A reference to a mesh
            /// </summary>
            public Mesh mesh;

            public MeshRendererPair(bool attachedToMeshFilter, Mesh mesh)
            {
                this.attachedToMeshFilter = attachedToMeshFilter;
                this.mesh = mesh;
            }

            public void Destruct()
            {
                if (mesh != null)
                {
                    DestroyImmediate(mesh);
                }
            }
        }


        /// <summary>
        /// This class represents a custom data structure that holds reference to a MeshRendererPair, the GameObject from which the MeshRendererPair was constructed and an Action object used to execute some code. 
        /// </summary>
        [System.Serializable]
        public class CustomMeshActionStructure
        {
            /// <summary>
            /// The MeshRendererPair constructed for the referenced GameObject. This contains the mesh associated with the GameObject if any and some other info about the mesh.
            /// </summary>
            public MeshRendererPair meshRendererPair;
            /// <summary>
            /// The GameObject with which this data structure is associated with.
            /// </summary>
            public GameObject gameObject;
            /// <summary>
            /// An action object that can hold some custom code to execute.
            /// </summary>
            public Action action;

            public CustomMeshActionStructure(MeshRendererPair meshRendererPair, GameObject gameObject, Action action)
            {
                this.meshRendererPair = meshRendererPair;
                this.gameObject = gameObject;
                this.action = action;
            }
        }



        /// <summary>
        /// This class holds all the available options for mesh simplification. An object of this class is needed by many of the Mesh Simplification methods for controlling the mesh simplification process.
        /// </summary>
        [System.Serializable]
        public class SimplificationOptions
        {

            /// <summary> The strength with which to reduce the polygons by. Greater strength results in fewer polygons but lower quality. The acceptable values are between [0-100] inclusive. </summary>
            public float simplificationStrength;

            /// <summary> If set to true the mesh is simplified without loosing too much quality. Please note that simplify lossless cannot guarantee optimal triangle count after simplification. It's best that you specify the simplificationStrength manually and leave this to false. Also in case if this is true then the "simplificationStrength" attribute will be disregarded.  </summary>
            public bool simplifyMeshLossless = false;

            /// <summary> Smart linking links vertices that are very close to each other. This helps in the mesh simplification process where holes or other serious issues could arise. Disabling this (where not needed) can cause a minor performance gain.</summary>
            public bool enableSmartlinking = true;

            /// <summary> This option (if set to true) preserves the mesh areas where the UV seams are made. These are the areas where different UV islands are formed (usually the shallow polygon conjested areas). </summary>
            public bool preserveUVSeamEdges = false;

            /// <summary> This option (if set to true)  preserves UV foldover areas. Usually these are the areas where sharp edges, corners or dents are formed in the mesh or simply the areas where the mesh folds over. </summary>
            public bool preserveUVFoldoverEdges = false;

            /// <summary> This option (if set to true)  preserves border edges of the mesh. Border edges are the edges that are unconnected and open. Preserving border edges might lead to lesser polygon reduction but can be helpful where you see serious mesh and texture distortions. </summary>
            public bool preserveBorderEdges = false;

            /// <summary> This option (if set to true) will take into account the preservation sphere (If specified in the SimplificationOptions). The preservation sphere retains the original quality of the mesh area enclosed within it while simplifying all other areas of the mesh. Please note that mesh simplification with preservation sphere might get slow.</summary>
            public bool regardPreservationSphere = false;

            /// <summary> The maximum passes the reduction algorithm does. Higher number is more expensive but can bring you closer to your target quality. 100 is the lowest allowed value. The default value of 100 works best for most of the meshes and should not be changed. </summary>
            public int maxIterations = 100;

            /// <summary> The agressiveness of the reduction algorithm to use for this LOD level. Higher number equals higher quality, but more expensive to run. Lowest value is 7. The default value of 7 works best for most of the meshes and should not be changed. </summary>
            public float aggressiveness = 7;

            /// <summary> If you specify to regard the tolerance sphere then you must set this to the world space center of the tolerance sphere (transform.position). Please note that if you're using "tranform.position" to get the center coordinates for an existing gameobject to act as a tolerance sphere instead of manually specifying them, then it might report incorrect center, if the pivot point of the object is not well centered. </summary>
            public Vector3? preservationSphereCenterWorldSpace = null;

            /// <summary> If you specify to regard the tolerance sphere then you must set this to the world space scale of the tolerance sphere (transform.lossyScale). Please note that if you're using "transform.lossyScale" to get the scale values for an existing gameobject to act as a tolerance sphere instead of manually specifying them, then it might report incorrect scale, if the pivot point of the object is not well centered. </summary>
            public Vector3? preservationSphereWorldScale = null;



            public SimplificationOptions() { }

            public SimplificationOptions(float simplificationStrength, bool simplifyOptimal, bool enableSmartlink, bool preserveUVSeamEdges, bool preserveUVFoldoverEdges, bool preserveBorderEdges, bool regardToleranceSphere, int maxIterations, float aggressiveness, Vector3? preservationSphereCenterWorldSpace, Vector3? preservationSphereWorldScale)
            {
                this.simplificationStrength = simplificationStrength;
                this.simplifyMeshLossless = simplifyOptimal;
                this.enableSmartlinking = enableSmartlink;
                this.preserveUVSeamEdges = preserveUVSeamEdges;
                this.preserveUVFoldoverEdges = preserveUVFoldoverEdges;
                this.preserveBorderEdges = preserveBorderEdges;
                this.regardPreservationSphere = regardToleranceSphere;
                this.maxIterations = maxIterations;
                this.aggressiveness = aggressiveness;
                this.preservationSphereCenterWorldSpace = preservationSphereCenterWorldSpace;
                this.preservationSphereWorldScale = preservationSphereWorldScale;

                if (this.regardPreservationSphere)
                {
                    if (this.preservationSphereCenterWorldSpace == null || this.preservationSphereWorldScale == null)
                    {
                        this.regardPreservationSphere = false;
                    }
                }
            }


        }



        /// <summary>
        /// Options that define how the model will be loaded and imported.
        /// </summary>
        [System.Serializable]
        public class OBJImportOptions : AsImpL.ImportOptions
        {

        }


        /// <summary>
        /// Options that define how the a GameObject will be exported to wavefront OBJ.
        /// </summary>
        [System.Serializable]
        public class OBJExportOptions
        {


            /// <summary>
            /// When checked, the position of models will be taken into account on export.
            /// </summary>
            public readonly bool applyPosition = true;
            /// <summary>
            /// When checked, the rotation of models will be taken into account on export.
            /// </summary>
            public readonly bool applyRotation = true;
            /// <summary>
            /// When checked, the scale of models will be taken into account on export.
            /// </summary>
            public readonly bool applyScale = true;
            /// <summary>
            /// Should the materials associated with the GameObject to export also be exported as .MTL files.
            /// </summary>
            public readonly bool generateMaterials = true;
            /// <summary>
            /// Should the textures associated with the materials also be exported.
            /// </summary>
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



        /// <summary>
        /// A wrapper class that holds a primitive numeric type and fakes them to act as reference types.
        /// </summary>
        /// <typeparam name="T"> Any primitive numeric type. Int, float, double, byte etc</typeparam>
        public class ReferencedNumeric<T> where T : struct,
        IComparable,
        IComparable<T>,
        IConvertible,
        IEquatable<T>,
        IFormattable
        {
            private T val;
            public T Value { get { return val; } set { val = value; } }

            public ReferencedNumeric(T value)
            {
                val = value;
            }
        }


#endregion DATA_STRUCTURES




#region PUBLIC_METHODS


        /// <summary>
        /// Simplifies the provided gameobject include the full nested children hierarchy with the settings provided. Any errors are thrown as exceptions with relevant information. Please note that the method won't simplify the object if the simplification strength provided in the SimplificationOptions is close to 0.
        /// </summary>
        /// <param name="toSimplify"> The gameobject to simplify.</param>
        /// <param name="simplificationOptions"> Provide a SimplificationOptions object which contains different parameters and rules for simplifying the meshes. </param>
        /// <param name="OnEachMeshSimplified"> This method will be called when a mesh is simplified. The method will be passed a gameobject whose mesh is simplified and some information about the original unsimplified mesh. If you donot want to receive this callback then you can pass null as an argument here.</param>
        /// <returns> The total number of triangles after simplifying the provided gameobject inlcuding the nested children hierarchies. Please note that the method returns -1 if the method doesn't simplify the object. </returns>

        public static int SimplifyObjectDeep(GameObject toSimplify, SimplificationOptions simplificationOptions, Action<GameObject, MeshRendererPair> OnEachMeshSimplified)
        {

            if (simplificationOptions == null)
            {
                throw new ArgumentNullException("simplificationOptions", "You must provide a SimplificationOptions object.");
            }

            int totalTriangles = 0;
            float simplificationStrength = simplificationOptions.simplificationStrength;


            if (toSimplify == null)
            {
                throw new ArgumentNullException("toSimplify", "You must provide a gameobject to simplify.");
            }

            if (!simplificationOptions.simplifyMeshLossless)
            {
                if (!(simplificationStrength >= 0 && simplificationStrength <= 100))
                {
                    throw new ArgumentOutOfRangeException("simplificationStrength", "The allowed values for simplification strength are between [0-100] inclusive.");
                }

                if (Mathf.Approximately(simplificationStrength, 0)) { return -1; }
            }




            ObjectMeshPairs objectMeshPairs = GetObjectMeshPairs(toSimplify, true);

            if (!AreAnyFeasibleMeshes(objectMeshPairs))
            {
                throw new InvalidOperationException("No mesh/meshes found nested under the provided gameobject to simplify.");
            }


            bool runOnThreads = false;

            int trianglesCount = CountTriangles(objectMeshPairs);

            if (trianglesCount >= 2000 && objectMeshPairs.Count >= 2)
            {
                runOnThreads = true;
            }

            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                runOnThreads = false;
            }


            float quality = 1f - (simplificationStrength / 100f);
            int totalMeshCount = objectMeshPairs.Count;
            int meshesHandled = 0;
            int threadsRunning = 0;
            bool isError = false;
#pragma warning disable
            string error = "";

            object threadLock1 = new object();
            object threadLock2 = new object();
            object threadLock3 = new object();


            if (runOnThreads)
            {

                List<CustomMeshActionStructure> meshAssignments = new List<CustomMeshActionStructure>();
                List<CustomMeshActionStructure> callbackFlusher = new List<CustomMeshActionStructure>();

                //System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                //watch.Start();

                foreach (var kvp in objectMeshPairs)
                {

                    GameObject gameObject = kvp.Key;

                    if (gameObject == null) { meshesHandled++; continue; }

                    MeshRendererPair meshRendererPair = kvp.Value;

                    if (meshRendererPair.mesh == null) { meshesHandled++; continue; }

                    var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();

                    SetParametersForSimplifier(simplificationOptions, meshSimplifier);

                    meshSimplifier.Initialize(meshRendererPair.mesh);


                    //while (threadsRunning == maxConcurrentThreads) { } // Don't create another thread if the max limit is reached wait for existing threads to clear

                    threadsRunning++;


                    while (callbackFlusher.Count > 0)
                    {
                        var meshInfo = callbackFlusher[0];

                        callbackFlusher.RemoveAt(0);

                        if (meshInfo == null) { continue; }

                        OnEachMeshSimplified?.Invoke(meshInfo.gameObject, meshInfo.meshRendererPair);
                    }



                    Vector3 ignoreSphereCenterLocal = Vector3.zero;
                    float sphereRadius = -1;

                    if (simplificationOptions.regardPreservationSphere)
                    {
                        ignoreSphereCenterLocal = gameObject.transform.InverseTransformPoint((Vector3)simplificationOptions.preservationSphereCenterWorldSpace);
                        sphereRadius = ((Vector3)simplificationOptions.preservationSphereWorldScale).x / 2f;
                    }


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

                                totalTriangles += reducedMesh.triangles.Length / 3;
                            }

                        );


                        try
                        {
                            if (!simplificationOptions.simplifyMeshLossless)
                            {
                                if (simplificationOptions.regardPreservationSphere)
                                {
                                    meshSimplifier.SimplifyMesh(quality, ignoreSphereCenterLocal, sphereRadius);
                                }

                                else
                                {
                                    meshSimplifier.SimplifyMesh(quality);
                                }

                            }

                            else
                            {
                                if (simplificationOptions.regardPreservationSphere)
                                {
                                    meshSimplifier.SimplifyMeshLossless(ignoreSphereCenterLocal, sphereRadius);
                                }
                                else
                                {
                                    meshSimplifier.SimplifyMeshLossless();
                                }
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


                            lock (threadLock3)
                            {
                                CustomMeshActionStructure callbackFlush = new CustomMeshActionStructure
                                (
                                    meshRendererPair,
                                    gameObject,
                                    () => { }
                                );

                                callbackFlusher.Add(callbackFlush);
                            }

                        }
#pragma warning disable
                        catch (Exception ex)
                        {
                            lock (threadLock2)
                            {
                                threadsRunning--;
                                meshesHandled++;
                                isError = true;
                                error = ex.ToString();
                                //structure?.action();
                                //OnEachSimplificationError?.Invoke(error, structure?.gameObject, structure?.meshRendererPair);
                            }
                        }

                    }, CancellationToken.None, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current);

                }

                //Wait for all threads to complete
                //Not reliable sometimes gets stuck
                //System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

                while (callbackFlusher.Count > 0)
                {
                    var meshInfo = callbackFlusher[0];

                    callbackFlusher.RemoveAt(0);

                    if (meshInfo == null) { continue; }

                    OnEachMeshSimplified?.Invoke(meshInfo.gameObject, meshInfo.meshRendererPair);
                }


                while (meshesHandled < totalMeshCount && !isError)
                {
                    while (callbackFlusher.Count > 0)
                    {
                        var meshInfo = callbackFlusher[0];

                        callbackFlusher.RemoveAt(0);

                        if (meshInfo == null) { continue; }

                        OnEachMeshSimplified?.Invoke(meshInfo.gameObject, meshInfo.meshRendererPair);
                    }
                }


                if (!isError)
                {
                    foreach (CustomMeshActionStructure structure in meshAssignments)
                    {
                        structure?.action();
                    }
                }

                else
                {
                    //OnError?.Invoke(error);
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

                    MeshRendererPair meshRendererPair = kvp.Value;

                    if (meshRendererPair.mesh == null) { continue; }


                    var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();

                    SetParametersForSimplifier(simplificationOptions, meshSimplifier);

                    //meshSimplifier.VertexLinkDistance = meshSimplifier.VertexLinkDistance / 10f;
                    meshSimplifier.Initialize(meshRendererPair.mesh);


                    Vector3 ignoreSphereCenterLocal = Vector3.zero;
                    float sphereRadius = -1;

                    if (simplificationOptions.regardPreservationSphere)
                    {
                        ignoreSphereCenterLocal = gameObject.transform.InverseTransformPoint((Vector3)simplificationOptions.preservationSphereCenterWorldSpace);
                        sphereRadius = ((Vector3)simplificationOptions.preservationSphereWorldScale).x / 2f;
                    }

                    if (!simplificationOptions.simplifyMeshLossless)
                    {
                        if (simplificationOptions.regardPreservationSphere)
                        {
                            meshSimplifier.SimplifyMesh(quality, ignoreSphereCenterLocal, sphereRadius);
                        }

                        else
                        {
                            meshSimplifier.SimplifyMesh(quality);
                        }
                    }

                    else
                    {
                        if (simplificationOptions.regardPreservationSphere)
                        {
                            meshSimplifier.SimplifyMeshLossless(ignoreSphereCenterLocal, sphereRadius);
                        }
                        else
                        {
                            meshSimplifier.SimplifyMeshLossless();
                        }
                    }

                    OnEachMeshSimplified?.Invoke(gameObject, meshRendererPair);

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

                    totalTriangles += reducedMesh.triangles.Length / 3;
                }
            }

            return totalTriangles;

        }



        /// <summary>
        /// Simplifies the meshes nested under the given gameobject(including itself) including the full nested children hierarchy with the settings provided. Retuns back a specialized data structure with the simplified meshes. Any errors are thrown as exceptions with relevant information. Please note that the method won't simplify the object if the simplification strength provided in the SimplificationOptions is close to 0.
        /// </summary>
        /// <param name="toSimplify"> The gameobject to simplify.</param>
        /// <param name="simplificationOptions"> Provide a SimplificationOptions object which contains different parameters and rules for simplifying the meshes. </param>
        /// <param name="OnEachMeshSimplified"> This method will be called when a mesh is simplified. The method will be passed a gameobject whose mesh is simplified and some information about the original unsimplified mesh.</param>
        /// <returns> A specialized data structure that holds information about all the simplified meshes and their information and the GameObjects with which they are associated. Please note that in case the simplificationStrength was near 0 the method doesn't simplify any meshes and returns null. </returns>

        public static ObjectMeshPairs SimplifyObjectDeep(GameObject toSimplify, SimplificationOptions simplificationOptions)
        {

            if (simplificationOptions == null)
            {
                throw new ArgumentNullException("simplificationOptions", "You must provide a SimplificationOptions object.");
            }

            float simplificationStrength = simplificationOptions.simplificationStrength;
            ObjectMeshPairs toReturn = new ObjectMeshPairs();


            if (toSimplify == null)
            {
                throw new ArgumentNullException("toSimplify", "You must provide a gameobject to simplify.");
            }

            if (!simplificationOptions.simplifyMeshLossless)
            {
                if (!(simplificationStrength >= 0 && simplificationStrength <= 100))
                {
                    throw new ArgumentOutOfRangeException("simplificationStrength", "The allowed values for simplification strength are between [0-100] inclusive.");
                }

                if (Mathf.Approximately(simplificationStrength, 0)) { return null; }
            }


            ObjectMeshPairs objectMeshPairs = GetObjectMeshPairs(toSimplify, true);

            if (!AreAnyFeasibleMeshes(objectMeshPairs))
            {
                throw new InvalidOperationException("No mesh/meshes found nested under the provided gameobject to simplify.");
            }


            bool runOnThreads = false;

            int trianglesCount = CountTriangles(objectMeshPairs);

            if (trianglesCount >= 2000 && objectMeshPairs.Count >= 2)
            {
                runOnThreads = true;
            }

            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                runOnThreads = false;
            }


            float quality = 1f - (simplificationStrength / 100f);
            int totalMeshCount = objectMeshPairs.Count;
            int meshesHandled = 0;
            int threadsRunning = 0;
            bool isError = false;
#pragma warning disable
            string error = "";

            object threadLock1 = new object();
            object threadLock2 = new object();


            if (runOnThreads)
            {

                List<CustomMeshActionStructure> meshAssignments = new List<CustomMeshActionStructure>();

                //System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                //watch.Start();


                foreach (var kvp in objectMeshPairs)
                {

                    GameObject gameObject = kvp.Key;

                    if (gameObject == null) { meshesHandled++; continue; }

                    MeshRendererPair meshRendererPair = kvp.Value;

                    if (meshRendererPair.mesh == null) { meshesHandled++; continue; }

                    var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();

                    SetParametersForSimplifier(simplificationOptions, meshSimplifier);

                    meshSimplifier.Initialize(meshRendererPair.mesh);


                    //while (threadsRunning == maxConcurrentThreads) { } // Don't create another thread if the max limit is reached wait for existing threads to clear

                    threadsRunning++;



                    Vector3 ignoreSphereCenterLocal = Vector3.zero;
                    float sphereRadius = -1;

                    if (simplificationOptions.regardPreservationSphere)
                    {
                        ignoreSphereCenterLocal = gameObject.transform.InverseTransformPoint((Vector3)simplificationOptions.preservationSphereCenterWorldSpace);
                        sphereRadius = ((Vector3)simplificationOptions.preservationSphereWorldScale).x / 2f;
                    }

                    Task.Factory.StartNew(() =>
                    {

                        CustomMeshActionStructure structure = new CustomMeshActionStructure
                        (

                            meshRendererPair,

                            gameObject,

                            () =>
                            {
                                var reducedMesh = meshSimplifier.ToMesh();
                                reducedMesh.bindposes = meshRendererPair.mesh.bindposes;
                                reducedMesh.name = meshRendererPair.mesh.name.Replace("-POLY_REDUCED", "") + "-POLY_REDUCED";

                                MeshRendererPair redMesh = new MeshRendererPair(meshRendererPair.attachedToMeshFilter, reducedMesh);

                                toReturn.Add(gameObject, redMesh);
                            }

                        );


                        try
                        {
                            if (!simplificationOptions.simplifyMeshLossless)
                            {
                                if (simplificationOptions.regardPreservationSphere)
                                {
                                    meshSimplifier.SimplifyMesh(quality, ignoreSphereCenterLocal, sphereRadius);
                                }

                                else
                                {
                                    meshSimplifier.SimplifyMesh(quality);
                                }

                            }

                            else
                            {
                                if (simplificationOptions.regardPreservationSphere)
                                {
                                    meshSimplifier.SimplifyMeshLossless(ignoreSphereCenterLocal, sphereRadius);
                                }
                                else
                                {
                                    meshSimplifier.SimplifyMeshLossless();
                                }
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
#pragma warning disable
                        catch (Exception ex)
                        {
                            lock (threadLock2)
                            {
                                threadsRunning--;
                                meshesHandled++;
                                isError = true;
                                error = ex.ToString();
                                //structure?.action();
                                //OnEachSimplificationError?.Invoke(error, structure?.gameObject, structure?.meshRendererPair);
                            }
                        }

                    }, CancellationToken.None, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current);

                }

                //Wait for all threads to complete
                //Not reliable sometimes gets stuck
                //System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();


                while (meshesHandled < totalMeshCount && !isError)
                {

                }


                if (!isError)
                {
                    foreach (CustomMeshActionStructure structure in meshAssignments)
                    {
                        structure?.action();
                    }
                }

                else
                {
                    //OnError?.Invoke(error);
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

                    MeshRendererPair meshRendererPair = kvp.Value;

                    if (meshRendererPair.mesh == null) { continue; }


                    var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();

                    SetParametersForSimplifier(simplificationOptions, meshSimplifier);

                    //meshSimplifier.VertexLinkDistance = meshSimplifier.VertexLinkDistance / 10f;
                    meshSimplifier.Initialize(meshRendererPair.mesh);


                    Vector3 ignoreSphereCenterLocal = Vector3.zero;
                    float sphereRadius = -1;

                    if (simplificationOptions.regardPreservationSphere)
                    {
                        ignoreSphereCenterLocal = gameObject.transform.InverseTransformPoint((Vector3)simplificationOptions.preservationSphereCenterWorldSpace);
                        sphereRadius = ((Vector3)simplificationOptions.preservationSphereWorldScale).x / 2f;
                    }

                    if (!simplificationOptions.simplifyMeshLossless)
                    {
                        if (simplificationOptions.regardPreservationSphere)
                        {
                            meshSimplifier.SimplifyMesh(quality, ignoreSphereCenterLocal, sphereRadius);
                        }

                        else
                        {
                            meshSimplifier.SimplifyMesh(quality);
                        }
                    }

                    else
                    {
                        if (simplificationOptions.regardPreservationSphere)
                        {
                            meshSimplifier.SimplifyMeshLossless(ignoreSphereCenterLocal, sphereRadius);
                        }
                        else
                        {
                            meshSimplifier.SimplifyMeshLossless();
                        }
                    }


                    var reducedMesh = meshSimplifier.ToMesh();
                    reducedMesh.bindposes = meshRendererPair.mesh.bindposes;   // Might cause issues
                    reducedMesh.name = meshRendererPair.mesh.name.Replace("-POLY_REDUCED", "") + "-POLY_REDUCED";


                    if (meshRendererPair.attachedToMeshFilter)
                    {
                        MeshFilter filter = gameObject.GetComponent<MeshFilter>();

                        MeshRendererPair redMesh = new MeshRendererPair(true, reducedMesh);
                        toReturn.Add(gameObject, redMesh);


                        if (filter != null)
                        {
                            filter.sharedMesh = reducedMesh;
                        }


                    }

                    else
                    {
                        SkinnedMeshRenderer sRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();

                        MeshRendererPair redMesh = new MeshRendererPair(false, reducedMesh);
                        toReturn.Add(gameObject, redMesh);

                        if (sRenderer != null)
                        {
                            sRenderer.sharedMesh = reducedMesh;
                        }
                    }

                }
            }

            return toReturn;

        }



        /// <summary>
        /// Simplifies the meshes provided in the "objectMeshPairs" argument and assigns the simplified meshes to the corresponding objects. Any errors are thrown as exceptions with relevant information. Please note that the method won't simplify the object if the simplification strength provided in the SimplificationOptions is close to 0.
        /// </summary>
        /// <param name="objectMeshPairs"> The ObjectMeshPairs data structure which holds relationship between objects and the corresponding meshes which will be simplified. You can get this structure by calling "GetObjectMeshPairs(GameObject forObject, bool includeInactive)" method.</param>
        /// <param name="simplificationOptions"> Provide a SimplificationOptions object which contains different parameters and rules for simplifying the meshes. </param>
        /// <param name="OnEachMeshSimplified"> This method will be called when a mesh is simplified. The method will be passed a gameobject whose mesh is simplified and some information about the original unsimplified mesh.  If you donot want to receive this callback then you can pass null as an argument here.</param>
        /// <returns> The total number of triangles after simplifying the provided gameobject inlcuding the nested children hierarchies. Please note that the method returns -1 is the method doesn't simplify the object. </returns>

        public static int SimplifyObjectDeep(ObjectMeshPairs objectMeshPairs, SimplificationOptions simplificationOptions, Action<GameObject, MeshRendererPair> OnEachMeshSimplified)
        {
            //System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            //watch.Start();

            if (simplificationOptions == null)
            {
                throw new ArgumentNullException("simplificationOptions", "You must provide a SimplificationOptions object.");
            }

            int totalTriangles = 0;
            float simplificationStrength = simplificationOptions.simplificationStrength;


            if (objectMeshPairs == null)
            {
                throw new ArgumentNullException("objectMeshPairs", "You must provide the objectMeshPairs structure to simplify.");
            }

            if (!simplificationOptions.simplifyMeshLossless)
            {
                if (!(simplificationStrength >= 0 && simplificationStrength <= 100))
                {
                    throw new ArgumentOutOfRangeException("simplificationStrength", "The allowed values for simplification strength are between [0-100] inclusive.");
                }

                if (Mathf.Approximately(simplificationStrength, 0)) { return -1; }
            }


            if (!AreAnyFeasibleMeshes(objectMeshPairs))
            {
                throw new InvalidOperationException("No mesh/meshes found nested under the provided gameobject to simplify.");
            }



            bool runOnThreads = false;

            int trianglesCount = CountTriangles(objectMeshPairs);

            if (trianglesCount >= 2000 && objectMeshPairs.Count >= 2)
            {
                runOnThreads = true;
            }

            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                runOnThreads = false;
            }


            float quality = 1f - (simplificationStrength / 100f);
            int totalMeshCount = objectMeshPairs.Count;
            int meshesHandled = 0;
            int threadsRunning = 0;
            bool isError = false;
#pragma warning disable
            string error = "";

            object threadLock1 = new object();
            object threadLock2 = new object();
            object threadLock3 = new object();


            if (runOnThreads)
            {


                List<CustomMeshActionStructure> meshAssignments = new List<CustomMeshActionStructure>();
                List<CustomMeshActionStructure> callbackFlusher = new List<CustomMeshActionStructure>();

                foreach (var kvp in objectMeshPairs)
                {

                    GameObject gameObject = kvp.Key;

                    if (gameObject == null) { meshesHandled++; continue; }

                    MeshRendererPair meshRendererPair = kvp.Value;

                    if (meshRendererPair.mesh == null) { meshesHandled++; continue; }

                    var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();

                    SetParametersForSimplifier(simplificationOptions, meshSimplifier);

                    meshSimplifier.Initialize(meshRendererPair.mesh);


                    //while (threadsRunning == maxConcurrentThreads) { } // Don't create another thread if the max limit is reached wait for existing threads to clear

                    threadsRunning++;



                    while (callbackFlusher.Count > 0)
                    {
                        var meshInfo = callbackFlusher[0];

                        callbackFlusher.RemoveAt(0);

                        if (meshInfo == null) { continue; }

                        OnEachMeshSimplified?.Invoke(meshInfo.gameObject, meshInfo.meshRendererPair);
                    }




                    Vector3 ignoreSphereCenterLocal = Vector3.zero;
                    float sphereRadius = -1;

                    if (simplificationOptions.regardPreservationSphere)
                    {
                        ignoreSphereCenterLocal = gameObject.transform.InverseTransformPoint((Vector3)simplificationOptions.preservationSphereCenterWorldSpace);
                        sphereRadius = ((Vector3)simplificationOptions.preservationSphereWorldScale).x / 2f;
                    }

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

                                totalTriangles += reducedMesh.triangles.Length / 3;
                            }

                        );


                        try
                        {
                            if (!simplificationOptions.simplifyMeshLossless)
                            {
                                if (simplificationOptions.regardPreservationSphere)
                                {
                                    meshSimplifier.SimplifyMesh(quality, ignoreSphereCenterLocal, sphereRadius);
                                }

                                else
                                {
                                    meshSimplifier.SimplifyMesh(quality);
                                }

                            }

                            else
                            {
                                if (simplificationOptions.regardPreservationSphere)
                                {
                                    meshSimplifier.SimplifyMeshLossless(ignoreSphereCenterLocal, sphereRadius);
                                }
                                else
                                {
                                    meshSimplifier.SimplifyMeshLossless();
                                }
                            }



                            // Create cannot be called from a background thread
                            lock (threadLock1)
                            {
                                meshAssignments.Add(structure);

                                threadsRunning--;
                                meshesHandled++;
                            }


                            lock (threadLock3)
                            {
                                CustomMeshActionStructure callbackFlush = new CustomMeshActionStructure
                                (
                                    meshRendererPair,
                                    gameObject,
                                    () => { }
                                );

                                callbackFlusher.Add(callbackFlush);
                            }

                        }
#pragma warning disable
                        catch (Exception ex)
                        {
                            lock (threadLock2)
                            {
                                threadsRunning--;
                                meshesHandled++;
                                isError = true;
                                error = ex.ToString();
                                //structure?.action();
                                //OnEachSimplificationError?.Invoke(error, structure?.gameObject, structure?.meshRendererPair);
                            }
                        }
                    }, CancellationToken.None, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current);

                }



                //Wait for all threads to complete
                //Not reliable sometimes gets stuck
                //System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();


                while (callbackFlusher.Count > 0)
                {
                    var meshInfo = callbackFlusher[0];

                    callbackFlusher.RemoveAt(0);

                    if (meshInfo == null) { continue; }

                    OnEachMeshSimplified?.Invoke(meshInfo.gameObject, meshInfo.meshRendererPair);
                }

                while (meshesHandled < totalMeshCount && !isError)
                {
                    while (callbackFlusher.Count > 0)
                    {
                        var meshInfo = callbackFlusher[0];

                        callbackFlusher.RemoveAt(0);

                        if (meshInfo == null) { continue; }

                        OnEachMeshSimplified?.Invoke(meshInfo.gameObject, meshInfo.meshRendererPair);
                    }
                }


                //watch.Stop();
                //Debug.Log("Elapsed Time   " + watch.ElapsedMilliseconds);

                if (!isError)
                {
                    foreach (CustomMeshActionStructure structure in meshAssignments)
                    {
                        structure?.action();
                    }
                }

                else
                {
                    //OnError?.Invoke(error);
                }

                //watch.Stop();
                //Debug.Log("Elapsed Time   " + watch.ElapsedMilliseconds );
                //Debug.Log("MESHESHANDLED  " + meshesHandled + "  Threads Allowed?  " + maxConcurrentThreads + "   Elapsed Time   "  +watch.Elapsed.TotalSeconds);

            }

            else
            {

                foreach (var kvp in objectMeshPairs)
                {

                    GameObject gameObject = kvp.Key;

                    if (gameObject == null) { continue; }

                    MeshRendererPair meshRendererPair = kvp.Value;

                    if (meshRendererPair.mesh == null) { continue; }


                    var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();

                    SetParametersForSimplifier(simplificationOptions, meshSimplifier);

                    //meshSimplifier.VertexLinkDistance = meshSimplifier.VertexLinkDistance / 10f;
                    meshSimplifier.Initialize(meshRendererPair.mesh);


                    Vector3 ignoreSphereCenterLocal = Vector3.zero;
                    float sphereRadius = -1;

                    if (simplificationOptions.regardPreservationSphere)
                    {
                        ignoreSphereCenterLocal = gameObject.transform.InverseTransformPoint((Vector3)simplificationOptions.preservationSphereCenterWorldSpace);
                        sphereRadius = ((Vector3)simplificationOptions.preservationSphereWorldScale).x / 2f;
                    }

                    if (!simplificationOptions.simplifyMeshLossless)
                    {
                        if (simplificationOptions.regardPreservationSphere)
                        {
                            meshSimplifier.SimplifyMesh(quality, ignoreSphereCenterLocal, sphereRadius);
                        }

                        else
                        {
                            meshSimplifier.SimplifyMesh(quality);
                        }
                    }

                    else
                    {
                        if (simplificationOptions.regardPreservationSphere)
                        {
                            meshSimplifier.SimplifyMeshLossless(ignoreSphereCenterLocal, sphereRadius);
                        }
                        else
                        {
                            meshSimplifier.SimplifyMeshLossless();
                        }
                    }

                    OnEachMeshSimplified?.Invoke(gameObject, meshRendererPair);

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

                    totalTriangles += reducedMesh.triangles.Length / 3;
                }

            }

            return totalTriangles;

        }



        /// <summary>
        /// Simplifies the meshes provided in the "meshesToSimplify" argument and returns the simplified meshes in a new list. Any errors are thrown as exceptions with relevant information.Please note that the returned list of simplified meshes doesn't guarantee the same order of meshes as supplied in the "meshesToSimplify" list. 
        /// </summary>
        /// <param name="meshesToSimplify"> The list of meshes to simplify.</param>
        /// <param name="simplificationOptions"> Provide a SimplificationOptions object which contains different parameters and rules for simplifying the meshes. Please note that preservationSphere won't work for this method. </param>
        /// <param name="OnEachMeshSimplified"> This method will be called when a mesh is simplified. The method will be passed the original mesh that was simplified. </param>
        /// <returns> The list of simplified meshes. </returns>

        public static List<Mesh> SimplifyMeshes(List<Mesh> meshesToSimplify, SimplificationOptions simplificationOptions, Action<Mesh> OnEachMeshSimplified)
        {

            //System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            //watch.Start();
            List<Mesh> simplifiedMeshes = new List<Mesh>();

            if (simplificationOptions == null)
            {
                throw new ArgumentNullException("simplificationOptions", "You must provide a SimplificationOptions object.");
            }

            int totalTriangles = 0;
            float simplificationStrength = simplificationOptions.simplificationStrength;


            if (meshesToSimplify == null)
            {
                throw new ArgumentNullException("meshesToSimplify", "You must provide a meshes list to simplify.");
            }

            if (meshesToSimplify.Count == 0)
            {
                throw new InvalidOperationException("You must provide a non-empty list of meshes to simplify.");
            }

            if (!simplificationOptions.simplifyMeshLossless)
            {
                if (!(simplificationStrength >= 0 && simplificationStrength <= 100))
                {
                    throw new ArgumentOutOfRangeException("simplificationStrength", "The allowed values for simplification strength are between [0-100] inclusive.");
                }

                if (Mathf.Approximately(simplificationStrength, 0)) { return null; }
            }



            bool runOnThreads = false;

            int trianglesCount = CountTriangles(meshesToSimplify);

            if (trianglesCount >= 2000 && meshesToSimplify.Count >= 2)
            {
                runOnThreads = true;
            }

            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                runOnThreads = false;
            }


            float quality = 1f - (simplificationStrength / 100f);
            int totalMeshCount = meshesToSimplify.Count;
            int meshesHandled = 0;
            int threadsRunning = 0;
            bool isError = false;
#pragma warning disable
            string error = "";

            object threadLock1 = new object();
            object threadLock2 = new object();
            object threadLock3 = new object();
            runOnThreads = true;

            if (runOnThreads)
            {


                List<CustomMeshActionStructure> meshAssignments = new List<CustomMeshActionStructure>();
                List<CustomMeshActionStructure> callbackFlusher = new List<CustomMeshActionStructure>();

                foreach (var meshToSimplify in meshesToSimplify)
                {

                    if (meshToSimplify == null) { meshesHandled++; continue; }


                    var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();

                    SetParametersForSimplifier(simplificationOptions, meshSimplifier);

                    meshSimplifier.Initialize(meshToSimplify);


                    //while (threadsRunning == maxConcurrentThreads) { } // Don't create another thread if the max limit is reached wait for existing threads to clear

                    threadsRunning++;


                    while (callbackFlusher.Count > 0)
                    {
                        var meshInfo = callbackFlusher[0];
                        callbackFlusher.RemoveAt(0);

                        OnEachMeshSimplified?.Invoke(meshInfo.meshRendererPair.mesh);
                    }


                    Task.Factory.StartNew(() =>
                    {

                        CustomMeshActionStructure structure = new CustomMeshActionStructure
                        (

                            null,

                            null,

                            () =>
                            {
                                var reducedMesh = meshSimplifier.ToMesh();
                                reducedMesh.bindposes = meshToSimplify.bindposes;
                                reducedMesh.name = meshToSimplify.name.Replace("-POLY_REDUCED", "") + "-POLY_REDUCED";

                                simplifiedMeshes.Add(reducedMesh);
                            }

                        );


                        try
                        {
                            if (!simplificationOptions.simplifyMeshLossless)
                            {
                                meshSimplifier.SimplifyMesh(quality);
                            }

                            else
                            {
                                meshSimplifier.SimplifyMeshLossless();
                            }


                            // Create cannot be called from a background thread
                            lock (threadLock1)
                            {
                                meshAssignments.Add(structure);

                                threadsRunning--;
                                meshesHandled++;
                            }


                            lock (threadLock3)
                            {
                                MeshRendererPair mRendererPair = new MeshRendererPair(true, meshToSimplify);

                                CustomMeshActionStructure callbackFlush = new CustomMeshActionStructure
                                (
                                    mRendererPair,
                                    null,
                                    () => { }
                                );

                                callbackFlusher.Add(callbackFlush);
                            }

                        }
#pragma warning disable
                        catch (Exception ex)
                        {
                            lock (threadLock2)
                            {
                                threadsRunning--;
                                meshesHandled++;
                                isError = true;
                                error = ex.ToString();
                                //structure?.action();
                                //OnEachSimplificationError?.Invoke(error, structure?.gameObject, structure?.meshRendererPair);
                            }
                        }
                    }, CancellationToken.None, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current);

                }



                //Wait for all threads to complete
                //Not reliable sometimes gets stuck
                //System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();


                while (callbackFlusher.Count > 0)
                {
                    var meshInfo = callbackFlusher[0];
                    callbackFlusher.RemoveAt(0);

                    OnEachMeshSimplified?.Invoke(meshInfo.meshRendererPair.mesh);
                }


                while (meshesHandled < totalMeshCount && !isError)
                {
                    while (callbackFlusher.Count > 0)
                    {
                        var meshInfo = callbackFlusher[0];
                        callbackFlusher.RemoveAt(0);

                        OnEachMeshSimplified?.Invoke(meshInfo.meshRendererPair.mesh);
                    }
                }


                //watch.Stop();
                //Debug.Log("Elapsed Time   " + watch.ElapsedMilliseconds);

                if (!isError)
                {
                    foreach (CustomMeshActionStructure structure in meshAssignments)
                    {
                        structure?.action();
                    }
                }

                else
                {
                    //OnError?.Invoke(error);
                }

                //watch.Stop();
                //Debug.Log("Elapsed Time   " + watch.ElapsedMilliseconds );
                //Debug.Log("MESHESHANDLED  " + meshesHandled + "  Threads Allowed?  " + maxConcurrentThreads + "   Elapsed Time   "  +watch.Elapsed.TotalSeconds);

            }

            else
            {

                foreach (var meshToSimplify in meshesToSimplify)
                {

                    if (meshToSimplify == null) { continue; }


                    var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();

                    SetParametersForSimplifier(simplificationOptions, meshSimplifier);

                    //meshSimplifier.VertexLinkDistance = meshSimplifier.VertexLinkDistance / 10f;
                    meshSimplifier.Initialize(meshToSimplify);


                    if (!simplificationOptions.simplifyMeshLossless)
                    {
                        meshSimplifier.SimplifyMesh(quality);
                    }

                    else
                    {
                        meshSimplifier.SimplifyMeshLossless();
                    }

                    OnEachMeshSimplified?.Invoke(meshToSimplify);

                    var reducedMesh = meshSimplifier.ToMesh();
                    reducedMesh.bindposes = meshToSimplify.bindposes;
                    reducedMesh.name = meshToSimplify.name.Replace("-POLY_REDUCED", "") + "-POLY_REDUCED";


                    simplifiedMeshes.Add(reducedMesh);

                }

            }

            return simplifiedMeshes;
        }



        /// <summary>
        /// This method returns a specialized DataStructure for the provided object. The key is a reference to a GameObject and the value is a MeshRendererPair which contains a reference to the mesh attached to the GameObject (key) and the type of mesh (Skinned or static).
        /// </summary>
        /// <param name="forObject"> The object for which the ObjectMeshPairs is constructed.</param>
        /// <param name="includeInactive"> If this is true then the method also considers the nested inactive children of the GameObject provided, otherwise it only considers the active nested children.</param>
        /// <returns> A specialized data structure that contains information about all the meshes nested under the provided GameObject. </returns>

        public static ObjectMeshPairs GetObjectMeshPairs(GameObject forObject, bool includeInactive)
        {

            if (forObject == null)
            {
                throw new ArgumentNullException("forObject", "You must provide a gameobject to get the ObjectMeshPairs for.");
            }

            ObjectMeshPairs objectMeshPairs = new ObjectMeshPairs();


            MeshFilter[] meshFilters = forObject.GetComponentsInChildren<MeshFilter>(includeInactive);


            if (meshFilters != null && meshFilters.Length != 0)
            {
                foreach (var filter in meshFilters)
                {
                    if (filter.sharedMesh)
                    {
                        //Debug.Log("Adding From Mesh Filter   "+ filter.sharedMesh.name + "  for gameobject  "+ filter.gameObject.name);
                        MeshRendererPair meshRendererPair = new MeshRendererPair(true, filter.sharedMesh);
                        objectMeshPairs.Add(filter.gameObject, meshRendererPair);
                    }
                }
            }


            SkinnedMeshRenderer[] sMeshRenderers = forObject.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive);

            if (sMeshRenderers != null && sMeshRenderers.Length != 0)
            {
                foreach (var renderer in sMeshRenderers)
                {
                    if (renderer.sharedMesh)
                    {
                        MeshRendererPair meshRendererPair = new MeshRendererPair(false, renderer.sharedMesh);
                        objectMeshPairs.Add(renderer.gameObject, meshRendererPair);
                    }
                }

            }


            return objectMeshPairs;

        }



        /// <summary>
        /// Imports a wavefront obj file provided by the absolute path. Please note that this method doesn't work on WebGL builds and will safely return.
        /// </summary>
        /// <param name="objAbsolutePath"> The absolute path to the obj file.</param>
        /// <param name="texturesFolderPath"> The absolute path to the folder containing the texture files associated with the model to load. If you don't want to load the associated textures or there are none then you can pass an empty or null to this argument.</param>
        /// <param name="materialsFolderPath"> The absolute path to the folder containing the material files assoicated with the model to load.  If you don't want to load the associated material or there is none then you can pass an empty or null to this argument.</param>
        /// <param name="OnSuccess"> The callback method that will be invoked when the import was successful. The method is passed in the imported GameObject as the argument.</param>
        /// <param name="OnError"> The callback method that will be invoked when the import was not successful. The method is passed in an exception that made the task unsuccessful.</param>
        /// <param name="importOptions"> Specify additional import options for custom importing.</param>

        public static async void ImportOBJFromFileSystem(string objAbsolutePath, string texturesFolderPath, string materialsFolderPath, Action<GameObject> OnSuccess, Action<Exception> OnError, OBJImportOptions importOptions = null)
        {

            UtilityServicesRuntime.OBJExporterImporter importerExporter = new UtilityServicesRuntime.OBJExporterImporter();
            bool isWorking = true;

            try
            {
                await importerExporter.ImportFromLocalFileSystem(objAbsolutePath, texturesFolderPath, materialsFolderPath, (GameObject importedObject) =>
                {
                    isWorking = false;
                    OnSuccess(importedObject);
                }, importOptions);
            }

            catch(Exception ex)
            {
                isWorking = false;
                OnError(ex);
            }

            
            while(isWorking)
            {
                await Task.Delay(1);
            }
        }



        /// <summary>
        /// Downloads a wavefront obj file from the direct URl passed and imports it. You can also specify the URL for different textures associated with the model and also the URL to the linked material file. This function also works on WebGL builds.
        /// </summary>
        /// <param name="objURL"> The direct URL to the obj file.</param>
        /// <param name="objName"> The name for the GameObject that will represent the imported obj.</param>
        /// <param name="diffuseTexURL"> The absolute URL to the associated Diffuse texture (Main texture). If the model has no diffuse texture on the material then you can pass in null or empty string to this parameter.</param>
        /// <param name="bumpTexURL"> The absolute URL to the associated Bump texture (Bump map). If the model has no bump map then you can pass in null or empty string to this parameter.</param>
        /// <param name="specularTexURL">The absolute URL to the associated Specular texture (Reflection map). If the model has no reflection map then you can pass in null or empty string to this parameter.</param>
        /// <param name="opacityTexURL"> The absolute URL to the associated Opacity texture (Transparency map). If the model has no transparency map then you can pass in null or empty string to this parameter.</param>
        /// <param name="materialURL"> If the model has an associated material file (.mtl) then pass in the absolute URL to that otherwise pass a null or empty string.</param>
        /// <param name="downloadProgress"> The object of type ReferencedNumeric of type float that is updated with the download progress percentage.</param>
        /// <param name="OnSuccess"> The callback method that will be invoked when the import was successful. The method is passed in the imported GameObject as the argument..</param>
        /// <param name="OnError"> The callback method that will be invoked when the import was not successful. The method is passed in an exception that made the task unsuccessful.</param>
        /// <param name="importOptions"> Specify additional import options for custom importing.</param>

        public static async void ImportOBJFromNetwork(string objURL, string objName, string diffuseTexURL, string bumpTexURL, string specularTexURL, string opacityTexURL, string materialURL, ReferencedNumeric<float> downloadProgress, Action<GameObject> OnSuccess, Action<Exception> OnError, OBJImportOptions importOptions = null)
        {

            UtilityServicesRuntime.OBJExporterImporter importerExporter = new UtilityServicesRuntime.OBJExporterImporter();
            bool isWorking = true;

#if !UNITY_WEBGL

            importerExporter.ImportFromNetwork(objURL, objName, diffuseTexURL, bumpTexURL, specularTexURL, opacityTexURL, materialURL, downloadProgress, (GameObject importedObject) =>
            {
                isWorking = false;
                OnSuccess(importedObject);
            }, 
            (Exception ex) => 
            {
                isWorking = false;
                OnError(ex);

            } , importOptions);

            
            while(isWorking)
            {      
                await Task.Delay(1);
            }

#else

            importerExporter.ImportFromNetworkWebGL(objURL, objName, diffuseTexURL, bumpTexURL, specularTexURL, opacityTexURL, materialURL, downloadProgress, (GameObject importedObject) =>
            {
                isWorking = false;
                OnSuccess(importedObject);
            },
            (Exception ex) =>
            {
                isWorking = false;
                OnError(ex);

            }, importOptions);


            //while (isWorking)
            //{
                // Some how wait without using threads.
            //}
#endif
        }



        /// <summary>
        /// Exports the provided GameObject to wavefront OBJ format with support for saving textures and materials. Please note that the method won't work on WebGL builds and will safely return.
        /// </summary>
        /// <param name="toExport"> The GameObject that will be exported.</param>
        /// <param name="exportPath"> The path to the folder where the file will be written.</param>
        /// <param name="exportOptions"> Some additional export options for customizing the export. </param>
        /// <param name="OnSuccess">The callback to be invoked on successful export. </param>
        /// <param name="OnError"> The callback method that will be invoked when the import was not successful. The method is passed in an exception that made the task unsuccessful.</param>

        public static async void ExportGameObjectToOBJ(GameObject toExport, string exportPath, Action OnSuccess, Action<Exception> OnError, OBJExportOptions exportOptions = null)
        {
            UtilityServicesRuntime.OBJExporterImporter importerExporter = new UtilityServicesRuntime.OBJExporterImporter();
            bool isWorking = true;

            try
            {
                importerExporter.ExportGameObjectToOBJ(toExport, exportPath, exportOptions, ()=> 
                {
                    isWorking = false;
                    OnSuccess();
                });
            }

            catch (Exception ex)
            {
                isWorking = false;
                OnError(ex);
            }


            while(isWorking)
            {
                await Task.Delay(1);
            }
        }



        /// <summary>
        /// Counts the number of triangles in the provided GameObject. If "countDeep" is true then the method counts all the triangles considering all the nested meshes in the children hierarchies of the given GameObject.
        /// </summary>
        /// <param name="countDeep"> If true the method also counts and considers the triangles of the nested children hierarchies for the given GameObject. </param>
        /// <param name="forObject"> The GameObject for which to count the triangles.</param>
        /// <returns> The total traingles summing the triangles count of all the meshes nested under the provided GameObject.</returns>

        public static int CountTriangles(bool countDeep, GameObject forObject)
        {
            int triangleCount = 0;

            if (forObject == null) { return 0; }


            if (countDeep)
            {
                MeshFilter[] meshFilters = forObject.GetComponentsInChildren<MeshFilter>(true);


                if (meshFilters != null && meshFilters.Length != 0)
                {
                    foreach (var filter in meshFilters)
                    {
                        if (filter.sharedMesh)
                        {
                            triangleCount += (filter.sharedMesh.triangles.Length) / 3;
                        }
                    }
                }


                SkinnedMeshRenderer[] sMeshRenderers = forObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);

                if (sMeshRenderers != null && sMeshRenderers.Length != 0)
                {
                    foreach (var renderer in sMeshRenderers)
                    {
                        if (renderer.sharedMesh)
                        {
                            triangleCount += (renderer.sharedMesh.triangles.Length) / 3;
                        }
                    }
                }
            }

            else
            {
                MeshFilter mFilter = forObject.GetComponent<MeshFilter>();
                SkinnedMeshRenderer sRenderer = forObject.GetComponent<SkinnedMeshRenderer>();

                if (mFilter && mFilter.sharedMesh)
                {
                    triangleCount = (mFilter.sharedMesh.triangles.Length) / 3;
                }

                else if (sRenderer && sRenderer.sharedMesh)
                {
                    triangleCount = (sRenderer.sharedMesh.triangles.Length) / 3;
                }
            }


            return triangleCount;
        }



        /// <summary>
        /// Counts the number of triangles in the provided meshes list.
        /// </summary>
        /// <param name="toCount"> The list of meshes whose triangles will be counted. </param>
        /// <returns> The total triangles summing the triangles count of all the meshes in the provided list. WIll return 0 if there are no meshes in the list</returns>

        public static int CountTriangles(List<Mesh> toCount)
        {
            int triangleCount = 0;

            if (toCount == null || toCount.Count == 0) { return 0; }

            foreach (var mesh in toCount)
            {
                if (mesh != null)
                {
                    triangleCount += (mesh.triangles.Length) / 3;
                }
            }

            return triangleCount;
        }


#endregion PUBLIC_METHODS




#region PRIVATE_METHODS


        private static void SetParametersForSimplifier(SimplificationOptions simplificationOptions, UnityMeshSimplifier.MeshSimplifier meshSimplifier)
        {
            meshSimplifier.EnableSmartLink = simplificationOptions.enableSmartlinking;
            meshSimplifier.PreserveUVSeamEdges = simplificationOptions.preserveUVSeamEdges;
            meshSimplifier.PreserveUVFoldoverEdges = simplificationOptions.preserveUVFoldoverEdges;
            meshSimplifier.PreserveBorderEdges = simplificationOptions.preserveBorderEdges;
            meshSimplifier.MaxIterationCount = simplificationOptions.maxIterations;
            meshSimplifier.Aggressiveness = simplificationOptions.aggressiveness;
        }


        private static bool AreAnyFeasibleMeshes(ObjectMeshPairs objectMeshPairs)
        {

            if (objectMeshPairs == null || objectMeshPairs.Count == 0) { return false; }


            foreach (KeyValuePair<GameObject, MeshRendererPair> item in objectMeshPairs)
            {

                MeshRendererPair meshRendererPair = item.Value;
                GameObject gameObject = item.Key;

                if (gameObject == null || meshRendererPair == null) { continue; }

                if (meshRendererPair.attachedToMeshFilter)
                {
                    MeshFilter filter = gameObject.GetComponent<MeshFilter>();

                    if (filter == null || meshRendererPair.mesh == null) { continue; }

                    return true;
                }

                else if (!meshRendererPair.attachedToMeshFilter)
                {
                    SkinnedMeshRenderer sRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();

                    if (sRenderer == null || meshRendererPair.mesh == null) { continue; }

                    return true;
                }
            }

            return false;
        }


        private static void AssignReducedMesh(GameObject gameObject, Mesh originalMesh, Mesh reducedMesh, bool attachedToMeshfilter, bool assignBindposes)
        {
            if (assignBindposes)
            {
                reducedMesh.bindposes = originalMesh.bindposes;
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


        private static int CountTriangles(ObjectMeshPairs objectMeshPairs)
        {
            int triangleCount = 0;

            if (objectMeshPairs == null) { return 0; }

            foreach (var item in objectMeshPairs)
            {
                if (item.Key == null || item.Value == null || item.Value.mesh == null)
                { continue; }

                triangleCount += (item.Value.mesh.triangles.Length) / 3;
            }

            return triangleCount;
        }


#endregion PRIVATE_METHODS


    }

}




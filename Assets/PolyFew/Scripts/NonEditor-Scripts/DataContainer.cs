#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using UnityEngine;


namespace PolyFew
{

    public class DataContainer : MonoBehaviour
    {


        [System.Serializable]
        public class ObjectsHistory : SerializableDictionary<GameObject, UndoRedoOps> { }

        [System.Serializable]
        public class ObjectMeshPairs : SerializableDictionary<GameObject, MeshRendererPair> { }



        [System.Serializable]
        public class MeshRendererPair
        {
            public bool attachedToMeshFilter;
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



        [System.Serializable]
        public class CustomMeshActionStructure
        {
            public MeshRendererPair meshRendererPair;
            public GameObject gameObject;
            public Action action;

            public CustomMeshActionStructure(MeshRendererPair meshRendererPair, GameObject gameObject, Action action)
            {
                this.meshRendererPair = meshRendererPair;
                this.gameObject = gameObject;
                this.action = action;
            }
        }





        [System.Serializable]
        public class ObjectHistory
        {
            public bool isReduceDeep;
            public ObjectMeshPairs objectMeshPairs;


            public ObjectHistory(bool isReduceDeep, ObjectMeshPairs objectMeshPairs)
            {
                this.isReduceDeep = isReduceDeep;
                this.objectMeshPairs = objectMeshPairs;
            }

            public void Destruct()
            {

                if (objectMeshPairs == null || objectMeshPairs.Count == 0)
                {
                    return;
                }

                foreach (var item in objectMeshPairs)
                {
                    item.Value.Destruct();
                }

                objectMeshPairs = null;
            }
        }



        [System.Serializable]
        public class UndoRedoOps
        {
            public GameObject gameObject;
            public List<ObjectHistory> undoOperations;
            public List<ObjectHistory> redoOperations;

            public UndoRedoOps(GameObject gameObject, List<ObjectHistory> undoOperations, List<ObjectHistory> redoOperations)
            {
                this.gameObject = gameObject;
                this.undoOperations = undoOperations;
                this.redoOperations = redoOperations;
            }


            public void Destruct()
            {
                if (undoOperations != null && undoOperations.Count > 0)
                {
                    foreach (var operation in undoOperations)
                    {
                        operation.Destruct();
                    }
                }

                if (redoOperations != null && redoOperations.Count > 0)
                {
                    foreach (var operation in redoOperations)
                    {
                        operation.Destruct();
                    }
                }

            }
        }




        [System.Serializable]
        public class LODLevelSettings
        {

            public float reductionStrength;
            public float transitionHeight;
            public bool preserveUVFoldover;
            public bool preserveUVSeams;
            public bool preserveBorders;
            public bool smartLinking;
            public float aggressiveness;
            public int maxIterations;
            public bool regardTolerance;
            public bool simplificationOptionsFoldout;



            public LODLevelSettings(float reductionStrength, float transitionHeight, bool preserveUVFoldover, bool preserveUVSeams, bool preserveBorders, bool smartLinking, float aggressiveness, int maxIterations, bool regardTolerance)
            {
                this.reductionStrength = reductionStrength;
                this.transitionHeight = transitionHeight;
                this.preserveUVFoldover = preserveUVFoldover;
                this.preserveUVSeams = preserveUVSeams;
                this.preserveBorders = preserveBorders;
                this.smartLinking = smartLinking;
                this.aggressiveness = aggressiveness;
                this.maxIterations = maxIterations;
                this.regardTolerance = regardTolerance;
            }
        }






        public ObjectsHistory objectsHistory;

        public ObjectMeshPairs objectMeshPairs;

        public List<GameObject> historyAddedObjects;   // This is the ordered list of GameObjects for which the undo/redo records are added

        public List<LODLevelSettings> currentLodLevelSettings;



        #region Inspector Drawer Vars

        public bool preserveBorders;
        public bool preserveUVSeams;
        public bool preserveUVFoldover;
        public bool smartLinking = true;
        public int maxIterations = 100;
        public float aggressiveness = 7;
        public bool reduceDeep;
        public bool isPreservationActive;
        public float sphereDiameter = 0.5f;
        public Vector3 oldSphereScale;
        public float reductionStrength;
        public bool reductionPending;
        public GameObject prevFeasibleTarget;
        public Transform prevSelection;
        public bool runOnThreads;
        public int triangleCount;
        [SerializeField]
        public UnityEngine.Object lastDrawer;
        public bool foldoutAutoLOD;

        #endregion Inspector Drawer Vars


    }
}


#endif
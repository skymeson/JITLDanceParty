using PolyFewRuntime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static PolyFewRuntime.PolyfewRuntime;
using static PolyFewRuntime.UtilityServicesRuntime;

public class PolygonReduction : MonoBehaviour
{

    public Slider reductionStrength;
    public Toggle preserveUVFoldover;
    public Toggle preserveUVSeams;
    public Toggle preserveBorders;
    public Toggle enableSmartLinking;
    public Toggle preserveFace;
    public InputField trianglesCount;
    public Text message;
    public Text progress;
    public Button exportButton;
    public Button importFromFileSystem;
    public Button importFromWeb;
    public Slider progressSlider;
    public GameObject uninteractivePanel;

    public GameObject targetObject;
    public Transform preservationSphere;
    public EventSystem eventSystem;

    private ObjectMeshPairs objectMeshPairs;
    private bool didApplyLosslessLast = false;
    private bool disableTemporary = false;
    private GameObject barabarianRef;
    private ReferencedNumeric<float> downloadProgress = new ReferencedNumeric<float>(0);
#pragma warning disable
    private bool isImportingFromNetwork;
    private bool isWebGL;



    // Start is called before the first frame update
    void Start()
    {
        if(Application.platform == RuntimePlatform.WebGLPlayer)
        {
            isWebGL = true;
        }

        uninteractivePanel.SetActive(false);
        exportButton.interactable = false;
        barabarianRef = targetObject;
        objectMeshPairs = PolyfewRuntime.GetObjectMeshPairs(targetObject, true);
        trianglesCount.text = PolyfewRuntime.CountTriangles(true, targetObject) + "";
    }



    // Update is called once per frame
    void Update()
    {
        if(!eventSystem) { return; }

        if(eventSystem.currentSelectedGameObject && eventSystem.currentSelectedGameObject.GetComponent<RectTransform>())
        {
            FlyCamera.deactivated = true;
        }  

        else
        {
            FlyCamera.deactivated = false;
        }

        if(isWebGL)
        {
            exportButton.gameObject.SetActive(false);
            importFromFileSystem.gameObject.SetActive(false);
        }
    }





    public void OnReductionChange(float value)
    {

        if(disableTemporary) { return; }

        didApplyLosslessLast = false;

        //System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();
        //w.Start();

        if (targetObject == null) { return; }
        if (Mathf.Approximately(0, value)) { return; }

        SimplificationOptions options = new SimplificationOptions();

        options.simplificationStrength = value;
        options.enableSmartlinking = enableSmartLinking.isOn;
        options.preserveBorderEdges = preserveBorders.isOn;
        options.preserveUVSeamEdges = preserveUVSeams.isOn;
        options.preserveUVFoldoverEdges = preserveUVFoldover.isOn;

        if (preserveFace.isOn)
        {
            options.regardPreservationSphere = true;
            options.preservationSphereCenterWorldSpace = preservationSphere.position;
            options.preservationSphereWorldScale = preservationSphere.lossyScale;
        }

        else { options.regardPreservationSphere = false; }

        
        trianglesCount.text = PolyfewRuntime.SimplifyObjectDeep(objectMeshPairs, options, (GameObject go, MeshRendererPair mInfo) =>
        {
            //Debug.Log("Simplified mesh  " + mInfo.mesh.name + " on GameObject  " + go.name);
        }) + "";
        

        //w.Stop();
        //Debug.Log("Elapsed   " + w.ElapsedMilliseconds);

    }




    public void SimplifyLossless()
    {
        //System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();
        //w.Start();

        disableTemporary = true;
        reductionStrength.value = 0;
        disableTemporary = false;

        didApplyLosslessLast = true;

        SimplificationOptions options = new SimplificationOptions
        {
            enableSmartlinking = enableSmartLinking.isOn,
            preserveBorderEdges = preserveBorders.isOn,
            preserveUVSeamEdges = preserveUVSeams.isOn,
            preserveUVFoldoverEdges = preserveUVFoldover.isOn,

            simplifyMeshLossless = true
        };

        if (preserveFace.isOn)
        {
            options.regardPreservationSphere = true;
            options.preservationSphereCenterWorldSpace = preservationSphere.position;
            options.preservationSphereWorldScale = preservationSphere.lossyScale;
        }

        else { options.regardPreservationSphere = false; }


        
        trianglesCount.text = PolyfewRuntime.SimplifyObjectDeep(objectMeshPairs, options, (GameObject go, MeshRendererPair mInfo) =>
        {
            Debug.Log("Simplified mesh  " + mInfo.mesh.name + " on GameObject  " + go.name);
        }) + "";
        

        //w.Stop();
        //Debug.Log("Elapsed   " + w.ElapsedMilliseconds);
    }



    public void ImportOBJ()
    {
        // This function loads an abj file named Meat.obj from the project's asset directory
        // This also loads the associated textures and materials

        GameObject importedObject;
        
        OBJImportOptions importOptions = new OBJImportOptions();
        importOptions.zUp = false;
        importOptions.localPosition = new Vector3(-2.199f, -1, -1.7349f);
        importOptions.localScale = new Vector3(0.045f, 0.045f, 0.045f);

        string objPath = Application.dataPath + "/PolyFew/demo/TestModels/Meat.obj";
        string texturesFolderPath = Application.dataPath + "/PolyFew/demo/TestModels/textures";
        string materialsFolderPath = Application.dataPath + "/PolyFew/demo/TestModels/materials";


        PolyfewRuntime.ImportOBJFromFileSystem(objPath, texturesFolderPath, materialsFolderPath, (GameObject imp) =>
        {

            importedObject = imp;
            Debug.Log("Successfully imported GameObject:   " + importedObject.name);
            barabarianRef.SetActive(false);
            targetObject = importedObject;
            ResetSettings();
            objectMeshPairs = PolyfewRuntime.GetObjectMeshPairs(targetObject, true);
            trianglesCount.text = PolyfewRuntime.CountTriangles(true, targetObject) + "";
            exportButton.interactable = true;
            importFromWeb.interactable = false;
            importFromFileSystem.interactable = false;
            preserveFace.interactable = false;
            disableTemporary = true;
            preservationSphere.gameObject.SetActive(false);
            disableTemporary = false;

        }, 
        (Exception ex)=> 
        {
            Debug.LogError("Failed to load OBJ file.   " + ex.ToString());

        }, importOptions);
        


    }




    public void ImportOBJFromNetwork()
    {
        // This function downloads and imports an obj file named onion.obj from the url below
        // This also loads the associated textures and materials given by the absolute URLs

        GameObject importedObject;
        isImportingFromNetwork = true;


        OBJImportOptions importOptions = new OBJImportOptions();
        importOptions.zUp = false;
        importOptions.localPosition = new Vector3(0.87815f, 1.4417f, -4.4708f);
        importOptions.localScale = new Vector3(0.0042f, 0.0042f, 0.0042f);

        string objURL = "https://brainfail.000webhostapp.com/models/onion.obj";
        string objName = "onion";
        string diffuseTexURL = "https://brainfail.000webhostapp.com/models/onion.jpg";
        string bumpTexURL = "";
        string specularTexURL = "";
        string opacityTexURL = "";
        string materialURL = "https://brainfail.000webhostapp.com/models/onion.mtl";


        progressSlider.value = 0;
        uninteractivePanel.SetActive(true);
        downloadProgress = new ReferencedNumeric<float>(0);

        StartCoroutine(UpdateProgress());

        PolyfewRuntime.ImportOBJFromNetwork(objURL, objName, diffuseTexURL, bumpTexURL, specularTexURL, opacityTexURL, materialURL, downloadProgress, (GameObject imp) =>
        {
            isImportingFromNetwork = false;
            importedObject = imp;
            barabarianRef.SetActive(false);
            targetObject = importedObject;
            ResetSettings();
            objectMeshPairs = PolyfewRuntime.GetObjectMeshPairs(targetObject, true);
            trianglesCount.text = PolyfewRuntime.CountTriangles(true, targetObject) + "";
            exportButton.interactable = true;
            uninteractivePanel.SetActive(false);
            importFromWeb.interactable = false;
            importFromFileSystem.interactable = false;
            preserveFace.interactable = false;
            disableTemporary = true;
            preservationSphere.gameObject.SetActive(false);
            disableTemporary = false;
        },
        (Exception ex)=> 
        {
            uninteractivePanel.SetActive(false);
            isImportingFromNetwork = false;
            Debug.LogError("Failed to download and import OBJ file.   " + ex.Message);

        } , importOptions);

       

    }





    public void ExportGameObjectToOBJ()
    {

        //The following exports the GameObject Onion to the persistent data path

        string exportPath = Application.persistentDataPath;

        GameObject exportObject = GameObject.Find("onion");

        if(exportObject)
        {
            exportObject = exportObject.transform.GetChild(0).GetChild(0).gameObject;
        }

        else
        {
            exportObject = GameObject.Find("Meat");
            if(!exportObject)
            {
                return;
            }

            else
            {
                exportObject = exportObject.transform.GetChild(0).GetChild(0).gameObject;
            }
        }



        OBJExportOptions exportOptions = new OBJExportOptions(true, true, true, true, true);

        PolyfewRuntime.ExportGameObjectToOBJ(exportObject, exportPath, () =>
        {

            Debug.Log("Successfully exported GameObject:  " + exportObject.name);
            string message = "Successfully exported the file to:  \n" + Application.persistentDataPath;
            StartCoroutine(ShowMessage(message));
            
        }, 
        (Exception ex) => 
        {
            Debug.LogError("Failed to export OBJ. " + ex.ToString());

        }, exportOptions );

    }




    public void OnToggleStateChanged(bool isOn)
    {
        if (disableTemporary) { return; }

        preservationSphere.gameObject.SetActive(preserveFace.isOn);

        if(didApplyLosslessLast)
        {
            SimplifyLossless();
        }
        else
        {
            OnReductionChange(reductionStrength.value);
        }
    }



    #region HelperFunctions



    public void Reset()
    {
        ResetSettings();

        AssignMeshesFromPairs();

        GameObject obj = GameObject.Find("onion");

        if (obj) { targetObject.SetActive(false); }

        else
        {
            obj = GameObject.Find("Meat");
            if (obj) { targetObject.SetActive(false); }
        }

        targetObject = barabarianRef;
        preserveFace.interactable = true;

        targetObject.SetActive(true);
        objectMeshPairs = PolyfewRuntime.GetObjectMeshPairs(targetObject, true);
        trianglesCount.text = PolyfewRuntime.CountTriangles(true, targetObject) + "";

        exportButton.interactable = false;
        importFromWeb.interactable = true;
        importFromFileSystem.interactable = true;

    }




    public static void OnSliderSelect()
    {
        FlyCamera.deactivated = true;
    }



    public static void OnSliderDeselect()
    {
        FlyCamera.deactivated = false;
    }



    private bool IsMouseOverUI(RectTransform uiElement)
    {
        Vector2 localMousePosition = uiElement.InverseTransformPoint(Input.mousePosition);

        if (uiElement.rect.Contains(localMousePosition))
        {
            return true;
        }

        return false;
    }



    private IEnumerator ShowMessage(string message)
    {
        Debug.Log(message);

        this.message.text = message;

        yield return new WaitForSeconds(4.5f);

        this.message.text = "";
    }




    private void ResetSettings()
    {
        disableTemporary = true;
        reductionStrength.value = 0;
        preserveUVSeams.isOn = false;
        preserveUVFoldover.isOn = false;
        preserveBorders.isOn = false;
        enableSmartLinking.isOn = true;
        preserveFace.isOn = false;
        preservationSphere.gameObject.SetActive(false);
        disableTemporary = false;

    }



    private IEnumerator UpdateProgress()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            progressSlider.value = downloadProgress.Value;
            progress.text = (int)downloadProgress.Value + "%";
        }
    }



    private void AssignMeshesFromPairs()
    {
        
        if (objectMeshPairs != null)
        {
            foreach (GameObject gameObject in objectMeshPairs.Keys)
            {
                if (gameObject != null)
                {

                    PolyfewRuntime.MeshRendererPair meshRendererPair = objectMeshPairs[gameObject];

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

    }

    #endregion Helper Functions

}

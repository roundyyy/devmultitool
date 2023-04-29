

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using System.IO;
using System;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;




public class DevMultitool : EditorWindow
{
    GameObject prefab;
    GameObject[] prefabs;
    bool randomReplacement;

    // tag changer
    string[] tags;
    int selectedTagIndex = 0;
    int selectedLayerIndex = 0;
    bool changeOnlyObjectsWithColliders = true;
    // lod tweaker
    private List<LODGroup> lodGroups = new List<LODGroup>();
    private float[] lodDistances = new float[5];

    private bool vrDistance = true;
    private bool protectLastLOD = true;
    private float cullingDistance = 500;

    private bool smallerThan = false;
    private bool biggerThan = false;
    private float smallerThanSize;
    private float biggerThanSize;

    private bool useCustomFOV = true;
    private float customFOV = 100.0f;

    private float tweakLodBias = 3.5f;
    // material baker
    public enum TextureSizes { _64 = 64, _128 = 128, _256 = 256, _512 = 512, _1024 = 1024, _2048 = 2048 }
    private Material sourceMaterial;
    private TextureSizes textureSize = TextureSizes._512;
    private Vector2 bakeTiling = new Vector2(1, 1);

    private bool enableGpuInstancing = false;

    // Add an enumeration to represent the shader types
    public enum ShaderType { Standard, Diffuse, VertexLit, DiffuseFast }

    // Add a new field to store the user's shader choice
    private ShaderType selectedShader;

    private Vector2 sourceMaterialTiling = Vector2.one;

    private float brightness = 1f;

    private bool disableAnisotropicFiltering = true;

    private string bakedTexturePath;
    private string bakedMaterialPath;
    //
    bool saveSceneBeforeOperation = false;
    public Material[] MaterialsForBaking;
    // public bool removeoldcolliders;
    public bool disableoldcolliders;
    public bool removemeshfilters;
    public bool removewholeobject;
    public bool destroysource;
    public bool destroysource2;
    public string newname = "NewObjectName";
    public string newname2 = "NewObjectName";
    public string newname3 = "NewObjectName";
    public string newname4 = "NewObjectName";
    public LayerMask layer;
    public float LightmapScale;
    float MinimumSize = 0f;
    float MaximumSize = 1f;
    public bool CastShadows = true;
    public Shader newShade;
    public double _lastTime = 0;
    public Texture2D image;
    public string shadername;
    public bool loop = true;
    public bool texton;
    public Mesh meshToSave;
    public LODFadeMode FadeMode;
    public string ReplacingShader = "Shader to replace";
    Vector2 scrollPos;
    // public bool removeoldlights;
    public bool disableoldlights;
    //  public bool removeoldaudio;
    public bool disableoldaudio;
    public bool onlymeshbaker;
    public bool animatedcrossfade;
    public GameObject parentformaterial;


    int toolbarInt = 0;
    string[] toolbarStrings = { "EXTRACT/MOVE/SORT", "ENABLE/DISABLE", "VARIOUS", "MATERIAL BAKER", "LOD TWEAKER", "REPLACE WITH PREFAB", "LAYER/TAG CHANGER", "EMPTY" };
    private const string FadeModeFieldName = "fadeMode";
    // private SerializedProperty fadeModeProperty = null;

    List<GameObject> createdGameObjects = new List<GameObject>();
    List<KeyValuePair<Renderer, bool>> rendererStates = new List<KeyValuePair<Renderer, bool>>();

    List<GameObject> movedGameObjects = new List<GameObject>();
    List<Transform> originalParents = new List<Transform>();

    List<GameObject> clonedColliders = new List<GameObject>();
    List<Collider> modifiedColliders = new List<Collider>();
    List<bool> originalColliderStates = new List<bool>();

    List<GameObject> clonedLights = new List<GameObject>();
    List<Light> modifiedLights = new List<Light>();
    List<bool> originalLightStates = new List<bool>();

    List<GameObject> clonedAudioSources = new List<GameObject>();
    List<AudioSource> modifiedAudioSources = new List<AudioSource>();
    List<bool> originalAudioSourceStates = new List<bool>();

    List<Renderer> modifiedRenderersEnable = new List<Renderer>();
    List<bool> originalRendererStatesEnable = new List<bool>();

    List<Renderer> modifiedRenderersDisable = new List<Renderer>();
    List<bool> originalRendererStatesDisable = new List<bool>();
    List<Collider> modifiedCollidersEnable = new List<Collider>();
    List<bool> originalColliderStatesEnable = new List<bool>();

    List<Collider> modifiedCollidersDisable = new List<Collider>();
    List<bool> originalColliderStatesDisable = new List<bool>();

    List<Light> modifiedLightsEnable = new List<Light>();
    List<bool> originalLightStatesEnable = new List<bool>();

    List<GameObject> objectsWithAddedColliders = new List<GameObject>();
    List<Collider> addedColliders = new List<Collider>();

    List<LODGroup> modifiedLODGroups = new List<LODGroup>();
    List<LODFadeMode> originalLODFadeModes = new List<LODFadeMode>();
    List<bool> originalAnimateCrossFadingStates = new List<bool>();

    Dictionary<GameObject, Mesh> originalMeshes = new Dictionary<GameObject, Mesh>();

    private Dictionary<Light, LightmapBakeType> originalLightBakeTypes = new Dictionary<Light, LightmapBakeType>();
    private Dictionary<Renderer, float> originalLightmapScales = new Dictionary<Renderer, float>();
    private Dictionary<Renderer, UnityEngine.Rendering.ShadowCastingMode> originalShadowCastingModes = new Dictionary<Renderer, UnityEngine.Rendering.ShadowCastingMode>();

    private Dictionary<GameObject, (GameObject, Transform, int)> previousStates;

    //Dictionary<Transform, Transform> originalParents2 = new Dictionary<Transform, Transform>();


    private bool unpackPrefabs = false;
    private bool ignoreLODGroupRenderers = true;

    private bool separateSubmeshes = false;

    string selectedFolderPath = "DevMultiTool/SavedMeshes";

    public Terrain _terrain;

    private bool disableTerrainTrees = false;

    bool removeFromLODGroup = false;

    int selectedLODLevel = 0;




    [MenuItem("Tools/DevMultitool")]

    public static void ShowWindow()
    {
        GetWindow<DevMultitool>(false, "Dev Multitool", true);

    }
    private void OnEnable()
    {
        tags = UnityEditorInternal.InternalEditorUtility.tags;
    }


    public static readonly string Symbols = "WARDUST_MODDING";
    public static readonly string[] Symbols2 = new string[] { "WARDUST_MODDING" };

    private void UpdateTilingFromSource()
    {
        Vector2 sourceTiling = sourceMaterial.GetTextureScale("_MainTex");
        bakeTiling.x = 1f / sourceTiling.x;
        bakeTiling.y = 1f / sourceTiling.y;
    }
    private void UpdateTilingFromNewEntry()
    {
        bakeTiling.x = 1f / sourceMaterialTiling.x;
        bakeTiling.y = 1f / sourceMaterialTiling.y;
    }

    private List<GameObject> FindObjectsWithMaterial(Material material)
    {
        List<GameObject> objectsWithMaterial = new List<GameObject>();
        Renderer[] renderers = GameObject.FindObjectsOfType<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            if (renderer.sharedMaterial == material)
            {
                objectsWithMaterial.Add(renderer.gameObject);
            }
        }

        return objectsWithMaterial;
    }

    [Obsolete]
    private void OnGUI()
    {
        //fadeModeProperty = serializedObject.FindProperty(FadeModeFieldName);

        image = Resources.Load("Logo", typeof(Texture2D)) as Texture2D;
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.padding = new RectOffset(10, 10, 10, 10);
        float aspectRatio = (float)image.width / image.height;
        int maxWidth = Mathf.RoundToInt(Screen.width * 0.8f); // limit the maximum width to 80% of the screen width
        int height = Mathf.RoundToInt(maxWidth / aspectRatio);
        GUILayout.Box(image, GUILayout.Width(maxWidth), GUILayout.Height(height));
        EditorGUILayout.Space();

        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
        labelStyle.fontSize = 20;
        //  labelStyle.alignment = TextAnchor.MiddleCenter; // set the label text to center horizontally within the box

        EditorGUILayout.Space();

        GUILayout.BeginVertical();

        GUILayout.Label("MULTIOBJECT SELECTION SUPPORTED", EditorStyles.boldLabel);
        GUILayout.Label("ver. 0.1", EditorStyles.boldLabel);
        saveSceneBeforeOperation = GUILayout.Toggle(saveSceneBeforeOperation, "Save scene before each operation");
        GUILayout.EndVertical();
        EditorGUILayout.Space();

        int maxButtonsPerRow = 4;
        int numRows = Mathf.CeilToInt((float)toolbarStrings.Length / maxButtonsPerRow);

        GUIStyle boldButtonStyle = new GUIStyle(EditorStyles.toolbarButton);
        boldButtonStyle.fontStyle = FontStyle.Bold;
        boldButtonStyle.fontSize = 12;
        boldButtonStyle.stretchWidth = true;
        boldButtonStyle.fixedWidth = 150;
        boldButtonStyle.fixedHeight = 30;

        // Modify the normal background property to darken the button background color
        Texture2D darkTexture = new Texture2D(1, 1);
        darkTexture.SetPixel(0, 0, new Color(0.5f, 0.5f, 0.5f));
        darkTexture.Apply();
        boldButtonStyle.normal.background = darkTexture;

        // Modify the active and onActive properties to change the appearance of the active and pressed button
        Color brightColor = new Color(0.7f, 0.7f, 0.7f);
        Texture2D brightTexture = new Texture2D(1, 1);
        brightTexture.SetPixel(0, 0, brightColor);
        brightTexture.Apply();
        boldButtonStyle.active.background = brightTexture;
        boldButtonStyle.onActive.background = brightTexture;

        // Modify the onNormal and onHover properties to change the appearance of the selected button
        Color darkColor = new Color(0.7f, 0.7f, 0.7f);
        Texture2D darkTexture2 = new Texture2D(1, 1);
        darkTexture2.SetPixel(0, 0, darkColor);
        darkTexture2.Apply();
        boldButtonStyle.onNormal.background = darkTexture2;
        boldButtonStyle.onHover.background = darkTexture2;

        GUILayout.BeginVertical(EditorStyles.toolbar, GUILayout.ExpandWidth(true));

        // Custom layout function to add a gap and line between rows in the selection grid
        int index = 0;
        for (int row = 0; row < numRows; row++)
        {
            GUILayout.BeginHorizontal();
            for (int col = 0; col < maxButtonsPerRow; col++)
            {
                if (index >= toolbarStrings.Length)
                {
                    break;
                }

                bool isButtonSelected = (toolbarInt == index);
                if (GUILayout.Toggle(isButtonSelected, toolbarStrings[index], boldButtonStyle, GUILayout.ExpandWidth(true)))
                {
                    toolbarInt = index;
                }
                index++;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
        }


        GUILayout.EndVertical();
        GUILayout.Space(80);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);



        switch (toolbarInt)
        {
            case 0:


                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space();
                GUILayout.Label("It will dig in selected game object(s) and extract only ones with renderers.", EditorStyles.boldLabel);
                GUILayout.Label("Also it will put them in to the new parent object with name provided below", EditorStyles.boldLabel);
                newname = EditorGUILayout.TextField("Parent name: ", newname);
                destroysource = EditorGUILayout.Toggle("Disable source renderer?", destroysource);

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("EXTRACT (CLONE) GAMEOBJECTS", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    createdGameObjects.Clear();
                    rendererStates.Clear();

                    float amount = 0;
                    float amount2 = 0;

                    GameObject parent = new GameObject(newname);
                    createdGameObjects.Add(parent);

                    foreach (GameObject go2 in Selection.gameObjects)
                    {
                        Renderer[] renderersInChildren = go2.GetComponentsInChildren<Renderer>();

                        foreach (Renderer renderer in renderersInChildren)
                        {
                            if (renderer.enabled)
                            {
                                GameObject found = renderer.gameObject;
                                GameObject go = Instantiate(found, found.transform.position, found.transform.rotation) as GameObject;
                                createdGameObjects.Add(go);

                                string goname = found.name;
                                go.name = goname;
                                go.transform.parent = parent.gameObject.transform;
                                amount++;

                                Renderer[] childRenderers = go.GetComponentsInChildren<Renderer>();
                                foreach (Renderer childRenderer in childRenderers)
                                {
                                    if (childRenderer.gameObject != go)
                                    {
                                        DestroyImmediate(childRenderer.gameObject);
                                    }
                                }

                                if (destroysource)
                                {
                                    rendererStates.Add(new KeyValuePair<Renderer, bool>(renderer, renderer.enabled));
                                    renderer.enabled = false;
                                    amount2++;
                                }
                            }
                        }
                    }

                    Debug.LogWarning(amount + " gameobjects cloned");
                    Debug.LogWarning(amount2 + " renderers disabled");
                }

                if (GUILayout.Button("UNDO", GUILayout.Width(80), GUILayout.Height(30)))
                {
                    foreach (GameObject go in createdGameObjects)
                    {
                        DestroyImmediate(go);
                    }

                    foreach (KeyValuePair<Renderer, bool> rendererState in rendererStates)
                    {
                        Renderer renderer = rendererState.Key;
                        bool initialState = rendererState.Value;
                        renderer.enabled = initialState;
                    }

                    createdGameObjects.Clear();
                    rendererStates.Clear();

                    Debug.LogWarning("All changes undone");
                }
                GUILayout.EndHorizontal();


                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space();
                GUILayout.Label("It will move all game objects (included parent) to new parent", EditorStyles.boldLabel);
                GUILayout.Label("The difference between this one and one above is that it moves all objects and not clone it (keep lightmap)", EditorStyles.boldLabel);
                newname = EditorGUILayout.TextField("Parent name: ", newname);

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("EXTRACT (MOVE) GAMEOBJECTS", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    movedGameObjects.Clear();
                    originalParents.Clear();

                    float amount = 0;

                    GameObject parent = new GameObject(newname);
                    foreach (GameObject go2 in Selection.gameObjects)
                    {
                        foreach (Transform child2 in go2.GetComponentsInChildren<Transform>())
                        {
                            if (child2.GetComponentsInChildren<Transform>() != null)
                            {
                                movedGameObjects.Add(child2.gameObject);
                                originalParents.Add(child2.parent);

                                child2.transform.parent = parent.gameObject.transform;
                                amount++;
                            }
                        }
                    }

                    Debug.LogWarning(amount + " gameobjects transfered");
                }

                if (GUILayout.Button("UNDO", GUILayout.Width(80), GUILayout.Height(30)))
                {
                    for (int i = 0; i < movedGameObjects.Count; i++)
                    {
                        GameObject movedGameObject = movedGameObjects[i];
                        Transform originalParent = originalParents[i];

                        movedGameObject.transform.parent = originalParent;
                    }

                    movedGameObjects.Clear();
                    originalParents.Clear();

                    Debug.LogWarning("All move changes undone");
                }

                GUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("Extracts all colliders from selected gameobject(s) and its children", EditorStyles.boldLabel);
                GUILayout.Label("Creates new Parent to store them and it sets their layer to user defined", EditorStyles.boldLabel);
                GUILayout.Label("Optionally  : disable old colliders from sources", EditorStyles.boldLabel);
                //  removeoldcolliders = EditorGUILayout.Toggle("Remove old colliders?", removeoldcolliders);
                disableoldcolliders = EditorGUILayout.Toggle("Disable old colliders?", disableoldcolliders);
                layer = EditorGUILayout.LayerField("Layer for new colldiers:", layer);
                newname2 = EditorGUILayout.TextField("Parent name: ", newname2);
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("EXTRACT (CLONE) COLLIDERS", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    clonedColliders.Clear();
                    modifiedColliders.Clear();
                    originalColliderStates.Clear();

                    float amount = 0;
                    GameObject parent = new GameObject();
                    parent.name = newname2;

                    foreach (GameObject go2 in Selection.gameObjects)
                    {
                        foreach (Transform child in go2.GetComponentsInChildren<Transform>())
                        {
                            foreach (var collider in child.gameObject.GetComponents<Collider>())
                            {
                                amount++;
                                string goname = collider.transform.gameObject.name;
                                GameObject obj = new GameObject(goname + " Collider");
                                clonedColliders.Add(obj);

                                obj.layer = layer;
                                obj.transform.parent = parent.transform;
                                obj.transform.position = collider.transform.position;
                                obj.transform.rotation = collider.transform.rotation;
                                obj.transform.localScale = collider.transform.lossyScale;

                                ComponentUtility.CopyComponent(collider);
                                ComponentUtility.PasteComponentAsNew(obj);

                                if (disableoldcolliders)
                                {
                                    originalColliderStates.Add(collider.enabled);
                                    modifiedColliders.Add(collider);
                                    collider.enabled = false;
                                }

                                // if (removeoldcolliders)
                                // {
                                //     originalColliderStates.Add(true);
                                //     modifiedColliders.Add(collider);
                                //     DestroyImmediate(collider);
                                // }
                            }
                        }
                    }

                    Debug.LogWarning(amount + " colliders cloned");

                    // if (removeoldcolliders)
                    // {
                    //     Debug.LogWarning(amount + " old colliders removed");
                    // }

                    if (disableoldcolliders)
                    {
                        Debug.LogWarning(amount + " old colliders disabled");
                    }
                }

                if (GUILayout.Button("UNDO", GUILayout.Width(80), GUILayout.Height(30)))
                {
                    foreach (GameObject obj in clonedColliders)
                    {
                        DestroyImmediate(obj);
                    }

                    for (int i = 0; i < modifiedColliders.Count; i++)
                    {
                        Collider collider = modifiedColliders[i];
                        bool originalState = originalColliderStates[i];

                        if (collider != null)
                        {
                            collider.enabled = originalState;
                        }
                    }

                    clonedColliders.Clear();
                    modifiedColliders.Clear();
                    originalColliderStates.Clear();

                    Debug.LogWarning("All collider changes undone");

                }

                GUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("Extracts all lights from selected gameobject(s) and its children", EditorStyles.boldLabel);
                GUILayout.Label("Creates new Parent to store them. If baked, needs rebaking", EditorStyles.boldLabel);
                GUILayout.Label("Optionally  : disables old lights from sources", EditorStyles.boldLabel);
                //removeoldlights = EditorGUILayout.Toggle("Remove old lights?", removeoldlights);
                disableoldlights = EditorGUILayout.Toggle("Disable old lights?", disableoldlights);

                newname3 = EditorGUILayout.TextField("Parent name: ", newname3);
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("EXTRACT (CLONE) LIGHTS", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    clonedLights.Clear();
                    modifiedLights.Clear();
                    originalLightStates.Clear();

                    float amount = 0;
                    GameObject parent = new GameObject();
                    parent.name = newname3;

                    foreach (GameObject go2 in Selection.gameObjects)
                    {
                        foreach (Transform child in go2.GetComponentsInChildren<Transform>())
                        {
                            foreach (var light in child.gameObject.GetComponents<Light>())
                            {
                                amount++;
                                string goname = light.transform.gameObject.name;
                                GameObject obj = new GameObject(goname + " Light");
                                clonedLights.Add(obj);

                                obj.transform.parent = parent.transform;
                                obj.transform.position = light.transform.position;
                                obj.transform.rotation = light.transform.rotation;
                                obj.transform.localScale = light.transform.lossyScale;

                                ComponentUtility.CopyComponent(light);
                                ComponentUtility.PasteComponentAsNew(obj);

                                if (disableoldlights)
                                {
                                    originalLightStates.Add(light.enabled);
                                    modifiedLights.Add(light);
                                    light.enabled = false;
                                }
                            }
                        }
                    }

                    Debug.LogWarning(amount + " lights cloned");

                    if (disableoldlights)
                    {
                        Debug.LogWarning(amount + " old lights disabled");
                    }
                }

                if (GUILayout.Button("UNDO", GUILayout.Width(80), GUILayout.Height(30)))
                {
                    foreach (GameObject obj in clonedLights)
                    {
                        DestroyImmediate(obj);
                    }

                    for (int i = 0; i < modifiedLights.Count; i++)
                    {
                        Light light = modifiedLights[i];
                        bool originalState = originalLightStates[i];

                        if (light != null)
                        {
                            light.enabled = originalState;
                        }
                    }

                    clonedLights.Clear();
                    modifiedLights.Clear();
                    originalLightStates.Clear();

                    Debug.LogWarning("All light changes undone");
                }

                GUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();


                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("Extracts all audio sources from selected gameobject(s) and its children", EditorStyles.boldLabel);
                GUILayout.Label("Creates new Parent to store them.", EditorStyles.boldLabel);
                GUILayout.Label("Optionally  : disables old audio sources from source game objects", EditorStyles.boldLabel);
                //  removeoldaudio = EditorGUILayout.Toggle("Remove old audio sources?", removeoldaudio);
                disableoldaudio = EditorGUILayout.Toggle("Disable old audio sources?", disableoldaudio);

                newname4 = EditorGUILayout.TextField("Parent name :", newname4);
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("EXTRACT (CLONE) AUDIO SOURCES", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    clonedAudioSources.Clear();
                    modifiedAudioSources.Clear();
                    originalAudioSourceStates.Clear();

                    float amount = 0;
                    GameObject parent = new GameObject();
                    parent.name = newname4;

                    foreach (GameObject go2 in Selection.gameObjects)
                    {
                        foreach (Transform child in go2.GetComponentsInChildren<Transform>())
                        {
                            foreach (var audioSource in child.gameObject.GetComponents<AudioSource>())
                            {
                                amount++;
                                string goname = audioSource.transform.gameObject.name;
                                GameObject obj = new GameObject(goname + " AudioSource");
                                clonedAudioSources.Add(obj);

                                obj.transform.parent = parent.transform;
                                obj.transform.position = audioSource.transform.position;
                                obj.transform.rotation = audioSource.transform.rotation;
                                obj.transform.localScale = audioSource.transform.lossyScale;

                                ComponentUtility.CopyComponent(audioSource);
                                ComponentUtility.PasteComponentAsNew(obj);

                                if (disableoldaudio)
                                {
                                    originalAudioSourceStates.Add(audioSource.enabled);
                                    modifiedAudioSources.Add(audioSource);
                                    audioSource.enabled = false;
                                }
                            }
                        }
                    }

                    Debug.LogWarning(amount + " audio sources cloned");
                }

                if (GUILayout.Button("UNDO", GUILayout.Width(80), GUILayout.Height(30)))
                {
                    foreach (GameObject obj in clonedAudioSources)
                    {
                        DestroyImmediate(obj);
                    }

                    for (int i = 0; i < modifiedAudioSources.Count; i++)
                    {
                        AudioSource audioSource = modifiedAudioSources[i];
                        bool originalState = originalAudioSourceStates[i];

                        if (audioSource != null)
                        {
                            audioSource.enabled = originalState;
                        }
                    }

                    clonedAudioSources.Clear();
                    modifiedAudioSources.Clear();
                    originalAudioSourceStates.Clear();

                    Debug.LogWarning("All audio source changes undone");
                }

                GUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                // EditorGUILayout.BeginVertical(EditorStyles.helpBox);




                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("It will dig in selected game object(s) and extract only ones with renderers", EditorStyles.boldLabel);
                GUILayout.Label("Also it will sort them by material and put in to parent with material name", EditorStyles.boldLabel);
                GUILayout.Label("Only game objects with renderer enabled will be extracted", EditorStyles.boldLabel);
                GUILayout.Label("If object have more than one material, then it will be transfered to separate parent.", EditorStyles.boldLabel);
                GUILayout.Label("Use MeshKit tool for example to extract submeshes to separate game objects ", EditorStyles.boldLabel);
                Color col = GUI.contentColor;
                GUI.contentColor = Color.red;
                GUILayout.Label("WARNING : NO UNDO. SAVE YOUR SCENE", EditorStyles.boldLabel);
                GUI.contentColor = col;
                //newname = EditorGUILayout.TextField(newname);
                //   destroysource2 = EditorGUILayout.Toggle("Disable source renderer?", destroysource2);
                EditorGUILayout.BeginHorizontal();
                unpackPrefabs = EditorGUILayout.Toggle(unpackPrefabs, GUILayout.Width(15));
                EditorGUILayout.LabelField("Unpack Prefabs");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                ignoreLODGroupRenderers = EditorGUILayout.Toggle(ignoreLODGroupRenderers, GUILayout.Width(15));
                EditorGUILayout.LabelField("Ignore objects with LOD group");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                separateSubmeshes = EditorGUILayout.Toggle(separateSubmeshes, GUILayout.Width(15));
                EditorGUILayout.LabelField("Separate Submeshes");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                removeFromLODGroup = EditorGUILayout.Toggle(removeFromLODGroup, GUILayout.Width(15));
                EditorGUILayout.LabelField("Remove from source LOD group");
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("SORT BY MATERIAL", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    //  int sortedMaterialsIndex = 1;
                    string sortedMaterialsName = "SortedMaterials";
                    GameObject sortedMaterialsParent = GameObject.Find(sortedMaterialsName);
                    if (sortedMaterialsParent == null)
                    {
                        sortedMaterialsParent = new GameObject(sortedMaterialsName);
                    }
                    Dictionary<string, Transform> materialParents = new Dictionary<string, Transform>();


                    // while (sortedMaterialsParent != null)
                    // {
                    //     sortedMaterialsName = "SortedMaterials" + sortedMaterialsIndex;
                    //     sortedMaterialsParent = GameObject.Find(sortedMaterialsName);
                    //     sortedMaterialsIndex++;
                    // }

                    sortedMaterialsParent = new GameObject(sortedMaterialsName);

                    foreach (GameObject go in Selection.gameObjects)
                    {
                        Renderer[] renderers = go.GetComponentsInChildren<Renderer>()
                            .Where(r => r.enabled
                                && r.gameObject.GetComponent<MeshRenderer>() != null
                                && (!PrefabUtility.IsPartOfAnyPrefab(r.gameObject) || unpackPrefabs)
                                && (!ignoreLODGroupRenderers || r.GetComponentInParent<LODGroup>() == null))
                            .ToArray();

                        if (renderers.Length > 0)
                        {
                            System.Array.Sort(renderers, (a, b) => a.sharedMaterial.name.CompareTo(b.sharedMaterial.name));
                            Transform parent = null;

                            foreach (Renderer renderer in renderers)
                            {
                                // Unpack nested prefab if necessary
                                if (unpackPrefabs)
                                {
                                    UnpackNestedPrefab(renderer.gameObject);
                                }

                                if (removeFromLODGroup && !ignoreLODGroupRenderers)
                                {
                                    LODGroup lodGroup = renderer.GetComponentInParent<LODGroup>();
                                    if (lodGroup != null)
                                    {
                                        RemoveRendererFromLODGroup(renderer, lodGroup);
                                    }
                                }

                                MeshFilter originalMeshFilter = renderer.GetComponent<MeshFilter>();
                                MeshRenderer originalMeshRenderer = renderer.GetComponent<MeshRenderer>();

                                if (separateSubmeshes && originalMeshRenderer.sharedMaterials.Length > 1)
                                {
                                    // Iterate through submeshes and separate them if there is more than one submesh
                                    for (int i = 0; i < originalMeshRenderer.sharedMaterials.Length; i++)
                                    {
                                        Material material = originalMeshRenderer.sharedMaterials[i];
                                        string materialName = material.name;

                                        if (!materialParents.ContainsKey(materialName))
                                        {
                                            parent = sortedMaterialsParent.transform.Find(materialName);
                                            if (parent == null)
                                            {
                                                parent = new GameObject(materialName).transform;
                                                parent.SetParent(sortedMaterialsParent.transform, false);
                                            }
                                            materialParents[materialName] = parent;
                                        }

                                        // Create a new game object and copy the needed components and properties
                                        GameObject newGO = new GameObject(renderer.gameObject.name + "_Submesh_" + i);
                                        newGO.transform.position = renderer.transform.position;
                                        newGO.transform.rotation = renderer.transform.rotation;
                                        newGO.transform.localScale = renderer.transform.localScale;

                                        MeshFilter newMeshFilter = newGO.AddComponent<MeshFilter>();
                                        MeshRenderer newMeshRenderer = newGO.AddComponent<MeshRenderer>();

                                        // Split the submesh
                                        Mesh newMesh = (Mesh)UnityEngine.Object.Instantiate(originalMeshFilter.sharedMesh);
                                        newMesh.SetTriangles(originalMeshFilter.sharedMesh.GetTriangles(i), 0);
                                        newMesh.subMeshCount = 1;
                                        newMesh.RecalculateBounds();
                                        newMesh.RecalculateNormals();
                                        newMeshFilter.sharedMesh = newMesh;
                                        newMeshRenderer.sharedMaterials = new Material[] { material };

                                        newGO.transform.SetParent(materialParents[materialName], true);
                                    }
                                }
                                else
                                {
                                    string materialName = renderer.sharedMaterial.name;
                                    if (!materialParents.ContainsKey(materialName))
                                    {
                                        parent = sortedMaterialsParent.transform.Find(materialName);
                                        if (parent == null)
                                        {
                                            parent = new GameObject(materialName).transform;
                                            parent.SetParent(sortedMaterialsParent.transform, false);
                                        }
                                        materialParents[materialName] = parent;
                                    }

                                    if (!PrefabUtility.IsPartOfPrefabInstance(renderer.transform))
                                    {
                                        renderer.transform.SetParent(materialParents[materialName], true);
                                    }
                                }
                            }
                        }
                    }
                }



                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUILayout.Label("It will extract (clone) selected LOD level objects from all lod groups found in selection", EditorStyles.boldLabel);
                GUILayout.Label("If LOD group doesn't have selected LOD level, then it will clone next one up.", EditorStyles.boldLabel);
                EditorGUILayout.Space();



                selectedLODLevel = EditorGUILayout.Popup("LOD Level to Clone", selectedLODLevel, new string[] { "LOD0", "LOD1", "LOD2", "LOD3", "LOD4", "LOD5" });
                GUI.contentColor = Color.red;
                GUILayout.Label("WARNING : NO UNDO. SAVE YOUR SCENE", EditorStyles.boldLabel);
                GUI.contentColor = col;
                // Add this button in your OnGUI() method
                if (GUILayout.Button("CLONE FROM LOD LEVEL", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    CloneFromSelectedLODLevel();
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUILayout.Label("It will look for prefabs inside selected game object and sort them together by type", EditorStyles.boldLabel);
                EditorGUILayout.Space();
                GUI.contentColor = Color.red;
                GUILayout.Label("WARNING : NO UNDO. SAVE YOUR SCENE", EditorStyles.boldLabel);
                GUI.contentColor = col;
                if (GUILayout.Button("SORT PREFABS", GUILayout.Width(300), GUILayout.Height(30)))
                {

                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    string sortedPrefabsName = "SortedPrefabs";
                    GameObject sortedPrefabsParent = new GameObject(sortedPrefabsName);

                    Dictionary<string, Transform> prefabParents = new Dictionary<string, Transform>();

                    foreach (GameObject go in Selection.gameObjects)
                    {
                        foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                        {
                            if (PrefabUtility.IsPartOfNonAssetPrefabInstance(t.gameObject) && !PrefabUtility.IsPartOfPrefabInstance(t.parent?.gameObject))
                            {
                                GameObject sourcePrefab = (GameObject)PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                                if (sourcePrefab != null)
                                {
                                    string prefabName = sourcePrefab.name;
                                    if (!prefabParents.ContainsKey(prefabName))
                                    {
                                        Transform newParent = new GameObject(prefabName).transform;
                                        newParent.SetParent(sortedPrefabsParent.transform, false);
                                        prefabParents.Add(prefabName, newParent);
                                    }

                                    t.SetParent(prefabParents[prefabName], true);
                                }
                            }
                        }
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("Extract (convert) terrain trees to scene game objects", EditorStyles.boldLabel);
                _terrain = (Terrain)EditorGUILayout.ObjectField(_terrain, typeof(Terrain), true);
                disableTerrainTrees = EditorGUILayout.Toggle("Disable terrain trees? (draw distance 0)", disableTerrainTrees);
                GUI.contentColor = Color.red;
                GUILayout.Label("WARNING : NO UNDO. SAVE YOUR SCENE", EditorStyles.boldLabel);
                GUI.contentColor = col;
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("EXTRACT TREES", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    ExportTrees();
                    if (disableTerrainTrees && _terrain != null)
                    {
                        _terrain.treeDistance = 0;
                    }
                }
                if (GUILayout.Button("CLEAR GENERATED TREES", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    Clear();
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                //  EditorGUILayout.EndVertical();

                break;

            case 1:
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUILayout.Label("It will dig in selected game object(s) and enable not active renderers (in children also)", EditorStyles.boldLabel);

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("ENABLE RENDERERS", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    modifiedRenderersEnable.Clear();
                    originalRendererStatesEnable.Clear();

                    float amount = 0;

                    foreach (GameObject go2 in Selection.gameObjects)
                    {
                        foreach (Transform child in go2.GetComponentsInChildren<Transform>())
                        {
                            Renderer renderer = child.gameObject.GetComponent<Renderer>();
                            if (renderer != null)
                            {
                                if (!renderer.enabled)
                                {
                                    modifiedRenderersEnable.Add(renderer);
                                    originalRendererStatesEnable.Add(renderer.enabled);
                                    renderer.enabled = true;
                                    amount++;
                                }
                            }
                        }
                    }
                    Debug.LogWarning(amount + " renderers enabled");
                }

                if (GUILayout.Button("UNDO", GUILayout.Width(80), GUILayout.Height(30)))
                {
                    for (int i = 0; i < modifiedRenderersEnable.Count; i++)
                    {
                        Renderer renderer = modifiedRenderersEnable[i];
                        bool originalState = originalRendererStatesEnable[i];

                        if (renderer != null)
                        {
                            renderer.enabled = originalState;
                        }
                    }

                    modifiedRenderersEnable.Clear();
                    originalRendererStatesEnable.Clear();

                    Debug.LogWarning("Enabled renderers changes undone");
                }

                GUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUILayout.Label("It will dig in selected game object(s) and disable active renderers (in children also)", EditorStyles.boldLabel);

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("DISABLE RENDERERS", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    modifiedRenderersDisable.Clear();
                    originalRendererStatesDisable.Clear();

                    float amount = 0;

                    foreach (GameObject go2 in Selection.gameObjects)
                    {
                        foreach (Transform child in go2.GetComponentsInChildren<Transform>())
                        {
                            Renderer renderer = child.gameObject.GetComponent<Renderer>();
                            if (renderer != null)
                            {
                                if (renderer.enabled)
                                {
                                    modifiedRenderersDisable.Add(renderer);
                                    originalRendererStatesDisable.Add(renderer.enabled);
                                    renderer.enabled = false;
                                    amount++;
                                }
                            }
                        }
                    }
                    Debug.LogWarning(amount + " renderers disabled");
                }

                if (GUILayout.Button("UNDO", GUILayout.Width(80), GUILayout.Height(30)))
                {
                    for (int i = 0; i < modifiedRenderersDisable.Count; i++)
                    {
                        Renderer renderer = modifiedRenderersDisable[i];
                        bool originalState = originalRendererStatesDisable[i];

                        if (renderer != null)
                        {
                            renderer.enabled = originalState;
                        }
                    }

                    modifiedRenderersDisable.Clear();
                    originalRendererStatesDisable.Clear();

                    Debug.LogWarning("Disabled renderers changes undone");
                }

                GUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUILayout.Label("It will dig in selected game object(s) and enable not active colliders (in children also)", EditorStyles.boldLabel);

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("ENABLE COLLIDERS", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    modifiedCollidersEnable.Clear();
                    originalColliderStatesEnable.Clear();

                    float amount = 0;

                    foreach (GameObject go2 in Selection.gameObjects)
                    {
                        foreach (Transform child in go2.GetComponentsInChildren<Transform>())
                        {
                            Collider collider = child.gameObject.GetComponent<Collider>();
                            if (collider != null)
                            {
                                if (!collider.enabled)
                                {
                                    modifiedCollidersEnable.Add(collider);
                                    originalColliderStatesEnable.Add(collider.enabled);
                                    collider.enabled = true;
                                    amount++;
                                }
                            }
                        }
                    }
                    Debug.LogWarning(amount + " colliders enabled");
                }

                if (GUILayout.Button("UNDO", GUILayout.Width(80), GUILayout.Height(30)))
                {
                    for (int i = 0; i < modifiedCollidersEnable.Count; i++)
                    {
                        Collider collider = modifiedCollidersEnable[i];
                        bool originalState = originalColliderStatesEnable[i];

                        if (collider != null)
                        {
                            collider.enabled = originalState;
                        }
                    }

                    modifiedCollidersEnable.Clear();
                    originalColliderStatesEnable.Clear();

                    Debug.LogWarning("Enabled colliders changes undone");
                }

                GUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUILayout.Label("It will dig in selected game object(s) and disable active colliders (in children also)", EditorStyles.boldLabel);

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("DISABLE COLLIDERS", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    modifiedCollidersDisable.Clear();
                    originalColliderStatesDisable.Clear();

                    float amount = 0;

                    foreach (GameObject go2 in Selection.gameObjects)
                    {
                        foreach (Transform child in go2.GetComponentsInChildren<Transform>())
                        {
                            Collider collider = child.gameObject.GetComponent<Collider>();
                            if (collider != null)
                            {
                                if (collider.enabled)
                                {
                                    modifiedCollidersDisable.Add(collider);
                                    originalColliderStatesDisable.Add(collider.enabled);
                                    collider.enabled = false;
                                    amount++;
                                }
                            }
                        }
                    }
                    Debug.LogWarning(amount + " colliders disabled");
                }

                if (GUILayout.Button("UNDO", GUILayout.Width(80), GUILayout.Height(30)))
                {
                    for (int i = 0; i < modifiedCollidersDisable.Count; i++)
                    {
                        Collider collider = modifiedCollidersDisable[i];
                        bool originalState = originalColliderStatesDisable[i];

                        if (collider != null)
                        {
                            collider.enabled = originalState;
                        }
                    }

                    modifiedCollidersDisable.Clear();
                    originalColliderStatesDisable.Clear();

                    Debug.LogWarning("Disabled colliders changes undone");
                }

                GUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("It will dig in selected game object(s) and enable not active lights (in children also)", EditorStyles.boldLabel);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("ENABLE LIGHTS", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    float amount = 0;

                    foreach (GameObject go2 in Selection.gameObjects)
                    {
                        foreach (Transform child in go2.GetComponentsInChildren<Transform>())
                        {
                            Light light = child.gameObject.GetComponent<Light>();
                            if (light != null)
                            {
                                if (!light.enabled)
                                {
                                    modifiedLightsEnable.Add(light);
                                    originalLightStatesEnable.Add(light.enabled);
                                    light.enabled = true;
                                    amount++;
                                }
                            }
                        }
                    }
                    Debug.LogWarning(amount + " light enabled");
                }

                if (GUILayout.Button("UNDO", GUILayout.Width(80), GUILayout.Height(30)))
                {
                    for (int i = 0; i < modifiedLightsEnable.Count; i++)
                    {
                        Light light = modifiedLightsEnable[i];
                        bool originalState = originalLightStatesEnable[i];

                        if (light != null)
                        {
                            light.enabled = originalState;
                        }
                    }

                    modifiedLightsEnable.Clear();
                    originalLightStatesEnable.Clear();

                    Debug.LogWarning("Enabled lights changes undone");
                }
                GUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("It will dig in selected game object(s) and disable active lights (in children also)", EditorStyles.boldLabel);

                List<Light> modifiedLightsDisable = new List<Light>();
                List<bool> originalLightStatesDisable = new List<bool>();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("DISABLE LIGHTS", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    float amount = 0;

                    foreach (GameObject go2 in Selection.gameObjects)
                    {
                        foreach (Transform child in go2.GetComponentsInChildren<Transform>())
                        {
                            Light light = child.gameObject.GetComponent<Light>();
                            if (light != null)
                            {
                                if (light.enabled)
                                {
                                    modifiedLightsDisable.Add(light);
                                    originalLightStatesDisable.Add(light.enabled);
                                    light.enabled = false;
                                    amount++;
                                }
                            }
                        }
                    }
                    Debug.LogWarning(amount + " light disabled");
                }

                if (GUILayout.Button("UNDO", GUILayout.Width(80), GUILayout.Height(30)))
                {
                    for (int i = 0; i < modifiedLightsDisable.Count; i++)
                    {
                        Light light = modifiedLightsDisable[i];
                        bool originalState = originalLightStatesDisable[i];

                        if (light != null)
                        {
                            light.enabled = originalState;
                        }
                    }

                    modifiedLightsDisable.Clear();
                    originalLightStatesDisable.Clear();

                    Debug.LogWarning("Disabled lights changes undone");
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();



                break;

            case 2:
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space();

                GUILayout.Label("It will save all meshes in children to files in DevMultiTool/SavedMeshes folder", EditorStyles.boldLabel);
                GUILayout.Label("Saved mesh will be assigned to game object automatically", EditorStyles.boldLabel);
                GUILayout.Label("OPTIONAL : Choose save path folder", EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();


                if (GUILayout.Button("CHOOSE FOLDER", GUILayout.Width(150), GUILayout.Height(30)))
                {
                    string chosenFolderPath = EditorUtility.OpenFolderPanel("Choose Folder for Saved Meshes", selectedFolderPath, "");
                    if (!string.IsNullOrEmpty(chosenFolderPath))
                    {
                        chosenFolderPath = FileUtil.GetProjectRelativePath(chosenFolderPath);
                        if (!string.IsNullOrEmpty(chosenFolderPath))
                        {
                            selectedFolderPath = chosenFolderPath;
                        }
                    }
                }

                if (GUILayout.Button("SAVE MESHES", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    float amount = 0;

                    foreach (GameObject go2 in Selection.gameObjects)
                    {
                        foreach (Transform child in go2.GetComponentsInChildren<Transform>())
                        {
                            MeshFilter meshFilter = child.gameObject.GetComponent<MeshFilter>();
                            if (meshFilter != null)
                            {
                                Mesh m = meshFilter.sharedMesh;
                                originalMeshes[child.gameObject] = m; // Save the original mesh reference

                                string randomName;
                                do
                                {
                                    randomName = "Mesh_" + UnityEngine.Random.Range(1, 100000001) + ".asset";
                                } while (File.Exists("DevMultiTool/SavedMeshes/" + randomName));

                                if (m != null)
                                {
                                    m.name = randomName;
                                    SaveMesh(m, m.name, true, true);
                                    meshFilter.sharedMesh = meshToSave;
                                    amount++;
                                }
                            }
                        }
                    }
                    Debug.LogWarning(amount + " meshes saved");
                }

                // Undo button
                if (GUILayout.Button("UNDO", GUILayout.Width(80), GUILayout.Height(30)))
                {
                    foreach (KeyValuePair<GameObject, Mesh> entry in originalMeshes)
                    {
                        MeshFilter meshFilter = entry.Key.GetComponent<MeshFilter>();
                        if (meshFilter != null)
                        {
                            meshFilter.sharedMesh = entry.Value;
                        }
                    }

                    originalMeshes.Clear();

                    Debug.LogWarning("Saved meshes changes undone");
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.LabelField("Current Path: " + selectedFolderPath);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("It will search in all selected objects and their children.", EditorStyles.boldLabel);
                GUILayout.Label("If object has mesh renderer, but no collider, it will add to it", EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("ADD COLLIDERS", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    float amount = 0;
                    foreach (GameObject go2 in Selection.gameObjects)
                    {
                        foreach (Transform child in go2.GetComponentsInChildren<Transform>())
                        {
                            if (child.gameObject.GetComponent<Renderer>() != null)
                            {
                                if (child.gameObject.GetComponent<Collider>() == null)
                                {
                                    Collider newCollider = child.gameObject.AddComponent<MeshCollider>();
                                    addedColliders.Add(newCollider);
                                    objectsWithAddedColliders.Add(child.gameObject);
                                    amount++;
                                }
                            }
                        }
                    }
                    Debug.Log(amount + " colliders created");
                }

                // Undo button
                if (GUILayout.Button("UNDO", GUILayout.Width(80), GUILayout.Height(30)))
                {
                    for (int i = 0; i < objectsWithAddedColliders.Count; i++)
                    {
                        GameObject obj = objectsWithAddedColliders[i];
                        Collider collider = addedColliders[i];

                        if (obj != null && collider != null)
                        {
                            DestroyImmediate(collider);
                        }
                    }

                    objectsWithAddedColliders.Clear();
                    addedColliders.Clear();

                    Debug.LogWarning("Added colliders changes undone");
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();


                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("It will search for not active renderers in selected game object(s) and its children and remove them ", EditorStyles.boldLabel);
                GUILayout.Label("Optionally: Can remove mesh filter", EditorStyles.boldLabel);


                removemeshfilters = EditorGUILayout.Toggle("Remove also mesh filters?      ", removemeshfilters);
                Color col2 = GUI.contentColor;
                GUI.contentColor = Color.red;
                GUILayout.Label("WARNING : NO UNDO. SAVE YOUR SCENE", EditorStyles.boldLabel);
                GUI.contentColor = col2;

                if (GUILayout.Button("KILL NOT ACTIVE RENDERERS", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    float amount = 0;
                    float amount2 = 0;
                    foreach (GameObject go2 in Selection.gameObjects)
                    {
                        foreach (Transform child in go2.GetComponentsInChildren<Transform>())
                        {
                            if (child.gameObject.GetComponent<MeshFilter>() != null && removemeshfilters && child.gameObject.GetComponent<Renderer>() == null)
                            {
                                amount2++;
                                Undo.RegisterCompleteObjectUndo(child.gameObject, "Remove Mesh Filter");
                                DestroyImmediate(child.gameObject.GetComponent<MeshFilter>());
                            }
                            if (child.gameObject.GetComponent<Renderer>() != null)
                            {
                                if (child.gameObject.GetComponent<Renderer>().enabled != true)
                                {
                                    Undo.RegisterCompleteObjectUndo(child.gameObject, "Remove Renderer");
                                    DestroyImmediate(child.gameObject.GetComponent<Renderer>());
                                    amount++;
                                    if (child.gameObject.GetComponent<MeshFilter>() != null && removemeshfilters)
                                    {
                                        amount2++;
                                        Undo.RegisterCompleteObjectUndo(child.gameObject, "Remove Mesh Filter");
                                        DestroyImmediate(child.gameObject.GetComponent<MeshFilter>());
                                    }
                                }
                            }
                        }
                    }
                    Debug.LogWarning(amount + " renderers destroyed");
                    Debug.LogWarning(amount2 + " mesh filters destroyed");
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("It will search for LOD groups and switch their cross fade mode ", EditorStyles.boldLabel);
                GUILayout.Label("If checked below then animated cross fade, if unchecked it will set to none ", EditorStyles.boldLabel);
                animatedcrossfade = EditorGUILayout.Toggle("Animated cross fade?", animatedcrossfade);
                // FadeMode = EditorGUILayout.PropertyField("FadeMode", FadeMode);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("MODIFY LOD GROUPS", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    float amount2 = 0;
                    foreach (GameObject go2 in Selection.gameObjects)
                    {
                        foreach (Transform child in go2.GetComponentsInChildren<Transform>())
                        {
                            LODGroup lodGroup = child.gameObject.GetComponent<LODGroup>();
                            if (lodGroup != null)
                            {
                                modifiedLODGroups.Add(lodGroup);
                                originalLODFadeModes.Add(lodGroup.fadeMode);
                                originalAnimateCrossFadingStates.Add(lodGroup.animateCrossFading);

                                if (animatedcrossfade == true)
                                {
                                    amount2++;
                                    lodGroup.fadeMode = LODFadeMode.CrossFade;
                                    lodGroup.animateCrossFading = animatedcrossfade;
                                }
                                if (animatedcrossfade == false)
                                {
                                    amount2++;
                                    lodGroup.fadeMode = LODFadeMode.None;
                                    lodGroup.animateCrossFading = false;
                                }
                            }
                        }
                    }
                    Debug.LogWarning(amount2 + " LOD groups modified");
                }

                // Undo button

                if (GUILayout.Button("UNDO", GUILayout.Width(80), GUILayout.Height(30)))
                {
                    for (int i = 0; i < modifiedLODGroups.Count; i++)
                    {
                        LODGroup lodGroup = modifiedLODGroups[i];
                        LODFadeMode originalFadeMode = originalLODFadeModes[i];
                        bool originalAnimateCrossFading = originalAnimateCrossFadingStates[i];

                        if (lodGroup != null)
                        {
                            lodGroup.fadeMode = originalFadeMode;
                            lodGroup.animateCrossFading = originalAnimateCrossFading;
                        }
                    }

                    modifiedLODGroups.Clear();
                    originalLODFadeModes.Clear();
                    originalAnimateCrossFadingStates.Clear();

                    Debug.LogWarning("Modified LOD groups changes undone");
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("It will search for empty game objects with no children and remove them ", EditorStyles.boldLabel);
                GUI.contentColor = Color.red;
                GUILayout.Label("WARNING : NO UNDO. SAVE YOUR SCENE", EditorStyles.boldLabel);
                GUI.contentColor = col2;


                if (GUILayout.Button("REMOVE EMPTY GAME OBJECTS", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    //float amount = 0;
                    float amount2 = 0;
                    foreach (GameObject go2 in Selection.gameObjects)
                    {
                        if (go2 != null)
                        {
                            foreach (Transform child in go2.GetComponentsInChildren<Transform>())
                            {
                                Component[] allComponents = child.GetComponents<Component>();
                                if (allComponents.Length == 1 && child.transform.childCount == 0)
                                {
                                    if (child.gameObject != null)
                                    {
                                        DestroyImmediate(child.gameObject);
                                        amount2++;
                                    }
                                }

                            }
                        }
                    }
                    // Debug.LogWarning(amount + " renderers destroyed");
                    Debug.LogWarning(amount2 + " Empty object removed");

                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("It will dig in selected game object(s) and set all lights to baked (in children also)", EditorStyles.boldLabel);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("SET LIGHT TO BAKED", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    float amount = 0;

                    foreach (GameObject go2 in Selection.gameObjects)
                    {
                        foreach (Transform child in go2.GetComponentsInChildren<Transform>())
                        {
                            Light light = child.gameObject.GetComponent<Light>();

                            if (light != null)
                            {
                                if (!originalLightBakeTypes.ContainsKey(light))
                                {
                                    originalLightBakeTypes[light] = light.lightmapBakeType;
                                }

                                amount++;
                                light.lightmapBakeType = LightmapBakeType.Baked;
                            }
                        }
                    }
                    Debug.LogWarning(amount + " lights set to baked");
                }

                if (GUILayout.Button("UNDO", GUILayout.Width(80), GUILayout.Height(30)))
                {
                    foreach (KeyValuePair<Light, LightmapBakeType> entry in originalLightBakeTypes)
                    {
                        entry.Key.lightmapBakeType = entry.Value;
                    }
                    originalLightBakeTypes.Clear();
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUILayout.Label("Set lightmap scale in selected game object(s) and its children", EditorStyles.boldLabel);
                GUILayout.Label("", EditorStyles.boldLabel);
                LightmapScale = EditorGUILayout.FloatField("Set Lighmap scale value : ", LightmapScale);
                CastShadows = EditorGUILayout.Toggle("Cast Shadows?   ", CastShadows);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("SET LIGHTMAP SCALE", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    float amount = 0;

                    foreach (GameObject go2 in Selection.gameObjects)
                    {
                        foreach (Transform child in go2.GetComponentsInChildren<Transform>())
                        {
                            Renderer rend = child.GetComponent<Renderer>();
                            if (rend != null && child.gameObject.isStatic)
                            {
                                if (!originalLightmapScales.ContainsKey(rend))
                                {
                                    originalLightmapScales[rend] = rend.lightmapScaleOffset.x;
                                }

                                if (!originalShadowCastingModes.ContainsKey(rend))
                                {
                                    originalShadowCastingModes[rend] = rend.shadowCastingMode;
                                }

                                SerializedObject so = new SerializedObject(rend);
                                so.FindProperty("m_ScaleInLightmap").floatValue = LightmapScale;
                                so.ApplyModifiedProperties();
                                amount++;

                                rend.shadowCastingMode = CastShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
                            }
                        }
                    }
                    Debug.LogWarning(amount + " gameobjects set");
                }
                if (GUILayout.Button("UNDO", GUILayout.Width(80), GUILayout.Height(30)))
                {
                    foreach (KeyValuePair<Renderer, float> entry in originalLightmapScales)
                    {
                        SerializedObject so = new SerializedObject(entry.Key);
                        so.FindProperty("m_ScaleInLightmap").floatValue = entry.Value;
                        so.ApplyModifiedProperties();
                    }
                    originalLightmapScales.Clear();

                    foreach (KeyValuePair<Renderer, UnityEngine.Rendering.ShadowCastingMode> entry in originalShadowCastingModes)
                    {
                        entry.Key.shadowCastingMode = entry.Value;
                    }
                    originalShadowCastingModes.Clear();
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();

                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("It will unpack all prefabs completely, also the ones inside selected game object/parent", EditorStyles.boldLabel);
                GUI.contentColor = Color.red;
                GUILayout.Label("WARNING : NO UNDO. SAVE YOUR SCENE", EditorStyles.boldLabel);
                GUI.contentColor = col2;
                if (GUILayout.Button("UNPACK PREFABS", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }

                    float amount = 0;

                    foreach (GameObject go2 in Selection.gameObjects)
                    {


                        foreach (Transform child in go2.GetComponentsInChildren<Transform>())

                        {
                            if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(child))
                            {
                                UnityEditor.PrefabUtility.UnpackPrefabInstance(child.gameObject,
                                      UnityEditor.PrefabUnpackMode.Completely,
                                      UnityEditor.InteractionMode.AutomatedAction);
                                amount++;
                            }

                        }

                    }
                    Debug.LogWarning(amount + " prefabs unpacked completely");


                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUILayout.Label("Search objects by layer and select them", EditorStyles.boldLabel);
                GUILayout.Label("You need to select parent first. It will search inside it", EditorStyles.boldLabel);
                GUILayout.Label("", EditorStyles.boldLabel);
                layer = EditorGUILayout.LayerField("Layer to search: ", layer);

                if (GUILayout.Button("Search and Select", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    float amount = 0;
                    List<GameObject> unityGameObjects = new List<GameObject>();
                    foreach (GameObject go2 in Selection.gameObjects)
                    {
                        foreach (Transform child in go2.GetComponentsInChildren<Transform>())
                        {
                            if (child.gameObject.layer == layer)
                            {

                                if (child.gameObject.GetComponent<Renderer>() != null)
                                {

                                    Renderer rend = child.GetComponent<Renderer>();
                                    unityGameObjects.Add(child.gameObject);
                                    GameObject[] arrayOfGameObjects = unityGameObjects.ToArray();
                                    Selection.objects = arrayOfGameObjects;
                                    amount++;

                                }
                            }


                        }
                    }
                    Debug.LogWarning(amount + " gameobjects selected");
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUILayout.Label("Search objects by their size.", EditorStyles.boldLabel);
                GUILayout.Label("This button allows you to search for and select objects based on their size.");
                GUILayout.Label("You can set a minimum and maximum size value, and the button will check each object's");
                GUILayout.Label("bounding box to see if it meets the size criteria. If an object's bounding box dimensions");
                GUILayout.Label("are smaller than the selected minimum size in all axes, it will be added to the selection.");
                GUILayout.Label("If an object's bounding box dimensions are larger than the selected maximum size in at");
                GUILayout.Label("least one axis, it will also be added to the selection.");
                MinimumSize = EditorGUILayout.FloatField("Minimum Size : ", MinimumSize);
                MaximumSize = EditorGUILayout.FloatField("Maximum Size : ", MaximumSize);
                //layer = EditorGUILayout.LayerField("Layer to set: ", layer);

                if (GUILayout.Button("Search and Select", GUILayout.Width(300), GUILayout.Height(30)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    float amount = 0;
                    List<GameObject> unityGameObjects = new List<GameObject>();
                    foreach (GameObject go2 in Selection.gameObjects)
                    {
                        foreach (Transform child in go2.GetComponentsInChildren<Transform>())
                        {
                            if (child.gameObject.GetComponent<MeshFilter>() != null)
                            {
                                Mesh mesh = child.GetComponent<MeshFilter>().sharedMesh;
                                if (mesh != null)
                                {
                                    if ((mesh.bounds.extents.x * child.transform.localScale.x * 2 >= MinimumSize) &&
                                        (mesh.bounds.extents.y * child.transform.localScale.y * 2 >= MinimumSize) &&
                                        (mesh.bounds.extents.z * child.transform.localScale.z * 2 >= MinimumSize) &&
                                        ((mesh.bounds.extents.x * child.transform.localScale.x * 2 <= MaximumSize) ||
                                         (mesh.bounds.extents.y * child.transform.localScale.y * 2 <= MaximumSize) ||
                                         (mesh.bounds.extents.z * child.transform.localScale.z * 2 <= MaximumSize)))
                                    {
                                        unityGameObjects.Add(child.gameObject);
                                        GameObject[] arrayOfGameObjects = unityGameObjects.ToArray();
                                        Selection.objects = arrayOfGameObjects;
                                        amount++;
                                    }
                                }
                            }
                        }
                    }
                    Debug.LogWarning(amount + " gameobjects selected");
                    if (amount == 0)
                    {
                        Selection.activeObject = null;
                        Debug.LogWarning("No gameobjects found matching the size criteria.");
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUILayout.Label("Prepare Modded scripts for editing or exporting", EditorStyles.boldLabel);
                GUILayout.Label("(Only for WARDUST modding and modified assets e.g. modded microsplat )", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("EDITING", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
                    List<string> allDefines = definesString.Split(';').ToList();
                    allDefines.AddRange(Symbols2.Except(allDefines));
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(
                        EditorUserBuildSettings.selectedBuildTargetGroup,
                        string.Join(";", allDefines.ToArray()));
                }
                if (GUILayout.Button("EXPORTING", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
                    List<string> allDefines = definesString.Split(';').ToList();
                    allDefines.Remove(Symbols);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(
                        EditorUserBuildSettings.selectedBuildTargetGroup,
                        string.Join(";", allDefines.ToArray()));
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                break;
            case 3:
                //GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.padding = new RectOffset(10, 10, 10, 10);

                EditorGUILayout.Space();

                // GUILayout.FlexibleSpace();
                // Create a new GUIStyle with a larger font size
                // GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
                labelStyle.fontSize = 20;
                labelStyle.alignment = TextAnchor.MiddleCenter; // set the label text to center horizontally within the box

                // Calculate the height of the label field based on the font size
                // float labelHeight2 = labelStyle.CalcHeight(new GUIContent("MATERIAL OPTIMIZER"), EditorGUIUtility.currentViewWidth);

                // EditorGUILayout.BeginVertical(boxStyle, GUILayout.Height(labelHeight + 20), GUILayout.ExpandWidth(true)); // add 20 for padding, and expand the width to fill the available space
                // EditorGUILayout.LabelField("MATERIAL OPTIMIZER", labelStyle, GUILayout.Height(labelHeight), GUILayout.ExpandWidth(true)); // set the label to expand in width
                // EditorGUILayout.EndVertical();

                //  GUILayout.FlexibleSpace();
                //  scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("This tool bakes a source material into a single texture, creating an optimized version for lower LODs. The new material will be called <sourceMaterial>_optimized.mat.", MessageType.Info);

                EditorGUILayout.Space();
                sourceMaterial = (Material)EditorGUILayout.ObjectField("Source Material", sourceMaterial, typeof(Material), false);

                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(boxStyle);
                EditorGUILayout.HelpBox("OPTIONAL. Might improve look on some materials. If your source material has tiling different than 1x1 you can use one of these options below. For built-in shaders use the first button. For custom shaders enter your tiling manually and press the second button. If your source tiling is lower than 1x1, baking results might not be great", MessageType.Info);
                if (GUILayout.Button("Set bake tiling automatically"))
                {
                    if (sourceMaterial != null)
                    {
                        UpdateTilingFromSource();
                    }
                    else
                    {
                        Debug.LogError("Source Material is missing.");
                    }
                }
                EditorGUILayout.LabelField("or enter manually :");
                EditorGUILayout.LabelField("Source material tiling", EditorStyles.boldLabel);
                sourceMaterialTiling = EditorGUILayout.Vector2Field("", sourceMaterialTiling);

                // Add a button to set tiling for baking based on the new tiling entry
                EditorGUILayout.Space();
                if (GUILayout.Button("Set Tiling manually"))
                {
                    UpdateTilingFromNewEntry();
                }
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(new GUIContent("Bake tiling ( 1/sourceTiling)", "Adjusts the number of times the source material is repeated (tiled) within the baked texture. Higher values result in more repetitions of the source material in the output texture."), EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Bake Tiling X:", GUILayout.Width(100));
                EditorGUILayout.LabelField(bakeTiling.x.ToString(), GUILayout.Width(50));
                EditorGUILayout.LabelField("Bake Tiling Y:", GUILayout.Width(100));
                EditorGUILayout.LabelField(bakeTiling.y.ToString(), GUILayout.Width(50));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Target material settings :", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                // Add the help box with the information about the shaders
                EditorGUILayout.HelpBox("VertexLit and DiffuseFast are the fastest options (even 50% faster), but they may lower the visual quality of the new material. Use them if you can accept the visual quality loss.", MessageType.Info);

                selectedShader = (ShaderType)EditorGUILayout.EnumPopup("Shader Type", selectedShader);
                EditorGUILayout.Space();
                textureSize = (TextureSizes)EditorGUILayout.EnumPopup("Texture Size", textureSize);

                EditorGUILayout.Space();
                enableGpuInstancing = EditorGUILayout.Toggle("Enable GPU Instancing", enableGpuInstancing);

                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Baking Options", EditorStyles.boldLabel);

                brightness = EditorGUILayout.Slider("Brightness", brightness, 0f, 2f);

                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                disableAnisotropicFiltering = EditorGUILayout.ToggleLeft("Disable Anisotropic Filtering", disableAnisotropicFiltering, GUILayout.Width(200));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
                if (GUILayout.Button("Bake Material", GUILayout.Height(40)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    if (sourceMaterial != null)
                    {
                        BakeMaterial();
                    }
                    else
                    {
                        Debug.LogError("Source Material is missing.");
                    }
                }

                EditorGUILayout.Space();
                if (GUILayout.Button("Take me to baked material"))
                {
                    if (!string.IsNullOrEmpty(bakedMaterialPath))
                    {
                        Material material = AssetDatabase.LoadAssetAtPath<Material>(bakedMaterialPath);
                        Selection.activeObject = material;
                    }
                    else
                    {
                        Debug.LogWarning("No baked material found.");
                    }
                }

                EditorGUILayout.Space();
                if (GUILayout.Button("Take me to baked texture"))
                {
                    if (!string.IsNullOrEmpty(bakedTexturePath))
                    {
                        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(bakedTexturePath);
                        Selection.activeObject = texture;
                    }
                    else
                    {
                        Debug.LogWarning("No baked texture found.");
                    }
                }
                break;

            case 4:

                EditorGUILayout.HelpBox("This tool allows you to set the distances for LOD levels in selected GameObjects with LOD Groups. The distances are set based on the real distance from the camera rather than the screen percentage.", MessageType.Info);
                GUILayout.Space(10);

                GUILayout.Label("Set LOD distances", labelStyle);
                GUILayout.Space(10);

                for (int i = 0; i < lodDistances.Length; i++)
                {
                    GUIContent lodDistanceLabel = new GUIContent($"LOD{i} distance:", $"Set the distance for LOD{i}.");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(lodDistanceLabel);
                    lodDistances[i] = EditorGUILayout.Slider(lodDistances[i], i > 0 ? lodDistances[i - 1] : 0, 1000);
                    if (EditorGUI.EndChangeCheck())
                    {
                        lodDistances[i] = Mathf.Clamp(lodDistances[i], i > 0 ? lodDistances[i - 1] : 0, 1000);
                    }
                    EditorGUILayout.EndHorizontal();
                }

                GUILayout.Space(10);

                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.HelpBox("This section is for setting distances for WarDust. Enable all of them. WD VR Distance will set 3.5 LOD bias for conversion, Use custom FOV and set to 100-105 as average VR headsets", MessageType.Info);
                GUILayout.Space(10);

                GUIContent vrDistanceLabel = new GUIContent("WD VR Distance (approx)", "Set LOD distances for VR.");
                vrDistance = EditorGUILayout.Toggle(vrDistanceLabel, vrDistance);
                if (vrDistance)
                {
                    GUILayout.Space(10);
                    GUIContent tweakLodBiasLabel = new GUIContent("Tweak LOD Bias", "Adjust this slider to get accurate results when in VR.");
                    tweakLodBias = EditorGUILayout.Slider(tweakLodBiasLabel, tweakLodBias, 2.5f, 4f);
                }
                GUILayout.Space(10);
                GUILayout.Label("Camera FOV:");

                GUIContent useCustomFOVLabel = new GUIContent("Use custom FOV", "Use a custom field of view for LOD calculations.");
                useCustomFOV = EditorGUILayout.BeginToggleGroup(useCustomFOVLabel, useCustomFOV);
                customFOV = EditorGUILayout.FloatField("Custom FOV", customFOV);

                EditorGUILayout.EndToggleGroup();

                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                GUIContent protectLastLODLabel = new GUIContent("Protect Last LOD (Culling)", "Prevent the last LOD level from being culled.");
                GUILayout.Label(protectLastLODLabel, labelStyle);
                protectLastLOD = EditorGUILayout.Toggle(protectLastLOD);
                if (protectLastLOD)
                {
                    GUILayout.Space(10);
                    GUIContent cullingDistanceLabel = new GUIContent("Minimum Culling Distance", "Set the minimum distance for culling.");
                    cullingDistance = EditorGUILayout.Slider(cullingDistanceLabel, cullingDistance, 0, 2000);
                }

                GUILayout.Space(10);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                GUILayout.Label("Modify only:", labelStyle);
                GUILayout.Space(10);

                GUIContent smallerThanLabel = new GUIContent("Smaller than", "Modify only GameObjects smaller than the specified size.");
                smallerThan = EditorGUILayout.BeginToggleGroup(smallerThanLabel, smallerThan);
                smallerThanSize = EditorGUILayout.FloatField("Size", smallerThanSize);
                EditorGUILayout.EndToggleGroup();

                GUIContent biggerThanLabel = new GUIContent("Bigger than", "Modify only GameObjects bigger than the specified size.");
                biggerThan = EditorGUILayout.BeginToggleGroup(biggerThanLabel, biggerThan);
                biggerThanSize = EditorGUILayout.FloatField("Size", biggerThanSize);
                EditorGUILayout.EndToggleGroup();

                GUILayout.Space(10);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                GUIContent applyDistancesLabel = new GUIContent("Apply Distances", "Apply the specified distances to the selected LOD Groups.");
                Color col3 = GUI.contentColor;
                GUI.contentColor = Color.red;
                GUILayout.Label("WARNING : NO UNDO. SAVE YOUR SCENE", EditorStyles.boldLabel);
                GUI.contentColor = col3;
                if (GUILayout.Button(applyDistancesLabel, GUILayout.Height(40)))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    ApplyLODDistances();
                }

                // GUI.color = defaultColor;
                break;
            case 5:
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("This tool will replace selected game objects with your prefab", EditorStyles.boldLabel);
                EditorGUILayout.Space();
                prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);


                EditorGUILayout.Space();
                randomReplacement = EditorGUILayout.Toggle("Random Replacement", randomReplacement);
                if (randomReplacement)
                {
                    EditorGUILayout.LabelField("Prefabs for random replacement:");
                    int newLength = EditorGUILayout.IntField("Size", prefabs == null ? 0 : prefabs.Length);
                    if (newLength != (prefabs == null ? 0 : prefabs.Length))
                    {
                        System.Array.Resize(ref prefabs, newLength);
                    }

                    for (int i = 0; i < (prefabs == null ? 0 : prefabs.Length); i++)
                    {
                        prefabs[i] = (GameObject)EditorGUILayout.ObjectField("Prefab " + (i + 1), prefabs[i], typeof(GameObject), false);
                    }
                }

                if (GUILayout.Button("Replace"))
                {
                    if (saveSceneBeforeOperation)
                    {
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            return;
                        }
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    var selection = Selection.gameObjects;

                    for (var i = selection.Length - 1; i >= 0; --i)
                    {
                        var selected = selection[i];
                        GameObject selectedPrefab = prefab;

                        if (randomReplacement && prefabs != null && prefabs.Length > 0)
                        {
                            int randomIndex = UnityEngine.Random.Range(0, prefabs.Length);
                            selectedPrefab = prefabs[randomIndex];
                        }

                        var prefabType = PrefabUtility.GetPrefabType(selectedPrefab);
                        GameObject newObject;

                        if (prefabType == PrefabType.Prefab)
                        {
                            newObject = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
                        }
                        else
                        {
                            newObject = Instantiate(selectedPrefab);
                            newObject.name = selectedPrefab.name;
                        }

                        if (newObject == null)
                        {
                            Debug.LogError("Error instantiating prefab");
                            break;
                        }

                        Undo.RegisterCreatedObjectUndo(newObject, "Replace With Prefabs");
                        newObject.transform.parent = selected.transform.parent;
                        newObject.transform.localPosition = selected.transform.localPosition;
                        newObject.transform.localRotation = selected.transform.localRotation;
                        newObject.transform.localScale = selected.transform.localScale;
                        newObject.transform.SetSiblingIndex(selected.transform.GetSiblingIndex());
                        Undo.DestroyObjectImmediate(selected);
                    }
                }


                if (GUILayout.Button("Undo"))
                {
                    Undo.PerformUndo();
                }


                GUI.enabled = false;
                EditorGUILayout.LabelField("Selection count: " + Selection.objects.Length);
                break;
            case 6:
                EditorGUILayout.BeginVertical("Box");
                GUILayout.Label("Change Children Tags and Layers", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("This tool allows you to change the tags and layers of selected objects and their children. You can choose to change only objects with colliders or all objects.", MessageType.Info);

                GUILayout.Space(10);

                changeOnlyObjectsWithColliders = EditorGUILayout.Toggle("Only objects with colliders", changeOnlyObjectsWithColliders);

                GUILayout.Space(10);

                EditorGUILayout.BeginVertical("Box");
                GUILayout.Label("Change Tags", EditorStyles.boldLabel);

                selectedTagIndex = EditorGUILayout.Popup("Select Tag", selectedTagIndex, tags);

                if (GUILayout.Button("Change Children Tags"))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    ChangeChildrenTags(tags[selectedTagIndex]);
                }
                EditorGUILayout.EndVertical();

                GUILayout.Space(10);

                EditorGUILayout.BeginVertical("Box");
                GUILayout.Label("Change Layers", EditorStyles.boldLabel);

                selectedLayerIndex = EditorGUILayout.LayerField("Select Layer", selectedLayerIndex);

                if (GUILayout.Button("Change Children Layers"))
                {
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                    ChangeChildrenLayers(selectedLayerIndex);
                }
                EditorGUILayout.EndVertical();

                GUILayout.Space(10);

                EditorGUILayout.BeginVertical("Box");
                GUILayout.Label("Make Terrain and Terrain Trees Climbable", EditorStyles.boldLabel);

                if (GUILayout.Button("Make terrain and terrain trees climbable"))

                {
                    MakeTerrainClimbable();
                    if (saveSceneBeforeOperation)
                    {
                        // Open window dialog to save scene
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            // User cancelled the save scene operation, return from OnGUI
                            return;
                        }
                        // Save the scene
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                }
                EditorGUILayout.EndVertical();

                GUILayout.Space(10);

                EditorGUILayout.EndVertical();
                break;
            case 7:
                break;




        }
        EditorGUILayout.EndScrollView();

    }
    public void SaveMesh(Mesh mesh, string name, bool makeNewInstance, bool optimizeMesh)
    {
        if (string.IsNullOrEmpty(selectedFolderPath)) return;

        if (!AssetDatabase.IsValidFolder(selectedFolderPath))
        {
            string guid = AssetDatabase.CreateFolder("Assets/DevMultiTool", "SavedMeshes");
            selectedFolderPath = AssetDatabase.GUIDToAssetPath(guid);
        }

        meshToSave = (makeNewInstance) ? Instantiate(mesh) as Mesh : mesh;

        if (optimizeMesh)
            MeshUtility.Optimize(meshToSave);

        AssetDatabase.CreateAsset(meshToSave, selectedFolderPath + "/" + name);
        AssetDatabase.SaveAssets();
    }
    public void ExportTrees()
    {
        if (_terrain == null)
        {
            Debug.LogError("No terrain selected");
            return;
        }
        TerrainData data = _terrain.terrainData;
        float width = data.size.x;
        float height = data.size.z;
        float y = data.size.y;
        // Create parent
        GameObject parent = GameObject.Find("TREES_EXPORTED");
        if (parent == null)
        {
            parent = new GameObject("TREES_EXPORTED");
        }
        // Create trees
        foreach (TreeInstance tree in data.treeInstances)
        {
            if (tree.prototypeIndex >= data.treePrototypes.Length)
                continue;
            var _tree = data.treePrototypes[tree.prototypeIndex].prefab;
            Vector3 position = new Vector3(
                tree.position.x * width,
                tree.position.y * y,
                tree.position.z * height) + _terrain.transform.position;
            Vector3 scale = new Vector3(tree.widthScale, tree.heightScale, tree.widthScale);
            GameObject go = Instantiate(_tree, position, Quaternion.Euler(0f, tree.rotation * Mathf.Rad2Deg, 0f), parent.transform) as GameObject;
            go.transform.localScale = scale;
        }
    }
    public void Clear()
    {
        DestroyImmediate(GameObject.Find("TREES_EXPORTED"));
    }
    private void BakeMaterial()
    {
        if (saveSceneBeforeOperation)
        {
            // Open window dialog to save scene
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                // User cancelled the save scene operation, return from OnGUI
                return;
            }
            // Save the scene
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        int maxTextureSize = SystemInfo.maxTextureSize;

        string sourcePath = AssetDatabase.GetAssetPath(sourceMaterial);
        string folderPath = "Assets";


        string roundyToolsPath = Path.Combine(folderPath, "DevMultiTool");
        if (!AssetDatabase.IsValidFolder(roundyToolsPath))
        {
            AssetDatabase.CreateFolder(folderPath, "DevMultiTool");
        }

        // Create the "MaterialOptimizer" folder if it doesn't exist
        string materialOptimizerPath = Path.Combine(roundyToolsPath, "MaterialBaker");
        if (!AssetDatabase.IsValidFolder(materialOptimizerPath))
        {
            AssetDatabase.CreateFolder(roundyToolsPath, "MaterialBaker");
        }

        // Create the "OptimizedMaterials" folder if it doesn't exist
        string optimizedMaterialsPath = Path.Combine(materialOptimizerPath, "OptimizedMaterials");
        if (!AssetDatabase.IsValidFolder(optimizedMaterialsPath))
        {
            AssetDatabase.CreateFolder(materialOptimizerPath, "OptimizedMaterials");
        }

        // Create the "BakedTextures" folder if it doesn't exist
        string bakedTexturesPath = Path.Combine(materialOptimizerPath, "BakedTextures");
        if (!AssetDatabase.IsValidFolder(bakedTexturesPath))
        {
            AssetDatabase.CreateFolder(materialOptimizerPath, "BakedTextures");
        }


        string fileName = Path.GetFileNameWithoutExtension(sourcePath);
        string newFileName = fileName + "_optimized";
        string newMaterialPath = Path.Combine(optimizedMaterialsPath, newFileName);


        Scene currentScene = SceneManager.GetActiveScene();
        Scene tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        SceneManager.SetActiveScene(tempScene);

        GameObject quad = new GameObject("Quad");
        quad.AddComponent<MeshFilter>();
        quad.AddComponent<MeshRenderer>();
        quad.transform.localScale = new Vector3(bakeTiling.x, bakeTiling.y, 1);

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
            new Vector3(0.5f, 0.5f, 0)
        };
        mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        mesh.normals = new Vector3[] { -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward };
        mesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(bakeTiling.x, 0),
            new Vector2(0, bakeTiling.y),
            new Vector2(bakeTiling.x, bakeTiling.y)
        };
        quad.GetComponent<MeshFilter>().mesh = mesh;
        quad.GetComponent<Renderer>().sharedMaterial = sourceMaterial;

        // Add Directional Light to the temporary scene
        GameObject lightObject = new GameObject("Directional Light");
        lightObject.AddComponent<Light>();
        lightObject.transform.rotation = Quaternion.Euler(50, -30, 0);
        lightObject.GetComponent<Light>().type = LightType.Directional;

        // Modify light intensity based on environment lighting strength
        lightObject.GetComponent<Light>().intensity = brightness;

        Camera camera = new GameObject("Camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, -1000, -1);
        quad.transform.position = new Vector3(0, -1000, 0);
        lightObject.transform.position = new Vector3(0, -1000, 0);
        camera.orthographic = true;
        camera.orthographicSize = 0.5f * Mathf.Max(bakeTiling.x, bakeTiling.y);
        // Set the camera's background color based on baking color and brightness
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.targetTexture = new RenderTexture((int)textureSize, (int)textureSize, 24, RenderTextureFormat.ARGB32);

        Shader shader = null;
        switch (selectedShader)
        {
            case ShaderType.Standard:
                shader = Shader.Find("Standard");
                break;
            case ShaderType.VertexLit:
                shader = Shader.Find("Legacy Shaders/VertexLit");
                break;
            case ShaderType.Diffuse:
                shader = Shader.Find("Legacy Shaders/Diffuse");
                break;
            case ShaderType.DiffuseFast:
                shader = Shader.Find("Legacy Shaders/Diffuse Fast");
                break;
        }
        Debug.Log($"Shader used: {shader.name}");
        Material newMaterial = new Material(shader);



        if (selectedShader == ShaderType.Standard)
        {
            newMaterial.SetFloat("_Glossiness", 0f);
        }
        newMaterial.enableInstancing = enableGpuInstancing;
        RenderTexture.active = camera.targetTexture;
        camera.Render();
        Texture2D newTexture = new Texture2D((int)textureSize, (int)textureSize, TextureFormat.RGB24, false);
        newTexture.ReadPixels(new Rect(0, 0, (int)textureSize, (int)textureSize), 0, 0);
        newTexture.Apply();
        RenderTexture.active = null;

        byte[] bytes = newTexture.EncodeToPNG();
        bakedTexturePath = Path.Combine(bakedTexturesPath, fileName + "_baked.png");

        File.WriteAllBytes(bakedTexturePath, bytes);
        AssetDatabase.ImportAsset(bakedTexturePath);
        Debug.Log($"Texture created: {fileName}_baked.png");
        Debug.Log($"Texture saved at path: {bakedTexturePath}");

        Texture2D importedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(bakedTexturePath);
        TextureImporter textureImporter = AssetImporter.GetAtPath(bakedTexturePath) as TextureImporter;

        if (disableAnisotropicFiltering)
        {
            textureImporter.anisoLevel = 0;
        }
        else
        {
            textureImporter.anisoLevel = 1;
        }

        textureImporter.maxTextureSize = (int)textureSize;

        textureImporter.SaveAndReimport();
        AssetDatabase.SaveAssets();
        newMaterial.mainTexture = importedTexture;
        newMaterial.mainTextureScale = new Vector2(1f / bakeTiling.x, 1f / bakeTiling.y);
        bakedMaterialPath = newMaterialPath + ".mat";

        Material existingMaterial = null;
        List<GameObject> objectsWithExistingMaterial = null;

        if (File.Exists(bakedMaterialPath))
        {
            existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(bakedMaterialPath);
            objectsWithExistingMaterial = FindObjectsWithMaterial(existingMaterial);
            AssetDatabase.DeleteAsset(bakedMaterialPath);
        }
        if (objectsWithExistingMaterial != null)
        {
            foreach (GameObject obj in objectsWithExistingMaterial)
            {
                obj.GetComponent<Renderer>().sharedMaterial = newMaterial;
            }
        }
        AssetDatabase.CreateAsset(newMaterial, newMaterialPath + ".mat");
        Debug.Log($"Material created: {newMaterial.name}");
        Debug.Log($"Material saved at path: {newMaterialPath}.mat");

        EditorSceneManager.CloseScene(tempScene, true);
        SceneManager.SetActiveScene(currentScene);


    }
    private bool CheckSizeConditions(LODGroup lodGroup)
    {
        if (smallerThan || biggerThan)
        {
            Renderer[] lod0Renderers = lodGroup.GetLODs()[0].renderers;
            Bounds combinedBounds = new Bounds();
            bool isFirstBounds = true;

            foreach (Renderer renderer in lod0Renderers)
            {
                if (isFirstBounds)
                {
                    combinedBounds = renderer.bounds;
                    isFirstBounds = false;
                }
                else
                {
                    combinedBounds.Encapsulate(renderer.bounds);
                }
            }

            Vector3 boundsSize = combinedBounds.size;

            bool smallerThanCondition = !smallerThan || (boundsSize.x <= smallerThanSize && boundsSize.y <= smallerThanSize && boundsSize.z <= smallerThanSize);
            bool biggerThanCondition = !biggerThan || (boundsSize.x >= biggerThanSize || boundsSize.y >= biggerThanSize || boundsSize.z >= biggerThanSize);

            return smallerThanCondition && biggerThanCondition;
        }

        return true;
    }

    private void ApplyLODDistances()
    {
        float originalLODBias = QualitySettings.lodBias;

        if (vrDistance)
        {
            QualitySettings.lodBias = tweakLodBias;
        }

        int modifiedObjectsCount = 0;

        foreach (var obj in Selection.gameObjects)
        {
            lodGroups.Clear();
            FindLODGroupsInChildren(obj.transform);
            foreach (LODGroup lodGroup in lodGroups)
            {
                if (!CheckSizeConditions(lodGroup))
                {
                    continue;
                }

                Undo.RecordObject(lodGroup, "Apply LOD Distances");
                SetLODDistances(lodGroup);
                modifiedObjectsCount++;
                Debug.Log($"Modified object: {lodGroup.gameObject.name}");
            }
        }

        if (vrDistance)
        {
            QualitySettings.lodBias = originalLODBias;
        }

        Debug.Log($"Total modified objects: {modifiedObjectsCount}");
    }

    private void FindLODGroupsInChildren(Transform parent)
    {
        LODGroup lodGroup = parent.GetComponent<LODGroup>();
        if (lodGroup != null)
        {
            lodGroups.Add(lodGroup);
        }

        foreach (Transform child in parent)
        {
            FindLODGroupsInChildren(child);
        }
    }

    private void SetLODDistances(LODGroup lodGroup)
    {
        LOD[] lods = lodGroup.GetLODs();
        int numLODs = Mathf.Min(lods.Length, lodDistances.Length);

        float prevScreenRelativeHeight = 1.0f;

        for (int i = 0; i < numLODs; i++)
        {
            float screenRelativeHeight;

            if (protectLastLOD && i == numLODs - 1)
            {
                screenRelativeHeight = Mathf.Clamp(DistanceToScreenRelativeHeight(cullingDistance, lodGroup), 0.0f, prevScreenRelativeHeight - 0.01f);
            }
            else
            {
                screenRelativeHeight = Mathf.Clamp(DistanceToScreenRelativeHeight(lodDistances[i], lodGroup), 0.0f, prevScreenRelativeHeight - 0.01f);
            }

            if (i > 0)
            {
                screenRelativeHeight = Mathf.Min(screenRelativeHeight, 0.99f);
            }

            if (i == 0 && screenRelativeHeight == 0.99f)
            {
                Debug.LogWarning($"LOD0 distance is set to 0.99 for object '{lodGroup.gameObject.name}' because the object is too large to meet distance conditions.");
            }

            lods[i].screenRelativeTransitionHeight = screenRelativeHeight;
            prevScreenRelativeHeight = screenRelativeHeight;
        }

        lodGroup.SetLODs(lods);
        lodGroup.RecalculateBounds();
    }

    private float DistanceToScreenRelativeHeight(float distance, LODGroup lodGroup)
    {
        float usedFov;

        if (useCustomFOV)
        {
            usedFov = customFOV;
        }
        else if (SceneView.lastActiveSceneView != null)
        {
            Camera sceneCamera = SceneView.lastActiveSceneView.camera;
            usedFov = sceneCamera.fieldOfView;
        }
        else
        {
            return 0;
        }

        float halfFovTan = Mathf.Tan(usedFov * 0.5f * Mathf.Deg2Rad);
        float worldSpaceSize = lodGroup.size;
        Vector3 scale = lodGroup.transform.lossyScale;
        float largestScale = Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
        worldSpaceSize *= largestScale;
        float screenHeight = (worldSpaceSize * halfFovTan) / distance;
        return screenHeight;
    }
    private Mesh GetSubmesh(Mesh mesh, int submeshIndex)
    {
        if (submeshIndex < 0 || submeshIndex >= mesh.subMeshCount)
        {
            Debug.LogError("Invalid submesh index.");
            return null;
        }

        int[] indices = mesh.GetIndices(submeshIndex);
        int vertexCount = indices.Length;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uv = new Vector2[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector4[] tangents = new Vector4[vertexCount];

        for (int i = 0; i < vertexCount; i++)
        {
            int index = indices[i];
            vertices[i] = mesh.vertices[index];
            uv[i] = mesh.uv[index];
            normals[i] = mesh.normals[index];
            tangents[i] = mesh.tangents[index];
        }

        Mesh submesh = new Mesh();
        submesh.vertices = vertices;
        submesh.uv = uv;
        submesh.normals = normals;
        submesh.tangents = tangents;
        submesh.triangles = Enumerable.Range(0, vertexCount).ToArray();

        return submesh;
    }

    private void UnpackNestedPrefab(GameObject gameObject)
    {
        while (PrefabUtility.IsPartOfPrefabInstance(gameObject))
        {
            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }

    private void RemoveRendererFromLODGroup(Renderer renderer, LODGroup lodGroup)
    {
        LOD[] lods = lodGroup.GetLODs();
        for (int i = 0; i < lods.Length; i++)
        {
            List<Renderer> lodRenderers = lods[i].renderers.ToList();
            if (lodRenderers.Contains(renderer))
            {
                lodRenderers.Remove(renderer);
                lods[i].renderers = lodRenderers.ToArray();
            }
        }
        lodGroup.SetLODs(lods);
        UnityEditor.EditorUtility.SetDirty(lodGroup);
    }
    private void CloneFromSelectedLODLevel()
    {
        string clonedFromLODsName = "ClonedFromLODs";
        GameObject clonedFromLODsParent = GameObject.Find(clonedFromLODsName);

        if (clonedFromLODsParent == null)
        {
            clonedFromLODsParent = new GameObject(clonedFromLODsName);
        }

        foreach (GameObject go in Selection.gameObjects)
        {
            LODGroup[] lodGroups = go.GetComponentsInChildren<LODGroup>();

            foreach (LODGroup lodGroup in lodGroups)
            {
                Renderer[] renderers = null;
                int clonedLODIndex = -1;

                for (int lodIndex = selectedLODLevel; lodIndex >= 0; lodIndex--)
                {
                    if (lodIndex < lodGroup.lodCount)
                    {
                        renderers = lodGroup.GetLODs()[lodIndex].renderers;

                        if (renderers.Length > 0)
                        {
                            clonedLODIndex = lodIndex;
                            break;
                        }
                    }
                }

                // If no LOD level was found, clone the highest available level.
                if (renderers == null || renderers.Length == 0)
                {
                    clonedLODIndex = lodGroup.lodCount - 1;
                    renderers = lodGroup.GetLODs()[clonedLODIndex].renderers;
                }

                // Clone the renderers and maintain their positions, rotations, and scales.
                if (renderers.Length > 0)
                {
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer != null)
                        {
                            GameObject clonedGO = Instantiate(renderer.gameObject);
                            clonedGO.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                            clonedGO.transform.localScale = renderer.transform.lossyScale;

                            clonedGO.name = renderer.gameObject.name + "_LOD" + clonedLODIndex;
                            clonedGO.transform.SetParent(clonedFromLODsParent.transform, true);
                        }
                    }
                    lodGroup.gameObject.SetActive(false);
                }
            }
        }
    }
    public void ChangeChildrenTags(string newTag)
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        int changedObjectsCount = 0;

        foreach (GameObject selectedObject in selectedObjects)
        {
            if (selectedObject != null)
            {
                if (EditorUtility.DisplayDialog("Change child tags", "Do you really want to change the tags of the selected objects?", "Change tags", "Cancel"))
                {
                    changedObjectsCount += ApplyActionToObjects(selectedObject, obj =>
                    {
                        Undo.RecordObject(obj, "Change Tag");
                        obj.tag = newTag;
                    });
                }
            }
        }

        Debug.Log("Changed tags for " + changedObjectsCount + " objects.");
    }

    public void ChangeChildrenLayers(int newLayer)
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        int changedObjectsCount = 0;

        foreach (GameObject selectedObject in selectedObjects)
        {
            if (selectedObject != null)
            {
                if (EditorUtility.DisplayDialog("Change child layers", "Do you really want to change the layers of the selected objects?", "Change layers", "Cancel"))
                {
                    changedObjectsCount += ApplyActionToObjects(selectedObject, obj =>
                    {
                        Undo.RecordObject(obj, "Change Layer");
                        obj.layer = newLayer;
                    });
                }
            }
        }

        Debug.Log("Changed layers for " + changedObjectsCount + " objects.");
    }

    private int ApplyActionToObjects(GameObject obj, System.Action<GameObject> action)
    {
        int changedObjectsCount = 0;

        if (!changeOnlyObjectsWithColliders || obj.GetComponent<Collider>() != null)
        {
            action(obj);
            changedObjectsCount++;
        }

        foreach (Transform childTransform in obj.transform)
        {
            changedObjectsCount += ApplyActionToObjects(childTransform.gameObject, action);
        }

        return changedObjectsCount;
    }
    private void MakeTerrainClimbable()
    {
        Terrain[] terrains = GameObject.FindObjectsOfType<Terrain>();
        int climbableLayer = 30;

        foreach (Terrain terrain in terrains)
        {

            Undo.RecordObject(terrain.gameObject, "Make Terrain Climbable");
            terrain.gameObject.layer = climbableLayer;
            Debug.Log("Terrain " + terrain.gameObject.name + " is now climbable");

        }
    }
}

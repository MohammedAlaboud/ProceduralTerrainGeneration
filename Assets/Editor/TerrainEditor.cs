using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using EditorGUITable; //obtained from the asset store to help build custom tables in the GUI (from https://assetstore.unity.com/packages/tools/gui/editor-gui-table-108795)

[CustomEditor(typeof(TerrainS))] //customer editor links code in here to that of the TerrainS so that other "of type terrain" objects can get the necessary data from here
[CanEditMultipleObjects] //make it posssible to link multiple objects

public class TerrainEditor : Editor //an editor class to setup the UI for the script rather than a playable program
{
    //serialized properties to be able to back to the ones in the TerrainS script
    SerializedProperty randomHeightRange;
    SerializedProperty heightMapImg;
    SerializedProperty heightMapSize;
    SerializedProperty perlinFrequency;
    SerializedProperty perlinAmplitude;
    SerializedProperty perlinFrequencyOffset;
    SerializedProperty perlinAmplitudeOffset;
    SerializedProperty octaves; //fractal Brownian Motion param
    SerializedProperty persistance; //fractal Brownian Motion param
    SerializedProperty fractalBrownianMotionSize;
    SerializedProperty resetBeforeApply;
    SerializedProperty voronoiTessellationSlopeFallOff;
    SerializedProperty voronoiTessellationSlopeDropOff;
    SerializedProperty voronoiTessellationMinHeight;
    SerializedProperty voronoiTessellationMaxHeight;
    SerializedProperty voronoiNumberOfSeeds;
    SerializedProperty voronoiTessellationAlgorithmType;
    SerializedProperty diamondSquareMinHeight;
    SerializedProperty diamondSquareMaxHeight;
    SerializedProperty diamondSquareHeightControlPower;
    SerializedProperty diamondSquareRoughnessControl;
    SerializedProperty smoothIterationCount;
    SerializedProperty maxVegetation;
    SerializedProperty vegetationSpacing;

    //serialized the lists to be able to link back to the ones in the TerrainS script with the corresponding subclass parameters automatically arranged using the GUI table 
    GUITableState fractalBrownianMotionParamsTable;
    SerializedProperty fractalBrownianMotionParams;
    GUITableState splatMapPropetiesTable;
    SerializedProperty splatsByAltitude;
    GUITableState vegetationsMapTable;
    SerializedProperty vegetations;


    //drop down menus - fold outs
    bool randomEnabled = false; //enables foldout for generating random heights
    bool heightsLoadingEnabled = false; //enables foldout for loading in a heightMap
    bool perlinNoiseEnabled = false; //enables foldout for applying fractal Brownian Motion 
    bool multipleFractalBrownianMotionEnabled = false; //enables foldout for applying multiple stacked fractal Brownian Motion
    bool voronoiTessellationEnabled = false; //enables foldout for applying Voronoi Tesslation algorithm to the terrain
    bool diamondSquareEnabled = false; //enables foldout for applying Diamond Square algorithm to the terrain
    bool smoothEnabled = false; //enables foldout for applying smooth function
    bool splatEnabled = false; //enables foldout for applying textures
    bool showHeightMapEnabled = false; //another foldout to display heightmap of terrain
    bool vegetationEnabled = false; //foldout to display vegetation drop down menu

    Texture2D heightMapTexture; //to store heightmap that will be loaded as a displayable 2D texture in the UI

    private void OnEnable() //rerun program everytime is it recompiled without having to manually run it
    {
        //links properties to variables with the same name in TerrainS script using string referencing 
        randomHeightRange = serializedObject.FindProperty("randomHeightRange"); //example: links randomHeightRange to the one with the same name in the Terrain script and modifying one changes the other
        heightMapImg = serializedObject.FindProperty("heightMapImg");
        heightMapSize = serializedObject.FindProperty("heightMapSize");
        perlinFrequency = serializedObject.FindProperty("perlinFrequency");
        perlinAmplitude = serializedObject.FindProperty("perlinAmplitude");
        perlinFrequencyOffset = serializedObject.FindProperty("perlinFrequencyOffset");
        perlinAmplitudeOffset = serializedObject.FindProperty("perlinAmplitudeOffset");
        octaves = serializedObject.FindProperty("octaves");
        persistance = serializedObject.FindProperty("persistance");
        fractalBrownianMotionSize = serializedObject.FindProperty("fractalBrownianMotionSize");
        resetBeforeApply = serializedObject.FindProperty("resetBeforeApply");
        fractalBrownianMotionParamsTable = new GUITableState("fractalBrownianMotionParamsTable"); //setup for table to accomodate fractalBrownianMotionParams list (using GUI Table Library)
        fractalBrownianMotionParams = serializedObject.FindProperty("fractalBrownianMotionParams");
        voronoiTessellationSlopeFallOff = serializedObject.FindProperty("voronoiTessellationSlopeFallOff");
        voronoiTessellationSlopeDropOff = serializedObject.FindProperty("voronoiTessellationSlopeDropOff");
        voronoiTessellationMinHeight = serializedObject.FindProperty("voronoiTessellationMinHeight");
        voronoiTessellationMaxHeight = serializedObject.FindProperty("voronoiTessellationMaxHeight");
        voronoiNumberOfSeeds = serializedObject.FindProperty("voronoiNumberOfSeeds");
        voronoiTessellationAlgorithmType = serializedObject.FindProperty("voronoiTessellationAlgorithmType");
        diamondSquareMinHeight = serializedObject.FindProperty("diamondSquareMinHeight");
        diamondSquareMaxHeight = serializedObject.FindProperty("diamondSquareMaxHeight");
        diamondSquareHeightControlPower = serializedObject.FindProperty("diamondSquareHeightControlPower");
        diamondSquareRoughnessControl = serializedObject.FindProperty("diamondSquareRoughnessControl");
        smoothIterationCount = serializedObject.FindProperty("smoothIterationCount");
        splatMapPropetiesTable = new GUITableState("splatMapPropetiesTable"); //setup for table to accomodate splatsByAltitude list (using GUI Table Library)
        splatsByAltitude = serializedObject.FindProperty("splatsByAltitude");
        vegetationsMapTable = new GUITableState("vegetationsTable"); //setup for table to accomodate vegetations list (using GUI Table Library)
        vegetations = serializedObject.FindProperty("vegetations");
        maxVegetation = serializedObject.FindProperty("maxVegetation");
        vegetationSpacing = serializedObject.FindProperty("vegetationSpacing");

        heightMapTexture = new Texture2D(513, 513, TextureFormat.ARGB32, false); //setup texture to display


    }

    Vector2 scrollPosition; //to initialize the value of scroll position for the scroll bar outside the display update loop to avoid reinitializing it constantly

    public override void OnInspectorGUI() //graphical UI in the inspector for our custom terrain editor (display update loop)
    {
        serializedObject.Update(); //updates all serialized values between this script and terrain script

        TerrainS terrain = (TerrainS)target; //links target variable to the other script/class that this script is linked to (on line 7) to allow us to access Terrain vars and methods easily

        //SCROLLBAR CODE
        Rect rect = EditorGUILayout.BeginVertical(); //amount of space in the inspector to shove UI data of script into
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Width(rect.width), GUILayout.Width(rect.height)); //create the scroll bar using the dimensions set for the UI (made at the script component) and adjust the scroll bar accordingly
        EditorGUI.indentLevel++; //increase indents in the inspector; mainly at the script component (scroll bar goes over foldout triangles so this helps make them completely visible again).

        EditorGUILayout.PropertyField(resetBeforeApply); //tick-box in UI to allow user to choose between applying changes to the terrain after reseting it or applying changes to the terrain with the previous existing data on it

        //set up UI in foldout for random heights function
        randomEnabled = EditorGUILayout.Foldout(randomEnabled, "Random Heights"); 
        if (randomEnabled) //open if clicked
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider); //horizontal separation
            GUILayout.Label("Set Random Heights Range", EditorStyles.boldLabel); //instruction label 
            EditorGUILayout.PropertyField(randomHeightRange); //allows for adjusting the random heights range values in the UI

            if (GUILayout.Button("Produce Random Heights")) //if apply button is pressed 
            {
                terrain.ProcessRandomTerrain(); //call corresponding function
            }
        }

        //set up UI in foldout for loading heightmaps function
        heightsLoadingEnabled = EditorGUILayout.Foldout(heightsLoadingEnabled, "Load Heightmap"); 
        if (heightsLoadingEnabled) //open if clicked
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider); //horizontal separation
            GUILayout.Label("Load Heights from Texture Map", EditorStyles.boldLabel); //text label
            EditorGUILayout.PropertyField(heightMapImg); //allows for adding in the 2D texture map in the UI (cool online resource for heightmaps is terrain.party)
            EditorGUILayout.PropertyField(heightMapSize); //allows for adjusting the heightmap size in the UI in case the height map loaded in is bigger (if it's smaller it won't look good)

            if(GUILayout.Button("Load Texture")) //if apply button is pressed 
            {
                terrain.ProcessHeightMap(); //call corresponding function
            }
        }

        //set up UI in foldout for applying fractalBrownianMotion function
        perlinNoiseEnabled = EditorGUILayout.Foldout(perlinNoiseEnabled, "Fractal Brownian Motion - Perlin Noise");
        if (perlinNoiseEnabled) //open if clicked
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider); //horizontal separation
            GUILayout.Label("Perlin Noise", EditorStyles.boldLabel); //text label
            EditorGUILayout.Slider(perlinFrequency, 0, 1, new GUIContent("Frequency")); //allows to adjust frequency values (from 0 to 1) 
            EditorGUILayout.Slider(perlinAmplitude, 0, 1, new GUIContent("Amplitude")); //allows to adjust amplitude values (from 0 to 1)
            //for intSlider -> increment the offsets in single int values as the coordinates for the heightmap are also in integers; 
            EditorGUILayout.IntSlider(perlinFrequencyOffset, 0, 10000, new GUIContent("Offset Frequency")); //allows to adjust amplitude offset values
            EditorGUILayout.IntSlider(perlinAmplitudeOffset, 0, 10000, new GUIContent("Offset Amplitude")); //allows to adjust amplitude offset values; adjusting the offset can result in various different terrain structures
            EditorGUILayout.IntSlider(octaves, 1, 10, new GUIContent("Octaves")); //adjust how many Perlin Noise curves (values) are created and combined
            EditorGUILayout.Slider(persistance, 0.1f, 10, new GUIContent("Persistance")); //to adjust the amount of change between each octave
            EditorGUILayout.Slider(fractalBrownianMotionSize, 0, 1, new GUIContent("Scale")); //to adjust the size

            if (GUILayout.Button("Apply fractal Brownian Motion")) //if apply button is pressed 
            {
                terrain.ApplyPerlinNoise(); //call corresponding function
            }
        }

        //set up UI in foldout for applying multipleFractalBrownianMotion function
        multipleFractalBrownianMotionEnabled = EditorGUILayout.Foldout(multipleFractalBrownianMotionEnabled, "Multiple Fractal Brownian Motion (BUG)");
        if (multipleFractalBrownianMotionEnabled) //open if clicked
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider); //horizontal separation
            GUILayout.Label("Multiple Fractal Brownian Motion", EditorStyles.boldLabel); //text label
            //create table to store list of fractalBrownianMotionParams
            fractalBrownianMotionParamsTable = GUITableLayout.DrawTable(fractalBrownianMotionParamsTable, fractalBrownianMotionParams);
            EditorGUILayout.BeginHorizontal(); //adding plus/minus buttons next to each other in one block/cell
            if (GUILayout.Button("+")) //pressing plus adds a FractalBrownianMotion with its params to adjust
            {
                terrain.addFractalBrownianMotion();
            } 
            if (GUILayout.Button("-")) //pressing minus removes the existing FractalBrownianMotion in the list (if there's more than one)
            {
                terrain.removeFractalBrownianMotion(); 
            } 
            EditorGUILayout.EndHorizontal(); //ensuring only buttons are added next to one another

            if (GUILayout.Button("Apply Multiple Fractal Brownian Motion")) //if apply button is pressed 
            {
                terrain.ApplyMultiplePerlinNoise(); //call corresponding function
            }
            
        }

        //set up UI in foldout for applying voronoiTesselation function
        voronoiTessellationEnabled = EditorGUILayout.Foldout(voronoiTessellationEnabled, "Voronoi Tessellation"); 
        if (voronoiTessellationEnabled) //open if clicked
        {

            EditorGUILayout.IntSlider(voronoiNumberOfSeeds, 1, 10, new GUIContent("Number of Seeds")); //to determine how many seeds are added onto the terrain (how many mountains)
            EditorGUILayout.Slider(voronoiTessellationSlopeFallOff, 0, 10, new GUIContent("Slope Fall Off")); //adjust slope fall off
            EditorGUILayout.Slider(voronoiTessellationSlopeDropOff, 0, 10, new GUIContent("Slope Drop Off")); //adjust slope drop off
            EditorGUILayout.Slider(voronoiTessellationMinHeight, 0, 1, new GUIContent("Max Height")); //determine the highest point seeds can be
            EditorGUILayout.Slider(voronoiTessellationMaxHeight, 0, 1, new GUIContent("Min Height")); //determine the lowest point seeds can be
            EditorGUILayout.PropertyField(voronoiTessellationAlgorithmType); //determine the type of algorithm that will be used


            if (GUILayout.Button("Apply Voronoi")) //if apply button is pressed 
            {
                terrain.ApplyVoronoiTessellation(); //call corresponding function
            }
        }

        //set up UI in foldout for applying diamond square function
        diamondSquareEnabled = EditorGUILayout.Foldout(diamondSquareEnabled, "Diamond Square"); 
        if (diamondSquareEnabled) //open if clicked
        {

            EditorGUILayout.PropertyField(diamondSquareMinHeight);
            EditorGUILayout.PropertyField(diamondSquareMaxHeight);
            EditorGUILayout.PropertyField(diamondSquareHeightControlPower);
            EditorGUILayout.PropertyField(diamondSquareRoughnessControl);

            if (GUILayout.Button("Apply Diamond Square")) //if apply button is pressed 
            {
                terrain.ApplyDiamondSquare(); //call corresponding function
            }
        }

        //set up UI in foldout for applying textures 
        splatEnabled = EditorGUILayout.Foldout(splatEnabled, "Splat Texturing"); 
        if (splatEnabled) //open if clicked
        {
            
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider); //horizontal separation
            GUILayout.Label("Splat Map", EditorStyles.boldLabel); //text label

            //create table to store list of splatsByAltitude
            splatMapPropetiesTable = GUITableLayout.DrawTable(splatMapPropetiesTable, splatsByAltitude);

            EditorGUILayout.BeginHorizontal(); //adding plus/minus buttons next to each other in one block/cell
            if (GUILayout.Button("+")) //pressing plus adds a FractalBrownianMotion with its params to adjust
            {
                terrain.addSplatMap();
            }
            if (GUILayout.Button("-")) //pressing minus removes the existing FractalBrownianMotion in the list (if there's more than one)
            {
                terrain.removeSplatMap();
            }
            EditorGUILayout.EndHorizontal(); //ensuring only buttons are added next to one another
            

            if (GUILayout.Button("Apply Textures")) //if apply button is pressed 
            {
                terrain.ApplySplatTexturing(); //call corresponding function
            }
        }

        //set up UI in foldout for generating vegetation
        vegetationEnabled = EditorGUILayout.Foldout(vegetationEnabled, "Vegetation (WORKING PROGRESS)"); //if foldout clicked
        if (vegetationEnabled)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider); //horizontal separation
            GUILayout.Label("Vegetation", EditorStyles.boldLabel); //text label
            EditorGUILayout.IntSlider(maxVegetation, 1, 10000, new GUIContent("Trees Max")); //to determine how many trees or vegetation to generate
            EditorGUILayout.IntSlider(vegetationSpacing, 1, 20, new GUIContent("Trees Spacing")); //to determine how much space to have between trees or vegetation

            //create table to store list of splatsByAltitude
            vegetationsMapTable = GUITableLayout.DrawTable(vegetationsMapTable, vegetations);

            EditorGUILayout.BeginHorizontal(); //adding plus/minus buttons next to each other in one block/cell
            if (GUILayout.Button("+")) //pressing plus adds a Vegetation type with its params to adjust
            {
                terrain.AddNewVegetation();
            }
            if (GUILayout.Button("-")) //pressing minus removes the existing Vegetation objects in the list (if there's more than one)
            {
                terrain.RemoveVegetation();
            }
            EditorGUILayout.EndHorizontal(); //ensuring only buttons are added next to one another


            if (GUILayout.Button("Apply Vegetation")) //if apply button is pressed 
            {
                terrain.ApplyVegetation(); //call corresponding function
            }

        }

            //set up UI in foldout for applying smoothing function
        smoothEnabled = EditorGUILayout.Foldout(smoothEnabled, "Smooth Function"); //if foldout clicked
        if (smoothEnabled)
        {
            EditorGUILayout.IntSlider(smoothIterationCount, 1, 10, new GUIContent("Smoothing Strength")); //to determine how many iteration of the smooth function is called

            if (GUILayout.Button("Apply Smooth"))//if apply button is pressed
            {
                terrain.ApplySmooth(); //call corresponding function
            }

        }

        EditorGUILayout.LabelField("", GUI.skin.horizontalScrollbar); //putting a line to separate the sections in the UI

        //creating a button for resetting the terain here
        if (GUILayout.Button("Reset Terrain")) //if apply button is pressed 
        {
            terrain.ResetTerrain(); //call corresponding function
        }

        showHeightMapEnabled = EditorGUILayout.Foldout(showHeightMapEnabled, "Display Terrain HeightMap"); //if foldout clicked
        if (showHeightMapEnabled)
        {
            //set up UI 
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            int heightMapTextureSize = (int)(EditorGUIUtility.currentViewWidth - 100);
            GUILayout.Label(heightMapTexture, GUILayout.Width(heightMapTextureSize), GUILayout.Height(heightMapTextureSize));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            //on refresh, process and load the heightmap
            if(GUILayout.Button("Refresh", GUILayout.Width(heightMapTextureSize)))
            {
                float[,] heightMap = terrain.tData.GetHeights(0, 0, terrain.tData.heightmapWidth, terrain.tData.heightmapHeight); //get heightmap data

                //go through all points
                for(int h = 0; h < terrain.tData.heightmapHeight; h++)
                {
                    for(int w = 0; w < terrain.tData.heightmapWidth; w++)
                    {
                        heightMapTexture.SetPixel(w, h, new Color(heightMap[w, h], heightMap[w, h], heightMap[w, h], 1));  //set the pixels for all channels and 1 for alpha
                    }
                }

                heightMapTexture.Apply(); //apply changes
            }

            //additional UI setup
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        //CLEAN-UP FOR SCROLLBAR CODE
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties(); //applies any changes that are made (changes applied instantly in display loop)
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

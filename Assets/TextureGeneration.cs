using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class TextureGeneration : EditorWindow
{
    //gloabl variables mainly consisting of paramters that will be included in the window for adjusting and changing from the window 
    string filename = "procedurallyGenerateTexture"; //default generated file name
    float perlinWidthSize = 0.001f; float perlinHeightSize = 0.001f; int perlinOcatves = 7; float perlinPersistance = 1.92f; float perlinSize; int perlinWidthOffset; int perlinHeightOffset; //setting up variables for Brownian Motion method as done in the TerrainS script previously
    bool toggleAlpha = false; bool toggleSeamless; bool toggleIntensityRemap; //bool values to toggle between different options for the textures

    //brightness and contrast values to modify with sliders later
    float brightness = 0.05f;
    float contrast = 0.05f;

    Texture2D generatedTexture; //will store the texture

    [MenuItem("Window/TextureGeneration")] //set up menu item using string command to add item in Window menu with the name Texture Generation
    public static void ShowWindow() //when the window set up above is clicked, it will call this function
    {
        EditorWindow.GetWindow(typeof(TextureGeneration)); //creating a new editor window that is of type the current script
    }

    private void OnEnable() //runs whenever this script is recompiled
    {
        generatedTexture = new Texture2D(513, 513, TextureFormat.ARGB32, false); //set up texture and assign it to variable (size set to be compatible with terrain) //pTexture
    }

    private void OnGUI()
    {
        //setting up the window 
        GUILayout.Label("Settings", EditorStyles.boldLabel); //label
        filename = EditorGUILayout.TextField("Texture File Name", filename); //filename field

        int windowWidthSize = (int)(EditorGUIUtility.currentViewWidth - 100); //get window width size to help fit texture to display (-100 for padding on each side)

        //adding adjustable paramters with range of possible values to adjust to
        perlinWidthSize = EditorGUILayout.Slider("Width Scale", perlinWidthSize, 0, 0.1f);
        perlinHeightSize = EditorGUILayout.Slider("Height Scale", perlinHeightSize, 0, 0.1f);
        perlinOcatves = EditorGUILayout.IntSlider("Octaves Amount", perlinOcatves, 1, 10);
        perlinPersistance = EditorGUILayout.Slider("Persistance", perlinPersistance, 1, 10);
        perlinSize = EditorGUILayout.Slider("Depth Scale", perlinSize, 0, 1);
        perlinWidthOffset = EditorGUILayout.IntSlider("Width Offset", perlinWidthOffset, 0, 1000);
        perlinHeightOffset = EditorGUILayout.IntSlider("Height Offset", perlinHeightOffset, 0, 1000);
        brightness = EditorGUILayout.Slider("Brightness", brightness, 0, 2);
        contrast = EditorGUILayout.Slider("Contrast", contrast, 0, 2);

        //adding toggles 
        toggleAlpha = EditorGUILayout.Toggle("Alpha", toggleAlpha); //turn on and off transparency
        toggleIntensityRemap = EditorGUILayout.Toggle("Remap", toggleIntensityRemap); //remap values to between zero and one
        toggleSeamless = EditorGUILayout.Toggle("Seamless", toggleSeamless); //create a texture that it seamless and will fit together with copies of itself when tiled

        //to center generate button in window
        GUILayout.BeginHorizontal(); 
        GUILayout.FlexibleSpace();

        //required to map pixel values and will be kept track of when creating the pixel values and colors
        float minColor = 1;
        float maxColor = 0;

        if(GUILayout.Button("Generate", GUILayout.Width(windowWidthSize)))
        {
            //Generation function is here

            int width = 513; //set width and height values
            int height = 513; 
            float pixelValue; //will store the value for the pixel generate by Perlin Noise waveforms (functions)
            Color pixelColor = Color.white; //pixel color set initially to white but will change depending on the pixel value calculated
            //nested for loop to access all pixels in image
            for(int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (toggleSeamless) //if seamless toggled
                    {
                        //how far across the texture the current x and y values are (values normalized between 0 and 1 to act as percentages) which determine how much to blend perlin values 
                        float xPercentage = (float)x / (float)width;
                        float yPercentage = (float)y / (float)height;

                        //work out four fractal Brownian Motion function (Stacked Perlin Waveforms) values at four locations 
                        //four locations for pixel value value in current tile, tile to the right, tile above, and positive diaganol tile. Together these determine how much to blend values to get a seamless result
                        float noiseZeroZero = ComputeFractalBrownianMotion((x + perlinWidthOffset) * perlinWidthSize, (y + perlinHeightOffset) * perlinHeightSize, perlinOcatves, perlinPersistance) * perlinSize;
                        float noiseZeroOne = ComputeFractalBrownianMotion((x + perlinWidthOffset) * perlinWidthSize, (y + perlinHeightOffset + height) * perlinHeightSize, perlinOcatves, perlinPersistance) * perlinSize;
                        float noiseOneZero = ComputeFractalBrownianMotion((x + perlinWidthOffset + width) * perlinWidthSize, (y + perlinHeightOffset) * perlinHeightSize, perlinOcatves, perlinPersistance) * perlinSize;
                        float noiseOneOne = ComputeFractalBrownianMotion((x + perlinWidthOffset) * perlinWidthSize, (y + perlinHeightOffset + height) * perlinHeightSize, perlinOcatves, perlinPersistance) * perlinSize;

                        //sum the values after scaling each one based on the x and y percentage values depending on their locations
                        float noiseTotal = (xPercentage * yPercentage * noiseZeroZero) + (xPercentage * (1 - yPercentage) * noiseZeroOne) + ((1 - xPercentage) * yPercentage * noiseOneZero) + ((1 - xPercentage) * (1 - yPercentage) * noiseOneOne);

                        //split the total into rgb values and use these values to find the greyscale values rather than having values between 0 and 1
                        float v = (int)(256 * noiseTotal) + 50; //+50 here is a random offset; similarly the values below also have random offsets set for them after tuning 
                        float r = Mathf.Clamp((int)noiseZeroZero, 0, 255); //red channel value
                        float g = Mathf.Clamp(v, 0, 255);  //green channel value
                        float b = Mathf.Clamp(v + 50, 0, 255); //blue channel valuye
                        float a = Mathf.Clamp(v + 100, 0, 255); //alpha channel value 

                        //from average of values to a greyscale value
                        pixelValue = (r + g + b) / (3 * 255.0f); 
                        
                    }
                    else
                    {
                        pixelValue = ComputeFractalBrownianMotion((x + perlinWidthOffset) * perlinWidthSize, (y + perlinHeightOffset) * perlinHeightSize, perlinOcatves, perlinPersistance) * perlinSize; //for each pixel, computer fractal Brownian Motion Perlin Noise value
                        
                    }

                    float colorValue = contrast * (pixelValue - 0.5f) + 0.5f * brightness; //set color value to the pixel (pixel contrast and brightness integrated here)

                    //when exiting the loop above, min and max color will be extreme ranges of those greyscale colors being generated, the values are fixed in to the bounds of the range to be used for mapping
                    if (minColor > colorValue){ minColor = colorValue; }
                    if (maxColor < colorValue){ maxColor = colorValue; }

                    pixelColor = new Color(colorValue, colorValue, colorValue, toggleAlpha ? colorValue : 1); //set pixel color to color value in each of RGB channels for a greyscaled result and the alpha value will be 0 or 1 based on toggle
                    generatedTexture.SetPixel(x, y, pixelColor); //set color back to corresponding pixel in texture
                }
            }
            if (toggleIntensityRemap) //if map is toggled
            {
                //nested for loop to access all points in 2D space
                for(int y = 0; y < height; y++)
                {
                    for(int x = 0; x < width; x++)
                    {
                        pixelColor = generatedTexture.GetPixel(x,y); //get pixel color
                        float colorValue = pixelColor.r; //store the color value (all channels should be the same so it doesn't matter what color channel we acces at greyscale)
                        colorValue = Map(colorValue, minColor, maxColor, 0, 1); //mapping
                        //apply to all pixel color channels
                        pixelColor.r = colorValue; 
                        pixelColor.g = colorValue;
                        pixelColor.b = colorValue;
                        generatedTexture.SetPixel(x, y, pixelColor); // then apply back to main texture being generated
                    }
                }
            }
            
            generatedTexture.Apply(false, false); //don't need mip map or readable
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        //to center texture in window
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label(generatedTexture, GUILayout.Width(windowWidthSize), GUILayout.Height(windowWidthSize));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        //to center save button in window
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Save", GUILayout.Width(windowWidthSize))) //if save button is pressed
        {
            byte[] bytes = generatedTexture.EncodeToPNG(); //create array of bytes from generated texture
            System.IO.Directory.CreateDirectory(Application.dataPath + "/SavedTextures"); //create directory to put the saved texture in (which should be in the assets folder)
            File.WriteAllBytes(Application.dataPath + "/SavedTextures/" + filename + ".png", bytes); //put the generated texture with the given name in the directory created
            //Need to manually enable read/write for these
            //Also if not given a new name, it will save over the one with the same name
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

    }

    public float ComputeFractalBrownianMotion(float width, float height, int octaves, float persistance) //this function will return a value that can be used in the heightmap to affect a vertex
    { //function takes in the width and height coordinate value on heightmap, the octaves which correspond to the number of curves, the persistance value which corresponds to the change between curves

        float sum = 0; //total count for height value being calculated
        float maximum = 0; //maximum count which adds up the amplitude that is used for each octave; used as mean value when returing the result to bring value back down to range between 0 and 1 
        float frequency = 1; //how close the waves are togther in the curve
        float amplitude = 1; //how high or low the waves can be within the curve

        //algorithm based on fractal Brownian Motion
        for (int i = 0; i < octaves; i++) //loop through for each octave
        {
            //using Unity's built in Perlin Noise function
            sum += Mathf.PerlinNoise(width * frequency, height * frequency) * amplitude; //generate Perlin Noise curve; formula is different to follow the formula of fractal Brownian Motion; sum is added up every loop
            maximum += amplitude; //increase max value by amplitude to keep track of count 
            amplitude *= persistance; //to modify whatever the inital amplitude value was so that every successive Perlin Noise value computed is changed (usually to smaller value for a more natural result)
            frequency *= 2; //doubles frequency (can be changed for different effect, not sure if its something that should be passed in as a parameter)
        }

        return sum / maximum; //return a value for a vertex in the heightmap
    }

    //for mapping pixel values (increase or decrease intensity difference between higher and lower intensity pixels)
    public float Map(float value, float min, float max, float targetMin, float targetMax) //parameters include pixel value, its range, and a target range
    {
        return ((value - min) * (targetMax - targetMin)) / ((max - min) + targetMin); //rescale the values
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

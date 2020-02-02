using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode] //to allow the code to execute whilst in the editor
public class TerrainS : MonoBehaviour
{
    public Vector2 randomHeightRange = new Vector2(0, 0.1f); //the terrain height range that can be chosen from (obtained 0.1 from testing)
    public Texture2D heightMapImg; //will the heightmap images which should be in greyscale
    public Vector3 heightMapSize = new Vector3(1, 1, 1); //to change the size of the heightmap here without changing the resolution. Default size will fit the terrain.

    public bool resetBeforeApply = true; //to determine wether to apply the map data onto the existing terrain data or to reset the terrain before applying the map data 

    //width (x) and height (y) scaling values for perlin noise to determine frequency and amplitude (default values obtained from testing)
    public float perlinFrequency = 0.01f;
    public float perlinAmplitude = 0.01f;
    //the offset values help remove symmetry that may look unnatural in a terrain with equal width and and height
    public int perlinFrequencyOffset = 0; //x offset
    public int perlinAmplitudeOffset = 0; //y offset
    //These variables are for Fractal Brownian Motion
    public int octaves = 3; //to determine how many Perlin Noise values (waves) to produce for the fractal Brownian Motion function
    public float persistance = 8; //to determine the amount of change between each of the perlin noise values produced (8 means it will increase amplitude per octave)
    public float fractalBrownianMotionSize = 0.07f; //to determine and adjust the scale of the result produced by the fractalBrownianMotion function

    public float voronoiTessellationSlopeFallOff = 0.2f; //determine how steep the slope will be between vertices
    public float voronoiTessellationSlopeDropOff = 0.6f; //to determine the rate of change of the slope fall off value between vertices (bend)
    public float voronoiTessellationMinHeight = 0.1f; // maximum height seed vertices can be
    public float voronoiTessellationMaxHeight = 0.5f; // min height seed vertices can be
    public int voronoiNumberOfSeeds = 5; //number of seeds added to the terrain through Voronoi Tesselation implemented algorithm
    public enum VoronoiTessellationAlgorithmType {Linear = 0, Exponential = 1, Combined = 2, ExponentialWithSin = 4} //to be able to choose between different formulae for the algorithm 
    public VoronoiTessellationAlgorithmType voronoiTessellationAlgorithmType = VoronoiTessellationAlgorithmType.Linear; //set it to use linear algorithm initially, and to allow to access the types

    public float diamondSquareMinHeight = -2f; //determine min height vertices can be modified to
    public float diamondSquareMaxHeight = 2f; //determine max height vertices can reach
    public float diamondSquareHeightControlPower = 2.0f; //determines how jagged or smooth the surfaces will be when Diamond Square algorithm is applied
    public float diamondSquareRoughnessControl = 2.0f;  //adjusts effect of damping value that will continually reduce the height when modifying vertices of smaller and smaller areas 

    public int smoothIterationCount = 1; //to determine how many times to apply the smooth function to the whole terrain


    //Embedding a class to represent the paramters for a fractal Brownian function within this one to allow for multiple of them to be applied to the map through the UI
    [System.Serializable]
    public class FractalBrownianMotionParams
    {
        //all the same paramters used for the fractal Brownian Function to allow for stacking functions to apply to the heightmap
        public float anotherPerlinFrequency = 0.01f;
        public float anotherPerlinAmplitude = 0.01f;
        public int anotherPerlinOctaves = 3;
        public float anotherPersistace = 8;
        public float anotherFractalBrownianMotionSize = 0.07f;
        public int anotherPerlinFrequencyOffset = 0;
        public int anotherPerlinAmplitudeOffset = 0;

        public bool remove = false; //to be able to remove additional function in the UI if it has been added
    }

    //create a list of the embedded class type so that it is possible to have several fractal Brownian Motion functions stacked with their parameters independently accessible
    public List<FractalBrownianMotionParams> fractalBrownianMotionParams = new List<FractalBrownianMotionParams>()
    {
        new FractalBrownianMotionParams() //needs to have at least one so that the GUI table is not empty (rule based on "Editor GUI Table" library) 
    };

    //Embedding another class to represent the paramters for the texture splats to allow for multiple textures to be applied to the map dteremined by specified heights through the UI
    [System.Serializable]
    public class SplatByAltitudes
    {
        public Texture2D texture = null; //to store the texture that be assinged
        //setting the maximum and minimum heights these textures will be applied to in the terrain
        public float minHeight = 0.1f;
        public float maxHeight = 0.2f;

        //steepness range given to textures to determine where they appear based on steepness of terrain
        public float minSteepness = 0f;
        public float maxSteepness = 90f; //from 0 to 90 ensures the texture can appear on any slope no matter the steepness

        //variables that will be used to directly modify the tiling settings paratmers of a terrain layer
        public Vector2 tileOffset = new Vector2(0,0); //how much to offset texture in each tile
        public Vector2 tileSize = new Vector2(50, 50); //the size is set here as the default size is too small and makes the tiling look obvious

        public float splatTexturingOffset = 0.1f; //height offset when applying textures
        public float splatTexturingNoiseWidthSize = 0.01f; //strength of noise effect in the x directions
        public float splatTexturingNoiseDepthSize = 0.01f; //strength of noise effect in the z directions (meant to write height rather than depth)
        public float splatTexturingNioseSize = 0.1f; //scale of overall noise effect

        public bool remove = false; //to be able to remove any added Splats (textures to apply with these params) in the UI
    }

    //create a list of the embedded class type so that it is possible to have several splats stacked to apply to the terrain surface
    public List<SplatByAltitudes> splatsByAltitude = new List<SplatByAltitudes>()
    {
        new SplatByAltitudes() //needs to have at least one so that the GUI table is not empty (rule based on "Editor GUI Table" library) 
    };

    //Yet another embedded class to represent the vegetation that will be added to the terrain and its parameters
    [System.Serializable]
    public class Vegetation
    {
        public GameObject mesh; //to hold the vegetation mesh game object

        public float vegetationMinHeight = 0.1f; //max height vegetation can appear at
        public float vegetationMaxHeight = 0.1f; //min height vegetation can appear at
        public float vegetationMinSlope = 0; //min slope vegetation can be on
        public float vegetationMaxSlope = 90; //max slope vegetation can be on

        public bool remove = false; //to be able to remove this object type from the table once its row created
    }

    //create a list of the embedded class type so that it is possible to have several vegetation types stacked to apply to the terrain surface
    public List<Vegetation> vegetations = new List<Vegetation>()
    {
        new Vegetation() //needs to have at least one so that the GUI table is not empty (rule based on "Editor GUI Table" library) 
    };

    public int maxVegetation = 5000; //to determine how much of the vegetation to add to the terrain
    public int vegetationSpacing = 5; //to determine the distance between the vegetation

    public Terrain t; //stores object of type Terrain (itself)
    public TerrainData tData; //stores the terrain's data including heightmaps, detail mesh positions, tree instances and terrain texture alpha maps which will need to be accessed

    public float[,] GetHeightMap() //to get heightmap data, and that data is either a reset terrain or the data of the modified terrain depending if the terrain needs to be reset or not before applying the data to the terrain 
    {
        if (!resetBeforeApply) //if user does not want to reset terrain
        {
            return tData.GetHeights(0, 0, tData.heightmapWidth, tData.heightmapHeight); //return the data of the main terrain whether it has been modified or not
        }
        else //otherwise
        {
            return new float[tData.heightmapWidth, tData.heightmapHeight]; //return a reset and unmodified terrain data map
        }
    }

    public void ApplyVegetation() //to process procedurally generating vegetation on terrain 
    {
        TreePrototype[] newTreePrototypes; //array of tree prototypes
        newTreePrototypes = new TreePrototype[vegetations.Count]; //set size to vegetations list as thats how many is needed to be stored
        int treeIndex = 0; //keep track of each one processed
        foreach (Vegetation tree in vegetations)
        { //loop through them all
            newTreePrototypes[treeIndex] = new TreePrototype(); //create a tree prototype at each index
            newTreePrototypes[treeIndex].prefab = tree.mesh; //assign the mesh/prefab passed through
            treeIndex++; //increase index
        }
        tData.treePrototypes = newTreePrototypes; //populate the tree area in the terrain UI properly

        List<TreeInstance> allVegetation = new List<TreeInstance>(); //array of TreeInstances

        //loop through all prototypes and add them to the terrain (added only after modifications to make the TreeInstance appear)
        //first two loops to go over all world loactions relative to the terrain (not same resolution as heightmap)
        for (int z = 0; z < tData.size.z; z += vegetationSpacing) //incrementing by tree spacing to ensure trees are not too close to one another
        {
            for (int x = 0; x < tData.size.x; x += vegetationSpacing)
            {
                for (int tp = 0; tp < tData.treePrototypes.Length; tp++) //loop around each tree prototype 
                {
                    float currentHeight = tData.GetHeight(x, z) / tData.size.y; //scaling height from terrain data and normalizing to value between 0 to 1
                    //setting max and min heights vegetation can appear at
                    float currentHeightStart = vegetations[tp].vegetationMinHeight; 
                    float currentHeightEnd = vegetations[tp].vegetationMaxSlope;

                    //only add trees between specified heights
                    if(currentHeight >= currentHeightStart && currentHeight <= currentHeightEnd)
                    {
                        TreeInstance i = new TreeInstance(); //for each tree put on the terrain and instance i is created for it
                        i.position = new Vector3((x + UnityEngine.Random.Range(-5.0f, 5.0f)) / tData.size.x, tData.GetHeight(x, z) / tData.size.y, (z + UnityEngine.Random.Range(-5.0f, 5.0f)) / tData.size.z); //set the position of the tree instance with randomized values

                        i.rotation = UnityEngine.Random.Range(0, 360); //set instance rotation
                        i.prototypeIndex = tp; //set instance prototype index
                        i.color = Color.white; //set instance color to make it show up
                        i.lightmapColor = Color.white; //set instance leightmap color to make it show up
                                                       //set height and width scales on instance
                        i.heightScale = 0.95f;
                        i.widthScale = 0.95f;

                        allVegetation.Add(i); //all the instance to the created TreeInstance list
                        if (allVegetation.Count >= maxVegetation) goto JUMPPOINTONE; //to ensure not too many trees are added
                    }
                }
            }
        }

        JUMPPOINTONE:
            tData.treeInstances = allVegetation.ToArray(); //apply back to terrain
    }

    public void AddNewVegetation() //to add new vegetation objects to the UI table
    {
        vegetations.Add(new Vegetation());
    }

    public void RemoveVegetation()
    {
        List<Vegetation> tempList = new List<Vegetation>(); //create an empty temporary list

        for (int i = 0; i < vegetations.Count; i++) //loop over the original list
        {
            if (!vegetations[i].remove) //any that user does not want to be removed (based on bool)
            {
                tempList.Add(vegetations[i]); //are added to the temporary list
            }
        }
        if (tempList.Count == 0) //if the temporary list is empty
        {
            tempList.Add(vegetations[0]); //make sure there is at least one item in the list or GUI Table Editor library/function will complain
        }

        vegetations = tempList; //set the original to the temporary list so that any items that are not added are removed instead
    }

    public List<Vector2> GetVertexNeighbours(Vector2 position, int areaWidth, int areaHeight)
    {
        List<Vector2> l = new List<Vector2>(); //list to store the neighbouring positions

        //nested for loop to iterate through the eight neighbouring positions that surround a vertex (go from -1 to +1 in the x and y directions of the map)
        for(int h = -1; h < 2; h++)
        {
            for(int w = -1; w < 2; w++)
            {
                if(!(w == 0 && h == 0)) //make sure to not include the position of the vertex itself, and only include its neighbours
                {
                    //add the neighbour positiions to the list 
                    //clamp values as vertices on the edges do no have eight neighbours
                    Vector2 vertexPosition = new Vector2(Mathf.Clamp(position.x + w, 0, areaWidth - 1), Mathf.Clamp(position.y + h, 0, areaHeight - 1)); 

                    //clamping values above causes some of the positions to be stores more than one so...
                    if (!l.Contains(vertexPosition)) //only add the neighbour position if it does not already exist in the list
                    {
                        l.Add(vertexPosition);
                    }
                }
            }
        }
        return l;
    }

    public void addSplatMap() //add a new item of type SplatByAtltitude in the list with the default values for the params
    {
        splatsByAltitude.Add(new SplatByAltitudes());
    }

    public void removeSplatMap() //remove the specified SpaltByAltitude type from the list depending...
    {
        List<SplatByAltitudes> tempList = new List<SplatByAltitudes>(); //create an empty temporary list

        for (int i = 0; i < splatsByAltitude.Count; i++) //loop over the original list
        {
            if (!splatsByAltitude[i].remove) //any that user does not want to be removed (based on bool)
            {
                tempList.Add(splatsByAltitude[i]); //are added to the temporary list
            }
        }
        if (tempList.Count == 0) //if the temporary list is empty
        {
            tempList.Add(splatsByAltitude[0]); //make sure there is at least one item in the list or GUI Table Editor library/function will complain
        }

        splatsByAltitude = tempList; //set the original to the temporary list so that any items that are not added are removed instead
    }

    //Unity has a getSteepness method but the point was to implement one to understand the fundamentals (this will not be used anymore though)
    public float GetVertexSteepness(float[,] heightmap, int wCoordinate, int hCoordinate, int width, int height) //will return steepness value based on comparison of two intersecting neighbours (passing in the heightmap, the position of the vertex, and the height and width values of the terrain)
    {
        float h = heightmap[wCoordinate, hCoordinate]; //get vertex data of which we want to compute the steepness value for

        //work out neighbours in x and y directions (y being z in Unity)
        int upWCoordinate = wCoordinate + 1;
        int upHCoordinate = hCoordinate + 1;

        //texture is seamless so if on the upper edge of the map, used the vertices wrapping around the area  
        if (upWCoordinate > width - 1) { upWCoordinate = wCoordinate - 1; }
        if (upHCoordinate > height - 1) { upHCoordinate = hCoordinate - 1; }

        //work out the differences using the neighbours
        float differenceOfWCoordinates = heightmap[upWCoordinate, hCoordinate] - h;
        float differenceOfHCoordinates = heightmap[wCoordinate, upHCoordinate] - h;

        Vector2 gradient = new Vector2(differenceOfWCoordinates, differenceOfHCoordinates); //gradient is vector made up of differences. 

        float steepness = gradient.magnitude; //the magnitude of the vector (values within) determine the value of that vertex; this uses Pythagoras Theorem

        return steepness; //value is returned (turns out to be between 0 and just slightly above 1.4)
    }

    public void ApplySplatTexturing()
    {
        TerrainLayer[] newTextures; //store textures being read in to add to the terrain
        newTextures = new TerrainLayer[splatsByAltitude.Count]; //initialize newTextures with the same size as splatsByAltitude list
        int textureIndex = 0; //to assign an index to each texture added 

        foreach(SplatByAltitudes s in splatsByAltitude) //loop through all the textures in the splatsByAltitude
        {
            //add them to the newTextures list and set their particular properties using TerrainLayer built in functions and the parameters set for the splatsByAltitude item
            newTextures[textureIndex] = new TerrainLayer(); 
            newTextures[textureIndex].diffuseTexture = s.texture; //set texture of the new one created to the be the texture added in for the heightmap
            newTextures[textureIndex].tileOffset = s.tileOffset; 
            newTextures[textureIndex].tileSize = s.tileSize;

            newTextures[textureIndex].diffuseTexture.Apply(true); //required to add all pixel values

            textureIndex++; //increment index so the next one added has a different index
        }
        tData.terrainLayers = newTextures; //set the terrain layer (surface) data to the new array with our textures in it to apply them back into the terrain

        float[,] heightMap = tData.GetHeights(0, 0, tData.heightmapWidth, tData.heightmapHeight); //get access to the terrain's heightmap data
        float[,,] splatMap = new float[tData.alphamapWidth, tData.alphamapHeight, tData.alphamapLayers]; //get access to the terrain's splatmap data

        //loop through every position in the splatMap and set a value for the textures on the layer that corresponds to the particular texture for that layer
        //for each position in the array, work out how much of each texture should be applied there -> which is determined by the layers
        //nexted for loop to access all positions of splat maps
        for (int h = 0; h < tData.alphamapHeight; h++) 
        {
            for(int w = 0; w < tData.alphamapWidth; w++)
            {
                float[] textureSplat = new float[tData.alphamapLayers]; //single column splat array to represent the layers and its size will be set to the number of layers 

                for(int t = 0; t < splatsByAltitude.Count; t++) //loop through all the textures that have been added
                {
                    //offset values for overlapping textures and offsetting the heights randomly. These will help with blending (case for the every single splat map)
                    float noise = Mathf.PerlinNoise(w * splatsByAltitude[t].splatTexturingNoiseWidthSize, h * splatsByAltitude[t].splatTexturingNoiseDepthSize) * splatsByAltitude[t].splatTexturingNioseSize; //to reiterate, depth is meant as height
                    float offset = splatsByAltitude[t].splatTexturingOffset + noise; 

                    //determine where the heights start and end for these textures with offset taken into consideration to force it to overlap (based on what user inputs through UI)
                    float currentHeightStartPosition = splatsByAltitude[t].minHeight - offset;
                    float currentHeightEndPosition = splatsByAltitude[t].maxHeight + offset;

                    //Unity's steepness function for terrains is used to get the vertex steepness. //Alpha and height maps are 90 degrees to one another, so to use positions in the splat map based on the heightmap, should switch the x and y values as well as the height and width values
                    float vertexSteepness = tData.GetSteepness(h / (float)tData.alphamapHeight, w / (float)tData.alphamapWidth); //the funtion only takes values from 0 to 1, so the values have been normalized; casting to float to get more accurate values

                    //if the current heightmap value is between the start and end heights               //and if steepness is within the slope range specified
                    if((heightMap[w,h] >= currentHeightStartPosition && heightMap[w, h] <= currentHeightEndPosition) && (vertexSteepness >= splatsByAltitude[t].minSteepness && vertexSteepness <= splatsByAltitude[t].maxSteepness))
                    {
                        textureSplat[t] = 1; //then set the splat value for that particular texture to 1 (texture will appear at that spot)
                    }
                }
                //the values of a column in a SplatMap have to add up to one as the normalized values set are from 0 to 1 on the maps
                Normalize(textureSplat); //values in the texture splat do not add up to one, so they have to be normalized to make it possible for several to be blended together and add up to 1 

                for(int c = 0; c < splatsByAltitude.Count; c++) 
                {
                    splatMap[w,h,c] = textureSplat[c]; //give the layer data for all layers
                }
            }
        }
        tData.SetAlphamaps(0,0, splatMap); //apply splat map back to terrain 
    }

    public void Normalize(float[] toNormalize) //normalize floats in given array
    {
        float sum = 0; //initialize sum variable

        for(int v = 0; v < toNormalize.Length; v++) //work out sum of all values in the array
        {
            sum += toNormalize[v];
        }

        for (int v = 0; v < toNormalize.Length; v++) //divide each value by the sum and update the array
        {
            toNormalize[v] /= sum;
        }
    }

    public void ApplySmooth() //Smoothing function to remove sharp peaks and reduce roughness
    {
        float[,] heightmap = tData.GetHeights(0,0, tData.heightmapWidth, tData.heightmapHeight); //get terrian heightmap directly without potentially ressetting it

        //Increasing the number of iterations to apply smoothing function to terrain several times takes some time to compute when applying the data to the terrain, so a progress bar is displayed
        float functionProgress = 0;
        EditorUtility.DisplayProgressBar("Smooth Function", "Loading", functionProgress);

        for(int smoothingIteration = 0; smoothingIteration < smoothIterationCount; smoothingIteration++) //determine how many times to apply smooth function to the terrain
        {
            //nested for loop to iterate through every vertex position in the map
            for (int h = 0; h < tData.heightmapHeight; h++)
            {
                for (int w = 0; w < tData.heightmapWidth; w++)
                {

                    float heightsSum = heightmap[w, h]; //add height of current position (get neighbour function does not include current vertex)
                    List<Vector2> neighbouringPositions = GetVertexNeighbours(new Vector2(w, h), tData.heightmapWidth, tData.heightmapHeight); //call function to get all of the current vertex's neighbours

                    foreach (Vector2 position in neighbouringPositions)
                    {
                        heightsSum += heightmap[(int)position.x, (int)position.y]; //add up all the heights of the neighbours
                    }

                    heightmap[w, h] = heightsSum / ((float)neighbouringPositions.Count + 1); //put the sum in the position of the current vertex and divide it by the number of neighbours including the current vertex to get the average of heights 
                }
            }
            //to update progress bar determined by how many times smoothing is meant to be applied
            functionProgress++;     EditorUtility.DisplayProgressBar("Smooth Function", "Loading", functionProgress/ smoothIterationCount);
        }

        tData.SetHeights(0, 0, heightmap);
        EditorUtility.ClearProgressBar(); //remove progress bar when function is complete
    }

    public void ApplyDiamondSquare() //Diamond Square function for more realsitic looking terrain (can be combined with other functions for more interesting results)
    {
        float[,] heightmap = GetHeightMap(); //get terrain heightmap
        int width = tData.heightmapWidth - 1; //get width of map; -1 as it needs to be even for Diamond Square algorithm (width and height of square map are equal)
        int areaSize = width; //specifying the size of the square area in which the vertex are being modified
        float minHeight = diamondSquareMinHeight; //determine min height vertices can be modified to
        float maxHeight = diamondSquareMaxHeight; //determine max height vertices can reach
        float heightControl = (float)Mathf.Pow(diamondSquareHeightControlPower, -1 * diamondSquareRoughnessControl) ;  //damping value to continually reduce the height when modifying vertices of smaller and smaller areas 

        //initializing variables for algorithm to specify where each vertex is relative to midpoints or neighbours
        int cornerW; int cornerH; //coordinate points for a vertex 
        int middleW; int middleH; //coordinate points for a midpoint vertex  
        int leftNeighbour; int rightNeighbour; int topNeighbour; int bottomNeighbour; //neighbour vertex (for square step stage)

        while(areaSize > 0) //to repeatedly separate the sqare area in four smaller equal (square) sections and repeat the steps for those smaller areas and so on until the whole terrain map is covered
        {
            //Diamond Step
            //looping around positions of heightmap in increments of a square size to get to the next value (one step means it jumps from corner to corner)
            for (int w = 0; w < width; w += areaSize)
            {
                for (int h = 0; h < width; h += areaSize)
                {
                    //set the values for the positions of the corners of the current area (area is square)
                    cornerW = (w + areaSize); cornerH = (h + areaSize);

                    //set values for the position of the midpoint of the area
                    middleW = (int)(w + areaSize / 2.0f); middleH = (int)(h + areaSize / 2.0f);

                    //set the height of the midpoint as the average heights of all corners with a scaling value included
                    heightmap[middleW, middleH] = (float)((heightmap[w, h] + heightmap[cornerW, h] + heightmap[w, cornerH] + heightmap[cornerW, cornerH]) / 4.0f + UnityEngine.Random.Range(minHeight, maxHeight));
                }
            }

            //Square Step
            //looping around positions of heightmap in increments of a square size to get to the next value (one step means it jumps from corner to corner)
            for (int w = 0; w < width; w += areaSize)
            {
                for (int h = 0; h < width; h += areaSize)
                {
                    //set the values for the positions of the corners of the current area (area is square)
                    cornerW = (w + areaSize); cornerH = (h + areaSize);

                    //set values for the position of the midpoint of the area
                    middleW = (int)(w + areaSize / 2.0f); middleH = (int)(h + areaSize / 2.0f);

                    //set values for positions of neighbouring vertices (neighbours of midpoint in the area reached at the current iteration)
                    rightNeighbour = (int)(middleW + areaSize);
                    topNeighbour = (int)(middleH + areaSize);
                    leftNeighbour = (int)(middleW - areaSize);
                    bottomNeighbour = (int)(middleH - areaSize);

                    //conditional here to skip over process at current iteration when neighboors for a vertex do not exit (processing vertices on the edges) to avoid out of index issues
                    if (leftNeighbour <= 0 || bottomNeighbour <= 0 || rightNeighbour >= width - 1 || topNeighbour >= width - 1) continue; //if any neighbour is not found, skip current iteration

                    //compute the height for bottom midpoint of the area
                    heightmap[middleW, h] = (float)((heightmap[middleW, middleH] + heightmap[w,h] + heightmap[middleW, bottomNeighbour] + heightmap[cornerW, h]) / 4.0f + UnityEngine.Random.Range(minHeight, maxHeight));

                    //compute the height for top midpoint of the area
                    heightmap[middleW, cornerH] = (float)((heightmap[w, cornerH] + heightmap[middleW, middleH] + heightmap[cornerW, cornerH] + heightmap[middleW, topNeighbour]) / 4.0f + UnityEngine.Random.Range(minHeight, maxHeight));

                    //compute the height for left midpoint of the area
                    heightmap[w, middleH] = (float)((heightmap[w, h] + heightmap[leftNeighbour, middleH] + heightmap[w, cornerH] + heightmap[middleW, middleH]) / 4.0f + UnityEngine.Random.Range(minHeight, maxHeight));

                    //compute the height for right midpoint of the area
                    heightmap[cornerW, middleH] = (float)((heightmap[cornerW, h] + heightmap[middleW, middleH] + heightmap[cornerW, cornerH] + heightmap[rightNeighbour, middleH]) / 4.0f + UnityEngine.Random.Range(minHeight, maxHeight));
                }
            }
            
            areaSize = (int)(areaSize / 2.0f); //keep diving area into smaller ones
            //damp heights every iteration as process progresses to smaller sub-areas
            minHeight *= heightControl;
            maxHeight *= heightControl;
        }
        
        tData.SetHeights(0, 0, heightmap); //apply changes to terrian
    }

    public void ApplyVoronoiTessellation()
    {
        float[,] heightMap = GetHeightMap(); //get heightmap from terrain data

        for(int s = 0; s < voronoiNumberOfSeeds; s++) //for every seed created
        {
            Vector3 seed = new Vector3(UnityEngine.Random.Range(0, tData.heightmapWidth), UnityEngine.Random.Range(voronoiTessellationMinHeight, voronoiTessellationMaxHeight), UnityEngine.Random.Range(0, tData.heightmapHeight)); //choose a random vertex location in the heightmap to be the seed/center (x is for width, y is for height, and z is for depth)

            if (heightMap[(int)seed.x, (int)seed.z] < seed.y) //if the height at the position is less than the  height of the seed being added
            {
                heightMap[(int)seed.x, (int)seed.z] = seed.y; //set the seed vertex (usings its coordinates to access it) to the height value we want at that location
            }
            else //otherwise don't add it to make sure there are no divots when a seed is added on a surface of another region (whose seed is higher)
            {
                continue; //do nothing
            }

            //take the seed and work out the heights of the vertices within its region which is determine by the height of the seed and the distances those vertices are from the seed
            Vector2 seedPosition = new Vector2(seed.x, seed.z);
            float farthestPossibleDistance = Vector2.Distance(new Vector2(0, 0), new Vector2(tData.heightmapWidth, tData.heightmapHeight)); //maximum distance a vertex could be from the seed on the terrain
                                                                                                                                            //nested for loop to access all relevant vertices on the map
            for (int h = 0; h < tData.heightmapHeight; h++)
            {
                for (int w = 0; w < tData.heightmapWidth; w++)
                {
                    if (!(w == seed.x && h == seed.z)) //to make sure the peak is not changed again or affected
                    {
                        float distanceToSeed = Vector2.Distance(seedPosition, new Vector2(w, h)) / farthestPossibleDistance; //work out the distance this vertex is away from seed (divide by max distance to make sure the result is normalised between 0 and 1)
                        float newHeight; ///store the value for the new height of the vertex at the location being iterated through

                        //Formulae to specify the heights of vertices determine by region it belongs to and its seed. It will decrease the height the farther away the vertex is from the seed.
                        //Can use a different formula to determine how steep and amount of change the slopes will be between each vertex. Choice of the formula used is determined by an enum
                        if (voronoiTessellationAlgorithmType == VoronoiTessellationAlgorithmType.Combined)
                        {
                            newHeight = seed.y - distanceToSeed * voronoiTessellationSlopeFallOff - Mathf.Pow(distanceToSeed, voronoiTessellationSlopeDropOff); //combination of linear and exponential formulae
                        }
                        else if(voronoiTessellationAlgorithmType == VoronoiTessellationAlgorithmType.Exponential)
                        {
                            newHeight = seed.y - Mathf.Pow(distanceToSeed, voronoiTessellationSlopeDropOff) * voronoiTessellationSlopeFallOff; //exponential or power algorithm
                        } 
                        else if (voronoiTessellationAlgorithmType == VoronoiTessellationAlgorithmType.ExponentialWithSin)
                        {
                            newHeight = seed.y - Mathf.Pow(distanceToSeed * 3, voronoiTessellationSlopeFallOff) - Mathf.Sin(distanceToSeed * 2 * Mathf.PI) / voronoiTessellationSlopeDropOff; //exponential algorithm that also utlizes the sin function
                        }
                        else //should be linear if its not the other two
                        {
                            newHeight = seed.y - distanceToSeed * voronoiTessellationSlopeFallOff; //linear algorithm
                        }

                        if (heightMap[w,h] < newHeight) //if the terrain does not already have the new heights generated by the Voronoi Tesselation process (to get multiple centers or peaks generated)
                        {
                            heightMap[w, h] = newHeight; //assign the vertex the height computed 
                        }
                        
                    }

                }
            }

        }

        tData.SetHeights(0, 0, heightMap); //apply it to the terrain
    }

    public void ApplyPerlinNoise() //function to apply perlin noise functions to the terrain (affects heights only) using fractal Brownian Motion algorithm (called from Utility script)
    {
        float[,] heightMap = GetHeightMap(); //get heightmap data from terrain

        for (int y = 0; y < tData.heightmapHeight; y++) //for every vertex across y axis; 
        {
            for (int w = 0; w < tData.heightmapWidth; w++) //for every vertex across x axis
            {
                //Calling the function to compute the fractal Brownian Motion. The width w and height y values are given to the function to compute the fractal Brownian Motion values for each vertex; additional parameters were given that were explain at initalization and in the function
                heightMap[w, y] += ComputeFractalBrownianMotion((w + perlinFrequencyOffset) * perlinFrequency, (y + perlinAmplitudeOffset) * perlinAmplitude, octaves, persistance) * fractalBrownianMotionSize;
                // assigning new values rather than adding to existing ones here
            }
        }

        tData.SetHeights(0, 0, heightMap); //apply changes to terrain
    }

    public void ApplyMultiplePerlinNoise() //to stack fractal Brownian Motion to have multiple groups of perlin noise functions with different parameters applied to the map
    {
        float[,] heightMap = GetHeightMap(); //get heightmap data from terrain

        for (int y = 0; y < tData.heightmapHeight; y++) //for every vertex across y axis; 
        {
            for (int w = 0; w < tData.heightmapWidth; w++) //for every vertex across x axis
            {
                foreach(FractalBrownianMotionParams fBMP in fractalBrownianMotionParams) //for each of the functions within the list, apply to the map
                {
                    //similar process to that of function above
                    heightMap[w, y] += ComputeFractalBrownianMotion((w + perlinFrequencyOffset) * perlinFrequency, (y + perlinAmplitudeOffset) * perlinAmplitude, octaves, persistance) * fractalBrownianMotionSize;
                }

            }
        }

        tData.SetHeights(0, 0, heightMap); //apply modified map to terrain
    }

    public void addFractalBrownianMotion() //add a new item of type FractalBrownianMotionParams in the list with the default params
    {
        fractalBrownianMotionParams.Add(new FractalBrownianMotionParams());
    }

    public void removeFractalBrownianMotion() //remove the specified FractalBrownianMotionParams type from the list depending...
    {
        List<FractalBrownianMotionParams> tempList = new List<FractalBrownianMotionParams>(); //create an empty temporary list

        for(int i = 0; i < fractalBrownianMotionParams.Count; i++) //loop over the original list
        {
            if (!fractalBrownianMotionParams[i].remove) //any that user does not want to be removed (based on bool)
            {
                tempList.Add(fractalBrownianMotionParams[i]); //are added to the temporary list
            }
        }
        if(tempList.Count == 0) //if the temporary list is empty
        {
            tempList.Add(fractalBrownianMotionParams[0]); //make sure there is at least one item in the list or GUI Table Editor library/function will complain
        }

        fractalBrownianMotionParams = tempList; //set the original to the temporary list so that any items that are not added are removed instead
    }

    public void ProcessRandomTerrain() //to generate random terrain
    {
        float[,] heightMap = GetHeightMap(); //will store the heightmap value of the main terrain (starting from 0,0) as a two dimensional list of floats to apply to the terrain
      
        //nested for loop to assign every value in the 2D list of the heightmap
        for (int w = 0; w < tData.heightmapWidth; w++) //for every vertex across x axis
        {
            for (int h = 0; h < tData.heightmapHeight; h++) //for every vertex across z axis
            {
                heightMap[w, h] += UnityEngine.Random.Range(randomHeightRange.x, randomHeightRange.y); //add the range random values rather than reassining them incase the terrain is edited beforehand(x and y here are to access the min and max of the range)
            }
        }
        tData.SetHeights(0, 0, heightMap); //assign height values back to the terrain starting at index (0,0) -> whole terrain 
    }

    public void ProcessHeightMap() //loads the heightmap 2D texture and applies it to the terrain
    {
        float[,] heightMap = GetHeightMap(); //initialize empty heightmap variable
        
        //nested for loop to assign every value in the 2D list of the heightmap
        for (int w = 0; w < tData.heightmapWidth; w++) //for every vertex across x axis
        {
            for (int h = 0; h < tData.heightmapHeight; h++) //for every vertex across z axis
            {
                //set the value for the height of the vertex at w,h position of the map to be the same as the intensity that is in the same position at the texture that we are giving it
                heightMap[w, h] += heightMapImg.GetPixel((int)(w * heightMapSize.x), (int)(h * heightMapSize.z)).grayscale * heightMapSize.y; //the color is converted to greyscale as mapping works best determined by intensity only
                //multiplied by height scale to be able to apply the height change
            }  

        }
        tData.SetHeights(0, 0, heightMap); //apply the modified heightmap to the terrain
    }

    public void ResetTerrain() //to reset the terrain
    {
        float[,] heightMap; //don't need to consider if the terrain has been modified or not when resetting it 
        heightMap = new float[tData.heightmapWidth, tData.heightmapHeight]; //new empty heightmap to apply to terrain

        //nested for loop to assign every value in the 2D list of the heightmap
        for (int w = 0; w < tData.heightmapWidth; w++) //for every vertex across x axis
        {
            for (int h = 0; h < tData.heightmapHeight; h++) //for every vertex across z axis
            {
                heightMap[w, h] = 0; //make all vertices' heights 0
            }
        }
        tData.SetHeights(0, 0, heightMap); //assign height values back to the terrain starting at index (0,0) -> whole terrain 
    }

    private void OnEnable() //help run everytime the script is updated
    {
        Debug.Log("Initializing Terrain"); //to let editor know when the script runs
        t = this.GetComponent<Terrain>(); //Grab the terrain component 
        tData = Terrain.activeTerrain.terrainData; //assign the terrain data itself to tData variable

    }

    void Awake() //occurs when script instance is loaded
    {
        //serialized give access to things within the editor
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]); //loaded from Unity's data to get access to the default tags
        SerializedProperty tags = tagManager.FindProperty("tags"); //to only obtain the default tags and store them in a list

        //add these tags to the list
        AddTag(tags, "Terrain");
        AddTag(tags, "Sky");
        AddTag(tags, "Water");

        tagManager.ApplyModifiedProperties(); //updates Unity to put the tags in
        this.gameObject.tag = "Terrain"; //assigning a tag to this terrain game object
    }

    void AddTag(SerializedProperty tags, string newTagName) //checks the exisitng tags and adds the tag with the given name if it does not already exist
    {
        bool exists = false; //bool to set if the tag exists or not

        for (int i = 0; i < tags.arraySize; i++) //checking if the tag already exists or not
        {
            SerializedProperty tag = tags.GetArrayElementAtIndex(i);
            if (tag.stringValue.Equals(newTagName))
            {
                exists = true;
                break;
            }
        }

        if (!exists) //if it doesn't exist, then add a new tag with the given name
        {
            tags.InsertArrayElementAtIndex(0);
            SerializedProperty newTagProperty = tags.GetArrayElementAtIndex(0);
            newTagProperty.stringValue = newTagName;
        }
    }

    //This funtion is used called from Perlin/fractal Brownian Motion funcitons   
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

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

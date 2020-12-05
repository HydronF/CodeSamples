using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct TerrainDataList
{
    [SerializeField] public List<TerrainData> terrainDatas;
}

public class ContourMap : MonoBehaviour
{
    // x: west to east; y: south to north
    Dictionary<Vector2Int, TerrainData> terrains = new Dictionary<Vector2Int, TerrainData>();

    Texture2D topoMap;
    // [Tooltip("Mininum height rate for peaks (0-1)")]
    float peakRange = 0.7f;
    // [Tooltip("Mininum height rate for hills (0-1)")] 
    float hillRange = 0.4f;
    // [Tooltip("Maximum height rate for water (0-1)")]
    float waterRange = 0.05f;

    // [Tooltip("Number of contour lines")]
    int bandCount = 8;
    // [Tooltip("Width of contour lines")]
    int bandWidth = 3;

    // [Tooltip("Color of peaks")]
    Color32 peakColor = new Color32(140, 88, 42, 255);
    // [Tooltip("Color of hills")]
    Color32 hillColor = new Color32(166, 105, 31, 255);
    // [Tooltip("Color of normal land")]
    Color32 baseColor = new Color32(191, 148, 96, 255);
    // [Tooltip("Color of water")]
    Color32 waterColor = new Color32(217, 184, 143, 255);
    // [Tooltip("Color of contour lines")]
    Color32 bandColor = new Color32(89, 43, 27, 255);

    public void GenerateMap(string mapFileName)
    {
        //topoMap = MapFromTerrain(terrain);
        topoMap = MapFromTerrainsInScene();
        byte[] bytes = topoMap.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/Temporary Assets/TerrainImageToolOutput/" + mapFileName + ".png", bytes);
        Debug.Log("Image saved to: " + Application.dataPath + "/Temporary Assets/ TerrainImageToolOutput/" + mapFileName +".png");
    }

    public Texture2D MapFromTerrainsInScene()
    {
        int count = 0;
        terrains.Clear();
        foreach (var te in Terrain.activeTerrains)
        {
            string noFront = te.name.Remove(0, 9);
            var numOnly = noFront.Remove(noFront.Length - 1);
            string[] nums = numOnly.Split(',');
            Vector2Int pos = new Vector2Int((int)(float.Parse(nums[0]) / 1000), (int)(float.Parse(nums[2]) / 1000));
            Debug.Log("Adding Terrain #" + count + " at " + pos);
            count++;
            terrains.Add(pos, te.terrainData);
        }

        // Get the number of tiles
        Vector2Int tileMin = new Vector2Int(int.MaxValue, int.MaxValue);
        Vector2Int tileMax = new Vector2Int(int.MinValue, int.MinValue);
        foreach (Vector2Int vec in terrains.Keys)
        {
            if (tileMin.x > vec.x) { tileMin.x = vec.x; }
            if (tileMin.y > vec.y) { tileMin.y = vec.y; }
            if (tileMax.x < vec.x) { tileMax.x = vec.x; }
            if (tileMax.y < vec.y) { tileMax.y = vec.y; }
        }

        // Calculate size of texture
        int heightmapRes = terrains[tileMin].heightmapResolution;
        int _width = heightmapRes * (tileMax.x - tileMin.x + 1);
        int _height = heightmapRes * (tileMax.y - tileMin.y + 1);

        Texture2D topoMap = new Texture2D(_width, _height);
        topoMap.anisoLevel = 16;

        // First pass to get height range
        float minHeight = 1f;
        float maxHeight = 0f;
        for (int row = tileMin.x; row <= tileMax.x; ++row)
        {
            for (int col = tileMin.y; col <= tileMax.y; ++col)
            {
                TerrainData data;
                if (terrains.TryGetValue(new Vector2Int(row, col), out data))
                {
                    float[,] heightmap = terrains[new Vector2Int(row, col)].GetHeights(0, 0, heightmapRes, heightmapRes);

                    for (int y = 0; y < heightmapRes; y++)
                    {
                        for (int x = 0; x < heightmapRes; x++)
                        {
                            if (minHeight > heightmap[y, x])
                            {
                                minHeight = heightmap[y, x];
                            }
                            if (maxHeight < heightmap[y, x])
                            {
                                maxHeight = heightmap[y, x];
                            }
                        }
                    }
                }
            }
        }

        // Second pass to draw texture
        for (int row = tileMin.x; row <= tileMax.x; ++row)
        {
            for (int col = tileMin.y; col <= tileMax.y; ++col)
            {
                TerrainData data;
                if (terrains.TryGetValue(new Vector2Int(row, col), out data))
                {
                    float[,] heightmap = terrains[new Vector2Int(row, col)].GetHeights(0, 0, heightmapRes, heightmapRes);
                    int startX = heightmapRes * (row - tileMin.x);
                    int startY = heightmapRes * (col - tileMin.y);

                    //Set background
                    for (int x = 0; x < heightmapRes; x++)
                    {
                        for (int y = 0; y < heightmapRes; y++)
                        {
                            if (BelowHeight(heightmap[y, x], waterRange, minHeight, maxHeight))
                            {
                                topoMap.SetPixel(startX + x, startY + y, waterColor);
                            }
                            else if (BelowHeight(heightmap[y, x], hillRange, minHeight, maxHeight))
                            {
                                topoMap.SetPixel(startX + x, startY + y, baseColor);
                            }
                            else if (BelowHeight(heightmap[y, x], peakRange, minHeight, maxHeight))
                            {
                                topoMap.SetPixel(startX + x, startY + y, hillColor);
                            }
                            else
                            {
                                topoMap.SetPixel(startX + x, startY + y, peakColor);
                            }
                        }
                    }

                    // Create height band list
                    float bandDistance = (maxHeight - minHeight) / bandCount;
                    List<float> bands = new List<float>();

                    // Add a line around water
                    bands.Add((maxHeight - minHeight) * waterRange + minHeight);

                    //Get ranges for bands
                    float r = minHeight + bandDistance;
                    while (r < maxHeight)
                    {
                        bands.Add(r);
                        r += bandDistance;
                    }


                    // Create slice buffer
                    bool[,] slice = new bool[heightmapRes, heightmapRes];

                    //Draw bands
                    for (int b = 0; b < bands.Count; b++)
                    {
                        //Get Slice
                        for (int y = 0; y < heightmapRes; y++)
                        {
                            for (int x = 0; x < heightmapRes; x++)
                            {
                                if (heightmap[y, x] >= bands[b])
                                {
                                    slice[x, y] = true;
                                }
                                else
                                {
                                    slice[x, y] = false;
                                }
                            }
                        }

                        for (int y = 1; y < heightmapRes - 1; y++)
                        {
                            for (int x = 1; x < heightmapRes - 1; x++)
                            {
                                if (slice[x, y] == true)
                                {
                                    if (slice[x - 1, y] == false || slice[x + 1, y] == false || slice[x, y - 1] == false || slice[x, y + 1] == false)
                                    {
                                        topoMap.SetPixel(startX + x, startY + y, bandColor);
                                        Color32[] bandColors = new Color32[bandWidth * bandWidth];
                                        for ( int i = 0; i < bandColors.Length; i++) { bandColors[i] = bandColor; }
                                        topoMap.SetPixels32(startX + x, startY + y, bandWidth, bandWidth, bandColors);
                                    }
                                }
                            }
                        }
                    }

                    topoMap.Apply();
                }
            }
        }

        return topoMap;
    }

    private static bool BelowHeight(float height, float rate, float minHeight, float maxHeight) {
        return (height - minHeight < (maxHeight-minHeight) * rate);
    }

    public void SetRanges(float newPeakRange, float newHillRange, float newWaterRange) {
        peakRange = newPeakRange;
        hillRange = newHillRange;
        waterRange = newWaterRange;
    }

    public void SetColors(Color32 newPeakColor, Color32 newHillColor, Color32 newBaseColor, Color32 newWaterColor) {
        peakColor = newPeakColor;
        hillColor = newHillColor;
        baseColor = newBaseColor;
        waterColor = newWaterColor;
    }
    
    public void SetBands(int newBandCount, int newBandWidth, Color32 newBandColor) {
        bandCount = newBandCount;
        bandWidth = newBandWidth;
        bandColor = newBandColor;
    }
}

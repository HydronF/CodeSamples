using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PixelContent
{
    Empty = 0,
    Solid = 1,
    Water = 2,
    Steam = 3,
    Ice = 4,
}

public class PixelManager : MonoBehaviour
{
    public struct Pixel
    {
        public PixelContent content;
        public Vector2 distToMove;
        public Vector2 velocity;
        public Vector2 acceleration;
    }

    Texture2D texture;
    public Pixel[,] pixelArray;
    public int width;
    public int height;
    
    // Movement parameters
    public float gravityScale;
    public float randThreshold;
    public float randVelocity;

    bool alternateFlag = false; // Alternate simulation order
    
    // Rendering
    public UnityEngine.UI.RawImage outputImage;
    public List<Color32> colorList;
    Color32[] renderColors;
    
    // Audio
    public AudioManager audioManager;
    int waterCount = 0;
    int fallingWater = 0;
    int movingWater = 0;

    void Start()
    {
        texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        pixelArray = new Pixel[width, height];
        renderColors = new Color32[width * height];
        outputImage.texture = texture;
        for (int i = 0; i < height; ++i)
        {
            pixelArray[0, i].content = PixelContent.Solid;
            pixelArray[width - 1, i].content = PixelContent.Solid;
            for (int j = 1; j < width - 1; ++j)
            {
                if (i == height - 1) {
                    pixelArray[j, i].content = PixelContent.Solid;
                }
            }
        }
    }

    void Update()
    {
        for (int i = 0; i < height; ++i)
        {
            for (int j = 0; j < width; ++j)
            {
                renderColors[i * width + j] = colorList[(int) pixelArray[j, i].content];
            }
        }
        texture.SetPixels32(renderColors);
        texture.Apply(false);
    }

    void FixedUpdate()
    {
        // We alternate the order on each frame to avoid unexpected patterns
        if (alternateFlag) {
            Random.InitState(System.Environment.TickCount);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    PixelMovement(x, y);
                }
            }
            alternateFlag = false;
        }
        else {
            Random.InitState(System.Environment.TickCount);
            for (int x = width - 1; x >= 0; x--)
            {
                for (int y = height - 1; y >= 0; y--)
                {
                    PixelMovement(x, y);
                }
            }
            alternateFlag = true;
        }
        audioManager.RefreshParameters(waterCount, fallingWater, movingWater);
        movingWater = 0;
        fallingWater = 0;
    }

    void PixelMovement(int x, int y)
    {
        // Update distance, velocity, and acceleration
        // We store let distToMove accumulate between frames and move the pixel only when its distToMove is large enough.
        pixelArray[x, y].distToMove += pixelArray[x, y].velocity * Time.fixedDeltaTime;
        pixelArray[x, y].velocity += pixelArray[x, y].acceleration * Time.fixedDeltaTime;
        pixelArray[x, y].acceleration = Vector2.zero;
        
        // Apply force based on pixel content
        switch (pixelArray[x, y].content)
        {
            case PixelContent.Water:
                WaterForce(x, y);
                break;
            default:
                break;
        }
        
        if (Mathf.Abs(pixelArray[x, y].distToMove.x) > 1 || Mathf.Abs(pixelArray[x, y].distToMove.y) > 1)
        {
            Move(x, y, pixelArray[x, y].distToMove);
        }
    }

    void WaterForce(int x, int y)
    {   
        if (y < height - 1)
        {
            PixelContent downContent = pixelArray[x, y + 1].content;
            if (downContent == PixelContent.Empty || downContent == PixelContent.Steam || downContent == PixelContent.Ice)
            {
                pixelArray[x, y].acceleration += new Vector2(0.0f, gravityScale * Time.fixedDeltaTime);
            }
            else
            {
                if (pixelArray[x, y].velocity.x > 0)
                {
                    pixelArray[x, y].velocity += new Vector2(Mathf.Abs(pixelArray[x, y].velocity.y) * 0.2f, 
                        -0.2f * pixelArray[x, y].velocity.y) * Time.deltaTime;
                }
                else
                {
                    pixelArray[x, y].velocity += new Vector2(Mathf.Abs(pixelArray[x, y].velocity.y) * -0.2f, 
                        -0.2f * pixelArray[x, y].velocity.y) * Time.deltaTime;
                }
            }
        }
        if (pixelArray[x, y].velocity.magnitude < randThreshold)
        {
            pixelArray[x, y].velocity = new Vector2((Random.Range(0, 2) * 2 - 1) * randVelocity, pixelArray[x, y].velocity.y);
        }
    }

    void Move(int x, int y, Vector2 distance)
    {
        int nextX = x;
        int nextY = y;
        
        // Get the destination
        if (distance.y > 1.0f)
        {
            nextY = y + 1;
            pixelArray[x, y].distToMove -= new Vector2(0.0f, 1.0f);
        }
        else if (distance.y < -1.0f)
        {
            nextY = y - 1;
            pixelArray[x, y].distToMove -= new Vector2(0.0f, -1.0f);
        }

        if (distance.x > 1.0f)
        {
            nextX = x + 1;
            pixelArray[x, y].distToMove -= new Vector2(1.0f, 0.0f);
        }
        else if (distance.x < -1.0f)
        {
            nextX = x - 1;
            pixelArray[x, y].distToMove -= new Vector2(-1.0f, 0.0f);
        }

        if (nextX < 0) nextX = 0;
        if (nextX >= width) nextX = width - 1;
        if (nextY < 0) nextY = 0;
        if (nextY >= height) nextY = height - 1;
        
        // Handle collisions
        switch (pixelArray[nextX, nextY].content)
        {
            case PixelContent.Solid:
                if (nextX != x) {
                    pixelArray[x, y].distToMove = new Vector2(0.0f, pixelArray[x, y].distToMove.y);
                    pixelArray[x, y].velocity = new Vector2(0.0f, pixelArray[x, y].velocity.y);
                }
                else if (nextY != y) {
                    pixelArray[x, y].distToMove = new Vector2(pixelArray[x, y].distToMove.x, 0.0f);
                    pixelArray[x, y].velocity = new Vector2(pixelArray[x, y].velocity.x, 0.0f);
                }
                break;                
            default:
                if (pixelArray[nextX, nextY].content != pixelArray[x, y].content)
                {
                    SwapPixel(x, y, nextX, nextY);
                    if (pixelArray[nextX, nextY].content == PixelContent.Water)
                    {
                        movingWater += 1;
                        if (nextY > y && pixelArray[nextX, nextY].velocity.y > 10.0f) {
                            fallingWater += 1;
                        }
                    }
                }
                else {
                    pixelArray[nextX, nextY].velocity += pixelArray[x, y].velocity * 0.5f;
                    pixelArray[x, y].velocity *= 0.4f;
                }
                break;
        }

    }

    public void SplashAt(int x, int y, int size)
    {
        // Create a splash at the specified position
        int splashedWater = 0;
        for (int i = x - size; i <= x + size; ++i)
        {
            for (int j = y - size; j <= y + size; ++j) 
            {
                if (IsValidCoord(i, j) && pixelArray[i, j].content == PixelContent.Water)
                {
                    pixelArray[i, j].velocity = new Vector2 ((i - x) * 6.0f, -50.0f);
                    splashedWater++;
                }
            }
        }
        audioManager.SplashSound(Mathf.Clamp(splashedWater / 100.0f, 0.0f, 1.0f));
    }

    void SwapPixel(int x1, int y1, int x2, int y2)
    {
        Pixel temp = pixelArray[x1, y1];
        pixelArray[x1, y1] = pixelArray[x2, y2];
        pixelArray[x2, y2] = temp;
    }

    public string OutputInfo(float xPos, float yPos)
    {
        // Used for debug
        int x = (int) ((1.0f - xPos / Screen.width) * width);
        int y = (int) ((1.0f - yPos / Screen.height) * height);
        if (IsValidCoord(x, y)) {
            string output = pixelArray[x, y].content.ToString() + "\n";
            output += "Position: " + x + ", " + y + "\n";
            output += "Distance: " + pixelArray[x, y].distToMove.ToString() + "\n";
            output += "Velocity: " + pixelArray[x, y].velocity.ToString() + "\n";
            return output;
        }
        return "Invalid";
    }

    private bool IsValidCoord(int x, int y)
    {
        return (x > 0 && x < width - 1 && y > 0 && y < height - 1);
    }

    public void SpawnWaterAt(int x, int y, int size) {
        for (int i = x - size; i <= x + size; ++i)
        {
            for (int j = y - size; j <= y + size; ++j)
            {
                if (IsValidCoord(i, j))
                {
                    if (pixelArray[i, j].content == PixelContent.Empty)
                    {
                        pixelArray[i, j].content = PixelContent.Water;
                        waterCount += 1;
                        pixelArray[i, j].acceleration += new Vector2(2.0f, 20.0f);
                    }
                    else if (pixelArray[i, j].content == PixelContent.Water)
                    {
                        pixelArray[i, j].acceleration += new Vector2(2.0f, 20.0f);
                    }
                }
            }
        }
    }
}


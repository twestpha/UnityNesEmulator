using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnvironmentMatch {
    public TileMatchData tile;
    public GameObject prefab;

    public bool IsMatch(int startX, int startY, byte[] bitmap){
        if(tile.data == null || tile.data.Length <= 0){
            return false;
        }

        // TODO make matching floatier, where less than some amount disqualifies
        // and more than some amount qualifies
        for(int y = 0; y < 16; ++y){
            for(int x = 0; x < 16; ++x){
                int offsetX = startX + x;
                int offsetY = startY + y;

                if(offsetY > 0 && offsetY < 240 && offsetX > 0 && offsetX < 256){
                    if(tile.data[(y * 16) + x] != bitmap[(offsetY * 256) + offsetX]){
                        return false;
                    }
                }
            }
        }

        return true;
    }
}

public class Marker {
    public const int WIDTH = 256;
    public const int HEIGHT = 240;

    public const int MARKER_SIZE = 18;
    public const int MARKER_MIN = 32;
    public const int MARKER_MAX = 208; // 240 - 32
    public const int MARKER_MAX_MOVE = 8;

    public int previousX;
    public int previousY;

    public int currentX;
    public int currentY;

    public int deltaX;
    public int deltaY;

    public int matchesFailed;

    public byte markerCenter;
    public byte[] up;
    public byte[] down;
    public byte[] left;
    public byte[] right;

    public Marker(int x, int y, byte[] bitmap){
        if(x < MARKER_MIN || x > MARKER_MAX || y < MARKER_MIN || y > MARKER_MAX){
            Debug.LogError("Invalid start position for marker");
            return;
        }

        previousX = x;
        previousY = y;

        currentX = x;
        currentY = y;

        matchesFailed = 0;

        markerCenter = bitmap[Flatten(x, y)];

        up = new byte[MARKER_SIZE];
        for(int i = 0; i < MARKER_SIZE; ++i){ up[i] = bitmap[Flatten(x, y - 1 - i)]; }

        down = new byte[MARKER_SIZE];
        for(int i = 0; i < MARKER_SIZE; ++i){ down[i] = bitmap[Flatten(x, y + 1 + i)]; }

        right = new byte[MARKER_SIZE];
        for(int i = 0; i < MARKER_SIZE; ++i){ right[i] = bitmap[Flatten(x + 1 + i, y)]; }

        left = new byte[MARKER_SIZE];
        for(int i = 0; i < MARKER_SIZE; ++i){ left[i] = bitmap[Flatten(x - 1 - i, y)]; }
    }

    public void ClampToBounds(ref int x, ref int y){
        if(x > WIDTH){ x = WIDTH; }
        if(x < 0){ x = 0; }
        if(y > HEIGHT){ y = HEIGHT; }
        if(y < 0){ y = 0; }
    }

    public bool FindMatch(byte[] bitmap){
        if(currentX < 0 || currentX > WIDTH){
            return false;
        }
        if(currentY < 0 || currentY > HEIGHT){
            return false;
        }

        int farDown = currentY + MARKER_MAX_MOVE;
        int farUp = currentY - MARKER_MAX_MOVE;
        int farRight = currentX + MARKER_MAX_MOVE;
        int farLeft = currentX - MARKER_MAX_MOVE;

        ClampToBounds(ref farLeft, ref farUp);
        ClampToBounds(ref farRight, ref farDown);

        if(farDown == 0 && farUp == 0){
            return false;
        }
        if(farRight == 0 && farLeft == 0){
            return false;
        }

        for(int y = farUp - 1; y < farDown; ++y){
            for(int x = farLeft - 1; x < farRight; ++x){
                // Slight remapping to check current position first
                int remapX = x < farLeft ? currentX : x;
                int remapY = y < farUp ? currentY : y;

                // If our center matches, we can start the matching process
                if(bitmap[Flatten(remapX, remapY)] == markerCenter){
                    bool foundMatch = true;
                    // If we find a full match, set deltas and bail
                    for(int i = 0; i < MARKER_SIZE; ++i){
                        int upIndex = (remapY - 1 - i);
                        bool upInRange = upIndex > 0;
                        bool upMatch = true;
                        if(upInRange){
                            upMatch = bitmap[Flatten(remapX, upIndex)] == up[i];
                        }
                        if(!upMatch){
                            foundMatch = false;
                            break;
                        }

                        int downIndex = (remapY + 1 + i);
                        bool downInRange = downIndex < HEIGHT;
                        bool downMatch = true;
                        if(downInRange){
                            downMatch = bitmap[Flatten(remapX, downIndex)] == down[i];
                        }
                        if(!downMatch){
                            foundMatch = false;
                            break;
                        }

                        int rightIndex = (remapX + 1 + i);
                        bool rightInRange = rightIndex < WIDTH;
                        bool rightMatch = true;
                        if(rightInRange){
                            rightMatch = bitmap[Flatten(rightIndex, remapY)] == right[i];
                        }
                        if(!rightMatch){
                            foundMatch = false;
                            break;
                        }

                        int leftIndex = (remapX - 1 - i);
                        bool leftInRange = leftIndex > 0;
                        bool leftMatch = true;
                        if(leftInRange){
                            leftMatch = bitmap[Flatten(leftIndex, remapY)] == left[i];
                        }
                        if(!leftMatch){
                            foundMatch = false;
                            break;
                        }
                    }

                    if(foundMatch){
                        matchesFailed = 0;

                        previousX = currentX;
                        previousY = currentY;

                        currentX = remapX;
                        currentY = remapY;

                        deltaX = currentX - previousX;
                        deltaY = currentY - previousY;

                        return true;
                    }
                }
            }
        }

        matchesFailed++;
        return false;
    }

    public void DrawMarkerToDisplay(Texture2D texture){
        texture.SetPixel(currentX, currentY, Color.red);
        for(int i = 0; i < MARKER_SIZE; ++i){
            int rightIndex = currentX + 1 + i;
            if(rightIndex < WIDTH){
                texture.SetPixel(rightIndex, currentY, Color.red);
            }

            int leftIndex = currentX - 1 - i;
            if(leftIndex > 0){
                texture.SetPixel(leftIndex, currentY, Color.red);
            }

            int downIndex = currentY + 1 + i;
            if(downIndex > 0){
                texture.SetPixel(currentX, downIndex, Color.red);
            }

            int upIndex = currentY - 1 - i;
            if(upIndex > 0){
                texture.SetPixel(currentX, upIndex, Color.red);
            }
        }
        texture.Apply();
    }

    private int Flatten(int x, int y){
        return (y * WIDTH) + x;
    }
}

public class EnvironmentDrawer : MonoBehaviour {
    public const int TILE_SIZE = 256;
    public const int TILE_ORIGIN_X = 128;
    public const int TILE_ORIGIN_Y = 128;

    public const int MARKER_COUNT = 32;
    public const int MIN_VALID_MARKER_COUNT = 4;

    public const int MINIMUM_VOTE_FOR_DELTA = 8;

    public const int PIXELS_PER_TILE = 16;
    public const float PIXELS_TO_WORLD = 0.12500f;

    public Texture2D display;
    public GameObject gameCamera;
    [Header("Matches")]
    public EnvironmentMatch[] matches;

    private byte[] bitmap;
    private GameObject[,] tiles;

    private bool started = false;
    private int totalDeltaX, totalDeltaY;

    private Emulator emu;
    private Marker[] markers;

    [Header("Output")]
    public Texture2D output;
    public int outputX, outputY;
    public bool writeOutput;
    public TileMatchData outputData;

    [Header("Tuning")]
    public float tuningOffsetX;
    public float tuningOffsetY;

    void Start(){
        emu = GetComponent<Emulator>();
        bitmap = emu.GetConsole().Ppu.BitmapData;

        tiles = new GameObject[TILE_SIZE, TILE_SIZE];

        markers = new Marker[MARKER_COUNT];
    }

    void Update(){
        if(started){
            // Purely debug/tools stuff
            int amount = Input.GetKey(KeyCode.LeftShift) ? 1 : PIXELS_PER_TILE;
            if(Input.GetKeyDown(KeyCode.A)){
                outputX -= amount;
            } else if(Input.GetKeyDown(KeyCode.D)){
                outputX += amount;
            } else if(Input.GetKeyDown(KeyCode.W)){
                outputY -= amount;
            } else if(Input.GetKeyDown(KeyCode.S)){
                outputY += amount;
            }

            if(outputX < 0){ outputX = 0; }
            if(outputX >= 240){ outputX = 240; }
            if(outputY < 0){ outputY = 0; }
            if(outputY >= 224){ outputY = 224; }

            for(int y = 0; y < PIXELS_PER_TILE; ++y){
                for(int x = 0; x < PIXELS_PER_TILE; ++x){
                    Color color = emu.palette[bitmap[(outputY + y) * 256 + (outputX + x)]];
                    output.SetPixel(x, y, color);
                }
            }
            output.Apply();

            if(writeOutput){
                writeOutput = false;
                outputData.data = new byte[PIXELS_PER_TILE * PIXELS_PER_TILE];

                for(int y = 0; y < 16; ++y){
                    for(int x = 0; x < 16; ++x){
                        outputData.data[(y * 16) + x] = bitmap[(outputY + y) * 256 + (outputX + x)];
                    }
                }

                Debug.Log("Wrote Output Data");
            }

            // Get "average" delta with voting
            // If not enough even find a match, we've done a scene change, and we need to refresh all markers
            Dictionary<int, int> xOffsetVote = new Dictionary<int, int>();
            Dictionary<int, int> yOffsetVote = new Dictionary<int, int>();
            int validMarkerCount = 0;

            for(int i = 0; i < MARKER_COUNT; ++i){
                if(markers[i].FindMatch(bitmap)){
                    if(xOffsetVote.ContainsKey(markers[i].deltaX)){
                        xOffsetVote[markers[i].deltaX]++;
                    } else {
                        xOffsetVote.Add(markers[i].deltaX, 1);
                    }

                    if(yOffsetVote.ContainsKey(markers[i].deltaY)){
                        yOffsetVote[markers[i].deltaY]++;
                    } else {
                        yOffsetVote.Add(markers[i].deltaY, 1);
                    }

                    validMarkerCount++;

                    markers[i].DrawMarkerToDisplay(display);
                }

                if(markers[i].matchesFailed > 20 /* frames */){
                    int xrand = 32 + (int)(Random.value * (float)(208 - 32));
                    int yrand = 32 + (int)(Random.value * (float)(208 - 32));

                    markers[i] = new Marker(
                        xrand, yrand,
                        bitmap
                    );
                }
            }

            // If not enough markers are valid, reset the screen
            if(validMarkerCount < MIN_VALID_MARKER_COUNT){
                Debug.Log("RESETTING");
                ResetAllMarkers();
                ResetAllTiles();
                return;
            }

            // TODO pick a random marker and reset it every once in a while?

            int highestXOffsetVote = 0;
            int xDelta = 0;
            foreach(KeyValuePair<int, int> entry in xOffsetVote){
                if(entry.Value > highestXOffsetVote){
                    highestXOffsetVote = entry.Value;
                    xDelta = entry.Key;
                }
            }

            int highestYOffsetVote = 0;
            int yDelta = 0;
            foreach(KeyValuePair<int, int> entry in yOffsetVote){
                if(entry.Value > highestYOffsetVote){
                    highestYOffsetVote = entry.Value;
                    yDelta = entry.Key;
                }
            }

            // Slide camera around based on delta
            if(highestXOffsetVote > MINIMUM_VOTE_FOR_DELTA){
                totalDeltaX += xDelta;
            }
            if(highestYOffsetVote > MINIMUM_VOTE_FOR_DELTA){
                totalDeltaY += yDelta;
            }

            gameCamera.transform.position += new Vector3((float)(-xDelta) * PIXELS_TO_WORLD, 0.0f, (float)(yDelta) * PIXELS_TO_WORLD);

            // Scan entire screen, try to match elements from Matches to place prefabs in those tiles slot
            for(int y = 15; y < 224; ++y){ // Don't do all of Y?
                for(int x = 0; x < 256; ++x){
                    // int x = 80; int y = 32;
                    // First, convert this x/y coordinate into offset space using total deltas
                    // BUG BUG BUG these mappings are super fucked up... :(
                    int offsetX = x - totalDeltaX;
                    int offsetY = y - totalDeltaY;
                    // Debug.Log("ALSKJDLKASJDLKASJDLKJ");
                    // Debug.Log(offsetX);
                    // Debug.Log(offsetY);

                    // Then, convert *that* into an index into tiles[]
                    int tileX = ((TILE_ORIGIN_X * PIXELS_PER_TILE) + offsetX) / PIXELS_PER_TILE;
                    int tileY = ((TILE_ORIGIN_Y * PIXELS_PER_TILE) + offsetY) / PIXELS_PER_TILE;

                    // Debug.Log(tileX);
                    // Debug.Log(tileY);

                    if(tileX < 0 || tileX >= TILE_SIZE || tileY < 0 || tileY >= TILE_SIZE){
                        Debug.LogError("TILE_SIZE is too small");
                        return;
                    }

                    if(tiles[tileX, tileY] == null){
                        for(int i = 0; i < matches.Length; ++i){
                            if(matches[i].IsMatch(x, y, bitmap)){
                                GameObject newTile = Object.Instantiate(matches[i].prefab);
                                tiles[tileX, tileY] = newTile;

                                float posX = ((tileX - TILE_ORIGIN_X) * PIXELS_PER_TILE) * PIXELS_TO_WORLD;
                                float posY = ((tileY - TILE_ORIGIN_Y) * PIXELS_PER_TILE) * PIXELS_TO_WORLD;

                                newTile.transform.position = new Vector3(posX + tuningOffsetX, 0.0f, -posY + tuningOffsetY);
                            }
                        }
                    }
                    // What if tiles need "updating"? Like a tile that we missed, or one that an sprite was covering?
                    // Maybe a quick-cheap comparison of "Hey, are you still that tile?" before all these shenanigans
                }
            }

            // TODO fade fullscreen tint in and out based on average screen darkness?
        } else {
            if(emu.started){
                started = true;
                ResetAllMarkers();
                ResetAllTiles();
            }
        }
    }

    void ResetAllMarkers(){
        totalDeltaX = 0;
        totalDeltaY = 0;

        gameCamera.transform.position = new Vector3(0.0f, 9.0f, 0.0f);

        for(int i = 0; i < MARKER_COUNT; ++i){
            // Probably put this in the constructor?
            int xrand = 32 + (int)(Random.value * (float)(208 - 32));
            int yrand = 32 + (int)(Random.value * (float)(208 - 32));

            // Debug.Log("(" + xrand + ", " + yrand + ")");
            markers[i] = new Marker(
                xrand, yrand,
                emu.GetConsole().Ppu.BitmapData
            );
        }
    }

    void ResetAllTiles(){
        for(int y = 0; y < TILE_SIZE; ++y){
            for(int x = 0; x < TILE_SIZE; ++x){
                if(tiles[x, y] != null){
                    Destroy(tiles[x, y]);
                }
            }
        }
    }
}

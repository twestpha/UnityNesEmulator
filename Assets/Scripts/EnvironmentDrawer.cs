using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

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

    public const int MARKER_MIN = 16;
    public const int MARKER_MAX = 224;

    public const int COLOR_BLOCK_SIZE = 3;

    public int currentX;
    public int currentY;
    public byte[,] colorBlock;

    public int matchesFailed;

    public bool centered;
    public bool up;
    public bool down;
    public bool left;
    public bool right;

    public Marker(int x, int y, byte[] bitmap){
        if(x < MARKER_MIN || x > MARKER_MAX || y < MARKER_MIN || y > MARKER_MAX){
            Debug.LogError("Invalid start position for marker");
            return;
        }

        currentX = x;
        currentY = y;

        colorBlock = new byte[COLOR_BLOCK_SIZE, COLOR_BLOCK_SIZE];
        for(int by = 0; by < COLOR_BLOCK_SIZE; ++by){
            for(int bx = 0; bx < COLOR_BLOCK_SIZE; ++bx){
                colorBlock[bx, by] = bitmap[Flatten(currentX + bx - 1, currentY + by - 1)];
            }
        }
    }

    public void CalculateMoveDirections(byte[] bitmap){
        // Try centered/up/down/left/right move by one
        // Record which one matches

        for(int i = 0; i < 5; ++i){
            int xOffset = 0;
            int yOffset = 0;

            if(i == 1){
                yOffset = -1;
            } else if(i == 2){
                yOffset = 1;
            } else if(i == 3){
                xOffset = -1;
            } else if(i == 4){
                xOffset = 1;
            }

            bool completeMatch = true;
            for(int y = 0; y < COLOR_BLOCK_SIZE; ++y){
                for(int x = 0; x < COLOR_BLOCK_SIZE; ++x){
                    completeMatch &= colorBlock[x, y] == bitmap[Flatten(currentX + x + xOffset - 1, currentY + y + yOffset - 1)];
                }

                if(!completeMatch){
                    break;
                }
            }

            if(i == 0){
                centered = completeMatch;
            } else if(i == 1){
                up = completeMatch;
            } else if(i == 2){
                down = completeMatch;
            } else if(i == 3){
                left = completeMatch;
            } else if(i == 4){
                right = completeMatch;
            }
        }

        for(int by = 0; by < COLOR_BLOCK_SIZE; ++by){
            for(int bx = 0; bx < COLOR_BLOCK_SIZE; ++bx){
                colorBlock[bx, by] = bitmap[Flatten(currentX + bx - 1, currentY + by - 1)];
            }
        }
    }

    public void DrawMarkerToDisplay(Texture2D texture){
        texture.SetPixel(currentX, currentY, Color.red);

        for(int y = 0; y < COLOR_BLOCK_SIZE; ++y){
            for(int x = 0; x < COLOR_BLOCK_SIZE; ++x){
                texture.SetPixel(currentX + x - 1, currentY + y - 1, Color.red);
            }
        }
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
    public const int RESET_MARKER_COUNT = 28;

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
    private float cameraStartHeight;

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

    [Header("Debug Reset")]
    public bool DebugReset;

    void Start(){
        emu = GetComponent<Emulator>();
        bitmap = emu.GetConsole().Ppu.BitmapData;

        tiles = new GameObject[TILE_SIZE, TILE_SIZE];

        markers = new Marker[MARKER_COUNT];

        cameraStartHeight = gameCamera.transform.position.y;
    }

    void Update(){
        if(started){
            #if UNITY_EDITOR
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
                    EditorUtility.SetDirty(outputData);
                    AssetDatabase.SaveAssets();
                }

                if(DebugReset){
                    DebugReset = false;
                    ResetAllTiles();
                    ResetAllMarkers();
                }
            #endif // UNITY_EDITOR

            // Get "average" delta with voting
            // If not enough for any direction, we've done a scene change, and we need to refresh the scene
            int allDirectionsCount = 0;
            int centeredCount = 0;
            int upCount = 0;
            int downCount = 0;
            int leftCount = 0;
            int rightCount = 0;

            for(int i = 0; i < MARKER_COUNT; ++i){
                markers[i].CalculateMoveDirections(bitmap);

                // bool allDirections = markers[i].centered && markers[i].up && markers[i].down && markers[i].left && markers[i].right;
                bool allDirections = false;

                if(!allDirections){
                    centeredCount += markers[i].centered ? 1 : 0;
                    upCount       += markers[i].up       ? 1 : 0;
                    downCount     += markers[i].down     ? 1 : 0;
                    leftCount     += markers[i].left     ? 1 : 0;
                    rightCount    += markers[i].right    ? 1 : 0;
                } else {
                    allDirectionsCount++;
                }

                // markers[i].DrawMarkerToDisplay(display);
                // display.Apply();
            }

            // If not enough markers are valid, reset the screen
            if(allDirectionsCount > RESET_MARKER_COUNT){
                Debug.Log("RESETTING");
                ResetAllMarkers();
                ResetAllTiles();
                return;
            }

            int deltaX = 0;
            int deltaY = 0;

            if(upCount > centeredCount && upCount > downCount && upCount > rightCount && upCount > leftCount){
                deltaY = -1;
            } else if(downCount > centeredCount && downCount > upCount && downCount > rightCount && downCount > leftCount){
                deltaY = 1;
            } else if(leftCount > centeredCount && leftCount > rightCount && leftCount > upCount  && leftCount > downCount){
                deltaX = -1;
            } else if(rightCount > centeredCount && rightCount > leftCount && rightCount > upCount  && rightCount > downCount){
                deltaX = 1;
            }

            totalDeltaX += deltaX;
            totalDeltaY += deltaY;

            gameCamera.transform.position += new Vector3((float)(-deltaX) * PIXELS_TO_WORLD, 0.0f, (float)(deltaY) * PIXELS_TO_WORLD);

            // Scan entire screen, try to match elements from Matches to place prefabs in those tiles slot
            for(int y = 16; y < 224; ++y){
                for(int x = 0; x < 256; ++x){ // Weird shit happens on the right side of the screen...?
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

        gameCamera.transform.position = new Vector3(0.0f, cameraStartHeight, 0.0f);

        int i = 0;
        for(int x = 0; x < 8; ++x){
            for(int y = 0; y < 4; ++y){
                int xpos = 32 + (x * 22);
                int ypos = 32 + (y * 44);

                markers[i] = new Marker(
                    xpos, ypos,
                    emu.GetConsole().Ppu.BitmapData
                );

                i++;
            }
        }
    }

    void ResetAllTiles(){
        for(int y = 0; y < TILE_SIZE; ++y){
            for(int x = 0; x < TILE_SIZE; ++x){
                if(tiles[x, y] != null){
                    DestroyImmediate(tiles[x, y]);
                    tiles[x, y] = null;
                }
            }
        }
    }
}

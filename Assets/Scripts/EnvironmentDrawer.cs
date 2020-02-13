using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct EnvironmentID {
    public ushort idA;
    public ushort idB;
    public ushort idC;
    public ushort idD;

    public EnvironmentID(ushort a_, ushort b_, ushort c_, ushort d_){
        idA = a_;
        idB = b_;
        idC = c_;
        idD = d_;
    }

    public static bool operator == (EnvironmentID lhs, EnvironmentID rhs) {
        return (lhs.idA == rhs.idA) && (lhs.idB == rhs.idB) && (lhs.idC == rhs.idC) && (lhs.idD == rhs.idD);
    }

    public static bool operator !=(EnvironmentID lhs, EnvironmentID rhs) {
        return !(lhs == rhs);
    }
}

[System.Serializable]
public class EnvironmentMatch {
    public EnvironmentID id;
    public GameObject prefab;
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
        int farUp = currentY + MARKER_MAX_MOVE;
        int farDown = currentY - MARKER_MAX_MOVE;
        int farRight = currentX + MARKER_MAX_MOVE;
        int farLeft = currentX - MARKER_MAX_MOVE;

        ClampToBounds(ref farLeft, ref farUp);
        ClampToBounds(ref farRight, ref farDown);

        if(farUp == 0 && farDown == 0){
            return false;
        }
        if(farRight == 0 && farLeft == 0){
            return false;
        }

        for(int y = farDown; y < farUp; ++y){
            for(int x = farLeft; x < farRight; ++x){
                // If our center matches, we can start the matching process
                if(bitmap[Flatten(x, y)] == markerCenter){
                    bool foundMatch = true;
                    // If we find a full match, set deltas and bail
                    for(int i = 0; i < MARKER_SIZE; ++i){
                        int upIndex = (y - 1 - i);
                        bool upInRange = upIndex > 0;
                        bool upMatch = true;
                        if(upInRange){
                            upMatch = bitmap[Flatten(x, upIndex)] == up[i];
                        }
                        if(!upMatch){
                            foundMatch = false;
                            break;
                        }

                        int downIndex = (y + 1 + i);
                        bool downInRange = downIndex < HEIGHT;
                        bool downMatch = true;
                        if(downInRange){
                            downMatch = bitmap[Flatten(x, downIndex)] == down[i];
                        }
                        if(!downMatch){
                            foundMatch = false;
                            break;
                        }

                        int rightIndex = (x + 1 + i);
                        bool rightInRange = rightIndex < WIDTH;
                        bool rightMatch = true;
                        if(rightInRange){
                            rightMatch = bitmap[Flatten(rightIndex, y)] == right[i];
                        }
                        if(!rightMatch){
                            foundMatch = false;
                            break;
                        }

                        int leftIndex = (x - 1 - i);
                        bool leftInRange = leftIndex > 0;
                        bool leftMatch = true;
                        if(leftInRange){
                            leftMatch = bitmap[Flatten(leftIndex, y)] == left[i];
                        }
                        if(!leftMatch){
                            foundMatch = false;
                            break;
                        }
                    }

                    if(foundMatch){
                        previousX = currentX;
                        previousY = currentY;

                        currentX = x;
                        currentY = y;

                        deltaX = currentX - previousX;
                        deltaY = currentY - previousY;

                        return true;
                    }
                }
            }
        }

        return false;
    }

    public void DrawMarkerToDisplay(Texture2D texture){
        // Make sure to clamp all indices into bitmap!!!
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
    public const int DRAW_SIZE = 32;

    // Rocks = 49, 165, 53, 102
    // Solid Wall = 1319, 46324, 2891, 54526
    // Vertical Wall = 0, 18557, 566, 123
    // Grass = 65280, ", ", "
    // Pillar = 3887, 61683, 2607, 43258
    // Stairs = 233, 30583, 30583, 112
    // Door = 13, 122, 1086, 15998

    // Roofs are complicated, they'll need a bunch of matches

    // Mapping of some hex value (representing the pattern table) to prefab
    // List of instantiated prefabs at name table "locations"
    public Texture2D display;
    public Texture2D emulatorDisplay;
    public GameObject nameTableOrigin;

    public EnvironmentMatch[] matches;

    public GameObject blankTile;

    private EnvironmentID[,] ids;
    private GameObject[,] instances;
    // private Dictionary<EnvironmentID, GameObject> mapping;

    private bool started = false;
    private int redraw = 0;

    private Emulator emu;
    private Nescafe.PpuMemory ppuMem;
    private ushort bgAddress;

    private Color[] colors;

    private Marker[] testMarkers;

    public bool useMarker;

    void Start(){
        emu = GetComponent<Emulator>();

        ids = new EnvironmentID[DRAW_SIZE, DRAW_SIZE];
        instances = new GameObject[DRAW_SIZE, DRAW_SIZE];

        redraw = 0;

        testMarkers = new Marker[16];

        // mapping = new Dictionary<EnvironmentID, GameObject>();
        // for(int i = 0; i < matches.Length; ++i){
        //     mapping.Add(matches[i].id, matches[i].prefab);
        // }

    }

    void Update(){
        // if(Input.GetKeyDown(KeyCode.A)){
        //     nmx--;
        // } else if(Input.GetKeyDown(KeyCode.D)){
        //     nmx++;
        // } else if(Input.GetKeyDown(KeyCode.W)){
        //     nmy--;
        // } else if(Input.GetKeyDown(KeyCode.S)){
        //     nmy++;
        // }

        if(started){
            if(useMarker == true){
                useMarker = false;

                for(int i = 0; i < 16; ++i){
                    int xrand = 32 + (int)(Random.value * (float)(208 - 32));
                    int yrand = 32 + (int)(Random.value * (float)(208 - 32));
                    Debug.Log("(" + xrand + ", " + yrand + ")");
                    testMarkers[i] = new Marker(
                        xrand, yrand,
                        emu.GetConsole().Ppu.BitmapData
                    );

                    testMarkers[i].DrawMarkerToDisplay(emulatorDisplay);

                }
                Debug.LogError("LKJASKDJLK");
            }

            if(testMarkers[0] != null){
                for(int i = 0; i < 16; ++i){
                    if(testMarkers[i].FindMatch(emu.GetConsole().Ppu.BitmapData)){
                        testMarkers[i].DrawMarkerToDisplay(emulatorDisplay);
                        // Debug.Log("DELTAS: (" + testMarker.deltaX + ", " + testMarker.deltaY + ")");
                    }
                }
            }




            // Go through all markers
            // If they all have "around" the same amount of movement, use that delta to scroll the screen some amount

            // if they're all invalid, we probably has a scene change. Invalidate them and setup all new ones.


            redraw++;
            if(redraw < 4){
                return;
            }
            redraw = 0;

            EnvironmentID newId = new EnvironmentID(0, 0, 0, 0);

            // If this continues to be shitty, let's just get the screen data right from the screen buffer :P
            // It'll be strange but always accurate
            // We'll have to detect scrolling manually (how...?) and have really specific pattern detection
            // But then it'll always visually match

            // for(int i = 0; i < 256 * 240; ++i){
            //     int x = i % 256;
            //     int y = i / 256;
            //
            //     Color color = palette[console.Ppu.BitmapData[i]];
            //     emulatorDisplay.SetPixel(x, y, color);
            // }

            // First, detect and track vertical and horizontal markers, then scroll the origin and offset stuff?
            for(int namey = 0; namey < DRAW_SIZE; ++namey){
                for(int namex = 0; namex < DRAW_SIZE; ++namex){

                }
            }
        } else {
            if(emu.started){
                started = true;
                // ppuMem = emu.GetConsole().PpuMemory;
                // bgAddress = emu.GetConsole().Ppu.GetBGPatternTableAddress();
            }
        }
    }
}

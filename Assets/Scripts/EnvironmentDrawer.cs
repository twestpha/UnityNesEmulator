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

public class Position2 {
    public int x;
    public int y;
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
                        matchesFailed = 0;

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

        matchesFailed++;
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
    public const int DRAW_WIDTH = 16;
    public const int DRAW_HEIGHT = 15;
    public const int MARKER_COUNT = 16;

    public Texture2D display;
    public Texture2D emulatorDisplay;
    public GameObject nameTableOrigin;

    public EnvironmentMatch[] matches;

    public GameObject blankTile;

    private EnvironmentID[,] ids;
    private GameObject[,] instances;

    private bool started = false;
    private int redraw = 0;

    private Emulator emu;
    private Marker[] markers;

    void Start(){
        emu = GetComponent<Emulator>();

        ids = new EnvironmentID[DRAW_WIDTH, DRAW_HEIGHT];
        instances = new GameObject[DRAW_WIDTH, DRAW_HEIGHT];

        redraw = 0;

        markers = new Marker[MARKER_COUNT];
    }

    void Update(){
        if(started){
            // Get "average" delta with voting
            // If not enough even find a match, we've done a scene change, and we need to refresh all markers

            // Still yet to come:
            // Detecting tiles, then drawing them to "correct" index
            // Sliding the "world" transform around based on the average delta
            Dictionary<int, int> xOffsetVote = new Dictionary<int, int>();
            Dictionary<int, int> yOffsetVote = new Dictionary<int, int>();
            int validMarkerCount = 0;

            for(int i = 0; i < MARKER_COUNT; ++i){
                if(markers[i].FindMatch(emu.GetConsole().Ppu.BitmapData)){
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

                    // Debug.Log("DELTAS: (" + testMarker.deltaX + ", " + testMarker.deltaY + ")");
                    markers[i].DrawMarkerToDisplay(emulatorDisplay);
                }

                if(markers[i].matchesFailed > 20 /* frames */){
                    int xrand = 32 + (int)(Random.value * (float)(208 - 32));
                    int yrand = 32 + (int)(Random.value * (float)(208 - 32));

                    markers[i] = new Marker(
                        xrand, yrand,
                        emu.GetConsole().Ppu.BitmapData
                    );
                }
            }

            // If not enough markers are valid, reset them all
            if(validMarkerCount < MARKER_COUNT / 4){
                ResetAllMarkers();
                return;
            }

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

            Debug.Log("DELTA: (" + xDelta + ", " + yDelta + ")");

            // redraw++;
            // if(redraw < 4){
            //     return;
            // }
            // redraw = 0;

            // EnvironmentID newId = new EnvironmentID(0, 0, 0, 0);

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
            // for(int namey = 0; namey < DRAW_SIZE; ++namey){
            //     for(int namex = 0; namex < DRAW_SIZE; ++namex){
            //
            //     }
            // }
        } else {
            if(emu.started){
                started = true;
                ResetAllMarkers();
            }
        }
    }

    void ResetAllMarkers(){
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
}

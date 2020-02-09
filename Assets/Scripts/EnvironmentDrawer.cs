using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO list of "matches" and corresponding prefab types for easy unity entry
// at start, setup mapping from long -> prefab using the above info
// And also keep a big long[] of tile ids and update it... every frame? Often?
// Maybe alternate frames between refreshing tilemap and "drawing" prefabs to screen
// Only redraw dirty ones :D

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

public class EnvironmentDrawer : MonoBehaviour {
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
    public GameObject nameTableOrigin;

    public EnvironmentMatch[] matches;

    private EnvironmentID[,] ids;
    private GameObject[,] instances;
    // private Dictionary<EnvironmentID, GameObject> mapping;

    private bool started = false;

    private Emulator emu;
    private Nescafe.PpuMemory ppuMem;
    private ushort bgAddress;

    private Color[] colors;

    public int nmx, nmy;

    public ushort A;
    public ushort B;
    public ushort C;
    public ushort D;

    void Start(){
        emu = GetComponent<Emulator>();

        ids = new EnvironmentID[64, 64];
        instances = new GameObject[64, 64];

        // mapping = new Dictionary<EnvironmentID, GameObject>();
        // for(int i = 0; i < matches.Length; ++i){
        //     mapping.Add(matches[i].id, matches[i].prefab);
        // }

        SetupColors();
    }

    void SetupColors(){
        colors = new Color[4];
        colors[0] = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        colors[1] = new Color(0.6f, 0.6f, 0.6f, 1.0f);
        colors[2] = new Color(0.3f, 0.3f, 0.3f, 1.0f);
        colors[3] = new Color(0.0f, 0.0f, 0.0f, 0.0f);
    }

    private Color GetColor(byte num){
        if(num == 0x03){
            return colors[0];
        } else if(num == 0x02){
            return colors[1];
        } else if(num == 0x01){
            return colors[2];
        } else {
            return colors[3];
        }
    }

    void Update(){
        if(Input.GetKeyDown(KeyCode.A)){
            nmx--;
        } else if(Input.GetKeyDown(KeyCode.D)){
            nmx++;
        } else if(Input.GetKeyDown(KeyCode.W)){
            nmy--;
        } else if(Input.GetKeyDown(KeyCode.S)){
            nmy++;
        }

        if(started){
            int updateTiles = 0;
            EnvironmentID newId = new EnvironmentID(0, 0, 0, 0);

            for(int namey = 0; namey < 64; ++namey){
                for(int namex = 0; namex < 64; ++namex){
                    // The max of those -> ushort memory = (ushort)(0x2000 + (x * 2) + (y * 0x20 * 2));
                    // 0x2000 + (126) + (3776)
                    // should equal 0x2FBF

                    EnvironmentID prevId = ids[namex, namey];

                    GetIdsForNameIndex(namex, namey, ref newId.idA, ref newId.idB, ref newId.idC, ref newId.idD);
                    if(newId != prevId){
                        // Honestly it's probably "cheapest" to just swap out the prefab right here
                        // i.e. only swap what changed
                        updateTiles++;

                        if(instances[namex, namey]){
                            Destroy(instances[namex, namey]);
                        }

                        ids[namex, namey] = newId;

                        GameObject match = null;
                        for(int i = 0; i < matches.Length; ++i){
                            if(matches[i].id == newId){
                                match = matches[i].prefab;
                                break;
                            }
                        }

                        if(match){
                            // Debug.Log("Matched Tile! " + newId.idA + ", " + newId.idB + ", " + newId.idC + ", " + newId.idD);

                            GameObject newInstance = Object.Instantiate(match, nameTableOrigin.transform);
                            newInstance.transform.localPosition = new Vector3(namex * 2.0f, 0.0f, namey * 2.0f);
                            instances[namex, namey] = newInstance;
                        } else {
                            instances[namex, namey] = null;
                        }

                    }
                }
            }
            if(updateTiles > 0){
                // Debug.LogError("Updated " + updateTiles + " tiles.");
            }

            DebugDrawNM();
        } else {
            if(emu.started){
                started = true;
                ppuMem = emu.GetConsole().PpuMemory;
                bgAddress = emu.GetConsole().Ppu.GetBGPatternTableAddress();
            }
        }
    }

    public void GetIdsForNameIndex(int x, int y, ref ushort a, ref ushort b, ref ushort c, ref ushort d){
        for(int i = 0; i < 4; ++i){
            // Get tiles in groups of 4
            // (+0   , +1)
            // (+0x20, +0x21)

            // Strides of 2x2
            ushort memory = (ushort)(0x2000 + (x * 2) + (y * 0x20 * 2));

            if(i == 0){
                memory += 0x00;
            } else if(i == 1){
                memory += 0x01;
            } else if(i == 2){
                memory += 0x20;
            } else if(i == 3){
                memory += 0x21;
            }

            byte nameAddress = ppuMem.Read(memory);

            for(int tiley = 0; tiley < 8 /* pixels tall */; ++tiley){
                ushort patternAddressLo = (ushort)(bgAddress + (nameAddress * 16) + tiley);
                ushort patternAddressHi = (ushort)(bgAddress + (nameAddress * 16) + tiley + 8);

                byte loColorByte = ppuMem.Read(patternAddressLo);
                byte hiColorByte = ppuMem.Read(patternAddressHi);

                // Striped Pattern (sideways :P)
                //  x
                //  ^
                //  |
                //  +--> Y
                //
                // 0x0000 0x0000   0x0000 0x0000
                // 0x0000 0x0000   0xFFFF 0xFFFF
                // 0x0000 0x0000   0x0000 0x0000
                // 0xFFFF 0xFFFF   0x0000 0x0000
                //
                // 0x0000 0x0000   0x0000 0x0000
                // 0x0000 0x0000   0xFFFF 0xFFFF
                // 0x0000 0x0000   0x0000 0x0000
                // 0xFFFF 0xFFFF   0x0000 0x0000

                if(i == 0 && tiley == 0){
                    a = (ushort)((hiColorByte << 8) | loColorByte);
                } else if(i == 1 && tiley == 2){
                    b = (ushort)((hiColorByte << 8) | loColorByte);
                } else if(i == 2 && tiley == 0){
                    c = (ushort)((hiColorByte << 8) | loColorByte);
                } else if(i == 3 && tiley == 2){
                    d = (ushort)((hiColorByte << 8) | loColorByte);
                }
            }
        }
    }

    void DebugDrawNM(){
        for(int i = 0; i < 4; ++i){
            // Get tiles in groups of 4
            // (+0   , +1)
            // (+0x20, +0x21)

            // Strides of 2x2
            ushort memory = (ushort)(0x2000 + (nmx) + (nmy * 0x20));
            int xPlus = 0;
            int yPlus = 0;

            if(i == 0){
                memory += 0x00;
                xPlus = 0;
                yPlus = 0;
            } else if(i == 1){
                memory += 0x01;
                xPlus = 8;
                yPlus = 0;
            } else if(i == 2){
                memory += 0x20;
                xPlus = 0;
                yPlus = 8;
            } else if(i == 3){
                memory += 0x21;
                xPlus = 8;
                yPlus = 8;
            }

            byte nameAddress = ppuMem.Read(memory);

            for(int tiley = 0; tiley < 8 /* pixels tall */; ++tiley){
                ushort patternAddressLo = (ushort)(bgAddress + (nameAddress * 16) + tiley);
                ushort patternAddressHi = (ushort)(bgAddress + (nameAddress * 16) + tiley + 8);

                byte loColorByte = ppuMem.Read(patternAddressLo);
                byte hiColorByte = ppuMem.Read(patternAddressHi);

                if(i == 0 && tiley == 0){
                    A = (ushort)((hiColorByte << 8) | loColorByte);
                } else if(i == 1 && tiley == 2){
                    B = (ushort)((hiColorByte << 8) | loColorByte);
                } else if(i == 2 && tiley == 0){
                    C = (ushort)((hiColorByte << 8) | loColorByte);
                } else if(i == 3 && tiley == 2){
                    D = (ushort)((hiColorByte << 8) | loColorByte);
                }

                for(int tilex = 0; tilex < 8 /* pixels wide */; ++tilex){
                    byte loColorBit = (byte)((loColorByte >> (7 - tilex)) & 1);
                    byte hiColorBit = (byte)((hiColorByte >> (7 - tilex)) & 1);
                    byte colorNum = (byte)((hiColorBit << 1) | (loColorBit) & 0x03);

                    display.SetPixel(tilex + xPlus, tiley + yPlus, GetColor(colorNum));
                }

            }
        }
        display.Apply();
    }
}

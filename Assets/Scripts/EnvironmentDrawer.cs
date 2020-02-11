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

    public int nmx, nmy;

    public ushort A;
    public ushort B;
    public ushort C;
    public ushort D;

    void Start(){
        emu = GetComponent<Emulator>();

        ids = new EnvironmentID[DRAW_SIZE, DRAW_SIZE];
        instances = new GameObject[DRAW_SIZE, DRAW_SIZE];

        redraw = 0;

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

            // First, detect and track vertical and horizontal markers, then scroll the origin and offset stuff?
            for(int namey = 0; namey < DRAW_SIZE; ++namey){
                for(int namex = 0; namex < DRAW_SIZE; ++namex){

                }
            }
        } else {
            // if(emu.started){
            //     started = true;
            //     ppuMem = emu.GetConsole().PpuMemory;
            //     bgAddress = emu.GetConsole().Ppu.GetBGPatternTableAddress();
            // }
        }
    }
}

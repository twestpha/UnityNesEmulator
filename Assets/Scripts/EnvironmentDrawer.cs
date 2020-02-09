using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentDrawer : MonoBehaviour {

    // Mapping of some hex value (representing the graphic) to prefab

    // List of instantiated prefabs at name table "locations"

    private bool started = false;

    private Emulator emu;
    private Nescafe.PpuMemory ppuMem;

    public byte TestByte;
    public byte TestByte2;

    private ushort bgAddress;

    public Texture2D display;

    void Start(){
        emu = GetComponent<Emulator>();
    }

    private Color GetColor(byte bits){
        if((bits & 0x03) == 0x03){
            return new Color(1.0f, 1.0f, 1.0f, 1.0f);
        } else if((bits & 0x02) == 0x02){
            return new Color(0.6f, 0.6f, 0.6f, 1.0f);
        } else if((bits & 0x01) == 0x01){
            return new Color(0.3f, 0.3f, 0.3f, 1.0f);
        } else {
            return new Color(0.0f, 0.0f, 0.0f, 0.0f);
        }
    }

    void Update(){
        if(!started){
            if(emu.started){
                started = true;
                ppuMem = emu.GetConsole().PpuMemory;
                bgAddress = emu.GetConsole().Ppu.GetBGPatternTableAddress();
            }
        }

        if(started){
            // Detect when nametable changed?

            // Get tiles in groups of 4
            // (+0   , +1)
            // (+0x20, +0x21)

            // Address of name table (points to something in pattern table)
            byte thingy = ppuMem.Read(0x2000);

            ushort addr = (ushort)(bgAddress + (thingy * 16));
            ushort addr2 = (ushort)(bgAddress + (thingy * 16) + 8);

            TestByte = ppuMem.Read(addr);
            TestByte2 = ppuMem.Read(addr2);

            display.SetPixel(0, 0, GetColor((byte)(TestByte >> 0)));
            display.SetPixel(1, 0, GetColor((byte)(TestByte >> 2)));
            display.SetPixel(2, 0, GetColor((byte)(TestByte >> 4)));
            display.SetPixel(3, 0, GetColor((byte)(TestByte >> 6)));

            display.SetPixel(4, 0, GetColor((byte)(TestByte2 >> 0)));
            display.SetPixel(5, 0, GetColor((byte)(TestByte2 >> 2)));
            display.SetPixel(6, 0, GetColor((byte)(TestByte2 >> 4)));
            display.SetPixel(7, 0, GetColor((byte)(TestByte2 >> 6)));

            display.Apply();

            // This indicates an entry in the "left" side of the pattern table
            // I think I can get... something? to do with the sprite from _sprites[] in ppu

            // Ultimately I'm gonna get an 8x8 pixel, with a 2 bits per pixel
            // So that's like 128 bits? 64 px * 2 bits per pixel...
            // Maybe just get the first two rows (32 int) and use that as a "hash"

            /*
            void FetchTileBitfieldLo()
            {
                ushort address = (ushort)(_bgPatternTableAddress + (_nameTableByte * 16) + FineY());
                _tileBitfieldLo = _memory.Read(address);
            }

            void FetchTileBitfieldHi()
            {
                ushort address = (ushort)(_bgPatternTableAddress + (_nameTableByte * 16) + FineY() + 8);
                _tileBitfieldHi = _memory.Read(address);
            }*/
        }
    }
}

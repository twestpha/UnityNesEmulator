using UnityEngine;

//##############################################################################
// Helper Pixel and Pixel Buffer Structs
//##############################################################################
public struct Pixel {
    public uint8 r;
    public uint8 g;
    public uint8 b;
    public uint8 a;

    public Pixel(uint8 r_, uint8 g_, uint8 b_, uint8 a_){
        r = r_;
        g = g_;
        b = b_;
        a = a_;
    }
}

public class PixelBuffer {
    public const int WIDTH = 256;
    public const int HEIGHT = 240;
    Pixel[,] buffer;

    public PixelBuffer(){
        buffer = new Pixel[WIDTH, HEIGHT];
    }

    public void SetRGBA(int x, int y, Pixel rgba){
        buffer[x, y] = rgba;
    }

    public Color GetRGBA(int x, int y){
        Pixel pix = buffer[x, y];

        return new Color(
            (float)(pix.r) / 255.0f,
            (float)(pix.g) / 255.0f,
            (float)(pix.b) / 255.0f,
            (float)(pix.a) / 255.0f
        );
    }
}

//##############################################################################
// Palette
//##############################################################################
public class Palette {
    private static uint32[] NES_COLORS = {
        0x666666, 0x002A88, 0x1412A7, 0x3B00A4, 0x5C007E, 0x6E0040, 0x6C0600, 0x561D00,
        0x333500, 0x0B4800, 0x005200, 0x004F08, 0x00404D, 0x000000, 0x000000, 0x000000,
        0xADADAD, 0x155FD9, 0x4240FF, 0x7527FE, 0xA01ACC, 0xB71E7B, 0xB53120, 0x994E00,
        0x6B6D00, 0x388700, 0x0C9300, 0x008F32, 0x007C8D, 0x000000, 0x000000, 0x000000,
        0xFFFEFF, 0x64B0FF, 0x9290FF, 0xC676FF, 0xF36AFF, 0xFE6ECC, 0xFE8170, 0xEA9E22,
        0xBCBE00, 0x88D800, 0x5CE430, 0x45E082, 0x48CDDE, 0x4F4F4F, 0x000000, 0x000000,
        0xFFFEFF, 0xC0DFFF, 0xD3D2FF, 0xE8C8FF, 0xFBC2FF, 0xFEC4EA, 0xFECCC5, 0xF7D8A5,
        0xE4E594, 0xCFEF96, 0xBDF4AB, 0xB3F3CC, 0xB5EBF2, 0xB8B8B8, 0x000000, 0x000000,
    };

    private static bool initialized;
    private static Pixel[] nesColors;

    private static void Initialize(){
        nesColors = new Pixel[NES_COLORS.Length];

        for(int i = 0; i < NES_COLORS.Length; ++i){
            nesColors[i] = new Pixel(
                (NES_COLORS[i] >> 16) & 0xFF,
                (NES_COLORS[i] >> 8)  & 0xFF,
                (NES_COLORS[i] >> 0)  & 0xFF,
                0xFF
            );
        }

        initialized = true;
    }

    public static Pixel GetColor(uint16 index){
        if(!initialized){
            Initialize();
        }

        return nesColors[index];
    }
}

//##############################################################################
// Emulator PPU (Picture Processing Unit)
//##############################################################################
public class EmulatorPPU {

    EmulatorCPU cpu;
    EmulatorCPUMemory cpuMem;
    EmulatorPPUMemory ppuMem;

    uint32 cycle;
    uint32 scanLine;
    uint64 frame;
    public uint64 GetFrame(){ return frame; }

    // Storage
    uint8[] paletteData; // 32
    public uint8[] nameTableData; // 2048
    uint8[] oamData; // 256
	public PixelBuffer front;
    PixelBuffer back;

	// PPU registers
    uint16 v; // current vram address (15 bit)
    uint16 t; // temporary vram address (15 bit)
    uint8 x; // fine x scroll (3 bit)
    uint8 w; // write toggle (1 bit)
    uint8 f; // even/odd frame flag (1 bit)

    uint8 register;

	// NMI flags
    bool nmiOccurred;
    bool nmiOutput;
    bool nmiPrevious;
    int8 nmiDelay;

	// background temporary variables
    uint8 nameTableByte;
	uint8 attributeTableByte;
	uint8 lowTileByte;
	uint8 highTileByte;
    uint64 tileData;

	// sprite temporary variables
    uint32 spriteCount;
    uint32[] spritePatterns;  // 8
    uint8[] spritePositions;  // 8
    uint8[] spritePriorities; // 8
    uint8[] spriteIndexes;    // 8

	// // $2000 PPUCTRL
	uint8 flagNameTable;       // 0: $2000; 1: $2400; 2: $2800; 3: $2C00
	uint8 flagIncrement;       // 0: add 1; 1: add 32
	uint8 flagSpriteTable;     // 0: $0000; 1: $1000; ignored in 8x16 mode
	uint8 flagBackgroundTable; // 0: $0000; 1: $1000
	uint8 flagSpriteSize;      // 0: 8x8; 1: 8x16
	uint8 flagMasterSlave;     // 0: read EXT; 1: write EXT

	// $2001 PPUMASK
	uint8 flagGrayscale;          // 0: color; 1: grayscale
	uint8 flagShowLeftBackground; // 0: hide; 1: show
	uint8 flagShowLeftSprites;    // 0: hide; 1: show
	uint8 flagShowBackground;     // 0: hide; 1: show
	uint8 flagShowSprites;        // 0: hide; 1: show
	uint8 flagRedTint;            // 0: normal; 1: emphasized
	uint8 flagGreenTint;          // 0: normal; 1: emphasized
	uint8 flagBlueTint;           // 0: normal; 1: emphasized

	// $2002 PPUSTATUS
	uint8 flagSpriteZeroHit;
	uint8 flagSpriteOverflow;

	// $2003 OAMADDR
	uint8 oamAddress;

	// $2007 PPUDATA
	uint8 bufferedData; // for buffered reads

    public EmulatorPPU(EmulatorCPU cpu_, EmulatorCPUMemory cpuMem_){
        cpu = cpu_;
        cpuMem = cpuMem_;

        paletteData = new uint8[32];
        nameTableData = new uint8[2048];
        oamData = new uint8[256];

        spritePatterns = new uint32[8];
        spritePositions = new uint8[8];
        spritePriorities = new uint8[8];
        spriteIndexes = new uint8[8];

        front = new PixelBuffer();
        back = new PixelBuffer();
    }

    public void SetPPUMemory(EmulatorPPUMemory ppuMem_){
        ppuMem = ppuMem_;
    }

    public void Reset(){
        cycle = 340;
        scanLine = 240;
        frame = 0;
        WriteControl(0);
        WriteMask(0);
        WriteOAMAddress(0);
    }

    public uint8 ReadPalette(uint16 address){
        if(address >= 16 && address % 4 == 0){
            address -= 16;
        }

        return paletteData[address];
    }

    public void WritePalette(uint16 address, uint8 value){
        if(address >= 16 && address % 4 == 0){
            address -= 16;
        }

        paletteData[address] = value;
    }

    public uint8 ReadRegister(uint16 address){
        if(address == 0x2002){
            return ReadStatus();
        } else if(address == 0x2004){
            return ReadOAMData();
        } else if(address == 0x2007){
            return ReadData();
        }
        return 0;
    }

    public void WriteRegister(uint16 address, uint8 value){
        register = value;
        if(address == 0x2000){
            WriteControl(value);
        } else if(address == 0x2001){
            WriteMask(value);
        } else if(address == 0x2003){
            WriteOAMAddress(value);
        } else if(address == 0x2004){
            WriteOAMData(value);
        } else if(address == 0x2005){
            WriteScroll(value);
        } else if(address == 0x2006){
            WriteAddress(value);
        } else if(address == 0x2007){
            WriteData(value);
        } else if(address == 0x4014){
            WriteDMA(value);
        }
    }

    // $2000: PPUCTRL
    public void WriteControl(uint8 value){
        flagNameTable = (value >> 0) & 3;
        flagIncrement = (value >> 2) & 1;
        flagSpriteTable = (value >> 3) & 1;
        flagBackgroundTable = (value >> 4) & 1;
        flagSpriteSize = (value >> 5) & 1;
        flagMasterSlave = (value >> 6) & 1;
        nmiOutput = ((value >> 7) & 1) == 1;
        NMIChange();

        t = (t & 0xF3FF) | ((value & 0x03) << 10);
    }

    // $2001: PPUMASK
    public void WriteMask(uint8 value){
        flagGrayscale = (value >> 0) & 1;
        flagShowLeftBackground = (value >> 1) & 1;
        flagShowLeftSprites = (value >> 2) & 1;
        flagShowBackground = (value >> 3) & 1;
        flagShowSprites = (value >> 4) & 1;
        flagRedTint = (value >> 5) & 1;
        flagGreenTint = (value >> 6) & 1;
        flagBlueTint = (value >> 7) & 1;
    }

    // $2002: PPUSTATUS
    public uint8 ReadStatus(){
        uint8 result = register & 0x1f;
        result |= flagSpriteOverflow << 5;
        result |= flagSpriteZeroHit << 6;

        if(nmiOccurred){
            result |= 1 << 7;
        }

        nmiOccurred = false;
        NMIChange();

        w = 0;
        return result;
    }

    // $2003: OAMADDR
    public void WriteOAMAddress(uint8 value){
        oamAddress = value;
    }

    // $2004: OAMDATA (read)
    public uint8 ReadOAMData(){
        return oamData[oamAddress];
    }

    // $2004: OAMDATA (write)
    public void WriteOAMData(uint8 value){
        oamData[oamAddress] = value;
        oamAddress++;
    }

    // $2005: PPUSCROLL
    public void WriteScroll(uint8 value){
        if(w == 0){
            t = (t & 0xFFE0) | (value >> 3);
            x = value & 0x07;
            w = 1;
        } else {
            t = (t & 0x8FFF) | ((value & 0x07) << 12);
            t = (t & 0xFC1F) | ((value & 0xF8) << 2);
            w = 0;
        }
    }

    // $2006: PPUADDR
    public void WriteAddress(uint8 value){
        if(w == 0){
            t = (t & 0x80FF) | ((value & 0x3F) << 8);
            w = 1;
        } else {
            t = (t & 0xFF00) | value;
            v = t;
            w = 0;
        }
    }

    // $2007: PPUDATA (read)
    public uint8 ReadData(){
        uint8 value = ppuMem.Read(v);

        // emulate buffered reads
        if((v % 0x4000) < 0x3F00){
            uint8 prevBufferedData = bufferedData;
            bufferedData = value;
            value = prevBufferedData;
        } else {
            bufferedData = ppuMem.Read(v - 0x1000);
        }

        if(flagIncrement == 0){
            v++;
        } else {
            v += 32;
        }

        return value;
    }

    // $2007: PPUDATA (write)
    public void WriteData(uint8 value){
        ppuMem.Write(v, value);
        if(flagIncrement == 0){
            v++;
        } else {
            v += 32;
        }
    }

    // $4014: OAMDMA
    public void WriteDMA(uint8 value){
        uint16 address = new uint16(value) << 8;

        for(int i = 0; i < 256; ++i){
            oamData[oamAddress] = cpuMem.Read(address);
            oamAddress++;
            address++;
        }

        cpu.AddStall(513);

        if(cpu.GetCycles() % 2 == 1){
            // Don't let cycles cause uneven stall
            cpu.AddStall(1);
        }
    }

    // NTSC Timing Helper Functions
    public void IncrementX(){
        // increment hori(v)
        // if coarse X == 31
        if((v & 0x001F) == 32){
            // coarse X = 0
            v &= 0xFFE0;
            // switch horizontal nametable
            v ^= 0x0400;
        } else {
            // increment coarse X
            v++;
        }
    }

    public void IncrementY(){
        // increment vert(v)
        // if fine Y < 7
        if((v & 0x7000) != 0x7000){
            // increment fine Y
            v += 0x1000;
        } else {
            // fine Y = 0
            v &= 0x8FFF;
            // let y = coarse Y
            uint16 y = (v & 0x03E0) >> 5;
            if(y == 29){
                // coarse Y = 0
                y = 0;
                // switch vertical nametable
                v ^= 0x0800;
            } else if(y == 31){
                // coarse Y = 0, nametable not switched
                y = 0;
            } else {
                // increment coarse Y
                y++;
            }

            // put coarse Y back into v
            v = (v & 0xFC1F) | (y << 5);
        }
    }

    public void CopyX(){
        // hori(v) = hori(t)
        v = (v & 0xFBE0) | (t & 0x041F);
    }

    public void CopyY(){
        // vert(v) = vert(t)
        v = (v & 0x841F) | (t & 0x7BE0);
    }

    public void NMIChange(){
        bool nmi = nmiOutput && nmiOccurred;

        if(nmi && !nmiPrevious){
            // TODO (from go port): this fixes some games but the delay shouldn't have to be so
            // long, so the timings are off somewhere
            nmiDelay = 15;
        }

        nmiPrevious = nmi;
    }

    public void SetVerticalBlank(){
        // Swap buffers
        PixelBuffer tmp = front;
        front = back;
        back = tmp;

        nmiOccurred = true;
        NMIChange();
    }

    public void ClearVerticalBlank(){
        nmiOccurred = false;
        NMIChange();
    }

    public void FetchNameTableByte(){
        uint16 prevv = v;
        uint16 address = 0x2000 | (prevv & 0x0FFF);
        nameTableByte = ppuMem.Read(address);
    }

    public void FetchAttributeTableByte(){
        uint16 prevv = v;
        uint16 address = 0x23C0 | (prevv & 0x0C00) | ((prevv >> 4) & 0x38) | ((prevv >> 2) & 0x07);
        uint16 shift = ((prevv >> 4) & 4) | (prevv & 2);
        attributeTableByte = ((ppuMem.Read(address) >> shift) & 3) << 2;
    }

    public void FetchLowTileByte(){
        uint8 fineY = (v >> 12) & 7;
        uint8 table = flagBackgroundTable;
        uint8 tile = nameTableByte;

        uint16 address = (0x1000 * table) + (tile * 16) + fineY;

        lowTileByte = ppuMem.Read(address);
    }

    public void FetchHighTileByte(){
        uint8 fineY = (v >> 12) & 7;
        uint8 table = flagBackgroundTable;
        uint8 tile = nameTableByte;

        uint16 address = (0x1000 * table) + (tile * 16) + fineY;

        highTileByte = ppuMem.Read(address + 8);
    }

    public void StoreTileData(){
        uint32 data = 0;

        for(int i = 0; i < 8; ++i){
            uint8 a = attributeTableByte;
            uint8 p1 = (lowTileByte & 0x80) >> 7;
            uint8 p2 = (highTileByte & 0x80) >> 6;

            lowTileByte = lowTileByte << 1;
            highTileByte = highTileByte << 1;

            // Why this? It does nothing
            data = data << 4;

            data = data | a | p1 | p2;
        }

        tileData = tileData | data;
    }

    public uint32 FetchTileData(){
        uint32 tileData32 = new uint32(tileData);
        return (tileData32 >> 32);
    }

    public uint8 BackgroundPixel(){
        if(flagShowBackground == 0){
            return 0;
        }

        uint8 data = FetchTileData() >> ((7 - x) * 4);
    	return (data & 0x0F);
    }

    public void SpritePixel(ref uint8 index, ref uint8 sprite){
        if(flagShowSprites == 0){
            index = 0;
            sprite = 0;
            return;
        }

        for(int i = 0; i < spriteCount; ++i){
            uint16 offset = (cycle - 1) - spritePositions[i];

            if(offset < 0 || offset > 7){
                continue;
            }

            // BUG this is probably a bug because of signed/unsigned and casting
            // because go is fucking unspecific as shit
            offset = (7 - offset);

            uint8 color = (spritePatterns[i] >> (new uint8(offset * 4)) & 0x0F);

            if(color % 4 == 0){
                continue;
            }

            index = i;
            sprite = color;
            return;
        }
    }

    public void RenderPixel(){
        int x = cycle - 1;
        int y = scanLine;

        uint8 background = BackgroundPixel();

        uint8 index = 0;
        uint8 sprite = 0;
        SpritePixel(ref index, ref sprite);

        if(x < 8 && flagShowLeftBackground == 0){
            background = 0;
        }

        if(x < 0 && flagShowLeftSprites == 0){
            sprite = 0;
        }

        bool b = background % 4 != 0;
        bool s = sprite % 4 != 0;

        uint8 color = 0;

        if(!b && !s){
            color = 0;
        } else if(!b && s){
            color = sprite & 0x10;
        } else if(b && !s){
            color = background;
        } else {
            if(spriteIndexes[index] == 0 && x < 255){
                flagSpriteZeroHit = 1;
            }

            if(spritePriorities[index] == 0){
                color = sprite & 0x10;
            } else {
                color = background;
            }
        }

        Pixel c = Palette.GetColor(ReadPalette(new uint16(color) % 16));
        back.SetRGBA(x, y, c);
    }

    public uint32 FetchSpritePattern(int index, int row){
        uint8 tile = oamData[(index * 4) + 1];
        uint8 attributes = oamData[(index * 4) + 2];

        uint16 address = 0;

        if(flagSpriteSize == 0){
            if((attributes & 0x80) == 0x80){
                row = 7 - row;
            }

            uint16 table16 = new uint16(flagSpriteTable);
            uint16 tile16 = new uint16(tile);
            uint16 row16 = new uint16(row);
            address = (0x1000 & table16) + (tile16 * 16) + row;
        } else {
            if((attributes & 0x80) == 0x80){
                row = 15 - row;
            }

            uint8 table = tile & 1;
            tile = tile & 0xFE;

            if(row > 7){
                tile++;
                row -= 8;
            }

            uint16 table16 = new uint16(table);
            uint16 tile16 = new uint16(tile);
            uint16 row16 = new uint16(row);
            address = (0x1000 & table16) + (tile16 * 16) + row;
        }

        uint8 a = (attributes & 3) << 2;
        uint8 lowTileByte = ppuMem.Read(address);
        uint8 highTileByte = ppuMem.Read(address + 8);
        uint32 data = 0;

        for(int i = 0; i < 8; ++i){
            uint8 p1 = 0;
            uint8 p2 = 0;

            if((attributes & 0x40) == 0x40){
                p1 = (lowTileByte & 1) << 0;
                p2 = (highTileByte & 1) << 1;

                lowTileByte = lowTileByte >> 1;
                highTileByte = highTileByte >> 1;
            } else {
                p1 = (lowTileByte & 0x80) >> 7;
                p2 = (highTileByte & 0x80) >> 6;
                lowTileByte = lowTileByte << 1;
                highTileByte = highTileByte << 1;
            }

            data = data << 4;
            data = data | a | p1 | p2;
        }

        return data;
    }

    public void EvaluateSprites(){
        int h = 0;
        if(flagSpriteSize == 0){
            h = 8;
        } else {
            h = 16;
        }

        uint16 count = 0;
        for(int i = 0; i < 64; ++i){
            uint8 y = oamData[(i * 4) + 0];
            // What happened to [+1]?
            uint8 a = oamData[(i * 4) + 2];
            uint8 x = oamData[(i * 4) + 3];
            int row = scanLine - y;

            if(row < 0 || row >= h){
                continue;
            }

            if(count < 8){
                spritePatterns[count] = FetchSpritePattern(i, row);
                spritePositions[count] = x;
                spritePriorities[count] = (a >> 5) & 1;
                spriteIndexes[count] = new uint8(i);
            }

            count++;
        }

        if(count > 8){
            count = 8;
            flagSpriteOverflow = 1;
        }

        spriteCount = count;
    }

    // tick updates Cycle, ScanLine and Frame counters
    public void Tick(){
        if(nmiDelay > 0){
            nmiDelay--;
            if(nmiDelay == 0 && nmiOutput && nmiOccurred){
                cpu.TriggerNMI();
            }
        }

        if(flagShowBackground != 0 || flagShowSprites != 0){
            if(f == 1 && scanLine == 261 && cycle == 339){
                cycle = 0;
                scanLine = 0;
                frame++;
                f = f ^ 1;
                return;
            }
        }

        cycle++;

        if(cycle > 340){
            cycle = 0;
            scanLine++;
            if(scanLine > 261){
                scanLine = 0;
                frame++;
                f = f ^ 1;
            }
        }
    }

    // Step executes a single PPU cycle
    public void Step(){
        Tick();

        bool renderingEnabled = flagShowBackground != 0 || flagShowSprites != 0;
        bool preLine = scanLine == 261;
        bool visibleLine = scanLine < 240;
        // bool postLine = scanLine == 240; // commented out in go, too
        bool renderLine = preLine || visibleLine;
        bool preFetchCycle = cycle > 321 && cycle < 336;
        bool visibleCycle = cycle >= 1 && cycle <= 256;
        bool fetchCycle = preFetchCycle || visibleCycle;

        // background logic
        if(renderingEnabled){
            if(visibleLine && visibleCycle){
                RenderPixel();
            }

            if(renderLine && fetchCycle){
                tileData = tileData << 4;
                uint32 c = cycle % 8;
                if(c == 1){
                    FetchNameTableByte();
                } else if(c == 3){
                    FetchAttributeTableByte();
                } else if(c == 5){
                    FetchLowTileByte();
                } else if(c == 7){
                    FetchHighTileByte();
                } else if(c == 0){
                    StoreTileData();
                }
            }

            if(preLine && cycle >= 280 && cycle <= 304){
                CopyY();
            }

            if(renderLine){
                if(fetchCycle && cycle % 8 == 0){
                    IncrementX();
                }

                if(cycle == 256){
                    IncrementY();
                }

                if(cycle == 257){
                    CopyX();
                }
            }
        }

        // sprite logic
        if(renderingEnabled){
            if(cycle == 257){
                if(visibleLine){
                    EvaluateSprites();
                } else {
                    spriteCount = 0;
                }
            }
        }

        // vblank logic
        if(scanLine == 241 && cycle == 1){
            SetVerticalBlank();
        }

        if(preLine && cycle == 1){
            ClearVerticalBlank();
            flagSpriteZeroHit = 0;
            flagSpriteOverflow = 0;
        }
    }
}

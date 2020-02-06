using UnityEngine;

public class EmulatorCartridgeHeader {
    public readonly uint32 NESFileMagicNumber = 0x1A53454E; // "NES "
    public uint32 magicNumber;
    public uint8 numPRG;
    public uint8 numCHR;
    public uint8 Control1;
    public uint8 Control2;
    public uint8 numRAM;

    public EmulatorCartridgeHeader(byte[] raw){
        magicNumber = (uint32)(raw[3]) << 24;
        magicNumber |= (uint32)(raw[2]) << 16;
        magicNumber |= (uint32)(raw[1]) << 8;
        magicNumber |= (uint32)(raw[0]);

        numPRG   = raw[4];
        numCHR   = raw[5];
        Control1 = raw[6];
        Control2 = raw[7];

        numRAM   = raw[8];
        // Unused 9
        // Unused 10
        // Unused 11
        // Unused 12
        // Unused 13
        // Unused 14
        // Unused 15
    }

    public bool Valid(){
        return magicNumber == NESFileMagicNumber;
    }
}

public class EmulatorCartridge {
    public const int SAVE_RAM_SIZE = 0x2000;

    public uint8[] PRG;
    public uint8[] CHR;
    public uint8[] SRAM;

    uint8 mapper;
    uint8 mirror;
    uint8 battery;

    private byte[] raw;
    public uint8[] ROM;

    public int romCount;
    public int prgCount;
    public int chrCount;

    int prgCopyProgress;
    int chrCopyProgress;
    int romCopyProgress;

    public EmulatorCartridge(byte[] raw_){
        raw = raw_;
        romCount = raw.Length - 16;
        ROM = new uint8[romCount];

        SRAM = new uint8[SAVE_RAM_SIZE];

        EmulatorCartridgeHeader header = new EmulatorCartridgeHeader(raw_);

        // verify header magic number
        if(!header.Valid()){
            Debug.LogError("Invalid NES File");
            return;
        }

        // mapper type
        uint8 mapper1 = header.Control1 >> 4;
        uint8 mapper2  = header.Control2 >> 4;
        mapper = mapper1 | (mapper2 << 4);
        // Debug.Log("Mapper: " + mapper);

        uint8 mirror1 = header.Control1 & 1;
        uint8 mirror2 = (header.Control1 >> 3) & 1;
        mirror = mirror1 | (mirror2 << 1);
        // Debug.Log("Mirror: " + mirror);

        // battery-backed RAM
        battery = header.Control1 & 1;

        // Trainers are optional flags for things like gamesharks to hook into
        // Just gonna leave this out for now
        // read trainer if present (unused)
        // if header.Control1&4 == 4 {
        //     trainer := make([]byte, 512)
        //     if _, err := io.ReadFull(file, trainer); err != nil {
        //         return nil, err
        //     }
        // }

        // // read prg-rom bank(s)
        prgCount = header.numPRG * 16384;
        PRG = new uint8[prgCount];
        prgCopyProgress = 0;
        // Debug.Log("PRG Count: " + header.numPRG);

        // read chr-rom bank(s)
        chrCount = header.numCHR * 8192;
        CHR = new uint8[chrCount];
        chrCopyProgress = 0;
        // Debug.Log("CHR Count: " + header.numCHR );

        // provide chr-rom/ram if not in file
        if(header.numCHR == 0){
            chrCount = 8192;
            CHR = new uint8[8192];
        }
    }

    public bool CopyComplete(){
        return (prgCopyProgress == prgCount) && (chrCopyProgress == chrCount) && (romCopyProgress == romCount);
    }

    public void ContinueMemoryCopy(){
        for(int i = 0; i < prgCount; ++i){
            PRG[i] = (uint8)(ROM[i]);
            prgCopyProgress++;
        }

        for(int i = 0; i < chrCount; ++i){
            PRG[i] = (uint8)(ROM[i]);
            chrCopyProgress++;
        }

        for(int i = 0; i < romCount; ++i){
            ROM[i] = raw[i + 16];
            romCopyProgress++;
        }
    }

    public int GetMapper(){
        return (int)(mapper);
    }

    public uint8 GetMirror(){
        return mirror;
    }

    public void SetMirror(uint8 mirror_){
        mirror = mirror_;
    }
}

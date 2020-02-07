using UnityEngine;

//##############################################################################
// EmulatorMemory
//##############################################################################
public class EmulatorMemory {
    public EmulatorMemory(){}
    public virtual uint8 Read(uint16 address){ return 0; }
    public virtual void Write(uint16 address, uint8 value){}
}

//##############################################################################
// EmulatorCPUMemory
//##############################################################################
public class EmulatorCPUMemory : EmulatorMemory {
    private uint8[] RAM;
    private EmulatorMapperCore mapper;
    private EmulatorPPU ppu;

    public EmulatorCPUMemory(uint8[] RAM_, EmulatorMapperCore mapper_){
        RAM = RAM_;
        mapper = mapper_;
    }

    public void SetPPU(EmulatorPPU ppu_){
        ppu = ppu_;
    }

    public override uint8 Read(uint16 address){
        if(address < 0x2000){
            return RAM[address % 0x0800];
        } else if(address < 0x4000){
            return ppu.ReadRegister((address % 8) + 0x2000);
        } else if(address == 0x4014){
            return ppu.ReadRegister(address);
        } else if(address == 0x4015){
            // return mem.console.APU.readRegister(address)
        } else if(address == 0x4016){
            // return mem.console.Controller1.Read()
            // return 0xFF; // Hold down all the buttons?
        } else if(address == 0x4017){
            // return mem.console.Controller2.Read()
        } else if(address < 0x6000){
            // I/O Registers...?
            Debug.Log("I/O Read?");
        } else if(address > 0x6000){
            return mapper.Read(address);
        } else {
            Debug.LogError("Unhandled CPU read at address: " + address);
        }

        return 0;
    }

    public override void Write(uint16 address, uint8 value){
        if(address < 0x2000){
            RAM[address % 0x0800] = value;
        } else if(address < 0x4000){
            ppu.WriteRegister((address % 8) + 0x2000, value);
        } else if(address < 0x4014){
            // mem.console.APU.writeRegister(address, value)
        } else if(address == 0x4014){
            ppu.WriteRegister(address, value);
        } else if(address == 0x4015){
            // mem.console.APU.writeRegister(address, value)
        } else if(address == 0x4016){
            // mem.console.Controller1.Write(value)
            // mem.console.Controller2.Write(value)
        } else if(address == 0x4017){
            // mem.console.APU.writeRegister(address, value)
        } else if(address < 0x6000){
            // I/O Registers...?
        } else if(address > 0x6000){
            mapper.Write(address, value);
        } else {
            Debug.LogError("Unhandled CPU write at address: " + address);
        }
    }
}

//##############################################################################
// EmulatorCPUMemory
//##############################################################################
public class EmulatorPPUMemory : EmulatorMemory {
    private EmulatorCartridge cart;
    private EmulatorMapperCore mapper;
    private EmulatorPPU ppu;

    public EmulatorPPUMemory(EmulatorCartridge cart_, EmulatorMapperCore mapper_, EmulatorPPU ppu_){
        cart = cart_;
        mapper = mapper_;
        ppu = ppu_;
    }

    // const (
    // 	MirrorHorizontal = 0
    // 	MirrorVertical   = 1
    // 	MirrorSingle0    = 2
    // 	MirrorSingle1    = 3
    // 	MirrorFour       = 4
    // )

    public static uint8[,] MIRROR_LOOKUP = {
        {0, 0, 1, 1},
        {0, 1, 0, 1},
        {0, 0, 0, 0},
        {1, 1, 1, 1},
        {0, 1, 2, 3},
    };

    uint16 MirrorAddress(uint8 mode, uint16 address){
        address = (address - 0x2000) % 0x1000;
        uint16 table = address / 0x4000;
        uint16 offset = address % 0x4000;
        return 0x2000 + (MIRROR_LOOKUP[mode, table] * 0x0400) + offset;
    }

    public override uint8 Read(uint16 address){
        address = address % 0x4000;

        if(address < 0x2000){
            return mapper.Read(address);
        } else if(address < 0x3F00){
            uint8 mode = cart.GetMirror();
            return ppu.nameTableData[MirrorAddress(mode, address) % 2048];
        } else if(address < 0x4000){
            return ppu.ReadPalette(address % 32);
        } else {
            Debug.LogError("Unhandled PPU read at address: " + address);
            return 0;
        }
    }

    public override void Write(uint16 address, uint8 value){
        address = address % 0x4000;

        if(address < 0x2000){
            mapper.Write(address, value);
        } else if(address < 0x3F00){
            uint8 mode = cart.GetMirror();
            ppu.nameTableData[MirrorAddress(mode, address) % 2048] = value;
        } else if(address < 0x4000){
            ppu.WritePalette(address % 32, value);
        } else {
            Debug.LogError("Unhandled PPU write at address: " + address);
        }
    }
}

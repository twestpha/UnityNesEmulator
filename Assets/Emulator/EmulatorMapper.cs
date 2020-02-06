using UnityEngine;

public class EmulatorMapperCore {
    EmulatorMapper mapper;

    public EmulatorMapperCore(EmulatorCartridge cart){
        uint8 mapperType = cart.GetMapper();

        if(mapperType == 0){
            Debug.Log("Using Mapper0");
            mapper = new EmulatorMapper0(cart);
        } else if(mapperType == 1){
            Debug.Log("Using Mapper1");
            mapper = new EmulatorMapper1(cart);
        } else {
            Debug.LogError("Unimplemented mapper type: " + mapperType);
        }
    }

    public void Step(){
        mapper.Step();
    }

    public uint8 Read(uint16 address){
        return mapper.Read(address);
    }

    public void Write(uint16 address, uint8 value){
        mapper.Write(address, value);
    }
}

//##############################################################################
// Mapper Base
//##############################################################################
public class EmulatorMapper {
    public virtual void Step(){}
    public virtual uint8 Read(uint16 address){ return 0; }
    public virtual void Write(uint16 address, uint8 value){}
}

//##############################################################################
// Mapper0
//##############################################################################
public class EmulatorMapper0 : EmulatorMapper {
    EmulatorCartridge cart;

    public EmulatorMapper0(EmulatorCartridge cart_){
        cart = cart_;
    }

    public override uint8 Read(uint16 address){
        // From https://wiki.nesdev.com/w/index.php/NROM

        if(address >= 0x6000 && address <= 0x7FFF){
            // CPU $6000-$7FFF: Family Basic only: PRG RAM, mirrored as necessary to fill entire 8 KiB window, write protectable with an external switch
            uint16 index = (address - 0x6000) % (cart.prgCount - 0x2000 /* 0x7FFF - 0x6000 */);
            return cart.PRG[index];
        } else if(address >= 0x8000 && address <= 0xBFFF){
            // CPU $8000-$BFFF: First 16 KB of ROM.
            return cart.ROM[address - 0x8000];
        } else if(address >= 0xC000 && address <= 0xFFFF){
            // CPU $C000-$FFFF: Last 16 KB of ROM (NROM-256) or mirror of $8000-$BFFF (NROM-128).
            if(false && cart.romCount > 0x4000){
                // This branch is never taken - not sure how to detect NROM-256
                return cart.ROM[address - 0x8000];
            } else {
                return cart.ROM[address - 0xC000];
            }
        } else {
            Debug.LogError("Unhandled Mapper0 read at address: " + address);
            return 0;
        }
    }

    public override void Write(uint16 address, uint8 value){
        // Should never write to a Mapper0, has no state
        Debug.LogError("Unhandled Mapper0 write at address: " + address);
    }
}

//##############################################################################
// Mapper 1
//##############################################################################
public class EmulatorMapper1 : EmulatorMapper {
    public const int OFFSETS_SIZE = 2;

    EmulatorCartridge cart;
    uint8 shiftRegister;
    uint8 control;
    uint8 prgMode;
    uint8 chrMode;
    uint8 prgBank;
    uint8 chrBank0;
    uint8 chrBank1;
    int[] PRGOffsets;
    int[] CHROffsets;

    public EmulatorMapper1(EmulatorCartridge cart_){
        PRGOffsets = new int[OFFSETS_SIZE];
        CHROffsets = new int[OFFSETS_SIZE];

        cart = cart_;

        shiftRegister = 0x10;
        PRGOffsets[1] = PRGBankOffset(-1);
    }

    public override uint8 Read(uint16 address){
        if(address < 0x2000){
            uint16 bank = address / 0x1000;
            uint16 offset = address % 0x1000;
            return cart.CHR[CHROffsets[bank] + offset];
        } else if(address > 0x8000){
            address = address - 0x8000;
            uint16 bank = address / 0x4000;
            uint16 offset = address % 0x4000;
            // Debug.Log("VALUE: " + (cart.PRG[PRGOffsets[bank] + offset]) + " at index " + (PRGOffsets[bank] + offset));
            return cart.PRG[PRGOffsets[bank] + offset];
        } else if(address > 0x6000){
            return cart.SRAM[address - 0x6000];
        } else {
            Debug.LogError("Unhandled Mapper1 read at address: " + address);
            return 0;
        }
    }

    public override void Write(uint16 address, uint8 value){
        if(address < 0x2000){
            uint16 bank = address / 0x1000;
            uint16 offset = address % 0x1000;
            cart.CHR[CHROffsets[bank] + offset] = value;
        } else if(address > 0x8000){
            address = address - 0x8000;
            uint16 bank = address / 0x4000;
            uint16 offset = address % 0x4000;
            cart.PRG[PRGOffsets[bank] + offset] = value;
        } else if(address > 0x6000){
            cart.SRAM[address - 0x6000] = value;
        } else {
            Debug.LogError("Unhandled Mapper1 write at address: " + address);
        }
    }

    public void LoadRegister(uint16 address, uint8 value){
        if((value & 0x80) == 0x80){
            shiftRegister = 0x10;
            WriteControl(control | 0x0C);
        } else {
            bool complete = (shiftRegister & 1) == 1;
            shiftRegister = shiftRegister >> 1;
            shiftRegister |= (value & 1) << 4;
            if(complete){
                WriteRegister(address, shiftRegister);
                shiftRegister = 0x10;
            }
        }
    }

    public void WriteRegister(uint16 address, uint8 value){
        if(address <= 0x9FFF){
            WriteControl(value);
        } else if(address <= 0xBFFF){
            WriteCHRBank0(value);
        } else if(address <= 0xDFFF){
            WriteCHRBank1(value);
        } else if(address <= 0xFFFF){
            WritePRGBank(value);
        }
    }

    public void WriteControl(uint8 value){

        control = value;
        chrMode = (value >> 4) & 1;
        prgMode = (value >> 2) & 3;
        uint8 mirror = value & 3;

        if(mirror == 0){
            // cart.SetMirror(MirrorSingle0); // fuck, where's this from?
        } else if(mirror == 1){
            // cart.SetMirror(MirrorSingle1);
        } else if(mirror == 2){
            // cart.SetMirror(MirrorVertical);
        } else if(mirror == 3){
            // cart.SetMirror(MirrorHorizontal);
        }

        UpdateOffsets();
    }

    public void WriteCHRBank0(uint8 value){
        chrBank0 = value;
        UpdateOffsets();
    }

    public void WriteCHRBank1(uint8 value){
        chrBank1 = value;
        UpdateOffsets();
    }

    public void WritePRGBank(uint8 value){
        prgBank = value;
        UpdateOffsets();
    }

    public int PRGBankOffset(int index){
        if(index >= 0x80){
            index -= 0x100;
        }

        index = index % (cart.PRG.Length / 0x4000);
        int offset = index * 0x4000;
        if(offset < 0){
            offset += cart.PRG.Length;
        }

        return offset;
    }

    public int CHRBankOffset(int index){
        if(index >= 0x80){
            index -= 0x100;
        }

        index = index % (cart.CHR.Length / 0x4000);
        int offset = index * 0x4000;
        if(offset < 0){
            offset += cart.CHR.Length;
        }

        return offset;
    }

    public void UpdateOffsets(){
        if(prgMode == 0 || prgMode == 1){
            PRGOffsets[0] = PRGBankOffset(prgBank & 0xFE);
            PRGOffsets[1] = PRGBankOffset(prgBank | 0x01);
        } else if(prgMode == 2){
            PRGOffsets[0] = 0;
            PRGOffsets[1] = PRGBankOffset(prgBank);
        } else if(prgMode == 3){
            PRGOffsets[0] = PRGBankOffset(prgBank);
            PRGOffsets[1] = PRGBankOffset(-1);
        }

        if(chrMode == 0){
            CHROffsets[0] = CHRBankOffset(chrBank0 & 0xFE);
    		CHROffsets[1] = CHRBankOffset(chrBank0 | 0x01);
        } else if(chrMode == 1){
            CHROffsets[0] = CHRBankOffset(chrBank0);
            CHROffsets[1] = CHRBankOffset(chrBank1);
        }
    }
}

//##############################################################################
// Mapper 2
//##############################################################################
public class EmulatorMapper2 : EmulatorMapper {
    EmulatorCartridge cart;
    uint16 prgBanks;
    uint16 prgBank1;
    uint16 prgBank2;

    public EmulatorMapper2(EmulatorCartridge cart_){
        cart = cart_;

        prgBanks = cart.prgCount / 0x4000;
        prgBank1 = 0;
        prgBank2 = prgBanks - 1;
    }

    public override uint8 Read(uint16 address){
        Debug.Log("Mapper2 reading address: " + address);
        if(address < 0x2000){
            return cart.CHR[address];
        } else if(address >= 0xC000){
            uint16 index = (prgBank2 * 0x4000) + (address - 0xC0000);
            Debug.Log(index);
            Debug.Log(cart.prgCount);
            return cart.PRG[index];
        } else if(address >= 0x8000){
            uint16 index = (prgBank1 * 0x4000) + (address - 0xC0000);
            return cart.PRG[index];
        } else if(address >= 0x6000){
            uint16 index = address - 0x6000;
            return cart.SRAM[index];
        } else {
            Debug.LogError("Unhandled Mapper2 read at address: " + address);
            return 0;
        }
    }

    public override void Write(uint16 address, uint8 value){
        if(address < 0x2000){
            cart.CHR[address] = (byte)(value);
        } else if(address >= 0x8000){
            prgBank1 = value % prgBanks;
        } else if(address >= 0x6000){
            uint16 index = address - 0x6000;
            cart.SRAM[index] = value;
        } else {
            Debug.LogError("Unhandled Mapper2 write at address: " + address);
        }
    }
}

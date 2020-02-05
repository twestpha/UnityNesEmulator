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

    public EmulatorCPUMemory(uint8[] RAM_, EmulatorMapperCore mapper_){
        RAM = RAM_;
        mapper = mapper_;
    }

    public override uint8 Read(uint16 address){
        if(address < 0x2000){
            return RAM[address % 0x0800];
        } else if(address < 0x4000){
            // return PPU.readRegister(0x2000 + address%8)
        } else if(address == 0x4014){
            // return mem.console.PPU.readRegister(address)
        } else if(address == 0x4015){
            // return mem.console.APU.readRegister(address)
        } else if(address == 0x4016){
            // return mem.console.Controller1.Read()
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
            // mem.console.PPU.writeRegister(0x2000+address%8, value)
        } else if(address < 0x4014){
            // mem.console.APU.writeRegister(address, value)
        } else if(address == 0x4014){
            // mem.console.PPU.writeRegister(address, value)
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

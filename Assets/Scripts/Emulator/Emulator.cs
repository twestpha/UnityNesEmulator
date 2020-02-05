using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Emulator : MonoBehaviour {
    public const int RAM_SIZE = 2048;
    public const int CPU_FREQUENCY = 1789773;

    public TextAsset ROMFile;

	// APU         *APU
	// PPU         *PPU
	// Controller1 *Controller
	// Controller2 *Controller

    private uint8[] ROM;
    private uint8[] RAM;

    private EmulatorMapperCore mapper;
    private EmulatorCartridge cart;
    private EmulatorCPUMemory cpuMem;
    private EmulatorCPU cpu;

    // Debug Crap
    public bool ShouldStep;
    public int PC, A, X, Y, SP;

    bool cartLoadComplete;

    void Start(){
        RAM = new uint8[RAM_SIZE];

        cart = new EmulatorCartridge(ROMFile.bytes);
        cartLoadComplete = false;

        mapper = new EmulatorMapperCore(cart);

        cpuMem = new EmulatorCPUMemory(RAM, mapper);
        cpu = new EmulatorCPU(cpuMem);
    }

    void Update(){
        // This must be complete before the rest of the emu runs
        cartLoadComplete = cart.CopyComplete();
        if(!cartLoadComplete){
            cart.ContinueMemoryCopy();
            cpu.Reset();

            PC = cpu.GetPC();
            A = cpu.GetA();
            X = cpu.GetX();
            Y = cpu.GetY();
            SP = cpu.GetSP();
        }

        if(cartLoadComplete && ShouldStep){
            Step();
            // StepSeconds(Time.deltaTime);
            ShouldStep = false;
            PC = cpu.GetPC();
            A = cpu.GetA();
            X = cpu.GetX();
            Y = cpu.GetY();
            SP = cpu.GetSP();
        }

        // TODO get PPU... texture? Then draw to set-aside texture that's rendering to a canvas? Sure, why not...
    }

    public void Reset(){
        cpu.Reset();
    }

    public int Step(){
        int cpuCycles = cpu.Step();
        int ppuCycles = cpuCycles * 3;

        for(int i = 0; i < ppuCycles; ++i){
            // PPU.Step();
            mapper.Step();
        }

        for(int i = 0; i < cpuCycles; ++i){
            // APU.Step();
        }

        return cpuCycles;
    }

    public int StepFrame(){
        // int cpuCycles = 0;
        // int prevFrame = PPU.Frame();
        // int prevFrame = 0;
        // while(prevFrame == PPU.Frame()){
        //     cpuCycles += Step();
        // }
        // return cpuCycles;
        return 0;
    }

    public void StepSeconds(float dt){
        int cycles = (int)(CPU_FREQUENCY * dt);
        while(cycles > 0){
            cycles -= Step();
        }
    }
}

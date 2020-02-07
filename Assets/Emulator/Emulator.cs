﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class Emulator : MonoBehaviour {
    public const int RAM_SIZE = 2048;
    public const int CPU_FREQUENCY = 1789773;

    public TextAsset ROMFile;
    public Texture2D emulatorDisplay;

	// APU         *APU
	// Controller1 *Controller
	// Controller2 *Controller

    private uint8[] RAM;

    private EmulatorMapperCore mapper;
    private EmulatorCartridge cart;
    private EmulatorCPUMemory cpuMem;
    private EmulatorCPU cpu;
    private EmulatorPPUMemory ppuMem;
    private EmulatorPPU ppu;

    // Debug Crap
    [Header("Program Counter")]
    public int PC;
    public int cyclesLastFrame;
    [Header("Registers")]
    public int A;
    public int X;
    public int Y;
    public int SP;
    [Header("Flags")]
    public bool C;
    public bool Z;
    public bool I;
    public bool D;
    public bool B;
    public bool U;
    public bool V;
    public bool N;

    bool cartLoadComplete;

    Thread emuUpdate;
    public bool emulatorStepping;

    void Start(){
        RAM = new uint8[RAM_SIZE];

        // Setup cart with raw bytes
        cart = new EmulatorCartridge(ROMFile.bytes);
        cartLoadComplete = false;

        // Setup mapper with cart rom
        mapper = new EmulatorMapperCore(cart);

        // Setup cpu memory and cpu with mapper, ram, and memory
        cpuMem = new EmulatorCPUMemory(RAM, mapper);
        cpu = new EmulatorCPU(cpuMem);

        // These rely on each other
        ppu = new EmulatorPPU(cpu, cpuMem);
        ppuMem = new EmulatorPPUMemory(cart, mapper, ppu);
        ppu.SetPPUMemory(ppuMem);

        // And of course, cpuMem relies on ppu
        cpuMem.SetPPU(ppu);

        // Setup thread and prevent it from running until cart fully loaded
        emulatorStepping = false;
        emuUpdate = new Thread(UpdateEmulator);
        emuUpdate.Start();
    }

    void UpdateEmulator(){
        while(true){
            // TODO put some yields in here?
            while(!emulatorStepping){}

            emulatorStepping = false;

            cyclesLastFrame = StepFrame();
            UpdateDebugVariables();
        }
    }

    void Update(){
        // This must be complete before the rest of the emu runs
        if(!cartLoadComplete){
            cart.ContinueMemoryCopy();
            cartLoadComplete = cart.CopyComplete();

            if(cartLoadComplete){
                Reset();
            }
        }

        RenderPPUToDisplay();

        // Then kick off the stepping again
        emulatorStepping = cartLoadComplete;
    }

    void RenderPPUToDisplay(){
        for(int y = 0; y < PixelBuffer.HEIGHT; ++y){
            for(int x = 0; x < PixelBuffer.WIDTH; ++x){
                emulatorDisplay.SetPixel(x, y, ppu.front.GetRGBA(x, y));
            }
        }

        emulatorDisplay.Apply();
    }

    void UpdateDebugVariables(){
        PC = cpu.GetPC();
        A = cpu.GetA();
        X = cpu.GetX();
        Y = cpu.GetY();
        SP = cpu.GetSP();
        C = cpu.GetC();
        Z = cpu.GetZ();
        I = cpu.GetI();
        D = cpu.GetD();
        B = cpu.GetB();
        U = cpu.GetU();
        V = cpu.GetV();
        N = cpu.GetN();
    }

    public void Reset(){
        cpu.Reset();
        ppu.Reset();
    }

    public int Step(){
        int cpuCycles = cpu.Step();
        int ppuCycles = cpuCycles * 3;

        for(int i = 0; i < ppuCycles; ++i){
            ppu.Step();
            mapper.Step();
        }

        // No APU for now
        // for(int i = 0; i < cpuCycles; ++i){
            // APU.Step();
        // }

        return cpuCycles;
    }

    public int StepFrame(){
        int cpuCycles = 0;
        uint64 frame = ppu.GetFrame();

        while(frame == ppu.GetFrame()){
            cpuCycles += Step();
        }

        return cpuCycles;
    }

    public void StepSeconds(float dt){
        long cycles = (long)(CPU_FREQUENCY * dt);
        while(cycles > 0){
            cycles -= Step();
        }
    }
}

using UnityEngine;

public enum AddressingMode {
    UNUSED,
    Absolute,
    AbsoluteX,
    AbsoluteY,
    Accumulator,
    Immediate,
    Implied,
    IndexedIndirect,
    Indirect,
    IndirectIndexed,
    Relative,
    ZeroPage,
    ZeroPageX,
    ZeroPageY,
}

public struct Instruction {
    public uint16 address;
    public uint16 pc;
    public AddressingMode mode;
}

public class EmulatorCPU {
    // Common shift amounts
    public readonly uint8 DOUBLEWORD = 16;
    public readonly uint8 WORD       = 8;
    public readonly uint8 BYTE       = 8;
    public readonly uint8 NIBBLE     = 4;

    // Reset Codes
    public readonly uint16 PC_RESET_ADDRESS = 0xFFFC;
    public readonly uint8 SP_RESET          = 0xFD;
    public readonly uint8 FLAGS_RESET_VALUE = 0x24; // Decimal | Unused

    // Interrupt Codes
    public readonly uint16 PC_NONMASKABLE = 0xFFFA;
    public readonly uint16 PC_REQUEST     = 0xFFFE;

    // Common Masking or Numerical Constants
    public readonly uint16 HIGH_NIBBLE = 0xFF00;
    public readonly uint16 LOW_NIBBLE  = 0x00FF;
    public readonly uint16 UINT8_MAX = 0x00FF;
    public readonly uint8 NEGATIVE_BIT = 0x80;

    public enum Interrupt {
        None,
        NonMaskable,
        Request,
    }

    // Mapping from instruction to adddressing mode
    public static uint8[] INSTRUCTION_MODES = {
        6, 7, 6, 7, 11, 11, 11, 11, 6, 5, 4, 5, 1, 1, 1, 1,
        10, 9, 6, 9, 12, 12, 12, 12, 6, 3, 6, 3, 2, 2, 2, 2,
        1, 7, 6, 7, 11, 11, 11, 11, 6, 5, 4, 5, 1, 1, 1, 1,
        10, 9, 6, 9, 12, 12, 12, 12, 6, 3, 6, 3, 2, 2, 2, 2,
        6, 7, 6, 7, 11, 11, 11, 11, 6, 5, 4, 5, 1, 1, 1, 1,
        10, 9, 6, 9, 12, 12, 12, 12, 6, 3, 6, 3, 2, 2, 2, 2,
        6, 7, 6, 7, 11, 11, 11, 11, 6, 5, 4, 5, 8, 1, 1, 1,
        10, 9, 6, 9, 12, 12, 12, 12, 6, 3, 6, 3, 2, 2, 2, 2,
        5, 7, 5, 7, 11, 11, 11, 11, 6, 5, 6, 5, 1, 1, 1, 1,
        10, 9, 6, 9, 12, 12, 13, 13, 6, 3, 6, 3, 2, 2, 3, 3,
        5, 7, 5, 7, 11, 11, 11, 11, 6, 5, 6, 5, 1, 1, 1, 1,
        10, 9, 6, 9, 12, 12, 13, 13, 6, 3, 6, 3, 2, 2, 3, 3,
        5, 7, 5, 7, 11, 11, 11, 11, 6, 5, 6, 5, 1, 1, 1, 1,
        10, 9, 6, 9, 12, 12, 12, 12, 6, 3, 6, 3, 2, 2, 2, 2,
        5, 7, 5, 7, 11, 11, 11, 11, 6, 5, 6, 5, 1, 1, 1, 1,
        10, 9, 6, 9, 12, 12, 12, 12, 6, 3, 6, 3, 2, 2, 2, 2,
    };

    // Mapping from instruction to size in bytes
    public static uint8[] INSTRUCTION_SIZES = {
        2, 2, 0, 0, 2, 2, 2, 0, 1, 2, 1, 0, 3, 3, 3, 0,
        2, 2, 0, 0, 2, 2, 2, 0, 1, 3, 1, 0, 3, 3, 3, 0,
        3, 2, 0, 0, 2, 2, 2, 0, 1, 2, 1, 0, 3, 3, 3, 0,
        2, 2, 0, 0, 2, 2, 2, 0, 1, 3, 1, 0, 3, 3, 3, 0,
        1, 2, 0, 0, 2, 2, 2, 0, 1, 2, 1, 0, 3, 3, 3, 0,
        2, 2, 0, 0, 2, 2, 2, 0, 1, 3, 1, 0, 3, 3, 3, 0,
        1, 2, 0, 0, 2, 2, 2, 0, 1, 2, 1, 0, 3, 3, 3, 0,
        2, 2, 0, 0, 2, 2, 2, 0, 1, 3, 1, 0, 3, 3, 3, 0,
        2, 2, 0, 0, 2, 2, 2, 0, 1, 0, 1, 0, 3, 3, 3, 0,
        2, 2, 0, 0, 2, 2, 2, 0, 1, 3, 1, 0, 0, 3, 0, 0,
        2, 2, 2, 0, 2, 2, 2, 0, 1, 2, 1, 0, 3, 3, 3, 0,
        2, 2, 0, 0, 2, 2, 2, 0, 1, 3, 1, 0, 3, 3, 3, 0,
        2, 2, 0, 0, 2, 2, 2, 0, 1, 2, 1, 0, 3, 3, 3, 0,
        2, 2, 0, 0, 2, 2, 2, 0, 1, 3, 1, 0, 3, 3, 3, 0,
        2, 2, 0, 0, 2, 2, 2, 0, 1, 2, 1, 0, 3, 3, 3, 0,
        2, 2, 0, 0, 2, 2, 2, 0, 1, 3, 1, 0, 3, 3, 3, 0,
    };

    // Mapping from instruction to number of cycles to execute (excluding conditional cycles)
    public static uint8[] INSTRUCTION_CYCLES = {
        7, 6, 2, 8, 3, 3, 5, 5, 3, 2, 2, 2, 4, 4, 6, 6,
        2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7,
        6, 6, 2, 8, 3, 3, 5, 5, 4, 2, 2, 2, 4, 4, 6, 6,
        2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7,
        6, 6, 2, 8, 3, 3, 5, 5, 3, 2, 2, 2, 3, 4, 6, 6,
        2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7,
        6, 6, 2, 8, 3, 3, 5, 5, 4, 2, 2, 2, 5, 4, 6, 6,
        2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7,
        2, 6, 2, 6, 3, 3, 3, 3, 2, 2, 2, 2, 4, 4, 4, 4,
        2, 6, 2, 6, 4, 4, 4, 4, 2, 5, 2, 5, 5, 5, 5, 5,
        2, 6, 2, 6, 3, 3, 3, 3, 2, 2, 2, 2, 4, 4, 4, 4,
        2, 5, 2, 5, 4, 4, 4, 4, 2, 4, 2, 4, 4, 4, 4, 4,
        2, 6, 2, 8, 3, 3, 5, 5, 2, 2, 2, 2, 4, 4, 6, 6,
        2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7,
        2, 6, 2, 8, 3, 3, 5, 5, 2, 2, 2, 2, 4, 4, 6, 6,
        2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7,
    };

    // Mapping from instruction to number of cycles used when page crossed
    public static uint8[] INSTRUCTION_PAGE_CYCLES = {
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 0, 1, 0, 0, 0, 0, 0, 1, 0, 1, 1, 1, 1, 1,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0,
    };

    // Mapping from instruction to 6502 name
    public static string[] INSTRUCTION_NAMES = {
        "BRK", "ORA", "KIL", "SLO", "NOP", "ORA", "ASL", "SLO",
        "PHP", "ORA", "ASL", "ANC", "NOP", "ORA", "ASL", "SLO",
        "BPL", "ORA", "KIL", "SLO", "NOP", "ORA", "ASL", "SLO",
        "CLC", "ORA", "NOP", "SLO", "NOP", "ORA", "ASL", "SLO",
        "JSR", "AND", "KIL", "RLA", "BIT", "AND", "ROL", "RLA",
        "PLP", "AND", "ROL", "ANC", "BIT", "AND", "ROL", "RLA",
        "BMI", "AND", "KIL", "RLA", "NOP", "AND", "ROL", "RLA",
        "SEC", "AND", "NOP", "RLA", "NOP", "AND", "ROL", "RLA",
        "RTI", "EOR", "KIL", "SRE", "NOP", "EOR", "LSR", "SRE",
        "PHA", "EOR", "LSR", "ALR", "JMP", "EOR", "LSR", "SRE",
        "BVC", "EOR", "KIL", "SRE", "NOP", "EOR", "LSR", "SRE",
        "CLI", "EOR", "NOP", "SRE", "NOP", "EOR", "LSR", "SRE",
        "RTS", "ADC", "KIL", "RRA", "NOP", "ADC", "ROR", "RRA",
        "PLA", "ADC", "ROR", "ARR", "JMP", "ADC", "ROR", "RRA",
        "BVS", "ADC", "KIL", "RRA", "NOP", "ADC", "ROR", "RRA",
        "SEI", "ADC", "NOP", "RRA", "NOP", "ADC", "ROR", "RRA",
        "NOP", "STA", "NOP", "SAX", "STY", "STA", "STX", "SAX",
        "DEY", "NOP", "TXA", "XAA", "STY", "STA", "STX", "SAX",
        "BCC", "STA", "KIL", "AHX", "STY", "STA", "STX", "SAX",
        "TYA", "STA", "TXS", "TAS", "SHY", "STA", "SHX", "AHX",
        "LDY", "LDA", "LDX", "LAX", "LDY", "LDA", "LDX", "LAX",
        "TAY", "LDA", "TAX", "LAX", "LDY", "LDA", "LDX", "LAX",
        "BCS", "LDA", "KIL", "LAX", "LDY", "LDA", "LDX", "LAX",
        "CLV", "LDA", "TSX", "LAS", "LDY", "LDA", "LDX", "LAX",
        "CPY", "CMP", "NOP", "DCP", "CPY", "CMP", "DEC", "DCP",
        "INY", "CMP", "DEX", "AXS", "CPY", "CMP", "DEC", "DCP",
        "BNE", "CMP", "KIL", "DCP", "NOP", "CMP", "DEC", "DCP",
        "CLD", "CMP", "NOP", "DCP", "NOP", "CMP", "DEC", "DCP",
        "CPX", "SBC", "NOP", "ISC", "CPX", "SBC", "INC", "ISC",
        "INX", "SBC", "NOP", "SBC", "CPX", "SBC", "INC", "ISC",
        "BEQ", "SBC", "KIL", "ISC", "NOP", "SBC", "INC", "ISC",
        "SED", "SBC", "NOP", "ISC", "NOP", "SBC", "INC", "ISC",
    };

    uint64 cycles;
    uint16 PC;
    // Debug Crap
    public int GetPC(){ return (int)(PC); }

    // Registers
    uint8 A, X, Y, SP;
    // Debug Crap
    public int GetA(){ return (int)(A); }
    public int GetX(){ return (int)(X); }
    public int GetY(){ return (int)(Y); }
    public int GetSP(){ return (int)(SP); }

    // Flags
    uint8 C, Z, I, D, B, U, V, N;
    // Debug Crap
    public bool GetC(){ return C > 0; }
    public bool GetZ(){ return Z > 0; }
    public bool GetI(){ return I > 0; }
    public bool GetD(){ return D > 0; }
    public bool GetB(){ return B > 0; }
    public bool GetU(){ return U > 0; }
    public bool GetV(){ return V > 0; }
    public bool GetN(){ return N > 0; }

    EmulatorCPUMemory mem;
    Interrupt interrupt;
    uint32 stall;

    public delegate void InstructionFunction(Instruction inst);
    private InstructionFunction[] functionTable;

    public EmulatorCPU(EmulatorCPUMemory cpuMemory_){
        mem = cpuMemory_;
        CreateFunctionTable();
    }

    public void CreateFunctionTable(){
        functionTable = new InstructionFunction[]{
            brk, ora, kil, slo, nop, ora, asl, slo,
            php, ora, asl, anc, nop, ora, asl, slo,
            bpl, ora, kil, slo, nop, ora, asl, slo,
            clc, ora, nop, slo, nop, ora, asl, slo,
            jsr, and, kil, rla, bit, and, rol, rla,
            plp, and, rol, anc, bit, and, rol, rla,
            bmi, and, kil, rla, nop, and, rol, rla,
            sec, and, nop, rla, nop, and, rol, rla,
            rti, eor, kil, sre, nop, eor, lsr, sre,
            pha, eor, lsr, alr, jmp, eor, lsr, sre,
            bvc, eor, kil, sre, nop, eor, lsr, sre,
            cli, eor, nop, sre, nop, eor, lsr, sre,
            rts, adc, kil, rra, nop, adc, ror, rra,
            pla, adc, ror, arr, jmp, adc, ror, rra,
            bvs, adc, kil, rra, nop, adc, ror, rra,
            sei, adc, nop, rra, nop, adc, ror, rra,
            nop, sta, nop, sax, sty, sta, stx, sax,
            dey, nop, txa, xaa, sty, sta, stx, sax,
            bcc, sta, kil, ahx, sty, sta, stx, sax,
            tya, sta, txs, tas, shy, sta, shx, ahx,
            ldy, lda, ldx, lax, ldy, lda, ldx, lax,
            tay, lda, tax, lax, ldy, lda, ldx, lax,
            bcs, lda, kil, lax, ldy, lda, ldx, lax,
            clv, lda, tsx, las, ldy, lda, ldx, lax,
            cpy, cmp, nop, dcp, cpy, cmp, dec, dcp,
            iny, cmp, dex, axs, cpy, cmp, dec, dcp,
            bne, cmp, kil, dcp, nop, cmp, dec, dcp,
            cld, cmp, nop, dcp, nop, cmp, dec, dcp,
            cpx, sbc, nop, isc, cpx, sbc, inc, isc,
            inx, sbc, nop, sbc, cpx, sbc, inc, isc,
            beq, sbc, kil, isc, nop, sbc, inc, isc,
            sed, sbc, nop, isc, nop, sbc, inc, isc,
        };
    }

    public void Reset(){
        // PC = Read16(PC_RESET_ADDRESS);
        PC = 0xC000; // TODO fix this?
        SP = SP_RESET;
        SetFlags(FLAGS_RESET_VALUE);
    }

    // Some way to display opcode and registers to "debugger"

    //##########################################################################
    // 6502 Helper methods - secondary behavior of the CPU
    //##########################################################################

    // Rerturns true if a memory page boundary (starting at 0x00FF + 1) was crossed
    public bool PageCrossed(uint16 a, uint16 b){
        return (a & HIGH_NIBBLE) != (b & HIGH_NIBBLE);
    }

    // Adds cycles specified by step inst
    public void AddBranchCycles(Instruction inst){
        cycles++;
        if(PageCrossed(inst.pc, inst.address)){
            cycles++;
        }
    }

    // Compares two bytes, setting the Z, N, and C flags appropriately
    public void Compare(uint8 a, uint8 b){
        SetZN(a - b);
        C = a >= b ? 1 : 0;
    }

    // Reads two bytes and returns a double word
    public uint16 Read16(uint16 address){
        int16 low = mem.Read(address);
        int16 high = mem.Read(address + 1);
        return (high << WORD) | low;
    }

    // Emulates a 6502 bug that caused the low byte to wrap without incrementing the high byte
    public uint16 Read16Bug(uint16 address){
        uint16 a = address;
        uint16 b = (a & HIGH_NIBBLE) | ((a & LOW_NIBBLE) + 1);

        uint16 low = mem.Read(a);
        uint16 high = mem.Read(b);

        return (high << 8) | low;
    }

    // Pushes a byte onto the stack
    public void Push(uint8 value){
        mem.Write(SP | 0x100, value); // TODO magic number
        SP--;
    }

    // Pulls a byte from the stack
    public uint8 Pull(){
        SP++;
        return mem.Read(SP | 0x100); // TODO magic number
    }

    // Pushes two bytes onto the stack
    public void Push16(uint16 value){
        uint8 high = value >> WORD;
        uint8 low = value & LOW_NIBBLE;
        Push(high);
        Push(low);
    }

    // Pulls two bytes from the stack
    public uint16 Pull16(){
        uint16 low = Pull();
        uint16 high = Pull();
        return (high << WORD) | low;
    }

    // Flags returns the processor status flags
    public uint8 Flags(){
        uint8 flags = 0;
        flags |= C << 0;
        flags |= Z << 1;
        flags |= I << 2;
        flags |= D << 3;
        flags |= B << 4;
        flags |= U << 5;
        flags |= V << 6;
        flags |= N << 7;
        return flags;
    }

    // SetFlags sets the processor status flags
    public void SetFlags(uint8 flags){
        C = flags >> 0 & 1;
        Z = flags >> 1 & 1;
        I = flags >> 2 & 1;
        D = flags >> 3 & 1;
        B = flags >> 4 & 1;
        U = flags >> 5 & 1;
        V = flags >> 6 & 1;
        N = flags >> 7 & 1;
    }

    // Sets the zero flag if the argument is zero
    public void SetZ(uint8 value){
        Z = value == 0 ? 1 : 0;
    }

    // Sets the negative flag if the argument is negative (high bit is set)
    public void SetN(uint8 value){
        N = (value & NEGATIVE_BIT) != 0 ? 1 : 0;
    }

    // Helper function to set both the zero flag and the negative flag
    public void SetZN(uint8 value){
        SetZ(value);
        SetN(value);
    }

    // Causes a non-maskable interrupt to occur on the next cycle
    public void TriggerNMI(){
        interrupt = Interrupt.NonMaskable;
    }

    // Causes an IRQ interrupt to occur on the next cycle
    public void TriggerIRQ(){
        if(I != 0){
            interrupt = Interrupt.Request;
        }
    }

    // Step executes a single CPU instruction
    public uint16 Step() {
        if(stall > 0){
            stall--;
            return 1;
        }

        uint64 prevCycles = cycles;

        if(interrupt == Interrupt.NonMaskable){
            HandleNMI();
        } else if(interrupt == Interrupt.Request){
            HandleIRQ();
        }

        interrupt = Interrupt.None;

        uint8 opcode = mem.Read(PC);
        // Debug.Log("STEP ------------------------------------");
        // Debug.Log("[" + PC + "] => " + opcode + " (" + INSTRUCTION_NAMES[opcode] + ")");

        AddressingMode mode = (AddressingMode)((int)INSTRUCTION_MODES[opcode]); // THIS IS WRONG LALALALAL

        uint16 address = 0;
        bool pageCrossed = false;

        if(mode == AddressingMode.Absolute){
            address = Read16(PC + 1);
        } else if(mode == AddressingMode.AbsoluteX){
            address = Read16(PC + 1) + X;
            pageCrossed = PageCrossed(address - X, address);
        } else if(mode == AddressingMode.AbsoluteY){
            address = Read16(PC + 1) + Y;
            pageCrossed = PageCrossed(address - Y, address);
        } else if(mode == AddressingMode.Accumulator){
            address = 0;
        } else if(mode == AddressingMode.Immediate){
            address = PC + 1;
        } else if(mode == AddressingMode.Implied){
            address = 0;
        } else if(mode == AddressingMode.IndexedIndirect){
            address = Read16Bug(mem.Read(PC + 1) + X);
        } else if(mode == AddressingMode.Indirect){
            address = Read16Bug(Read16(PC + 1));
        } else if(mode == AddressingMode.IndirectIndexed){
            address = Read16Bug(mem.Read(PC + 1) + Y);
            pageCrossed = PageCrossed(address - Y, address);
        } else if(mode == AddressingMode.Relative){
            uint16 offset = mem.Read(PC + 1);
            if(offset < 0x80){ // TODO magic number
                address = PC + 2 + offset;
            } else {
                address = PC + 2 + offset - 0x100;
            }
        } else if(mode == AddressingMode.ZeroPage){
            address = mem.Read(PC + 1);
        } else if(mode == AddressingMode.ZeroPageX){
            address = (mem.Read(PC + 1) + X) & LOW_NIBBLE;
        } else if(mode == AddressingMode.ZeroPageY){
            address = (mem.Read(PC + 1) + Y) & LOW_NIBBLE;
        }

        PC += INSTRUCTION_SIZES[opcode];
        cycles += INSTRUCTION_CYCLES[opcode];

        if(pageCrossed){
            cycles += INSTRUCTION_PAGE_CYCLES[opcode];
        }

        Instruction inst = new Instruction();
        inst.address = address;
        inst.pc = PC;
        inst.mode = mode;
        // Debug.Log("  Address: " + address);
        // Debug.Log("       PC: " + PC);
        // Debug.Log("     Mode: " + mode);

        functionTable[opcode](inst);

        return (cycles - prevCycles);
    }

    // Handle a Non-Maskable Interrupt
    public void HandleNMI(){
        Push16(PC);
        php(new Instruction());
        PC = Read16(PC_NONMASKABLE);
        I = 1;
        cycles += 7;
    }

    // Handle an Interrupt Request
    public void HandleIRQ(){
        Push16(PC);
        php(new Instruction());
        PC = Read16(PC_REQUEST);
        I = 1;
        cycles += 7;
    }

    //##########################################################################
    // 6502 Instructions
    //##########################################################################
    // ADC - Add with Carry
    private void adc(Instruction inst){
        uint8 a = A;
        uint8 b = mem.Read(inst.address);
        uint8 c = C;

        uint16 sum = a + b + c;
        A = sum;
        SetZN(A);

        C = sum > UINT8_MAX ? 1 : 0;

        if((((a ^ b) & 0x80) == 0) && (((a ^ A) & 0x80) != 0)){
            V = 1;
        } else {
            V = 0;
        }
    }

    // AND - Logical AND
    private void and(Instruction inst){
        A = A & mem.Read(inst.address);
        SetZN(A);
    }

    // ASL - Arithmetic Shift Left
    private void asl(Instruction inst){
        if(inst.mode == AddressingMode.Accumulator){
            C = (A >> 7) & 1;
            A = A << 1;
            SetZN(A);
        } else {
            uint8 value = mem.Read(inst.address);
            C = (value >> 7) & 1;
            value = value << 1;
            mem.Write(inst.address, value);
            SetZN(value);
        }
    }

    // BCC - Branch if Carry Clear
    private void bcc(Instruction inst){
        if(C == 0){
            PC = inst.address;
            AddBranchCycles(inst);
        }
    }

    // BCS - Branch if Carry Set
    private void bcs(Instruction inst){
        if(C != 0){
            PC = inst.address;
            AddBranchCycles(inst);
        }
    }

    // BEQ - Branch if Equal
    private void beq(Instruction inst){
        if(Z != 0){
            PC = inst.address;
            AddBranchCycles(inst);
        }
    }

    // BIT - Bit Test
    private void bit(Instruction inst){
        uint8 value = mem.Read(inst.address);
        V = (value >> 6) & 1;
        SetZ(value & A);
        SetN(value);
    }

    // BMI - Branch if Minus
    private void bmi(Instruction inst){
        if(N != 0){
            PC = inst.address;
            AddBranchCycles(inst);
        }
    }

    // BNE - Branch if Not Equal
    private void bne(Instruction inst){
        if(Z == 0){
            PC = inst.address;
            AddBranchCycles(inst);
        }
    }

    // BPL - Branch if Positive
    private void bpl(Instruction inst){
        if(N == 0){
            PC = inst.address;
            AddBranchCycles(inst);
        }
    }

    // BRK - Force Interrupt
    private void brk(Instruction inst){
        Push16(PC);
        php(inst);
        sei(inst);
        PC = Read16(PC_REQUEST); // TODO magic number: why request?
    }

    // BVC - Branch if Overflow Clear
    private void bvc(Instruction inst){
        if(V == 0){
            PC = inst.address;
            AddBranchCycles(inst);
        }
    }

    // BVS - Branch if Overflow Set
    private void bvs(Instruction inst){
        if(V != 0){
            PC = inst.address;
            AddBranchCycles(inst);
        }
    }

    // CLC - Clear Carry Flag
    private void clc(Instruction inst){
        C = 0;
    }

    // CLD - Clear Decimal Mode
    private void cld(Instruction inst){
        D = 0;
    }

    // CLI - Clear Interrupt Disable
    private void cli(Instruction inst){
        I = 0;
    }

    // CLV - Clear Overflow Flag
    private void clv(Instruction inst){
        V = 0;
    }

    // CMP - Compare
    private void cmp(Instruction inst){
        uint8 value = mem.Read(inst.address);
        Compare(A, value);
    }

    // CPX - Compare X Register
    private void cpx(Instruction inst){
        uint8 value = mem.Read(inst.address);
        Compare(X, value);
    }

    // CPY - Compare Y Register
    private void cpy(Instruction inst){
        uint8 value = mem.Read(inst.address);
        Compare(Y, value);
    }

    // DEC - Decrement Memory
    private void dec(Instruction inst){
        uint8 value = mem.Read(inst.address) - 1;
        mem.Write(inst.address, value);
        SetZN(value);
    }

    // DEX - Decrement X Register
    private void dex(Instruction inst){
        X--;
        SetZN(X);
    }

    // DEY - Decrement Y Register
    private void dey(Instruction inst){
        Y--;
        SetZN(Y);
    }

    // EOR - Exclusive OR
    private void eor(Instruction inst){
        // A = A ^ Read(inst.address);
        SetZN(A);
    }

    // INC - Increment Memory
    private void inc(Instruction inst){
        uint8 value = mem.Read(inst.address) + 1;
        mem.Write(inst.address, value);
        SetZN(value);
    }

    // INX - Increment X Register
    private void inx(Instruction inst){
        X++;
        SetZN(X);
    }

    // INY - Increment Y Register
    private void iny(Instruction inst){
        Y++;
        SetZN(Y);
    }

    // JMP - Jump
    private void jmp(Instruction inst){
        PC = inst.address;
    }

    // JSR - Jump to Subroutine
    private void jsr(Instruction inst){
        Push16(PC - 1);
        PC = inst.address;
    }

    // LDA - Load Accumulator
    private void lda(Instruction inst){
        A = mem.Read(inst.address);
        SetZN(A);
    }

    // LDX - Load X Register
    private void ldx(Instruction inst){
        X = mem.Read(inst.address);
        SetZN(X);
    }

    // LDY - Load Y Register
    private void ldy(Instruction inst){
        Y = mem.Read(inst.address);
        SetZN(Y);
    }

    // LSR - Logical Shift Right
    private void lsr(Instruction inst){
        if(inst.mode == AddressingMode.Accumulator){
            C = A & 1;
            A = A >> 1;
            SetZN(A);
        } else {
            uint8 value = mem.Read(inst.address);
            value = value >> 1;
            mem.Write(inst.address, value);
            SetZN(value);
        }
    }

    // NOP - No Operation
    private void nop(Instruction inst){
    }

    // ORA - Logical Inclusive OR
    private void ora(Instruction inst){
        A = A | mem.Read(inst.address);
        SetZN(A);
    }

    // PHA - Push Accumulator
    private void pha(Instruction inst){
        Push(A);
    }

    // PHP - Push Processor Status
    private void php(Instruction inst){
        Push(Flags() | 0x10); // TODO replace magic number
    }

    // PLA - Pull Accumulator
    private void pla(Instruction inst){
        A = Pull();
        SetZN(A);
    }

    // PLP - Pull Processor Status
    private void plp(Instruction inst){
        SetFlags((Pull() & 0xEF) | 0x20); // TODO replace magic numbers
    }

    // ROL - Rotate Left
    private void rol(Instruction inst){
        if(inst.mode == AddressingMode.Accumulator){
            uint8 c = C;
            C = (A >> 7) & 1;
            A = (A << 1) | c;
            SetZN(A);
        } else {
            uint8 c = C;
            uint8 value = mem.Read(inst.address);
            C = (value >> 7) & 1;
            A = (value << 1) | c;
            mem.Write(inst.address, value);
            SetZN(value);
        }
    }

    // ROR - Rotate Right
    private void ror(Instruction inst){
        if(inst.mode == AddressingMode.Accumulator){
            uint8 c = C;
            C = A & 1;
            A = (A >> 1) | (c << 7);
            SetZN(A);
        } else {
            uint8 c = C;
            uint8 value = mem.Read(inst.address);
            C = value & 1;
            value = (value >> 1) | (c << 7);
            mem.Write(inst.address, value);
            SetZN(value);
        }
    }

    // RTI - Return from Interrupt
    private void rti(Instruction inst){
        SetFlags((Pull() & 0xEF) | 0x20); // TODO replace magic numbers
        PC = Pull16();
    }

    // RTS - Return from Subroutine
    private void rts(Instruction inst){
        PC = Pull16() + 1;
    }

    // SBC - Subtract with Carry
    private void sbc(Instruction inst){
        uint8 a = A;
        uint8 b = mem.Read(inst.address);
        uint8 c = C;

        uint16 sum = a - b - (1 - c);
        A = sum;
        SetZN(A);

        // if int(a)-int(b)-int(1-c) >= 0 {...}
        C = sum >= 0 ? 1 : 0; // potential BUG

        if((((a ^ b) & 0x80) != 0) && (((a ^ A) & 0x80) != 0)){
            V = 1;
        } else {
            V = 0;
        }
    }

    // SEC - Set Carry Flag
    private void sec(Instruction inst){
        C = 1;
    }

    // SED - Set Decimal Flag
    private void sed(Instruction inst){
        D = 1;
    }

    // SEI - Set Interrupt Disable
    private void sei(Instruction inst){
        I = 1;
    }

    // STA - Store Accumulator
    private void sta(Instruction inst){
        mem.Write(inst.address, A);
    }

    // STX - Store X Register
    private void stx(Instruction inst){
        mem.Write(inst.address, X);
    }

    // STY - Store Y Register
    private void sty(Instruction inst){
        mem.Write(inst.address, Y);
    }

    // TAX - Transfer Accumulator to X
    private void tax(Instruction inst){
        X = A;
        SetZN(X);
    }

    // TAY - Transfer Accumulator to Y
    private void tay(Instruction inst){
        Y = A;
        SetZN(Y);
    }

    // TSX - Transfer Stack Pointer to X
    private void tsx(Instruction inst){
        X = SP;
        SetZN(X);
    }

    // TXA - Transfer X to Accumulator
    private void txa(Instruction inst){
        A = X;
        SetZN(A);
    }

    // TXS - Transfer X to Stack Pointer
    private void txs(Instruction inst){
        SP = X;
    }

    // TYA - Transfer Y to Accumulator
    private void tya(Instruction inst){
        A = Y;
        SetZN(A);
    }

    // illegal opcodes below
    private void ahx(Instruction inst){
    }

    private void alr(Instruction inst){
    }

    private void anc(Instruction inst){
    }

    private void arr(Instruction inst){
    }

    private void axs(Instruction inst){
    }

    private void dcp(Instruction inst){
    }

    private void isc(Instruction inst){
    }

    private void kil(Instruction inst){
    }

    private void las(Instruction inst){
    }

    private void lax(Instruction inst){
    }

    private void rla(Instruction inst){
    }

    private void rra(Instruction inst){
    }

    private void sax(Instruction inst){
    }

    private void shx(Instruction inst){
    }

    private void shy(Instruction inst){
    }

    private void slo(Instruction inst){
    }

    private void sre(Instruction inst){
    }

    private void tas(Instruction inst){
    }

    private void xaa(Instruction inst){
    }
}

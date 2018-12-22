using System;

// MOS 6502 - NTSC 2A03 (RP2A03[G]) or PAL 2A07 (RP2A07G)
public class CPU
{
    #region CONSTANTS

    public const byte INTERRUPT_NMI = 0x2;
    public const byte INTERRUPT_IRQ = 0x3;

    private enum MODE:byte
    {
        Absolute        = 0x1,
        AbsoluteX       = 0x2,
        AbsoluteY       = 0x3,
        Accumulator     = 0x4,
        Immediate       = 0x5,
        Implied         = 0x6,
        IndexedIndirect = 0x7,
        Indirect        = 0x8,
        IndirectIndexed = 0x9,
        Relative        = 0xA,
        ZeroPage        = 0xB,
        ZeroPageX       = 0xC,
        ZeroPageY       = 0xD,
    }

    private static readonly byte[] OP_MODE = new byte[]
    {
        0x6, 0x7, 0x6, 0x7, 0xB, 0xB, 0xB, 0xB,
        0x6, 0x5, 0x4, 0x5, 0x1, 0x1, 0x1, 0x1,
        0xA, 0x9, 0x6, 0x9, 0xC, 0xC, 0xC, 0xC,
        0x6, 0x3, 0x6, 0x3, 0x2, 0x2, 0x2, 0x2,
        0x1, 0x7, 0x6, 0x7, 0xB, 0xB, 0xB, 0xB,
        0x6, 0x5, 0x4, 0x5, 0x1, 0x1, 0x1, 0x1,
        0xA, 0x9, 0x6, 0x9, 0xC, 0xC, 0xC, 0xC,
        0x6, 0x3, 0x6, 0x3, 0x2, 0x2, 0x2, 0x2,
        0x6, 0x7, 0x6, 0x7, 0xB, 0xB, 0xB, 0xB,
        0x6, 0x5, 0x4, 0x5, 0x1, 0x1, 0x1, 0x1,
        0xA, 0x9, 0x6, 0x9, 0xC, 0xC, 0xC, 0xC,
        0x6, 0x3, 0x6, 0x3, 0x2, 0x2, 0x2, 0x2,
        0x6, 0x7, 0x6, 0x7, 0xB, 0xB, 0xB, 0xB,
        0x6, 0x5, 0x4, 0x5, 0x8, 0x1, 0x1, 0x1,
        0xA, 0x9, 0x6, 0x9, 0xC, 0xC, 0xC, 0xC,
        0x6, 0x3, 0x6, 0x3, 0x2, 0x2, 0x2, 0x2,
        0x5, 0x7, 0x5, 0x7, 0xB, 0xB, 0xB, 0xB,
        0x6, 0x5, 0x6, 0x5, 0x1, 0x1, 0x1, 0x1,
        0xA, 0x9, 0x6, 0x9, 0xC, 0xC, 0xD, 0xD,
        0x6, 0x3, 0x6, 0x3, 0x2, 0x2, 0x3, 0x3,
        0x5, 0x7, 0x5, 0x7, 0xB, 0xB, 0xB, 0xB,
        0x6, 0x5, 0x6, 0x5, 0x1, 0x1, 0x1, 0x1,
        0xA, 0x9, 0x6, 0x9, 0xC, 0xC, 0xD, 0xD,
        0x6, 0x3, 0x6, 0x3, 0x2, 0x2, 0x3, 0x3,
        0x5, 0x7, 0x5, 0x7, 0xB, 0xB, 0xB, 0xB,
        0x6, 0x5, 0x6, 0x5, 0x1, 0x1, 0x1, 0x1,
        0xA, 0x9, 0x6, 0x9, 0xC, 0xC, 0xC, 0xC,
        0x6, 0x3, 0x6, 0x3, 0x2, 0x2, 0x2, 0x2,
        0x5, 0x7, 0x5, 0x7, 0xB, 0xB, 0xB, 0xB,
        0x6, 0x5, 0x6, 0x5, 0x1, 0x1, 0x1, 0x1,
        0xA, 0x9, 0x6, 0x9, 0xC, 0xC, 0xC, 0xC,
        0x6, 0x3, 0x6, 0x3, 0x2, 0x2, 0x2, 0x2,
    };

    private static readonly ushort[] OP_CYCLES = new ushort[]
    {
        7, 6, 2, 8, 3, 3, 5, 5,
        3, 2, 2, 2, 4, 4, 6, 6,
        2, 5, 2, 8, 4, 4, 6, 6,
        2, 4, 2, 7, 4, 4, 7, 7,
        6, 6, 2, 8, 3, 3, 5, 5,
        4, 2, 2, 2, 4, 4, 6, 6,
        2, 5, 2, 8, 4, 4, 6, 6,
        2, 4, 2, 7, 4, 4, 7, 7,
        6, 6, 2, 8, 3, 3, 5, 5,
        3, 2, 2, 2, 3, 4, 6, 6,
        2, 5, 2, 8, 4, 4, 6, 6,
        2, 4, 2, 7, 4, 4, 7, 7,
        6, 6, 2, 8, 3, 3, 5, 5,
        4, 2, 2, 2, 5, 4, 6, 6,
        2, 5, 2, 8, 4, 4, 6, 6,
        2, 4, 2, 7, 4, 4, 7, 7,
        2, 6, 2, 6, 3, 3, 3, 3,
        2, 2, 2, 2, 4, 4, 4, 4,
        2, 6, 2, 6, 4, 4, 4, 4,
        2, 5, 2, 5, 5, 5, 5, 5,
        2, 6, 2, 6, 3, 3, 3, 3,
        2, 2, 2, 2, 4, 4, 4, 4,
        2, 5, 2, 5, 4, 4, 4, 4,
        2, 4, 2, 4, 4, 4, 4, 4,
        2, 6, 2, 8, 3, 3, 5, 5,
        2, 2, 2, 2, 4, 4, 6, 6,
        2, 5, 2, 8, 4, 4, 6, 6,
        2, 4, 2, 7, 4, 4, 7, 7,
        2, 6, 2, 8, 3, 3, 5, 5,
        2, 2, 2, 2, 4, 4, 6, 6,
        2, 5, 2, 8, 4, 4, 6, 6,
        2, 4, 2, 7, 4, 4, 7, 7,
    };

    private static readonly ushort[] OP_SIZE = new ushort[]
    {
        1, 2, 0, 0, 2, 2, 2, 0,
        1, 2, 1, 0, 3, 3, 3, 0,
        2, 2, 0, 0, 2, 2, 2, 0,
        1, 3, 1, 0, 3, 3, 3, 0,
        3, 2, 0, 0, 2, 2, 2, 0,
        1, 2, 1, 0, 3, 3, 3, 0,
        2, 2, 0, 0, 2, 2, 2, 0,
        1, 3, 1, 0, 3, 3, 3, 0,
        1, 2, 0, 0, 2, 2, 2, 0,
        1, 2, 1, 0, 3, 3, 3, 0,
        2, 2, 0, 0, 2, 2, 2, 0,
        1, 3, 1, 0, 3, 3, 3, 0,
        1, 2, 0, 0, 2, 2, 2, 0,
        1, 2, 1, 0, 3, 3, 3, 0,
        2, 2, 0, 0, 2, 2, 2, 0,
        1, 3, 1, 0, 3, 3, 3, 0,
        2, 2, 0, 0, 2, 2, 2, 0,
        1, 0, 1, 0, 3, 3, 3, 0,
        2, 2, 0, 0, 2, 2, 2, 0,
        1, 3, 1, 0, 0, 3, 0, 0,
        2, 2, 2, 0, 2, 2, 2, 0,
        1, 2, 1, 0, 3, 3, 3, 0,
        2, 2, 0, 0, 2, 2, 2, 0,
        1, 3, 1, 0, 3, 3, 3, 0,
        2, 2, 0, 0, 2, 2, 2, 0,
        1, 2, 1, 0, 3, 3, 3, 0,
        2, 2, 0, 0, 2, 2, 2, 0,
        1, 3, 1, 0, 3, 3, 3, 0,
        2, 2, 0, 0, 2, 2, 2, 0,
        1, 2, 1, 0, 3, 3, 3, 0,
        2, 2, 0, 0, 2, 2, 2, 0,
        1, 3, 1, 0, 3, 3, 3, 0,
    };

    private static readonly ushort[] OP_PAGE_CYCLE = new ushort[]
    {
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 0, 0, 0, 0, 0, 0,
        0, 1, 0, 0, 1, 1, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 0, 0, 0, 0, 0, 0,
        0, 1, 0, 0, 1, 1, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 0, 0, 0, 0, 0, 0,
        0, 1, 0, 0, 1, 1, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 0, 0, 0, 0, 0, 0,
        0, 1, 0, 0, 1, 1, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        1, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 0, 1, 0, 0, 0, 0,
        0, 1, 0, 1, 1, 1, 1, 1,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 0, 0, 0, 0, 0, 0,
        0, 1, 0, 0, 1, 1, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 0, 0, 0, 0, 0, 0,
        0, 1, 0, 0, 1, 1, 0, 0,
    };

    private readonly string[] OP_LABELS = new string[]
    {
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

    private readonly Action[] OP;
    private Action[] InitOP()
    {
        return new Action[]
        {
            BRK, ORA, KIL, SLO, NOP, ORA, ASL, SLO,
            PHP, ORA, ASL, ANC, NOP, ORA, ASL, SLO,
            BPL, ORA, KIL, SLO, NOP, ORA, ASL, SLO,
            CLC, ORA, NOP, SLO, NOP, ORA, ASL, SLO,
            JSR, AND, KIL, RLA, BIT, AND, ROL, RLA,
            PLP, AND, ROL, ANC, BIT, AND, ROL, RLA,
            BMI, AND, KIL, RLA, NOP, AND, ROL, RLA,
            SEC, AND, NOP, RLA, NOP, AND, ROL, RLA,
            RTI, EOR, KIL, SRE, NOP, EOR, LSR, SRE,
            PHA, EOR, LSR, ALR, JMP, EOR, LSR, SRE,
            BVC, EOR, KIL, SRE, NOP, EOR, LSR, SRE,
            CLI, EOR, NOP, SRE, NOP, EOR, LSR, SRE,
            RTS, ADC, KIL, RRA, NOP, ADC, ROR, RRA,
            PLA, ADC, ROR, ARR, JMP, ADC, ROR, RRA,
            BVS, ADC, KIL, RRA, NOP, ADC, ROR, RRA,
            SEI, ADC, NOP, RRA, NOP, ADC, ROR, RRA,
            NOP, STA, NOP, SAX, STY, STA, STX, SAX,
            DEY, NOP, TXA, XAA, STY, STA, STX, SAX,
            BCC, STA, KIL, AHX, STY, STA, STX, SAX,
            TYA, STA, TXS, TAS, SHY, STA, SHX, AHX,
            LDY, LDA, LDX, LAX, LDY, LDA, LDX, LAX,
            TAY, LDA, TAX, LAX, LDY, LDA, LDX, LAX,
            BCS, LDA, KIL, LAX, LDY, LDA, LDX, LAX,
            CLV, LDA, TSX, LAS, LDY, LDA, LDX, LAX,
            CPY, CMP, NOP, DCP, CPY, CMP, DEC, DCP,
            INY, CMP, DEX, AXS, CPY, CMP, DEC, DCP,
            BNE, CMP, KIL, DCP, NOP, CMP, DEC, DCP,
            CLD, CMP, NOP, DCP, NOP, CMP, DEC, DCP,
            CPX, SBC, NOP, ISC, CPX, SBC, INC, ISC,
            INX, SBC, NOP, SBC, CPX, SBC, INC, ISC,
            BEQ, SBC, KIL, ISC, NOP, SBC, INC, ISC,
            SED, SBC, NOP, ISC, NOP, SBC, INC, ISC,
        };
    }

    #endregion

    #region MEMORY / DATA

    // REGISTERS

    private ushort PC;  // program counter

    private byte SP;    // stack pointer
    //private byte SR;    // status register

    private byte 
        A,     // accumulator
        X,     // index register X
        Y;     // index register Y

    // PROCESSOR STATUS

    private byte 
        PS_C,  // carry flag
        PS_Z,  // zero flag
        PS_I,  // interrupt disable flag
        PS_D,  // decimal mode flag
        PS_B,  // break command flag
        PS_U,  // UNUSED flag
        PS_V,  // overflow flag
        PS_N;  // negative flag

    private const byte PS_TRUE = 1;
    private const byte PS_FALSE = 0;

    private byte PS
    {
        get
        {
            return (byte)
            (
                PS_C << 0 |
                PS_Z << 1 |
                PS_I << 2 |
                PS_D << 3 |
                PS_B << 4 |
                PS_U << 5 |
                PS_V << 6 |
                PS_N << 7
            );
        }

        set
        {
            PS_C = (byte)((value >> 0) & PS_TRUE);
            PS_Z = (byte)((value >> 1) & PS_TRUE);
            PS_I = (byte)((value >> 2) & PS_TRUE);
            PS_D = (byte)((value >> 3) & PS_TRUE);
            PS_B = (byte)((value >> 4) & PS_TRUE);
            PS_U = (byte)((value >> 5) & PS_TRUE);
            PS_V = (byte)((value >> 6) & PS_TRUE);
            PS_N = (byte)((value >> 7) & PS_TRUE);
        }
    }

    // INTERNAL VARIABLES

    private NES NES;

    private byte CycleAddrMode;
    private ushort CycleAddr;
    
    // PUBLIC VARIABLES

    public ulong CycleCounter;
    public byte Interrupt;
    public int Stall;

    #endregion
    
    public CPU(NES nes)
    {
        NES = nes;

        OP = InitOP();
    }

    #region [ STORAGE OPCODES ]

    private void LDA() // Load Accumulator
    {
        A = NES.MMC.ReadByte(Bank.CPU, CycleAddr);
        SetZN(A);
    }
    private void LDX() // Load X Register
    {
        X = NES.MMC.ReadByte(Bank.CPU, CycleAddr);
        SetZN(X);
    }
    private void LDY() // Load Y Register
    {
        Y = NES.MMC.ReadByte(Bank.CPU, CycleAddr);
        SetZN(Y);
    }
    private void STA() // Store Accumulator
    {
        NES.MMC.WriteByte(Bank.CPU, CycleAddr, A);
    }
    private void STX() // Store X Register
    {
        NES.MMC.WriteByte(Bank.CPU, CycleAddr, X);
    }
    private void STY() // Store Y Register
    {
        NES.MMC.WriteByte(Bank.CPU, CycleAddr, Y);
    }
    private void TAX() // Transfer Accumulator to X
    {
        X = A;
        SetZN(X);
    }
    private void TAY() // Transfer Accumulator to Y
    {
        Y = A;
        SetZN(Y);
    }
    private void TSX() // Transfer Stack Pointer to X
    {
        X = SP;
        SetZN(X);
    }
    private void TXA() // Transfer X to Accumulator
    {
        A = X;
        SetZN(A);
    }
    private void TXS() // Transfer X to Stack Pointer
    {
        SP = X;
    }
    private void TYA() // Transfer Y to Accumulator
    {
        A = Y;
        SetZN(A);
    }

    #endregion

    #region [ MATH OPCODES ]

    private void ADC() // Add with Carry
    {
        byte a = A;
        byte b = NES.MMC.ReadByte(Bank.CPU, CycleAddr);
        byte c = PS_C;

        var tmp = a + b + c;
        A = (byte)tmp;
        SetZN(A);

        PS_C = (tmp > 0xFF) ? PS_TRUE : PS_FALSE;

        PS_V = (((a ^ b) & 0x80) == 0 && ((a ^ A) & 0x80) != 0) ? PS_TRUE : PS_FALSE;
    }
    private void SBC() // Subtract with Carry
    {
        byte a = A;
        byte b = NES.MMC.ReadByte(Bank.CPU, CycleAddr);
        byte c = PS_C;

        A = (byte)(a - b - (1 - c));
        SetZN(A);

        PS_C = (a - b - (1 - c) >= 0) ? PS_TRUE : PS_FALSE;

        PS_V = (((a ^ b) & 0x80) != 0 && ((a ^ A) & 0x80) != 0) ? PS_TRUE : PS_FALSE;
    }

    private void DEC() // Decrement Memory
    {
        byte val = (byte)(NES.MMC.ReadByte(Bank.CPU, CycleAddr) - 1);
        NES.MMC.WriteByte(Bank.CPU, CycleAddr, val);
        SetZN(val);
    }
    private void INC() // Increment Memory
    {
        byte val = (byte)(NES.MMC.ReadByte(Bank.CPU, CycleAddr) + 1);
        NES.MMC.WriteByte(Bank.CPU, CycleAddr, val);
        SetZN(val);
    }

    private void DEX() // Decrement X Register
    {
        SetZN(--X);
    }
    private void INX() // Increment X Register
    {
        SetZN(++X);
    }

    private void DEY() // Decrement Y Register
    {
        SetZN(--Y);
    }
    private void INY() // Increment Y Register
    {
        SetZN(++Y);
    }

    #endregion

    #region [ BITWISE OPCODES ]

    private void ASL() // Arithmetic Shift Left
    {
        if (CycleAddrMode == (byte)MODE.Accumulator)
        {
            PS_C = (byte)((A >> 7) & PS_TRUE);
            A <<= 1;
            SetZN(A);
        }
        else
        {
            byte val = NES.MMC.ReadByte(Bank.CPU, CycleAddr);
            PS_C = (byte)((val >> 7) & PS_TRUE);
            val <<= 1;
            NES.MMC.WriteByte(Bank.CPU, CycleAddr, val);
            SetZN(val);
        }
    }
    private void LSR() // Logical Shift Right
    {
        if (CycleAddrMode == (byte)MODE.Accumulator)
        {
            PS_C = (byte)(A & PS_TRUE);
            A >>= 1;
            SetZN(A);
        }
        else
        {
            byte val = NES.MMC.ReadByte(Bank.CPU, CycleAddr);
            PS_C = (byte)(val & PS_TRUE);
            val >>= 1;
            NES.MMC.WriteByte(Bank.CPU, CycleAddr, val);
            SetZN(val);
        }
    }

    private void BIT() // Bit Test
    {
        byte val = NES.MMC.ReadByte(Bank.CPU, CycleAddr);
        PS_V = (byte)((val >> 6) & PS_TRUE);
        SetZ((byte)(val & A));
        SetN(val);
    }
    private void EOR() // Exclusive OR
    {
        A = (byte)(A ^ NES.MMC.ReadByte(Bank.CPU, CycleAddr));
        SetZN(A);
    }
    private void AND() // Logical AND
    {
        A = (byte)(A & NES.MMC.ReadByte(Bank.CPU, CycleAddr));
        SetZN(A);
    }
    private void ORA() // Logical Inclusive OR
    {
        A = (byte)(A | NES.MMC.ReadByte(Bank.CPU, CycleAddr));
        SetZN(A);
    }

    private void ROL() // Rotate Left
    {
        if (CycleAddrMode == (byte)MODE.Accumulator)
        {
            byte c = PS_C;
            PS_C = (byte)((A >> 7) & 0x1);
            A = (byte)((A << 1) | c);
            SetZN(A);
        }
        else
        {
            byte c = PS_C;
            byte val = NES.MMC.ReadByte(Bank.CPU, CycleAddr);
            PS_C = (byte)((val >> 7) & 0x1);
            val = (byte)((val << 1) | c);
            NES.MMC.WriteByte(Bank.CPU, CycleAddr, val);
            SetZN(val);
        }
    }
    private void ROR() // Rotate Right
    {
        if (CycleAddrMode == (byte)MODE.Accumulator)
        {
            byte c = PS_C;
            PS_C = (byte)(A & PS_TRUE);
            A = (byte)((A >> 1) | (c << 7));
            SetZN(A);
        }
        else
        {
            byte c = PS_C;
            byte val = NES.MMC.ReadByte(Bank.CPU, CycleAddr);
            PS_C = (byte)(val & 0x1);
            val = (byte)((val >> 1) | (c << 7));
            NES.MMC.WriteByte(Bank.CPU, CycleAddr, val);
            SetZN(val);
        }
    }

    #endregion

    #region [ BRANCH OPCODES ]

    private void BCC() // branch if carry clear
    {
        if (PS_C == PS_TRUE) return;

        CycleCounter += PagesDiffer(PC, CycleAddr) ? 2UL : 1UL;
        PC = CycleAddr;
    }
    private void BCS() // branch if carry set
    {
        if (PS_C == PS_FALSE) return;

        CycleCounter += PagesDiffer(PC, CycleAddr) ? 2UL : 1UL;
        PC = CycleAddr;
    }
    private void BEQ() // branch if equal
    {
        if (PS_Z == PS_FALSE) return;

        CycleCounter += PagesDiffer(PC, CycleAddr) ? 2UL : 1UL;
        PC = CycleAddr;
    }
    private void BNE() // branch if not equal
    {
        if (PS_Z == PS_TRUE) return;

        CycleCounter += PagesDiffer(PC, CycleAddr) ? 2UL : 1UL;
        PC = CycleAddr;
    }
    private void BMI() // branch if minus
    {
        if (PS_N == PS_FALSE) return;

        CycleCounter += PagesDiffer(PC, CycleAddr) ? 2UL : 1UL;
        PC = CycleAddr;
    }
    private void BPL() // branch if positive
    {
        if (PS_N == PS_TRUE) return;

        CycleCounter += PagesDiffer(PC, CycleAddr) ? 2UL : 1UL;
        PC = CycleAddr;
    }
    private void BVC() // branch if overflow clear
    {
        if (PS_V == PS_TRUE) return;

        CycleCounter += PagesDiffer(PC, CycleAddr) ? 2UL : 1UL;
        PC = CycleAddr;
    }
    private void BVS() // branch if overflow set
    {
        if (PS_V == PS_FALSE) return;

        CycleCounter += PagesDiffer(PC, CycleAddr) ? 2UL : 1UL;
        PC = CycleAddr;
    }

    #endregion

    #region [ JUMP OPCODES ]

    private void JMP() // jump
    {
        PC = CycleAddr;
    }
    private void JSR() // jump to subroutine 
    {
        PushShort((ushort)(PC - 1));
        PC = CycleAddr;
    }
    private void RTI() // return from interrupt
    {
        PS = (byte)(PullByte() & 0xEF | 0x20);
        PC = PullShort();
    }
    private void RTS() // return from subroutine
    {
        PC = (ushort)(PullShort() + 1);
    }

    #endregion

    #region [ REGISTER OPCODES ]

    private void CLC() // Clear Carry Flag
    {
        PS_C = PS_FALSE;
    }
    private void CLD() // Clear Decimal Mode
    {
        PS_D = PS_FALSE;
    }
    private void CLI() // Clear Interrupt Disable
    {
        PS_I = PS_FALSE;
    }
    private void CLV() // Clear Overflow Flag
    {
        PS_V = PS_FALSE;
    }

    private void CMP() // Compare
    {
        Compare(A, NES.MMC.ReadByte(Bank.CPU, CycleAddr));
    }
    private void CPX() // Compare X Register
    {
        Compare(X, NES.MMC.ReadByte(Bank.CPU, CycleAddr));
    }
    private void CPY() // Compare Y Register
    {
        Compare(Y, NES.MMC.ReadByte(Bank.CPU, CycleAddr));
    }

    private void SEC() // Set Carry Flag
    {
        PS_C = PS_TRUE;
    }
    private void SED() // Set Decimal Flag
    {
        PS_D = PS_TRUE;
    }
    private void SEI() // Set Interrupt Disable
    {
        PS_I = PS_TRUE;
    }

    #endregion

    #region [ STACK OPCODES ]

    private void PHA() // Push accumulator
    {
        PushByte(A);
    }
    private void PLA() // Pull accumulator
    {
        A = PullByte();
        SetZN(A);
    }
    private void PHP() // Push processor status
    {
        PushByte((byte)(PS | 0x10));
    }
    private void PLP() // Pull processor status
    {
        PS = (byte)(PullByte() & 0xEF | 0x20);
    }

    #endregion

    #region [ SYSTEM OPCODES ]

    private void IRQ() // IRQ Interrupt
    {
        PushShort(PC);
        PushByte((byte)(PS | 0x10));
        PC = NES.MMC.ReadShort(Bank.CPU, 0xFFFE);
        PS_I = PS_TRUE;
        CycleCounter += 7;
    }
    private void NMI() // Non-Maskable Interrupt
    {
        PushShort(PC);
        PushByte((byte)(PS | 0x10)); // Z
        PC = NES.MMC.ReadShort(Bank.CPU, 0xFFFA);
        PS_I = PS_TRUE;
        CycleCounter += 7;
    }
    private void BRK() // Force Interrupt
    {
        PushShort(PC);
        PushByte((byte)(PS | 0x10)); // Z
        PS_I = PS_TRUE;
        PC = NES.MMC.ReadShort(Bank.CPU, 0xFFFE);
    }
    private void NOP() { } // No Operation

    #endregion

    #region [ UNOFFICIAL OPCODES ]

    private void KIL() { }

    private void SLO() { }
    private void RLA() { }
    private void SRE() { }
    private void RRA() { }
    private void SAX() { }
    private void LAX() { }
    private void DCP() { }
    private void ISC() { }
    private void ANC() { }
    private void ALR() { }
    private void ARR() { }
    private void XAA() { }
    private void AHX() { }
    private void AXS() { }
    private void TAS() { }
    private void SHY() { }
    private void SHX() { }
    private void LAS() { }

    #endregion

    #region STACK

    private void PushByte(byte val)
    {
        NES.MMC.WriteByte(Bank.CPU, (ushort)(0x100 | SP--), val);
    }
    private void PushShort(ushort val)
    {
        byte hi = (byte)(val >> 8);
        byte lo = (byte)(val & 0xFF);
        PushByte(hi);
        PushByte(lo);
    }

    private byte PullByte()
    {
        return NES.MMC.ReadByte(Bank.CPU, (ushort)(0x100 | ++SP));
    }
    private ushort PullShort()
    {
        byte lo = PullByte();
        byte hi = PullByte();
        return (ushort)(hi << 8 | lo);
    }

    private void Compare(byte a, byte b)
    {
        SetZN((byte)(a - b));

        PS_C = a >= b ? PS_TRUE : PS_FALSE; 
    }

    #endregion

    #region GENERAL

    private bool PagesDiffer(ushort addrA, ushort addrB)
    {
        return (addrA & 0xFF00) != (addrB & 0xFF00);
    }

    private void SetZ(byte val)
    {
        // sets the zero flag if the argument is zero
        PS_Z = val == 0 ? PS_TRUE : PS_FALSE;
    }

    private void SetN(byte val)
    {
        // sets the negative flag if the argument is negative (high bit is set)
        PS_N = (val & 0x80) != 0 ? PS_TRUE : PS_FALSE;
    }

    private void SetZN(byte val)
    {
        // sets the zero flag if the argument is zero
        PS_Z = val == 0 ? PS_TRUE : PS_FALSE;

        // sets the negative flag if the argument is negative (high bit is set)
        PS_N = (val & 0x80) != 0 ? PS_TRUE : PS_FALSE;
    }

    public void Reset()
    {
        //PC = NES.MMC.ReadByte(Bank.CPU, 0xFFFD); // PC = byte at $FFFD * 256 + byte at $FFFC 
        //PC *= 256;
        PC = NES.MMC.ReadShort(Bank.CPU, 0xFFFC);

        SP = 0xFD;
        PS = 0x24;

        CycleCounter = 12;
    }

    public void Power()
    {
        Reset();
    }

    public int Step()
    {
        if (Stall > 0)
        {
            Stall -= 1;
            return 1;
        }

        ulong cyclesOld = CycleCounter;

        switch (Interrupt)
        {
            case INTERRUPT_NMI:
                NMI();
                Interrupt = 0;
                break;
            case INTERRUPT_IRQ:
                IRQ();
                Interrupt = 0;
                break;
        }

        var op = (int)NES.MMC.ReadByte(Bank.CPU, PC);
        var op_func = OP[op];
        var op_size = OP_SIZE[op];
        var op_cycles = OP_CYCLES[op];
        
        if (NES.Debug)
        {
            var arg1 = "$" + op.ToString("X2");
            var arg2 = op_size > 1 ? string.Format("${0:X2}", NES.MMC.ReadByte(Bank.CPU, (ushort)(PC + 1))) : string.Empty;
            var arg3 = op_size > 2 ? string.Format("${0:X2}", NES.MMC.ReadByte(Bank.CPU, (ushort)(PC + 2))) : string.Empty;
            var opcode_str = OP_LABELS[op];

            NES.LOG.WriteLine(string.Format("{0,4:X4}  {1,3} {2,3} {3,3}  {4,-32} A:{5,2:X2} X:{6,2:X2} Y:{7,2:X2} P:{8,2:X2} SP:{9,2:X2} CYC:{10,3} SL:{11,3}",
                PC, arg1, arg2, arg3, opcode_str, A, X, Y, PS, SP, (CycleCounter * 3) % 341, NES.PPU.Scanline));
            NES.LOG.Flush();
        }

        CycleAddrMode = OP_MODE[op];
        ushort addr;

        switch (CycleAddrMode)
        {
            case (byte)MODE.Absolute:
                addr = PC;
                addr += 1;
                CycleAddr = NES.MMC.ReadShort(Bank.CPU, addr);
                break;
            case (byte)MODE.AbsoluteX:
                addr = PC;
                addr += 1;
                CycleAddr = NES.MMC.ReadShort(Bank.CPU, addr);
                CycleAddr += X;
                if (PagesDiffer((ushort)(CycleAddr - X), CycleAddr))
                    CycleCounter += OP_PAGE_CYCLE[op];
                break;
            case (byte)MODE.AbsoluteY:
                addr = PC;
                addr += 1;
                CycleAddr = NES.MMC.ReadShort(Bank.CPU, addr);
                CycleAddr += Y;
                if (PagesDiffer((ushort)(CycleAddr - Y), CycleAddr))
                    CycleCounter += OP_PAGE_CYCLE[op];
                break;
            case (byte)MODE.Accumulator:
                CycleAddr = 0;
                break;
            case (byte)MODE.Immediate:
                CycleAddr = PC;
                CycleAddr += 1;
                break;
            case (byte)MODE.Implied:
                CycleAddr = 0;
                break;
            case (byte)MODE.Indirect:
                addr = NES.MMC.ReadShort(Bank.CPU, (ushort)(PC + 1));
                CycleAddr = NES.MMC.ReadShortAlt(Bank.CPU, addr);
                break;
            case (byte)MODE.IndexedIndirect: // (Indirect, X)
                addr = (ushort)(NES.MMC.ReadByte(Bank.CPU, (ushort)(PC + 1)) + X);
                CycleAddr = NES.MMC.ReadShortAlt(Bank.CPU, (ushort)(addr & 0xFF));
                break;
            case (byte)MODE.IndirectIndexed: // (Indirect), Y
                addr = NES.MMC.ReadByte(Bank.CPU, (ushort)(PC + 1));
                CycleAddr = (ushort)(NES.MMC.ReadShortAlt(Bank.CPU, addr) + Y);
                if (PagesDiffer((ushort)(CycleAddr - Y), CycleAddr))
                    CycleCounter += OP_PAGE_CYCLE[op];
                break;
            case (byte)MODE.Relative:
                addr = PC;
                addr += 1;
                addr = NES.MMC.ReadByte(Bank.CPU, addr);
                if (addr < 0x80)
                {
                    CycleAddr = PC;
                    CycleAddr += 2;
                    CycleAddr += addr;
                }
                else
                {
                    CycleAddr = PC;
                    CycleAddr += 2;
                    CycleAddr += addr;
                    CycleAddr -= 0x100;
                }
                break;
            case (byte)MODE.ZeroPage:
                addr = PC;
                addr += 1;
                CycleAddr = NES.MMC.ReadByte(Bank.CPU, addr);
                break;
            case (byte)MODE.ZeroPageX:
                addr = (ushort)(PC + 1);
                CycleAddr = (ushort)((NES.MMC.ReadByte(Bank.CPU, addr) + X) & 0xFF);
                break;
            case (byte)MODE.ZeroPageY:
                addr = (ushort)(PC + 1);
                CycleAddr = (ushort)((NES.MMC.ReadByte(Bank.CPU, addr) + Y) & 0xFF);
                break;
        }

        PC += op_size;
        CycleCounter += op_cycles;

        op_func.Invoke();

        return (int)(CycleCounter - cyclesOld);
    }

    #endregion
}

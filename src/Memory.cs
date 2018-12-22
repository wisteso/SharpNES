using System;
using System.IO;

public class Memory
{
    private NES NES;

    public Memory(NES nes)
    {
        NES = nes;
    }

    public void Power()
    {
        
    }

    // MEMORY (64k)

    // http://wiki.nesdev.com/w/index.php/CPU_memory_map
    // RAM          ($0000)  (2k) 7 pages? 0 = critical variables (zero page), 1 = stack??, 2-7 sprites and other data?
    // ^ Mirrors    ($0800)  (6k)
    // PPU          ($2000)  (8b)
    // ^ Mirrors    ($2008)  (~8k)
    // APU + IO     ($4000)  (32b)
    // Cart Data    ($4020)  (48k - 32b)
    //      Most Common Allocation
    //          ????         ($4020)  (8k - 32b)
    //          Cart RAM     ($6000)  (8k)
    //          Cart ROM     ($8000)  (32K)
    //private byte[][] MEMORY = new byte[2][];

    private byte[] RAM = new byte[2048];

    // http://wiki.nesdev.com/w/index.php/PPU_memory_map

    public byte ReadByte(Bank bank, ushort addr)
    {
        if (bank == Bank.CPU)
        {
            if (addr < 0x2000)
            {
                return RAM[addr % 0x800]; // mirrored four times
            }
            else if (addr < 0x4000)
            {
                return NES.PPU.ReadRegister((ushort)(0x2000 + (addr % 8)));
            }
            else if (addr < 0x6000)
            {
                if (addr == 0x4014) return NES.PPU.ReadRegister(addr);
                //else if (addr == 0x4015) return NES.APU.ReadRegister(addr);
                else if (addr == 0x4016) return NES.CONTROLLER_1.Read();
                else if (addr == 0x4017) return NES.CONTROLLER_2.Read();
                return 0;
            }
            else // >= 6000
            {
                return NES.MAPPER.Read(addr);
            }
        }
        else
        {
            addr %= 0x4000;

            if (addr < 0x2000)
                return NES.MAPPER.Read(addr);
            else if (addr < 0x4000)
                return NES.PPU.ReadRegisterPPU(addr);
            else
                throw new Exception("Illegal read at " + addr.ToString("X4"));
        }
    }
    public ushort ReadShort(Bank bank, ushort addr)
    {
        byte lo = ReadByte(bank, addr);
        byte hi = ReadByte(bank, (ushort)(addr + 1));

        return (ushort)(hi << 8 | lo);
    }
    public ushort ReadShortAlt(Bank bank, ushort addr)
    {
        byte lo = ReadByte(bank, addr);
        byte hi = ReadByte(bank, (ushort)((addr & 0xFF00) | (byte)(addr + 1)));

        return (ushort)(hi << 8 | lo);
    }

    public void WriteByte(Bank bank, ushort addr, byte val)
    {
        if (bank == Bank.CPU)
        {
            if (addr < 0x2000)
            {
                RAM[addr % 0x0800] = val;
            }
            else if (addr < 0x4000)
            {
                NES.PPU.WriteRegister((ushort)(0x2000 + (addr % 8)), val);
            }
            else if (addr < 0x6000)
            {
                if (addr == 0x4014) NES.PPU.WriteRegister(addr, val);
                //else if (addr == 0x4015) NES.APU.WriteRegister(addr, val);
                else if (addr == 0x4016) NES.CONTROLLER_1.Write(val);
                else if (addr == 0x4017) NES.CONTROLLER_2.Write(val);
            }
            else
            {
                NES.MAPPER.Write(addr, val);
            }
        }
        else
        {
            addr %= 0x4000;

            if (addr < 0x2000)
                NES.MAPPER.Write(addr, val);
            else if (addr < 0x4000)
                NES.PPU.WriteRegisterPPU(addr, val);
            else
                throw new Exception("Illegal write at " + addr.ToString("X4"));
        }
    }

    private int[,] MIRROR = new int[,]
    {
        { 0, 0, 1, 1 },
        { 0, 1, 0, 1 },
        { 0, 0, 0, 0 },
        { 1, 1, 1, 1 },
        { 0, 1, 2, 3 },
    };

    public ushort MirrorAddress(ushort addr, byte mode)
    {
        addr = (ushort)((addr - 0x2000) % 0x1000);
        var table = addr / 0x0400;
        var offset = addr % 0x0400;
        return (ushort)(0x2000 + MIRROR[mode, table] * 0x0400 + offset);
    }

    public const byte Horizontal = 0;
    public const byte Vertical = 1;
    public const byte Single0 = 2;
    public const byte Single1 = 3;
    public const byte Four = 4;
}

public enum Bank
{
    CPU = 0, PPU = 1
}

public interface Controller
{
    void Write(byte val);

    byte Read();
}

public class NullController : Controller
{
    public byte Read() { return 0; }
    public void Write(byte val) { }
}
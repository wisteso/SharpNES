using System;

public class CNROM : Mapper
{
    // CHR Bank     (internal, $E000-$FFFF)
    // PRG Bank 0   (internal, $A000-$BFFF)
    // PRG Bank 1   (internal, $C000-$DFFF)

    private int
        chrBank, prgBank1, prgBank2;

    public CNROM(NES nes) { }

    public override void Load(byte[] prg, byte[] chr)
    {
        base.Load(prg, chr);

        chrBank = 0;
        prgBank1 = 0;
        prgBank2 = (prg.Length / 0x4000) - 1;
    }

    public override byte Read(ushort addr)
    {
        if (addr < 0x2000)
        {
            return CHR[chrBank * 0x2000 + addr];
        }
        else if (addr >= 0xC000)
        {
            return PRG[prgBank2 * 0x4000 + (addr - 0xC000)];
        }
        else if (addr >= 0x8000)
        {
            return PRG[prgBank1 * 0x4000 + (addr - 0x8000)];
        }
        else if (addr >= 0x6000)
        {
            return SRAM[addr - 0x6000];
        }
        else
        {
            throw new Exception("Unhandled mapper read at addr: " + addr.ToString("X4"));
        }
    }

    public override void Write(ushort addr, byte val)
    {
        if (addr < 0x2000)
        {
            CHR[chrBank * 0x2000 + addr] = val;
        }
        else if (addr >= 0x8000)
        {
            chrBank = (val & 0x3);
        }
        else if (addr >= 0x6000)
        {
            SRAM[addr - 0x6000] = val;
        }
        else
        {
            throw new Exception("Unhandled mapper write at addr: " + addr.ToString("X4"));
        }
    }
}
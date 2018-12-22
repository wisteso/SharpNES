using System;

public class MMC1 : Mapper
{
    // PRG Bank     (internal, $E000-$FFFF)
    // CHR Bank 0   (internal, $A000-$BFFF)
    // CHR Bank 1   (internal, $C000-$DFFF)
    // Control      (internal, $8000-$9FFF)

    private byte
        shiftRegister,
        control,
        prgMode, chrMode,
        prgBank, chrBank0, chrBank1;

    private int[]
        prgOffsets = new int[2],
        chrOffsets = new int[2] { 0x0000, 0x1000 };

    public MMC1(NES nes) { }

    public override void Load(byte[] prg, byte[] chr)
    {
        base.Load(prg, chr);

        shiftRegister = 0x10;
        prgOffsets[1] = BankOffsetPRG(-1);
    }

    public override byte Read(ushort addr)
    {
        if (addr < 0x2000)
        {
            var bank = addr / 0x1000;
            var offset = addr % 0x1000;
            return CHR[chrOffsets[bank] + offset];
        }
        else if (addr >= 0x8000)
        {
            addr -= 0x8000;
            var bank = addr / 0x4000;
            var offset = addr % 0x4000;
            return PRG[prgOffsets[bank] + offset];
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
            var bank = addr / 0x1000;
            var offset = addr % 0x1000;
            CHR[chrOffsets[bank] + offset] = val;
        }
        else if (addr >= 0x8000)
        {
            LoadRegister(addr, val);
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

    private void LoadRegister(ushort addr, byte val)
    {
        if ((val & 0x80) == 0x80)
        {
            shiftRegister = 0x10;
            WriteControl((byte)(control | 0x0C));
        }
        else
        {
            bool complete = (shiftRegister & 1) == 1;
            shiftRegister >>= 1;
            shiftRegister = (byte)(shiftRegister | ((val & 1) << 4));
            if (complete)
            {
                WriteRegister(addr, shiftRegister);
                shiftRegister = 0x10;
            }
        }
    }

    private void WriteRegister(ushort addr, byte val)
    {
        if (addr <= 0x9FFF)
        {
            WriteControl(val);
        }
        else if (addr <= 0xBFFF)
        {
            // writeCHRBank0
            chrBank0 = val;
            UpdateOffsets();
        }
        else if (addr <= 0xDFFF)
        {
            // writeCHRBank1
            chrBank1 = val;
            UpdateOffsets();
        }
        else if (addr <= 0xFFFF)
        {
            // writePRGBank
            prgBank = (byte)(val & 0x0F);
            UpdateOffsets();
        }
    }

    private void WriteControl(byte val)
    {
        control = val;
        chrMode = (byte)((val >> 4) & 1);
        prgMode = (byte)((val >> 2) & 3);

        switch (val & 3)
        {
            case 0:
                MirrorMode = Memory.Single0;
                break;
            case 1:
                MirrorMode = Memory.Single1;
                break;
            case 2:
                MirrorMode = Memory.Vertical;
                break;
            case 3:
                MirrorMode = Memory.Horizontal;
                break;
        }

        UpdateOffsets();
    }

    private int BankOffsetPRG(int index)
    {
        if (index >= 0x80) index -= 0x100;

        index %= PRG.Length / 0x4000;

        var offset = index * 0x4000;

        return offset < 0 ? offset + PRG.Length : offset;
    }

    private int BankOffsetCHR(int index)
    {
        if (index >= 0x80) index -= 0x100;

        index %= CHR.Length / 0x1000;

        var offset = index * 0x1000;

        return offset < 0 ? offset + CHR.Length : offset;
    }

    // PRG ROM bank mode (0, 1: switch 32 KB at $8000, ignoring low bit of bank number;
    //                    2: fix first bank at $8000 and switch 16 KB bank at $C000;
    //                    3: fix last bank at $C000 and switch 16 KB bank at $8000)
    // CHR ROM bank mode (0: switch 8 KB at a time; 1: switch two separate 4 KB banks)
    private void UpdateOffsets()
    {
        switch (prgMode)
        {
            case 0:
            case 1:
                prgOffsets[0] = BankOffsetPRG(prgBank & 0xFE);
                prgOffsets[1] = BankOffsetPRG(prgBank | 0x01);
                break;
            case 2:
                prgOffsets[0] = 0;
                prgOffsets[1] = BankOffsetPRG(prgBank);
                break;
            case 3:
                prgOffsets[0] = BankOffsetPRG(prgBank);
                prgOffsets[1] = BankOffsetPRG(-1);
                break;
        }

        switch (chrMode)
        {
            case 0:
                chrOffsets[0] = BankOffsetCHR(chrBank0 & 0xFE);
                chrOffsets[1] = BankOffsetCHR(chrBank0 | 0x01);
                break;
            case 1:
                chrOffsets[0] = BankOffsetCHR(chrBank0);
                chrOffsets[1] = BankOffsetCHR(chrBank1);
                break;
        }
    }
}
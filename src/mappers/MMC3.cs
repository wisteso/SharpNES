using System;

public class MMC3 : Mapper
{
    private byte
        register, prgMode, chrMode, reload, counter;

    private byte[]
        registers = new byte[8];

    private int[]
        prgOffsets = new int[4],
        chrOffsets = new int[8];

    private bool irqEnable;

    private NES NES;

    public MMC3(NES nes) { NES = nes; }

    public override void Load(byte[] prg, byte[] chr)
    {
        base.Load(prg, chr);

        prgOffsets[0] = BankOffsetPRG(0);
        prgOffsets[1] = BankOffsetPRG(1);
        prgOffsets[2] = BankOffsetPRG(-2);
        prgOffsets[3] = BankOffsetPRG(-1);
    }

    public override void Step()
    {
        if (NES.PPU.Cycle != 260) return; // should be 260, but 280 is workaround

        if (NES.PPU.Scanline > 239 && NES.PPU.Scanline < 261) return;

        if (NES.PPU.flagShowBackground == 0 && NES.PPU.flagShowSprites == 0) return;

        if (counter == 0)
        {
            counter = reload;
        }
        else
        {
            counter -= 1;
            if (counter == 0 && irqEnable)
                NES.CPU.Interrupt = CPU.INTERRUPT_IRQ;
        }
    }

    public override byte Read(ushort addr)
    {
        if (addr < 0x2000)
        {
            var bank = addr / 0x0400;
            var offset = addr % 0x0400;
            return CHR[chrOffsets[bank] + offset];
        }
        else if (addr >= 0x8000)
        {
            addr -= 0x8000;
            var bank = addr / 0x2000;
            var offset = addr % 0x2000;
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
            var bank = addr / 0x0400;
            var offset = addr % 0x0400;
            CHR[chrOffsets[bank] + offset] = val;
        }
        else if (addr >= 0x8000)
        {
            WriteRegister(addr, val);
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

    private void WriteRegister(ushort addr, byte val)
    {
        if (addr <= 0x9FFF && addr % 2 == 0) // writeBankSelect
        {
            prgMode = (byte)((val >> 6) & 1);
            chrMode = (byte)((val >> 7) & 1);
            register = (byte)(val & 7);
            UpdateOffsets();
        }
        else if (addr <= 0x9FFF && addr % 2 == 1) // writeBankData
        {
            registers[register] = val;
            UpdateOffsets();
        }
        else if (addr <= 0xBFFF && addr % 2 == 0) // writeMirror
        {
            MirrorMode = (val & 1) == 0 ? Memory.Vertical : Memory.Horizontal;
        }
        else if (addr <= 0xBFFF && addr % 2 == 1) // writeProtect
        {
            // do nothing
        }
        else if (addr <= 0xDFFF && addr % 2 == 0) // writeIRQLatch
        {
            reload = val;
        }
        else if (addr <= 0xDFFF && addr % 2 == 1) // writeIRQReload
        {
            counter = 0;
        }
        else if (addr <= 0xFFFF && addr % 2 == 0) // writeIRQDisable
        {
            irqEnable = false;
        }
        else if (addr <= 0xFFFF && addr % 2 == 1) // writeIRQEnable
        {
            irqEnable = true;
        }
    }

    private int BankOffsetPRG(int index)
    {
        if (index >= 0x80) index -= 0x100;

        index %= PRG.Length / 0x2000;

        var offset = index * 0x2000;

        return offset < 0 ? offset + PRG.Length : offset;
    }

    private int BankOffsetCHR(int index)
    {
        if (index >= 0x80) index -= 0x100;

        index %= CHR.Length / 0x0400;

        var offset = index * 0x0400;

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
                prgOffsets[0] = BankOffsetPRG(registers[6]);
                prgOffsets[1] = BankOffsetPRG(registers[7]);
                prgOffsets[2] = BankOffsetPRG(-2);
                prgOffsets[3] = BankOffsetPRG(-1);
                break;
            case 1:
                prgOffsets[0] = BankOffsetPRG(-2);
                prgOffsets[1] = BankOffsetPRG(registers[7]);
                prgOffsets[2] = BankOffsetPRG(registers[6]);
                prgOffsets[3] = BankOffsetPRG(-1);
                break;
        }

        switch (chrMode)
        {
            case 0:
                chrOffsets[0] = BankOffsetCHR(registers[0] & 0xFE);
                chrOffsets[1] = BankOffsetCHR(registers[0] | 0x01);
                chrOffsets[2] = BankOffsetCHR(registers[1] & 0xFE);
                chrOffsets[3] = BankOffsetCHR(registers[1] | 0x01);
                chrOffsets[4] = BankOffsetCHR(registers[2]);
                chrOffsets[5] = BankOffsetCHR(registers[3]);
                chrOffsets[6] = BankOffsetCHR(registers[4]);
                chrOffsets[7] = BankOffsetCHR(registers[5]);
                break;
            case 1:
                chrOffsets[0] = BankOffsetCHR(registers[2]);
                chrOffsets[1] = BankOffsetCHR(registers[3]);
                chrOffsets[2] = BankOffsetCHR(registers[4]);
                chrOffsets[3] = BankOffsetCHR(registers[5]);
                chrOffsets[4] = BankOffsetCHR(registers[0] & 0xFE);
                chrOffsets[5] = BankOffsetCHR(registers[0] | 0x01);
                chrOffsets[6] = BankOffsetCHR(registers[1] & 0xFE);
                chrOffsets[7] = BankOffsetCHR(registers[1] | 0x01);
                break;
        }
    }
}
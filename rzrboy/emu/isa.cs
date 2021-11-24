using System.Collections;

namespace emu
{
    public static class isa
    {
        public static readonly IBuilder[] Instr = new IBuilder[256];
        private static readonly IBuilder[] ExtInstr = new IBuilder[256];

        private class ExtInstruction : IInstruction // this is just a proxy to extension ISA
        {
            private IInstruction cur = null;

            public bool Eval(Reg reg, Mem mem)
            {
                if (cur == null)
                {
                    byte opcode = mem.rom[reg.PC++]; // fetch
                    cur = ExtInstr[opcode].Build();
                    return true;
                }
                else
                {
                    return cur.Eval(reg, mem);
                }
            }
        }

        private class ExtBuilder : IBuilder
        {
            public IInstruction Build() { return new ExtInstruction(); }
        }

        public readonly static op nop = (Reg reg, Mem mem) => true;

        // read next byte from mem[pc++]
        public static op ldrom(Reg8 target) => (Reg reg, Mem mem) => { reg[target] = mem.rom[reg.PC++]; return true; };
        public static op[] ldrom(Reg8 t1, Reg8 t2) => new op[] { ldrom(t1), ldrom(t2) };
        public static op[] ldrom(Reg16 target) => new op[] {
            (Reg reg, Mem mem) => { reg[target] = mem.rom[reg.PC++]; return true; },
            (Reg reg, Mem mem) => { reg[target] |= (ushort)(mem.rom[reg.PC++] << 8); return true; }
        };

        // reg to reg
        public static op ldreg(Reg8 dst, Reg8 src) => (reg, mem) => { reg[dst] = reg[src]; return true; };
        // reg to address
        public static op ldreg(Reg8 dst, Reg16 src_addr) => (reg, mem) => { reg[dst] = mem[reg[src_addr]]; return true; };
        // address to reg
        public static op ldreg(Reg16 dst_addr, Reg8 src) => (reg, mem) => { mem[reg[dst_addr]] = reg[src]; return true; };

        private delegate IBuilder Build<Y, X>(Y y, X x);
        private static void Fill<Y, X>(byte offsetX, byte stepY, Build<Y, X> builder, IEnumerable<Y> ys, IEnumerable<X> xs) 
        {
            foreach (Y y in ys)
            {
                foreach ((X x, int i) in xs.Indexed())
                {
                    Instr[offsetX + i] = builder(y, x);
                }
                offsetX += stepY;
            }
        }

        static isa() 
        {
            Instr[0xCB] = new ExtBuilder();

            Instr[0x00] = new Builder(nop);
            Instr[0x01] = new Builder(ldrom(Reg8.B, Reg8.C));
            Instr[0x02] = new Builder(ldreg(Reg16.BC, Reg8.A));

            // single byte reg moves
            // LD B, B | LD B, C ...
            // LD D, B | LD D, C ...
            Fill(offsetX: 0x40, stepY: 0x10,
                (Reg8 dst, Reg8 src) => ldreg(dst, src).get(),
                ys: new[] { Reg8.B, Reg8.D, Reg8.H },
                xs: new[] { Reg8.B, Reg8.C, Reg8.D, Reg8.E, Reg8.H, Reg8.L });

            // LD (HL), B | LD (HL), C ...
            Fill(offsetX: 0x70, stepY: 0,
                (Reg16 dst, Reg8 src) => ldreg(dst, src).get(),
                ys: new[] { Reg16.HL },
                xs: new[] { Reg8.B, Reg8.C, Reg8.D, Reg8.E, Reg8.H, Reg8.L });

            //Instr[0x76] = new Builder(halt);
            // LD (HL), A
            Instr[0x77] = ldreg(Reg16.HL, Reg8.A).get();

            // LD r, (HL)
            Instr[0x46] = ldreg(Reg8.B, Reg16.HL).get();
            Instr[0x56] = ldreg(Reg8.D, Reg16.HL).get();
            Instr[0x76] = ldreg(Reg8.H, Reg16.HL).get();

        }
    }

    static class LoopExtensions
    {
        public static IEnumerable<(T item, int index)> Indexed<T>(this IEnumerable<T> self) =>  self.Select((item, index) => (item, index));
        public static Builder get(this op op) => new Builder(op);
        public static Builder Add(this op op, op other) { var b = new Builder(op); b.Add(other); return b; }
    }
}

using System.Diagnostics;

namespace emu
{
    public  class Isa
    {
        private static readonly IBuilder[] m_instructions = new IBuilder[256];
        private static readonly IBuilder[] m_extInstructions = new IBuilder[256];

        public IBuilder this[byte opcode] { get => m_instructions[opcode]; private set => m_instructions[opcode] = value;  }

        private class ExtInstruction : IInstruction // this is just a proxy to extension ISA
        {
            private IInstruction? cur = null;

            public IEnumerable<string> Disassemble(ushort pc, Mem mem)
            {
                byte opcode = mem.rom[pc]; // fetch
                return m_extInstructions[opcode].Build().Disassemble(++pc, mem);
            }

            public bool Eval(Reg reg, Mem mem)
            {
                if (cur == null)
                {
                    byte opcode = mem.rom[reg.PC++]; // fetch, 1 M-cycle
                    cur = m_extInstructions[opcode].Build();
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

        public static dis operand(Reg8 reg) => (pc, mem) => reg.ToString();
        public static dis[] operand(Reg8 dst, Reg8 src) => new dis[] { operand(dst), operand(src) };

        // reg to reg
        public static op ldreg(Reg8 dst, Reg8 src) => (reg, mem) => { reg[dst] = reg[src]; return true; };
        public static op ldreg(Reg16 dst, Reg16 src) => (reg, mem) => { reg[dst] = reg[src]; return true; };

        // reg to address
        public static op ldadr(Reg8 dst, Reg16 src_addr) => (reg, mem) => { reg[dst] = mem[reg[src_addr]]; return true; };
        // address to reg
        public static op ldadr(Reg16 dst_addr, Reg8 src) => (reg, mem) => { mem[reg[dst_addr]] = reg[src]; return true; };

        private delegate IBuilder Build<Y, X>(Y y, X x);

        // returns next opcode for validation
        private static int Fill<Y, X>(byte offsetX, byte stepY, Build<Y, X> builder, IEnumerable<Y> ys, IEnumerable<X> xs) 
        {
            foreach (Y y in ys)
            {
                foreach ((X x, int i) in xs.Indexed())
                {
                    Debug.Assert(m_instructions[offsetX + i] == null);
                    m_instructions[offsetX + i] = builder(y, x);
                }
                offsetX += stepY;
            }
            return offsetX - stepY + xs.Count();
        }
        private static int Fill<Y, X>(byte offsetX, Build<Y, X> builder, Y y, IEnumerable<X> xs)
        {
            return Fill(offsetX, 1, builder, new[] { y }, xs);
        }
        private static int Fill<Y, X>(byte offsetX, byte stepY, Build<Y, X> builder, IEnumerable<Y> ys, X x)
        {
            return Fill(offsetX, stepY, builder, ys, new[] { x });
        }

        public Isa() 
        {
            Reg8[] bcdehl = { Reg8.B, Reg8.C, Reg8.D, Reg8.E, Reg8.H, Reg8.L };

            this[0xCB] = new ExtBuilder();

            this[0x00] = nop.get();

            // single byte reg moves
            // LD B, B | LD B, C ...
            // LD [B D H], [B C D E H L]
            Fill(offsetX: 0x40, stepY: 0x10,
                (Reg8 dst, Reg8 src) => ldreg(dst, src).get(),
                ys: new[] { Reg8.B, Reg8.D, Reg8.H },
                xs: bcdehl);

            // LD [B D H], (HL)
            this[0x46] = ldadr(Reg8.B, Reg16.HL).get();
            this[0x56] = ldadr(Reg8.D, Reg16.HL).get();
            this[0x66] = ldadr(Reg8.H, Reg16.HL).get();
            //this[0x76] = halt.get();

            // LD [B D H], A
            this[0x47] = ldreg(Reg8.B, Reg8.A).get();
            this[0x57] = ldreg(Reg8.D, Reg8.A).get();
            this[0x67] = ldreg(Reg8.H, Reg8.A).get();
            this[0x77] = ldadr(Reg16.HL, Reg8.A).get();

            // LD [C E L], [B C D E H L]
            Fill(offsetX: 0x48, stepY: 0x10,
                (Reg8 dst, Reg8 src) => ldreg(dst, src).get(),
                ys: new[] { Reg8.C, Reg8.E, Reg8.L },
                xs: bcdehl);

            // LD [C E L A], (HL)
            this[0x4E] = ldadr(Reg8.C, Reg16.HL).get();
            this[0x5E] = ldadr(Reg8.E, Reg16.HL).get();
            this[0x6E] = ldadr(Reg8.L, Reg16.HL).get();
            this[0x7E] = ldadr(Reg8.A, Reg16.HL).get();

            // LD [C E L A], A
            this[0x4F] = ldreg(Reg8.C, Reg8.A).get();
            this[0x5F] = ldreg(Reg8.E, Reg8.A).get();
            this[0x6F] = ldreg(Reg8.L, Reg8.A).get();
            this[0x7F] = ldreg(Reg8.A, Reg8.A).get();

            // LD (HL), [B C D E H L]
            Fill(offsetX: 0x70, stepY: 0,
                (Reg16 dst, Reg8 src) => ldadr(dst, src).get(),
                ys: new[] { Reg16.HL },
                xs: bcdehl);

            // LD [B D H], (HL)
            this[0x46] = ldadr(Reg8.B, Reg16.HL).get();
            this[0x56] = ldadr(Reg8.D, Reg16.HL).get();
            this[0x76] = ldadr(Reg8.H, Reg16.HL).get();

            // LD (HL), A ( the one right after HALT)
            this[0x77] = ldadr(Reg16.HL, Reg8.A).get();

            // LD A, [B C D E H L]
            Fill(offsetX: 0x78,
                (Reg8 dst, Reg8 src) => ldreg(dst, src).get(),
                y: Reg8.A,
                xs: bcdehl);
        }
    }

    static class LoopExtensions
    {
        public static IEnumerable<(T item, int index)> Indexed<T>(this IEnumerable<T> self) =>  self.Select((item, index) => (item, index));
    }
}

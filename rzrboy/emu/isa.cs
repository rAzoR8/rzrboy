using System.Diagnostics;

namespace emu
{
    public partial class Isa
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

            this[0x00] = nop;

            // 16bit loads
            // LD (BC), .DB16
            this[0x01] = ldimm(Reg16.BC);
            this[0x11] = ldimm(Reg16.DE);
            this[0x21] = ldimm(Reg16.HL);
            this[0x31] = ldimm(Reg16.SP);

            // 8bit loads
            // LD (BC DE), A
            this[0x02] = ldadr(Reg16.BC, Reg8.A);
            this[0x12] = ldadr(Reg16.DE, Reg8.A);
            this[0x22] = Ops.ldhlplus(Reg8.A).Get("LD") + $"(HL+)" + "A";
            this[0x32] = Ops.ldhlminus(Reg8.A).Get("LD") + $"(HL-)" + "A";

            // LD [B D H], .DB8
            this[0x06] = ldimm(Reg8.B);
            this[0x16] = ldimm(Reg8.D);
            this[0x26] = ldimm(Reg8.H);
            // LD HL, .DB16
            this[0x36] = ldimm(Reg16.HL);

            // LD (.DB16), SP
            this[0x8] = ldimm16_sp();

            // single byte reg moves
            // LD B, B | LD B, C ...
            // LD [B D H], [B C D E H L]
            Fill(offsetX: 0x40, stepY: 0x10,
                (Reg8 dst, Reg8 src) => ldreg(dst, src),
                ys: new[] { Reg8.B, Reg8.D, Reg8.H },
                xs: bcdehl);

            // LD [B D H], (HL)
            this[0x46] = ldadr(Reg8.B, Reg16.HL);
            this[0x56] = ldadr(Reg8.D, Reg16.HL);
            this[0x66] = ldadr(Reg8.H, Reg16.HL);
            //this[0x76] = halt;

            // LD [B D H], A
            this[0x47] = ldreg(Reg8.B, Reg8.A);
            this[0x57] = ldreg(Reg8.D, Reg8.A);
            this[0x67] = ldreg(Reg8.H, Reg8.A);
            this[0x77] = ldadr(Reg16.HL, Reg8.A);

            // LD [C E L], [B C D E H L]
            Fill(offsetX: 0x48, stepY: 0x10,
                (Reg8 dst, Reg8 src) => ldreg(dst, src),
                ys: new[] { Reg8.C, Reg8.E, Reg8.L },
                xs: bcdehl);

            // LD [C E L A], (HL)
            this[0x4E] = ldadr(Reg8.C, Reg16.HL);
            this[0x5E] = ldadr(Reg8.E, Reg16.HL);
            this[0x6E] = ldadr(Reg8.L, Reg16.HL);
            this[0x7E] = ldadr(Reg8.A, Reg16.HL);

            // LD [C E L A], A
            this[0x4F] = ldreg(Reg8.C, Reg8.A);
            this[0x5F] = ldreg(Reg8.E, Reg8.A);
            this[0x6F] = ldreg(Reg8.L, Reg8.A);
            this[0x7F] = ldreg(Reg8.A, Reg8.A);

            // LD (HL), [B C D E H L]
            Fill(offsetX: 0x70, stepY: 0,
                (Reg16 dst, Reg8 src) => ldadr(dst, src),
                ys: new[] { Reg16.HL },
                xs: bcdehl);

            // LD [B D H], (HL)
            this[0x46] = ldadr(Reg8.B, Reg16.HL);
            this[0x56] = ldadr(Reg8.D, Reg16.HL);
            this[0x76] = ldadr(Reg8.H, Reg16.HL);

            // LD (HL), A ( the one right after HALT)
            this[0x77] = ldadr(Reg16.HL, Reg8.A);

            // LD A, [B C D E H L]
            Fill(offsetX: 0x78,
                (Reg8 dst, Reg8 src) => ldreg(dst, src),
                y: Reg8.A,
                xs: bcdehl);

            DebugReport();
        }

        private void DebugReport() 
        {
            Mem mem = new();
            Reg reg = new();

            int count = 0;

            void Print(byte i, byte ext)
            {
                IBuilder builder = this[i];
                Debug.Write($"OP 0x{i:X2}:0x{ext:X2} ");
                if (builder != null) 
                {
                    Debug.WriteLine(builder.Build().ToString(reg, mem));
                    count++;
                }
                else
                {
                    Debug.WriteLine($"not implemented");
                }
            }

            for (ushort i = 0; i <= 255; i++)
            {
                mem[reg.PC] = (byte)i;
                if(i != 0xCB)
                {
                    Print((byte)i, 0);
                }
            }

            //mem[reg.PC] = 0xCB;
            //for (ushort j = 0; j <= 255; j++)
            //{
            //    mem[(ushort)(reg.PC + 1)] = (byte)j;
            //    Print(0xCB, (byte)j);
            //}

            Debug.WriteLine($"{count} out of 511 Instructions implemented: {100.0f*count/511.0f}%");
        }
    }

    static class LoopExtensions
    {
        public static IEnumerable<(T item, int index)> Indexed<T>(this IEnumerable<T> self) =>  self.Select((item, index) => (item, index));
    }
}

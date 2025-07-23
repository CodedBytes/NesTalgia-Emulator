using System.Linq;
using NesTalgia_EMU.Core.Memory;
using static NesTalgia_EMU.Core.CPU.Cpu;

namespace NesTalgia_EMU.Core.CPU
{
    /// <summary>
    /// Classe Reponsável pela emulação da CPU 6502. 
    /// <para>Esta classe realiza a emulação de forma quase completa do processador do Nintendo Entertainment System (NES).</para>
    /// <para>Foi realizada uma implementação de mais instruções, as chamadas não oficiais / não documentadas, para ter maior
    /// compatibilidade possível com os jogos comerciais.</para>
    /// </summary>
    public class Cpu
    {
        public byte A = 0x00, X = 0x00, Y = 0x00, SP = 0x00, Status = 0x00;
        public ushort PC = 0x0000;
        private MemoryMap? bus;
        public Instruction[] Lookup = new Instruction[256];
        private int clock_count = 0;
        public ushort LastExecutedPC, temp = 0x0000, addr_abs = 0x0000, addr_rel = 0x0000;
        private byte fetched = 0x00, cpuCycle = 0, opcode = 0x00;

        // Delegate para o método de instrução
        public delegate byte Inst();

        // Delegate para o método do modo de endereçamento
        public delegate byte AddrMode();

        /// <summary>
        /// Struct para organizar o lookup de instruções.
        /// </summary>
        public struct Instruction
        {
            public string Name;
            public Inst InstructionMethod;
            public AddrMode AddressModeMethod;
            public byte Cycles;

            /// <summary>
            /// Construtor para a struct de instrução.
            /// </summary>
            /// <param name="name">Nome da instrução</param>
            /// <param name="inst">Função da instrução.</param>
            /// <param name="addrMode">Função da instrução de endereçamento.</param>
            /// <param name="cycles">Ciclos de execução.</param>
            public Instruction(string name, Inst inst, AddrMode addrMode, byte cycles)
            {
                Name = name;
                InstructionMethod = inst;
                AddressModeMethod = addrMode;
                Cycles = cycles;
            }
        }

        /// <summary>
        /// Flags de status da CPU.
        /// </summary>
        [Flags]
        public enum StatusFlags : byte
        {
            C = 1 << 0,
            Z = 1 << 1,
            I = 1 << 2,
            D = 1 << 3,
            B = 1 << 4,
            U = 1 << 5,
            V = 1 << 6,
            N = 1 << 7
        }

        /// <summary>
        /// Constructor da CPU 6502. Ela realiza o inicio da tabela de instruções em uma grade de 16x16 instruções. dando 256 instruções possiveis entre nativas e não oficiais.
        /// </summary>
        public Cpu() { BuildLookupTable(); }
        public void ConnectBus(MemoryMap b) { bus = b; }

        /// <summary>
        /// Tabela de instruções com 16x16 (256 instruções).
        /// </summary>
        private void BuildLookupTable()
        {
            // Tabela 16x16
            Lookup[0x00] = new Instruction("BRK", BRK, IMM, 7); Lookup[0x01] = new Instruction("ORA", ORA, IZX, 6); Lookup[0x02] = new Instruction("???", XXX, IMP, 2); Lookup[0x03] = new Instruction("???", XXX, IMP, 8); Lookup[0x04] = new Instruction("???", NOP, IMP, 3); Lookup[0x05] = new Instruction("ORA", ORA, ZP0, 3); Lookup[0x06] = new Instruction("ASL", ASL, ZP0, 5); Lookup[0x07] = new Instruction("???", XXX, IMP, 5); Lookup[0x08] = new Instruction("PHP", PHP, IMP, 3); Lookup[0x09] = new Instruction("ORA", ORA, IMM, 2); Lookup[0x0A] = new Instruction("ASL", ASL, IMP, 2); Lookup[0x0B] = new Instruction("???", XXX, IMP, 2); Lookup[0x0C] = new Instruction("NOP", NOP, IMP, 4); Lookup[0x0D] = new Instruction("ORA", ORA, ABS, 4); Lookup[0x0E] = new Instruction("ASL", ASL, ABS, 6); Lookup[0x0F] = new Instruction("???", XXX, IMP, 6);
            Lookup[0x10] = new Instruction("BPL", BPL, REL, 2); Lookup[0x11] = new Instruction("ORA", ORA, IZY, 5); Lookup[0x12] = new Instruction("???", XXX, IMP, 2); Lookup[0x13] = new Instruction("???", XXX, IMP, 8); Lookup[0x14] = new Instruction("???", NOP, IMP, 4); Lookup[0x15] = new Instruction("ORA", ORA, ZPX, 4); Lookup[0x16] = new Instruction("ORA", ASL, ZPX, 6); Lookup[0x17] = new Instruction("???", XXX, IMP, 6); Lookup[0x18] = new Instruction("CLC", CLC, IMP, 2); Lookup[0x19] = new Instruction("ORA", ORA, ABY, 4); Lookup[0x1A] = new Instruction("???", NOP, IMP, 2); Lookup[0x1B] = new Instruction("???", XXX, IMP, 7); Lookup[0x1C] = new Instruction("NOP", NOP, IMP, 4); Lookup[0x1D] = new Instruction("ORA", ORA, ABX, 4); Lookup[0x1E] = new Instruction("ASL", ASL, ABX, 7); Lookup[0x1F] = new Instruction("???", XXX, IMP, 7);
            Lookup[0x20] = new Instruction("JSR", JSR, ABS, 6); Lookup[0x21] = new Instruction("AND", AND, IZX, 6); Lookup[0x22] = new Instruction("???", XXX, IMP, 2); Lookup[0x23] = new Instruction("???", XXX, IMP, 8); Lookup[0x24] = new Instruction("BIT", BIT, ZP0, 3); Lookup[0x25] = new Instruction("AND", AND, ZP0, 3); Lookup[0x26] = new Instruction("ORA", ROL, ZP0, 5); Lookup[0x27] = new Instruction("???", XXX, IMP, 5); Lookup[0x28] = new Instruction("PLP", PLP, IMP, 4); Lookup[0x29] = new Instruction("AND", AND, IMM, 2); Lookup[0x2A] = new Instruction("ROL", ROL, IMP, 2); Lookup[0x2B] = new Instruction("???", XXX, IMP, 2); Lookup[0x2C] = new Instruction("BIT", BIT, ABS, 4); Lookup[0x2D] = new Instruction("AND", AND, ABS, 4); Lookup[0x2E] = new Instruction("ROL", ROL, ABS, 6); Lookup[0x2F] = new Instruction("???", XXX, IMP, 6);
            Lookup[0x30] = new Instruction("BMI", BMI, REL, 2); Lookup[0x31] = new Instruction("AND", AND, IZY, 5); Lookup[0x32] = new Instruction("???", XXX, IMP, 2); Lookup[0x33] = new Instruction("???", XXX, IMP, 8); Lookup[0x34] = new Instruction("???", NOP, IMP, 4); Lookup[0x35] = new Instruction("AND", AND, ZPX, 4); Lookup[0x36] = new Instruction("ORA", ROL, ZPX, 6); Lookup[0x37] = new Instruction("???", XXX, IMP, 6); Lookup[0x38] = new Instruction("SEC", SEC, IMP, 2); Lookup[0x39] = new Instruction("AND", AND, ABY, 4); Lookup[0x3A] = new Instruction("???", NOP, IMP, 2); Lookup[0x3B] = new Instruction("???", XXX, IMP, 7); Lookup[0x3C] = new Instruction("NOP", NOP, IMP, 4); Lookup[0x3D] = new Instruction("AND", AND, ABX, 4); Lookup[0x3E] = new Instruction("ROL", ROL, ABX, 7); Lookup[0x3F] = new Instruction("???", XXX, IMP, 7);
            Lookup[0x40] = new Instruction("RTI", RTI, IMP, 6); Lookup[0x41] = new Instruction("EOR", EOR, IZX, 6); Lookup[0x42] = new Instruction("???", XXX, IMP, 2); Lookup[0x43] = new Instruction("???", XXX, IMP, 8); Lookup[0x44] = new Instruction("???", NOP, IMP, 3); Lookup[0x45] = new Instruction("EOR", EOR, ZP0, 3); Lookup[0x46] = new Instruction("ORA", LSR, ZP0, 5); Lookup[0x47] = new Instruction("???", XXX, IMP, 5); Lookup[0x48] = new Instruction("PHA", PHA, IMP, 3); Lookup[0x49] = new Instruction("EOR", EOR, IMM, 2); Lookup[0x4A] = new Instruction("LSR", LSR, IMP, 2); Lookup[0x4B] = new Instruction("???", XXX, IMP, 2); Lookup[0x4C] = new Instruction("JMP", JMP, ABS, 3); Lookup[0x4D] = new Instruction("EOR", EOR, ABS, 4); Lookup[0x4E] = new Instruction("LSR", LSR, ABS, 6); Lookup[0x4F] = new Instruction("???", XXX, IMP, 6);
            Lookup[0x50] = new Instruction("BVC", BVC, REL, 2); Lookup[0x51] = new Instruction("EOR", EOR, IZY, 5); Lookup[0x52] = new Instruction("???", XXX, IMP, 2); Lookup[0x53] = new Instruction("???", XXX, IMP, 8); Lookup[0x54] = new Instruction("???", NOP, IMP, 4); Lookup[0x55] = new Instruction("EOR", EOR, ZPX, 4); Lookup[0x56] = new Instruction("ORA", LSR, ZPX, 6); Lookup[0x57] = new Instruction("???", XXX, IMP, 6); Lookup[0x58] = new Instruction("CLI", CLI, IMP, 2); Lookup[0x59] = new Instruction("EOR", EOR, ABY, 4); Lookup[0x5A] = new Instruction("???", NOP, IMP, 2); Lookup[0x5B] = new Instruction("???", XXX, IMP, 7); Lookup[0x5C] = new Instruction("NOP", NOP, IMP, 4); Lookup[0x5D] = new Instruction("EOR", EOR, ABX, 4); Lookup[0x5E] = new Instruction("LSR", LSR, ABX, 7); Lookup[0x5F] = new Instruction("???", XXX, IMP, 7);
            Lookup[0x60] = new Instruction("RTS", RTS, IMP, 6); Lookup[0x61] = new Instruction("ADC", ADC, IZX, 6); Lookup[0x62] = new Instruction("???", XXX, IMP, 2); Lookup[0x63] = new Instruction("???", XXX, IMP, 8); Lookup[0x64] = new Instruction("???", NOP, IMP, 3); Lookup[0x65] = new Instruction("ADC", ADC, ZP0, 3); Lookup[0x66] = new Instruction("ORA", ROR, ZP0, 5); Lookup[0x67] = new Instruction("???", XXX, IMP, 5); Lookup[0x68] = new Instruction("PLA", PLA, IMP, 4); Lookup[0x69] = new Instruction("ADC", ADC, IMM, 2); Lookup[0x6A] = new Instruction("ROR", ROR, IMP, 2); Lookup[0x6B] = new Instruction("???", XXX, IMP, 2); Lookup[0x6C] = new Instruction("JMP", JMP, IND, 5); Lookup[0x6D] = new Instruction("ADC", ADC, ABS, 4); Lookup[0x6E] = new Instruction("ROR", ROR, ABS, 6); Lookup[0x6F] = new Instruction("???", XXX, IMP, 6);
            Lookup[0x70] = new Instruction("BVS", BVS, REL, 2); Lookup[0x71] = new Instruction("ADC", ADC, IZY, 5); Lookup[0x72] = new Instruction("???", XXX, IMP, 2); Lookup[0x73] = new Instruction("???", XXX, IMP, 8); Lookup[0x74] = new Instruction("???", NOP, IMP, 4); Lookup[0x75] = new Instruction("ADC", ADC, ZPX, 4); Lookup[0x76] = new Instruction("ORA", ROR, ZPX, 6); Lookup[0x77] = new Instruction("???", XXX, IMP, 6); Lookup[0x78] = new Instruction("SEI", SEI, IMP, 2); Lookup[0x79] = new Instruction("ADC", ADC, ABY, 4); Lookup[0x7A] = new Instruction("???", NOP, IMP, 2); Lookup[0x7B] = new Instruction("???", XXX, IMP, 7); Lookup[0x7C] = new Instruction("NOP", NOP, IMP, 4); Lookup[0x7D] = new Instruction("ADC", ADC, ABX, 4); Lookup[0x7E] = new Instruction("ROR", ROR, ABX, 7); Lookup[0x7F] = new Instruction("???", XXX, IMP, 7);
            Lookup[0x80] = new Instruction("???", NOP, IMP, 2); Lookup[0x81] = new Instruction("STA", STA, IZX, 6); Lookup[0x82] = new Instruction("???", NOP, IMP, 2); Lookup[0x83] = new Instruction("???", XXX, IMP, 6); Lookup[0x84] = new Instruction("STY", STY, ZP0, 3); Lookup[0x85] = new Instruction("STA", STA, ZP0, 3); Lookup[0x86] = new Instruction("ORA", STX, ZP0, 3); Lookup[0x87] = new Instruction("???", XXX, IMP, 3); Lookup[0x88] = new Instruction("DEY", DEY, IMP, 2); Lookup[0x89] = new Instruction("???", NOP, IMM, 2); Lookup[0x8A] = new Instruction("TXA", TXA, IMP, 2); Lookup[0x8B] = new Instruction("???", XXX, IMP, 2); Lookup[0x8C] = new Instruction("STY", STY, ABS, 4); Lookup[0x8D] = new Instruction("STA", STA, ABS, 4); Lookup[0x8E] = new Instruction("STX", STX, ABS, 4); Lookup[0x8F] = new Instruction("???", XXX, IMP, 4);
            Lookup[0x90] = new Instruction("BCC", BCC, REL, 2); Lookup[0x91] = new Instruction("STA", STA, IZY, 6); Lookup[0x92] = new Instruction("???", XXX, IMP, 2); Lookup[0x93] = new Instruction("???", XXX, IMP, 6); Lookup[0x94] = new Instruction("STY", STY, ZPX, 4); Lookup[0x95] = new Instruction("STA", STA, ZPX, 4); Lookup[0x96] = new Instruction("ORA", STX, ZPY, 4); Lookup[0x97] = new Instruction("???", XXX, IMP, 4); Lookup[0x98] = new Instruction("TYA", TYA, IMP, 2); Lookup[0x99] = new Instruction("STA", STA, ABY, 5); Lookup[0x9A] = new Instruction("TXS", TXS, IMP, 2); Lookup[0x9B] = new Instruction("???", XXX, IMP, 5); Lookup[0x9C] = new Instruction("NOP", NOP, IMP, 5); Lookup[0x9D] = new Instruction("STA", STA, ABX, 5); Lookup[0x9E] = new Instruction("???", XXX, IMP, 5); Lookup[0x9F] = new Instruction("???", XXX, IMP, 5);
            Lookup[0xA0] = new Instruction("LDY", LDY, IMM, 2); Lookup[0xA1] = new Instruction("LDA", LDA, IZX, 6); Lookup[0xA2] = new Instruction("LDX", LDX, IMM, 2); Lookup[0xA3] = new Instruction("???", XXX, IMP, 6); Lookup[0xA4] = new Instruction("LDY", LDY, ZP0, 3); Lookup[0xA5] = new Instruction("LDA", LDA, ZP0, 3); Lookup[0xA6] = new Instruction("ORA", LDX, ZP0, 3); Lookup[0xA7] = new Instruction("???", XXX, IMP, 3); Lookup[0xA8] = new Instruction("TAY", TAY, IMP, 2); Lookup[0xA9] = new Instruction("LDA", LDA, IMM, 2); Lookup[0xAA] = new Instruction("TAX", TAX, IMP, 2); Lookup[0xAB] = new Instruction("???", XXX, IMP, 2); Lookup[0xAC] = new Instruction("LDY", LDY, ABS, 4); Lookup[0xAD] = new Instruction("LDA", LDA, ABS, 4); Lookup[0xAE] = new Instruction("LDX", LDX, ABS, 4); Lookup[0xAF] = new Instruction("???", XXX, IMP, 4);
            Lookup[0xB0] = new Instruction("BCS", BCS, REL, 2); Lookup[0xB1] = new Instruction("LDA", LDA, IZY, 5); Lookup[0xB2] = new Instruction("???", XXX, IMP, 2); Lookup[0xB3] = new Instruction("???", XXX, IMP, 5); Lookup[0xB4] = new Instruction("LDY", LDY, ZPX, 4); Lookup[0xB5] = new Instruction("LDA", LDA, ZPX, 4); Lookup[0xB6] = new Instruction("ORA", LDX, ZPY, 4); Lookup[0xB7] = new Instruction("???", XXX, IMP, 4); Lookup[0xB8] = new Instruction("CLV", CLV, IMP, 2); Lookup[0xB9] = new Instruction("LDA", LDA, ABY, 4); Lookup[0xBA] = new Instruction("TSX", TSX, IMP, 2); Lookup[0xBB] = new Instruction("???", XXX, IMP, 4); Lookup[0xBC] = new Instruction("LDY", LDY, ABX, 4); Lookup[0xBD] = new Instruction("LDA", LDA, ABX, 4); Lookup[0xBE] = new Instruction("LDX", LDX, ABY, 4); Lookup[0xBF] = new Instruction("???", XXX, IMP, 4);
            Lookup[0xC0] = new Instruction("CPY", CPY, IMM, 2); Lookup[0xC1] = new Instruction("CMP", CMP, IZX, 6); Lookup[0xC2] = new Instruction("???", NOP, IMP, 2); Lookup[0xC3] = new Instruction("???", XXX, IMP, 8); Lookup[0xC4] = new Instruction("CPY", CPY, ZP0, 3); Lookup[0xC5] = new Instruction("CMP", CMP, ZP0, 3); Lookup[0xC6] = new Instruction("ORA", DEC, ZP0, 5); Lookup[0xC7] = new Instruction("???", XXX, IMP, 5); Lookup[0xC8] = new Instruction("INY", INY, IMP, 2); Lookup[0xC9] = new Instruction("CMP", CMP, IMM, 2); Lookup[0xCA] = new Instruction("DEX", DEX, IMP, 2); Lookup[0xCB] = new Instruction("???", XXX, IMP, 2); Lookup[0xCC] = new Instruction("CPY", CPY, ABS, 4); Lookup[0xCD] = new Instruction("CMP", CMP, ABS, 4); Lookup[0xCE] = new Instruction("DEC", DEC, ABS, 6); Lookup[0xCF] = new Instruction("???", XXX, IMP, 6);
            Lookup[0xD0] = new Instruction("BNE", BNE, REL, 2); Lookup[0xD1] = new Instruction("CMP", CMP, IZY, 5); Lookup[0xD2] = new Instruction("???", XXX, IMP, 2); Lookup[0xD3] = new Instruction("???", XXX, IMP, 8); Lookup[0xD4] = new Instruction("???", NOP, IMP, 4); Lookup[0xD5] = new Instruction("CMP", CMP, ZPX, 4); Lookup[0xD6] = new Instruction("ORA", DEC, ZPX, 6); Lookup[0xD7] = new Instruction("???", XXX, IMP, 6); Lookup[0xD8] = new Instruction("CLD", CLD, IMP, 2); Lookup[0xD9] = new Instruction("CMP", CMP, ABY, 4); Lookup[0xDA] = new Instruction("NOP", NOP, IMP, 2); Lookup[0xDB] = new Instruction("???", XXX, IMP, 7); Lookup[0xDC] = new Instruction("NOP", NOP, IMP, 4); Lookup[0xDD] = new Instruction("CMP", CMP, ABX, 4); Lookup[0xDE] = new Instruction("DEC", DEC, ABX, 7); Lookup[0xDF] = new Instruction("???", XXX, IMP, 7);
            Lookup[0xE0] = new Instruction("CPX", CPX, IMM, 2); Lookup[0xE1] = new Instruction("SBC", SBC, IZX, 6); Lookup[0xE2] = new Instruction("???", NOP, IMP, 2); Lookup[0xE3] = new Instruction("???", XXX, IMP, 8); Lookup[0xE4] = new Instruction("CPX", CPX, ZP0, 3); Lookup[0xE5] = new Instruction("SBC", SBC, ZP0, 3); Lookup[0xE6] = new Instruction("ORA", INC, ZP0, 5); Lookup[0xE7] = new Instruction("???", XXX, IMP, 5); Lookup[0xE8] = new Instruction("INX", INX, IMP, 2); Lookup[0xE9] = new Instruction("SBC", SBC, IMM, 2); Lookup[0xEA] = new Instruction("NOP", NOP, IMP, 2); Lookup[0xEB] = new Instruction("SBC", SBC, IMP, 2); Lookup[0xEC] = new Instruction("CPX", CPX, ABS, 4); Lookup[0xED] = new Instruction("SBC", SBC, ABS, 4); Lookup[0xEE] = new Instruction("INC", INC, ABS, 6); Lookup[0xEF] = new Instruction("???", XXX, IMP, 6);
            Lookup[0xF0] = new Instruction("BEQ", BEQ, REL, 2); Lookup[0xF1] = new Instruction("SBC", SBC, IZY, 5); Lookup[0xF2] = new Instruction("???", XXX, IMP, 2); Lookup[0xF3] = new Instruction("???", XXX, IMP, 8); Lookup[0xF4] = new Instruction("???", NOP, IMP, 4); Lookup[0xF5] = new Instruction("SBC", SBC, ZPX, 4); Lookup[0xF6] = new Instruction("ORA", INC, ZPX, 6); Lookup[0xF7] = new Instruction("???", XXX, IMP, 6); Lookup[0xF8] = new Instruction("SED", SED, IMP, 2); Lookup[0xF9] = new Instruction("SBC", SBC, ABY, 4); Lookup[0xFA] = new Instruction("NOP", NOP, IMP, 2); Lookup[0xFB] = new Instruction("???", XXX, IMP, 7); Lookup[0xFC] = new Instruction("NOP", NOP, IMP, 4); Lookup[0xFD] = new Instruction("SBC", SBC, ABX, 4); Lookup[0xFE] = new Instruction("INC", INC, ABX, 7); Lookup[0xFF] = new Instruction("???", XXX, IMP, 7);
        }

        /// <summary>
        /// Responsável por realizar a leitura do BUS da CPU.
        /// </summary>
        /// <param name="addr">Endereço a ser passado para o BUS</param>
        /// <returns>Retorna os dados processados pela BUS</returns>
        private byte Read(ushort addr) => bus.cpuRead(addr, false);

        /// <summary>
        /// Responsável por realizar a escrita no BUS da CPU.
        /// </summary>
        /// <param name="addr">Endereço a ser passado para o BUS.</param>
        private void Write(ushort addr, byte data) => bus.cpuWrite(addr, data);

        /// <summary>
        /// Pega o valor de alguma flag passada para a função
        /// </summary>
        /// <param name="f">Flag da CPU.</param>
        /// <returns>Retorna os bits da flag, seja ela 0 ou 1.</returns>
        private byte GetFlag(StatusFlags f)
        {
            byte flag = (byte)f;
            return ((Status & flag) > 0) ? (byte)1 : (byte)0;
        }

        /// <summary>
        /// Responsavel apenas por completar o ciclo da CPU.
        /// </summary>
        /// <returns>Retorna verdadeiro se o ciclo foi completado ou falso se não.</returns>
        public bool complete() { return cpuCycle == 0; }

        /// <summary>
        /// Força o 6502 para um estado conhecido pelos programadores.
        /// <para>Isso é feito de forma bruta na CPU resetando os registros em 0x00 e todos os estados são
        /// definidos para 0x00.</para><para>Exceto para bits sem uso, que no caso sempre ficam em 1.</para>
        /// </summary>
        public void Reset()
        {
            // Pegando o endereço do counter
            addr_abs = 0xFFFC;
            ushort lo = Read((ushort)(addr_abs + 0));
            ushort hi = Read((ushort)(addr_abs + 1));

            // Setando
            PC = (ushort)((hi << 8) | lo);

            // Resetando os registros internos
            A = 0;
            X = 0;
            Y = 0;
            SP = 0xFD;
            Status = 0x00 | (byte)StatusFlags.U;

            // Limpa Helpers internos para debug
            addr_rel = 0x0000;
            addr_abs = 0x0000;
            fetched = 0x00;

            // Reset consome 8 ciclos
            cpuCycle = 8;
        }

        /// <summary>
        /// A requisição de interrompimento (IRQ) é uma complexa operação feita apenas se a flag 'Disable Interrupt' estiver em 1.
        /// <para>Essa requisição pode acontecer a qualquer momento, más não queremos que ela seja destrutiva para o programa.</para>
        /// <para>Então a instrução é liberada para finalização quando o ciclo for 0, fazendo com q o couter do programa em questão fica hospedado no stack.</para>
        /// <para>Isso é implementado pela isntrução RTI e assim que essa requisição acontecer, similar a um reset, o endereço do programa sendo rodado vai para 0xFFFE, sendo setado depois para o counter do programa. </para>
        /// </summary>
        public void interruptorRequest()
        {
            if (GetFlag(StatusFlags.I) == 0)
            {
                // Puxa o contador do programa para o stack.
                // Como são 16-bits, ele puxa duas vezes.
                Write((ushort)(0x0100 + SP), (byte)((PC >> 8) & 0x00FF));
                SP--;
                Write((ushort)(0x0100 + SP), (byte)(PC & 0x00FF));
                SP--;

                // Então puxa o registro do status para o stack
                SetFlag(StatusFlags.B, false);
                SetFlag(StatusFlags.U, true);
                SetFlag(StatusFlags.I, true);
                Write((ushort)(0x0100 + SP), Status);
                SP--;

                // Começa a ler a localização do novo contador do programa com endereços fixos
                addr_abs = 0xFFFE;
                ushort lo = Read((ushort)(addr_abs + 0));
                ushort hi = Read((ushort)(addr_abs + 1));
                PC = (ushort)((hi << 8) | lo);

                // A requisição demora 7 ciclos;
                cpuCycle = 7;
            }
        }

        /// <summary>
        /// O NonMaskableInterrupt (Famoso NMI) não pode ser ignorado pela CPU.
        /// <para>Ele itnerage da mesma forma que um IRQ regular, mas ele lê o novo endereço do contador do programa da localização 0xFFFA</para>
        /// </summary>
        public void NonMaskableInterrupt()
        {
            // empilha PC atual (baixo primeiro)
            Write((ushort)(0x0100 + SP), (byte)((PC >> 8) & 0x00FF));
            SP--;
            Write((ushort)(0x0100 + SP), (byte)(PC & 0x00FF));
            SP--;

            // empilha status com B=0, U=1
            SetFlag(StatusFlags.B, false);
            SetFlag(StatusFlags.U, true);
            SetFlag(StatusFlags.I, true);
            Write((ushort)(0x0100 + SP), Status);
            SP--;

            ushort lo = Read(0xFFFA);
            ushort hi = Read(0xFFFB);
            PC = (ushort)((hi << 8) | lo);

            cpuCycle = 8;
        }



        /// <summary>
        /// Responsável pelo clock da CPU.
        /// <para>A cada ciclo da CPU, é executado este código.</para>
        /// </summary>
        /// <exception cref="Exception">Necessaária para parar a emulação e gerar log.</exception>
        public void Clock()
        {
            LastExecutedPC = PC;
            if (cpuCycle == 0)
            {
                opcode = Read(PC);

                // É necessario sempre setar a flag U para 1
                SetFlag(StatusFlags.U, true);

                // Aumenta o contador do programa
                PC++;

                // Faz a Verificação dos opcodes, caso tenha algum não implementado, o emulador dispara um erro e descreve o erro no log.
                // Caso passe pela verificação, ele adiciona o ciclo a CPU.
                if (opcode < 0 || opcode >= Lookup.Length)
                {
                    //logger.Log("CPU::CLOCK", $"Opcode inválido: {opcode:X2}");
                    Console.WriteLine($"Opcode inválido: {opcode:X2}");
                    throw new Exception($"Opcode desconhecido - Checar o log para melhores instruções.");
                }
                cpuCycle = Lookup[opcode].Cycles;

                // busca dados intermediarios usando um modo de endereçamento necessario para a ação
                byte additional_cycle1 = Lookup[opcode].AddressModeMethod();
                byte additional_cycle2 = Lookup[opcode].InstructionMethod();

                // Adicionando ciclos
                cpuCycle += (byte)(additional_cycle1 & additional_cycle2);

                // Setando flag U sempre como true para 1 bit
                SetFlag(StatusFlags.U, true);
            }
            clock_count++;

            // Tira os ciclos restantes para essa operação
            cpuCycle--;
        }

        // -- nop temporario
        public byte XXX() { return 0; }
        private byte IMM() { addr_abs = PC++; return 0; }
        private byte ZP0()
        {
            addr_abs = Read(PC);
            PC++;
            addr_abs &= 0x00FF;
            return 0;
        }
        private byte IMP() { fetched = A; return 0; }
        private byte ZPX()
        { 
            addr_abs = (ushort)(Read(PC) + X);
            PC++;
            addr_abs &= 0x00FF;
            return 0;
        }

        private byte ZPY()
        {
            addr_abs = (ushort)(Read(PC) + Y);
            PC++;
            addr_abs &= 0x00FF;
            return 0;
        }

        private byte REL()
        {
            addr_rel = Read(PC);
            PC++;

            if ((addr_rel & 0x80) != 0) addr_rel |= 0xFF00;
            return 0;
        }

        private byte ABS()
        {
            ushort lo = Read(PC);
            PC++;
            ushort hi = Read(PC);
            PC++;

            addr_abs = (ushort)((hi << 8) | lo);
            return 0;
        }
        private byte ABX()
        {
            ushort lo = Read(PC);
            PC++;
            ushort hi = Read(PC);
            PC++;

            addr_abs = (ushort)((hi << 8) | lo);
            addr_abs += X;

            if ((addr_abs & 0xFF00) != (hi << 8)) return 1;
            return 0;
        }
        private byte ABY()
        {
            ushort lo = Read(PC);
            PC++;
            ushort hi = Read(PC);
            PC++;

            addr_abs = (ushort)((hi << 8) | lo);
            addr_abs += Y;

            if ((addr_abs & 0xFF00) != (hi << 8)) return 1;
            return 0;
        }
        private byte IND()
        {
            ushort ptr_lo = Read(PC);
            PC++;
            ushort ptr_hi = Read(PC);
            PC++;

            ushort ptr = (ushort)((ptr_hi << 8) | ptr_lo);

            if (ptr_lo == 0x00FF) 
                addr_abs = (ushort)((Read((ushort)(ptr & 0xFF00)) << 8) | Read((ushort)(ptr + 0)));
            else 
                addr_abs = (ushort)((Read((ushort)(ptr + 1)) << 8) | Read((ushort)(ptr + 0)));

            return 0;
        }

        private byte IZX()
        {
            ushort t = Read(PC);
            PC++;

            ushort lo = Read((ushort)((t + (ushort)X) & 0x00FF));
            ushort hi = Read((ushort)((t + (ushort)X + 1) & 0x00FF));

            addr_abs = (ushort)((hi << 8) | lo);
            return 0;
        }

        private byte IZY()
        {
            ushort t = Read(PC);
            PC++;

            ushort lo = Read((ushort)(t & 0x00FF));
            ushort hi = Read((ushort)((t + 1) & 0x00FF));

            addr_abs = (ushort)((hi << 8) | lo);
            addr_abs += Y;

            if ((addr_abs & 0xFF00) != (hi << 8)) return 1;
            return 0;
        }

        /// <summary>
        /// Esta função obtém os dados usados ​​pela instrução em uma variável numérica conveniente.
        /// </summary>
        /// <returns></returns>
        public byte Fetch()
        {
            if (!(Lookup[opcode].AddressModeMethod == IMP)) fetched = Read(addr_abs);
            return fetched;
        }


        ///// ------------------------------- INSTRUÇÕES
        private byte ADC()
        {
            Fetch();
            temp = (ushort)(A + fetched + GetFlag(StatusFlags.C));
            SetFlag(StatusFlags.C, temp > 255);
            SetFlag(StatusFlags.Z, (temp & 0x00FF) == 0);
            SetFlag(StatusFlags.V, (~(A ^ fetched) & (A ^ temp) & 0x0080) != 0);
            SetFlag(StatusFlags.N, (temp & 0x80) != 0);
            A = (byte)(temp & 0x00FF);
            return 1;
        }

        private byte SBC()
        {
            Fetch();
            ushort value = (ushort)(fetched ^ 0x00FF);
            temp = (ushort)(A + value + GetFlag(StatusFlags.C));
            SetFlag(StatusFlags.C, (temp & 0xFF00) != 0);
            SetFlag(StatusFlags.Z, (temp & 0x00FF) == 0);
            SetFlag(StatusFlags.V, ((temp ^ A) & (temp ^ value) & 0x0080) != 0);
            SetFlag(StatusFlags.N, (temp & 0x0080) != 0);
            A = (byte)(temp & 0x00FF);
            return 1;
        }

        private byte AND()
        {
            Fetch();
            A = (byte)(A & fetched);
            SetFlag(StatusFlags.Z, A == 0x00);
            SetFlag(StatusFlags.N, (A & 0x80) != 0);
            return 1;
        }

        private byte ASL()
        {
            Fetch();
            temp = (ushort)(fetched << 1);
            SetFlag(StatusFlags.C, (temp & 0xFF00) > 0);
            SetFlag(StatusFlags.Z, (temp & 0x00FF) == 0x00);
            SetFlag(StatusFlags.N, (temp & 0x80) != 0);
            if (Lookup[opcode].AddressModeMethod == IMP) A = (byte)(temp & 0x00FF);
            else Write(addr_abs, (byte)(temp & 0x00FF));
            return 0;
        }

        private byte BCC()
        {
            if (GetFlag(StatusFlags.C) == 0)
            {
                cpuCycle++;
                addr_abs = (ushort)(PC + addr_rel);

                if ((addr_abs & 0xFF00) != (PC & 0xFF00)) cpuCycle++;

                PC = addr_abs;
            }
            return 0;
        }

        private byte BCS()
        {
            if (GetFlag(StatusFlags.C) == 1)
            {
                cpuCycle++;
                addr_abs = (ushort)(PC + addr_rel);

                if ((addr_abs & 0xFF00) != (PC & 0xFF00)) cpuCycle++;

                PC = addr_abs;
            }
            return 0;
        }

        private byte BEQ()
        {
            if (GetFlag(StatusFlags.Z) == 1)
            {
                cpuCycle++;
                addr_abs = (ushort)(PC + addr_rel);

                if ((addr_abs & 0xFF00) != (PC & 0xFF00)) cpuCycle++;

                PC = addr_abs;
            }
            return 0;
        }

        private byte BIT()
        {
            Fetch();
            temp = (ushort)(A & fetched);
            SetFlag(StatusFlags.Z, (temp & 0x00FF) == 0x00);
            SetFlag(StatusFlags.N, (fetched & (1 << 7)) != 0);
            SetFlag(StatusFlags.V, (fetched & (1 << 6)) != 0);
            return 0;
        }

        private byte BMI()
        {
            if (GetFlag(StatusFlags.N) == 1)
            {
                cpuCycle++;
                addr_abs = (ushort)(PC + addr_rel);

                if ((addr_abs & 0xFF00) != (PC & 0xFF00))
                    cpuCycle++;

                PC = addr_abs;
            }
            return 0;
        }

        private byte BNE()
        {
            if (GetFlag(StatusFlags.Z) == 0)
            {
                cpuCycle++;
                addr_abs = (ushort)(PC + addr_rel);

                if ((addr_abs & 0xFF00) != (PC & 0xFF00))
                    cpuCycle++;

                PC = addr_abs;
            }
            return 0;
        }

        private byte BPL()
        {
            if (GetFlag(StatusFlags.N) == 0)
            {
                cpuCycle++;
                addr_abs = (ushort)(PC + addr_rel);

                if ((addr_abs & 0xFF00) != (PC & 0xFF00)) cpuCycle++;

                PC = addr_abs;
            }
            return 0;
        }

        private byte BRK()
        {
            PC++;
            SetFlag(StatusFlags.I, true);
            ushort returnAddress = (ushort)(PC + 1);
            Write((ushort)(0x0100 + SP), (byte)(returnAddress & 0x00FF));
            SP--;
            Write((ushort)(0x0100 + SP), (byte)((returnAddress >> 8) & 0x00FF));
            SP--;

            // empilha status com B setado
            byte flagsToPush = (byte)(Status | 0x10);
            Write((ushort)(0x0100 + SP), flagsToPush);
            SP--;

            SetFlag(StatusFlags.B, false);
            PC = (ushort)(Read(0xFFFE) | (Read(0xFFFF) << 8));

            return 0;
        }

        private byte BVC()
        {
            if (GetFlag(StatusFlags.V) == 0)
            {
                cpuCycle++;
                addr_abs = (ushort)(PC + addr_rel);

                if ((addr_abs & 0xFF00) != (PC & 0xFF00)) cpuCycle++;
                PC = addr_abs;
            }
            return 0;
        }

        private byte BVS()
        {
            if (GetFlag(StatusFlags.V) == 1)
            {
                cpuCycle++;
                addr_abs = (ushort)(PC + addr_rel);

                if ((addr_abs & 0xFF00) != (PC & 0xFF00)) cpuCycle++;
                PC = addr_abs;
            }
            return 0;
        }

        private byte CLC() { SetFlag(StatusFlags.C, false); return 0; }
        private byte CLD() { SetFlag(StatusFlags.D, false); return 0; }
        private byte CLI() { SetFlag(StatusFlags.I, false); return 0; }
        private byte CLV() { SetFlag(StatusFlags.V, false); return 0; }
        private byte CMP()
        {
            Fetch();
            temp = (ushort)(A - fetched);
            SetFlag(StatusFlags.C, A >= fetched);
            SetFlag(StatusFlags.Z, (temp & 0x00FF) == 0x0000);
            SetFlag(StatusFlags.N, (temp & 0x0080) != 0);
            return 1;
        }
        private byte CPX()
        {
            Fetch();
            temp = (ushort)(X - fetched);
            SetFlag(StatusFlags.C, X >= fetched);
            SetFlag(StatusFlags.Z, (temp & 0x00FF) == 0x0000);
            SetFlag(StatusFlags.N, (temp & 0x0080) != 0);
            return 0;
        }
        private byte CPY()
        {
            Fetch();
            temp = (ushort)(Y - fetched);
            SetFlag(StatusFlags.C, Y >= fetched);
            SetFlag(StatusFlags.Z, (temp & 0x00FF) == 0x0000);
            SetFlag(StatusFlags.N, (temp & 0x0080) != 0);
            return 0;
        }

        private byte DEC()
        {
            Fetch();
            temp = (ushort)(fetched - 1);
            Write(addr_abs, (byte)(temp & 0x00FF));
            SetFlag(StatusFlags.Z, (temp & 0x00FF) == 0x0000);
            SetFlag(StatusFlags.N, (temp & 0x0080) != 0);
            return 0;
        }

        private byte DEX()
        {
            X--;
            SetFlag(StatusFlags.Z, X == 0x00);
            SetFlag(StatusFlags.N, (X & 0x80) != 0);
            return 0;
        }

        private byte DEY()
        {
            Y--;
            SetFlag(StatusFlags.Z, Y == 0x00);
            SetFlag(StatusFlags.N, (Y & 0x80) != 0);
            return 0;
        }

        private byte EOR()
        {
            Fetch();
            A = (byte)(A ^ fetched);
            SetFlag(StatusFlags.Z, A == 0x00);
            SetFlag(StatusFlags.N, (A & 0x80) != 0);
            return 1;
        }
        private byte INC()
        {
            Fetch();
            temp = (ushort)(fetched + 1);
            Write(addr_abs, (byte)(temp & 0x00FF));
            SetFlag(StatusFlags.Z, (temp & 0x00FF) == 0x0000);
            SetFlag(StatusFlags.N, (temp & 0x0080) != 0);
            return 0;
        }

        private byte INX()
        {
            X++;
            SetFlag(StatusFlags.Z, X == 0x00);
            SetFlag(StatusFlags.N, (X & 0x80) != 0);
            return 0;
        }
        private byte INY()
        {
            Y++;
            SetFlag(StatusFlags.Z, Y == 0x00);
            SetFlag(StatusFlags.N, (Y & 0x80) != 0);
            return 0;
        }
        private byte JMP()
        {
            PC = addr_abs;
            return 0;
        }

        private byte JSR()
        {
            PC--;
            Write((ushort)(0x0100 + SP), (byte)((PC >> 8) & 0x00FF));
            SP--;
            Write((ushort)(0x0100 + SP), (byte)(PC & 0X00FF));
            SP--;

            PC = addr_abs;
            return 0;
        }

        private byte LDA()
        {
            Fetch();
            A = fetched;
            SetFlag(StatusFlags.Z, A == 0x00);
            SetFlag(StatusFlags.N, (A & 0x80) != 0);
            return 1;
        }

        private byte LDX()
        {
            Fetch();
            X = fetched;
            SetFlag(StatusFlags.Z, X == 0x00);
            SetFlag(StatusFlags.N, (X & 0x80) != 0);
            return 1;
        }
        private byte LDY()
        {
            Fetch();
            Y = fetched;
            SetFlag(StatusFlags.Z, Y == 0x00);
            SetFlag(StatusFlags.N, (Y & 0x80) != 0);
            return 1;
        }

        private byte LSR()
        {
            Fetch();
            SetFlag(StatusFlags.C, (fetched & 0x0001) != 0);
            temp = (ushort)(fetched >> 1);
            SetFlag(StatusFlags.Z, (temp & 0x00FF) == 0x0000);
            SetFlag(StatusFlags.N, (temp & 0x0080) != 0);
            if (Lookup[opcode].AddressModeMethod == IMP) 
                A = (byte)(temp & 0x00FF);
            else 
                Write(addr_abs, (byte)(temp & 0x00FF));
            return 0;
        }

        private byte NOP()
        {
            return opcode switch
            {
                0x1C or 0x3C or 0x5C or 0x7C or 0xDC or 0xFC => 1,
                _ => 0,
            };
        }


        private byte ORA()
        {
            Fetch();
            A = (byte)(A | fetched);
            SetFlag(StatusFlags.Z, A == 0x00);
            SetFlag(StatusFlags.N, (A & 0x80) != 0);
            return 1;
        }

        private byte PHA()
        {
            Write((ushort)(0x0100 + SP), A);
            SP--;
            return 0;
        }

        private byte PHP()
        {
            Write((ushort)(0x0100 + SP), (byte)(Status | (byte)(StatusFlags.B | StatusFlags.U)));
            SetFlag(StatusFlags.B, false);
            SetFlag(StatusFlags.U, false);
            SP--;
            return 0;
        }


        // PLA - Pull Accumulator from Stack
        private byte PLA()
        {
            SP++;
            A = Read((ushort)(0x0100 + SP));
            SetFlag(StatusFlags.Z, A == 0x00);
            SetFlag(StatusFlags.N, (A & 0x80) != 0);
            return 0;
        }

        // PLP - Pull Processor Status from Stack
        private byte PLP()
        {
            SP++;
            Status = Read((ushort)(0x0100 + SP));
            SetFlag(StatusFlags.U, true);
            return 0;
        }

        private byte ROL()
        {
            Fetch();
            ushort result = (ushort)((fetched << 1) | (GetFlag(StatusFlags.C)));
            SetFlag(StatusFlags.C, (result & 0xFF00) != 0);
            SetFlag(StatusFlags.Z, (result & 0x00FF) == 0);
            SetFlag(StatusFlags.N, (result & 0x0080) != 0);

            if (Lookup[opcode].AddressModeMethod == IMP) A = (byte)(result & 0x00FF);
            else Write(addr_abs, (byte)(result & 0x00FF));

            return 0;
        }

        private byte ROR()
        {
            byte value;
            if (Lookup[opcode].AddressModeMethod == IMP) value = A;
            else value = Fetch();

            // O bit 0 recebe o carry anterior
            ushort result = (ushort)((GetFlag(StatusFlags.C) << 7) | (value >> 1));

            // Atualiza carry com o bit 0 original de value
            SetFlag(StatusFlags.C, (value & 0x01) != 0);
            SetFlag(StatusFlags.Z, (result & 0xFF) == 0);
            SetFlag(StatusFlags.N, (result & 0x80) != 0);

            if (Lookup[opcode].AddressModeMethod == IMP) A = (byte)(result & 0xFF);
            else Write(addr_abs, (byte)(result & 0xFF));

            return 0;
        }



        private byte RTI()
        {
            SP++;
            Status = Read((ushort)(0x0100 + SP));
            Status &= (byte)~StatusFlags.B;
            Status &= (byte)~StatusFlags.U;

            SP++;
            PC = Read((ushort)(0x0100 + SP));
            SP++;
            PC |= (ushort)(Read((ushort)(0x0100 + SP)) << 8);

            return 0;
        }


        private byte RTS()
        {
            SP++;
            PC = Read((ushort)(0x0100 + SP));
            SP++;
            PC |= (ushort)(Read((ushort)(0x0100 + SP)) << 8);

            PC++;
            return 0;
        }
        private byte SEC() { SetFlag(StatusFlags.C, true); return 0; }
        private byte SED() { SetFlag(StatusFlags.D, true); return 0; }
        private byte SEI() { SetFlag(StatusFlags.I, true); return 0; }
        private byte STA() { Write(addr_abs, A); return 0; }
        private byte STX() { Write(addr_abs, X); return 0; }
        private byte STY() { Write(addr_abs, Y); return 0; }


        // Transferências
        private byte TAX()
        {
            X = A;
            SetFlag(StatusFlags.Z, X == 0x00);
            SetFlag(StatusFlags.N, (X & 0x80) != 0);
            return 0;
        }
        private byte TAY()
        {
            Y = A;
            SetFlag(StatusFlags.Z, Y == 0x00);
            SetFlag(StatusFlags.N, (Y & 0x80) != 0);
            return 0;
        }
        private byte TSX()
        {
            X = SP;
            SetFlag(StatusFlags.Z, X == 0x00);
            SetFlag(StatusFlags.N, (X & 0x80) != 0);
            return 0;
        }
        private byte TXA()
        {
            A = X;
            SetFlag(StatusFlags.Z, A == 0x00);
            SetFlag(StatusFlags.N, (A & 0x80) != 0);
            return 0;
        }
        private byte TXS() { SP = X; return 0; }
        private byte TYA()
        {
            A = Y;
            SetFlag(StatusFlags.Z, A == 0x00);
            SetFlag(StatusFlags.N, (A & 0x80) != 0);
            return 0;
        }

        private void SetFlag(StatusFlags f, bool v)
        {
            if (v) Status |= (byte)f;
            else Status &= (byte)~f;
        }

        /// <summary>
        /// Esta função atua como um dissassembler para as instruções do 6502. 
        /// Sendo assim, ela lê os endereços de memória do 6502 e retorna um dicionário com o endereço e a instrução correspondente, me retornando uma lista como um log em Hexadecimal.
        /// </summary>
        /// <param name="nStart"></param>
        /// <param name="nStop"></param>
        /// <returns></returns>
        public Dictionary<ushort, string> Disassemble(ushort nStart, ushort nStop)
        {
            uint addr = nStart;
            byte value = 0x00, lo = 0x00, hi = 0x00;
            var mapLines = new Dictionary<ushort, string>();
            ushort lineAddr;

            string Hex(uint n, int d) { return n.ToString("X").PadLeft(d, '0'); }

            while (addr <= nStop)
            {
                lineAddr = (ushort)addr;
                string sInst = "$" + Hex(addr, 4) + ": ";

                byte opcode = bus.cpuRead((ushort)addr++, true);

                // Evita erro de índice fora do array
                if (opcode >= Lookup.Length || opcode < 0)
                {
                    sInst += $"??? (Invalid opcode ${Hex(opcode, 2)})";
                    mapLines[lineAddr] = sInst;
                    continue;
                }

                var instruction = Lookup[opcode];
                sInst += instruction.Name + " ";
                var mode = instruction.AddressModeMethod;

                if (mode == IMP)
                {
                    sInst += " {IMP}";
                }
                else if (mode == IMM)
                {
                    value = bus.cpuRead((ushort)addr++, true);
                    sInst += "#$" + Hex(value, 2) + " {IMM}";
                }
                else if (mode == ZP0)
                {
                    lo = bus.cpuRead((ushort)addr++, true);
                    sInst += "$" + Hex(lo, 2) + " {ZP0}";
                }
                else if (mode == ZPX)
                {
                    lo = bus.cpuRead((ushort)addr++, true);
                    sInst += "$" + Hex(lo, 2) + ", X {ZPX}";
                }
                else if (mode == ZPY)
                {
                    lo = bus.cpuRead((ushort)addr++, true);
                    sInst += "$" + Hex(lo, 2) + ", Y {ZPY}";
                }
                else if (mode == IZX)
                {
                    lo = bus.cpuRead((ushort)addr++, true);
                    sInst += "($" + Hex(lo, 2) + ", X) {IZX}";
                }
                else if (mode == IZY)
                {
                    lo = bus.cpuRead((ushort)addr++, true);
                    sInst += "($" + Hex(lo, 2) + "), Y {IZY}";
                }
                else if (mode == ABS)
                {
                    lo = bus.cpuRead((ushort)addr++, true);
                    hi = bus.cpuRead((ushort)addr++, true);
                    sInst += "$" + Hex((ushort)((hi << 8) | lo), 4) + " {ABS}";
                }
                else if (mode == ABX)
                {
                    lo = bus.cpuRead((ushort)addr++, true);
                    hi = bus.cpuRead((ushort)addr++, true);
                    sInst += "$" + Hex((ushort)((hi << 8) | lo), 4) + ", X {ABX}";
                }
                else if (mode == ABY)
                {
                    lo = bus.cpuRead((ushort)addr++, true);
                    hi = bus.cpuRead((ushort)addr++, true);
                    sInst += "$" + Hex((ushort)((hi << 8) | lo), 4) + ", Y {ABY}";
                }
                else if (mode == IND)
                {
                    lo = bus.cpuRead((ushort)addr++, true);
                    hi = bus.cpuRead((ushort)addr++, true);
                    sInst += "($" + Hex((ushort)((hi << 8) | lo), 4) + ") {IND}";
                }
                else if (mode == REL)
                {
                    value = bus.cpuRead((ushort)addr++, true);
                    ushort targetAddr = (ushort)(addr + (sbyte)value);
                    sInst += "$" + Hex(value, 2) + " [$" + Hex(targetAddr, 4) + "] {REL}";
                }

                mapLines[lineAddr] = sInst;
            }

            return mapLines;
        }
    }

}
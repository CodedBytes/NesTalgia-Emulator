using NesTalgia_EMU.Core.CPU;
using NesTalgia_EMU.Core.Input;
using NesTalgia_EMU.Core.PPU;
using NesTalgia_EMU.Core.ROM;

namespace NesTalgia_EMU.Core.Memory
{
    /// <summary>
    /// Classe responsável pela memória da CPU do NES.
    /// </summary>
    public class MemoryMap
    {
        public readonly byte[] ram = new byte[2048];// 2KB de memoria BUS
        public Ppu ppu;
        public Cpu cpu = new Cpu();
        public Cartridge cart;
        public Joypad joy;
        private int nSystemClockCounter = 0;// Clock de execução do sistema, usado para sincronizar a CPU com a PPU.

        /// <summary>
        /// Responsavel por iniciar o mapeamento de memória do NES.
        /// </summary>
        /// <param name="ppu">Objeto representante da PPU.</param>
        /// <param name="joy">Objeto representante dos controles.</param>
        public MemoryMap(Ppu ppu, Joypad joy)
        {
            // Limpando a ram, s� por precau��o 
            this.ppu = ppu;
            this.joy = joy;
            cpu.ConnectBus(this);
        }

        /// <summary>
        /// Função para escrever os dados na CPU.
        /// </summary>
        /// <param name="addr">Endereço de 2 bytes EX: $B2 $1A</param>
        /// <param name="data">Retorna para a CPU a instrução de 8bits a ser executada na cpu ou dados de periféricos.</param>
        public void cpuWrite(int addr, byte data)
        {
            if (addr == 0x4016 || addr == 0x4017)
            {
                joy.Write((ushort)addr, data);
            }
            else if (addr >= 0x0000 && addr <= 0x1FFF)
            {
                ram[addr & 0x07FF] = data;
            }
            else if (addr >= 0x2000 && addr <= 0x3FFF)
                ppu.CpuWrite((ushort)(addr & 0x0007), data);
            else if (cart != null)
                cart.cpuWrite(addr, data);

            
        }

        /// <summary>
        /// Função para ler os dados da CPU.
        /// </summary>
        /// <param name="addr">Endereço de 2 bytes - ex: $A1 $10</param>
        /// <param name="bReadOnly">Trigger para definição de leitura dos dados passados.</param>
        /// <returns>Retorna os dados de perifericos ou instrução executada pela CPU.</returns>
        public byte cpuRead(int addr, bool bReadOnly = false)
        {
            byte data = 0x00;
            if (addr == 0x4016 || addr == 0x4017)
            {
                data = joy.Read((ushort)addr);
            }
            else if (addr >= 0x0000 && addr <= 0x1FFF)
                data = ram[addr & 0x07FF];
            else if (addr >= 0x2000 && addr <= 0x3FFF)
                data = ppu.CpuRead((ushort)(addr & 0x0007), bReadOnly);
            else if (cart != null && cart.cpuRead(addr, out data))
               return data;


            return data;
        }

        /// <summary>
        /// Se encarregado de realizar o insert do cartucho no NES.
        /// </summary>
        /// <param name="rom">Objeto da ROM do jogo carregado.</param>
        public void InsertCartridge(Cartridge rom)
        {
            this.cart = rom;
            ppu.ConnectCartridge(rom);
        }

        /// <summary>
        /// Efetua o reset dos componentes do NES.
        /// </summary>
        public void reset()
        {
            cpu.Reset();
            cart.Reset();
            ppu.reset();
            joy.Reset();
            nSystemClockCounter = 0;
        }

        /// <summary>
        /// Clock de repetição para sincronização da CPU com a PPU
        /// <para>A CPU roda 3 vezes mais lento que a PPU, então chamamos o clock da cpu a cada 3 vezes que essa função é chamada.</para>
        /// </summary>
        public void Clock()
        {
            ppu.Clock();

            if (nSystemClockCounter % 3 == 0) cpu.Clock();
            if (ppu.nmi)
            {
                ppu.nmi = false;
                cpu.NonMaskableInterrupt();
            }
            
            nSystemClockCounter++;
        }

    }
}
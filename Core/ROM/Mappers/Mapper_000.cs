namespace NesTalgia_EMU.Core.ROM.Mappers
{
    /// <summary>
    /// Classe responsavel pelo mapper 0, padrão para bancos de 8KB originais do NES.
    /// </summary>
    public class Mapper_000 : Mapper
    {
        /// <summary>
        /// Construtor do mapper 0, apenas pare referenciar os bancos PRG e CHR da ROM por enquanto.
        /// </summary>
        /// <param name="prgBanks">ROM PRG</param>
        /// <param name="chrBanks">ROM CHR</param>
        public Mapper_000(byte prgBanks, byte chrBanks) : base(prgBanks, chrBanks){ }

        /// <summary>
        /// Processo de leitura de mapeamento da ROM vinda da CPU.
        /// </summary>
        /// <param name="addr">Endereço a ser lido</param>
        /// <param name="mapped_addr">Endereço mapeado na ROM</param>
        /// <returns>Retorna verdadeiro se o mapeamento foi feito ou falso se não</returns>
        public override bool cpuMapRead(int addr, out int mapped_addr)
        {
            if (addr >= 0x8000 && addr <= 0xFFFF)
            {
                mapped_addr = addr & (nPRGBanks > 1 ? 0x7FFF : 0x3FFF);
                return true;
            }

            mapped_addr = 0;
            return false;
        }

        /// <summary>
        /// Processo de escrita do mapeamento na CPU.
        /// </summary>
        /// <param name="addr">Endereço relacionado ao processo.</param>
        /// <param name="mapped_addr">Endereço mapeado.</param>
        /// <param name="data">Dados que serao utilizados pela CPU.</param>
        /// <returns>Retorna verdadeiro se foi escrito ou falso se nao foi</returns>
        public override bool cpuMapWrite(int addr, out int mapped_addr, byte data)
        {
            if (addr >= 0x8000 && addr <= 0xFFFF)
            {
                mapped_addr = addr & (nPRGBanks > 1 ? 0x7FFF : 0x3FFF);
                return true;
            }

            mapped_addr = 0;
            return false;
        }

        /// <summary>
        /// Processo de leitura de mapeamento da ROM vinda da PPU.
        /// </summary>
        /// <param name="addr">Endereço a ser lido</param>
        /// <param name="mapped_addr">Endereço mapeado na ROM</param>
        /// <returns>Retorna verdadeiro se o mapeamento foi feito ou falso se não</returns>
        public override bool ppuMapRead(int addr, out int mapped_addr)
        {
            if (addr >= 0x0000 && addr <= 0x1FFF)
            {
                mapped_addr = addr;
                return true;
            }

            mapped_addr = 0;
            return false;
        }

        /// <summary>
        /// Processo de escrita de mapeamento da ROM para a PPU.
        /// </summary>
        /// <param name="addr">Endereço do processo</param>
        /// <param name="mapped_addr">Endereço mapeado da ROM</param>
        /// <returns>Retorna verdadeiro se o mapeamento foi enviado ou falso se não</returns>
        public override bool ppuMapWrite(int addr, out int mapped_addr)
        {
            if (addr >= 0x0000 && addr <= 0x1FFF)
            {
                if (nCHRBanks == 0)
                {
                    // Se cair aqui ele trata como RAM.
                    mapped_addr = addr;
                    return true;
                }
            }

            mapped_addr = 0;
            return false;
        }

        // Reset do cartucho refletido no mapper.
        public override void reset() {}
    }
}

namespace NesTalgia_EMU.Core.ROM
{
    /// <summary>
    /// Classe base dos mappers do NES. 
    /// Mappers são utilizados para realizar mapeamentos de sessão no chip grafico do NES, 
    /// permitindo não só 8kb de ROM mas sim ROMS com mais de um setor de 8kb, como 16kb ou 32kb como mais comuns em jogos com muitas animações.
    /// </summary>
    public abstract class Mapper
    {
        protected byte nPRGBanks;
        protected byte nCHRBanks;

        /// <summary>
        /// Responsável por iniciar as variaveis de PRG e CHR da ROM, e iniciando o reset do cartucho.
        /// </summary>
        /// <param name="prgBanks">ROM PRG</param>
        /// <param name="chrBanks">ROM CHR</param>
        public Mapper(byte prgBanks, byte chrBanks)
        {
            nPRGBanks = prgBanks;
            nCHRBanks = chrBanks;
            reset();
        }

        // Transforma o endereço da BUS da CPU em um offset da ROM PRG
        public abstract bool cpuMapRead(int addr, out int mapped_addr);
        public abstract bool cpuMapWrite(int addr, out int mapped_addr, byte data);

        // Transforma o endereço da BUS da PPU em um ofset da ROM CHR.
        public abstract bool ppuMapRead(int addr, out int mapped_addr);
        public abstract bool ppuMapWrite(int addr, out int mapped_addr);

        // Reset do cartucho
        public abstract void reset();
    }
}

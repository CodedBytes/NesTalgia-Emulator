using System;
using System.IO;
using System.Runtime.InteropServices;
using NesTalgia_EMU.Core.ROM.Mappers;

namespace NesTalgia_EMU.Core.ROM
{
    /// <summary>
    /// Classe responsável por carregar e gerenciar a ROM do NES.
    /// </summary>
    public class Cartridge
    {
        /// <summary>
        /// Responsavel pelo Mirroring dos cartuchos de NES.
        /// </summary>
        public enum MIRROR
        {
            HORIZONTAL,
            VERTICAL,
            ONESCREEN_LO,
            ONESCREEN_HI,
        }

        // Variaveis para incialização.
        public MIRROR mirror = MIRROR.HORIZONTAL;
        private bool bImageValid = false;
        private byte nMapperID = 0;
        private byte nPRGBanks = 0;
        private byte nCHRBanks = 0;
        public Mapper? pMapper;

        /// <summary>
        /// Header das ROMs em iNES
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct sHeader
        {
            public uint ident;                // 0x00000000 - "NES" seguido de 0x1A
            public byte prg_rom_chunks;       // 0x00000004
            public byte chr_rom_chunks;       // 0x00000005
            public byte mapper1;              // 0x00000006
            public byte mapper2;              // 0x00000007
            public byte prg_ram_size;         // 0x00000008
            public byte tv_system1;           // 0x00000009
            public byte tv_system2;           // 0x0000000A
            public byte unused10;             // 0x0000000B
            public byte unused11;
            public byte unused12;
            public byte unused13;
            public byte unused14;
            public byte unused15;
            public byte unused16;
            public byte unused17;
        }
        public sHeader header;
        public byte[] vPRGMemory;
        public byte[] vCHRMemory;

        /// <summary>
        /// Responsável por carregar a ROM do NES a partir de um arquivo.
        /// </summary>
        /// <param name="sFileName">O caminho completo do arquivo carregado pelo emulador.</param>
        /// <exception cref="InvalidDataException">Exception para dados invalidos.</exception>
        /// <exception cref="NotSupportedException">Exception para ROM não suportada.</exception>
        public Cartridge(string sFileName)
        {
            using var ifs = new FileStream(sFileName, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(ifs);

            // Lê o cabeçalho do arquivo (16 bytes)
            var headerBytes = reader.ReadBytes(16);
            header = ByteArrayToStruct<sHeader>(headerBytes);

            // Valida assinatura NES + EOF
            // 0x1A53454E igual a 'N' 'E' 'S' 0x1A, little endian
            if (header.ident != 0x1A53454E) throw new InvalidDataException("Arquivo não é um arquivo iNES válido.");

            // Pula trainer de 512 bytes, se existir (bit 2 do mapper1)
            if ((header.mapper1 & 0x04) != 0) reader.BaseStream.Seek(512, SeekOrigin.Current);

            // Calcula o Mapper ID a partir dos nibbles altos dos bytes 6 e 7
            nMapperID = (byte)(((header.mapper2 & 0xF0) | ((header.mapper1 & 0xF0) >> 4)));

            // Configura o tipo de espelhamento baseado no bit 0 do mapper1
            mirror = (header.mapper1 & 0x01) != 0 ? MIRROR.VERTICAL : MIRROR.HORIZONTAL;

            // Lê bancos PRG ROM
            nPRGBanks = header.prg_rom_chunks;
            vPRGMemory = reader.ReadBytes(nPRGBanks * 16384);

            // Lê bancos CHR ROM ou aloca CHR RAM se zero
            nCHRBanks = header.chr_rom_chunks;
            if (nCHRBanks == 0) vCHRMemory = new byte[8192]; // CHR RAM 8KB padrão 
            else vCHRMemory = reader.ReadBytes(nCHRBanks * 8192);

            // Instancia o mapper correto
            switch (nMapperID)
            {
                case 0:// Mapper padrão de 8kb do NES.
                    pMapper = new Mapper_000(nPRGBanks, nCHRBanks);
                    break;

                default:// Caso não implementado, a rom não pode ser emulada.
                    throw new NotSupportedException($"Mapper {nMapperID} não suportado.");
            }

            // LOG do tamanho da ROM
            Console.WriteLine($"nCHRBanks: {nCHRBanks}");
            Console.WriteLine($"CHR size: {vCHRMemory.Length} bytes");

            // Caso de tudo certo, a imagem é válida.
            bImageValid = true;
        }

        /// <summary>
        /// Método auxiliar para converter byte[] para struct.
        /// </summary>
        /// <typeparam name="T">Estrutura final</typeparam>
        /// <param name="bytes">Bytes a serem transformados em struct</param>
        /// <returns>Retorna o mesmo byte[] mas em struct.</returns>
        private static T ByteArrayToStruct<T>(byte[] bytes) where T : struct
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }

        /// <summary>
        /// Controla a escrita dos dados do cartucho na CPU.
        /// </summary>
        /// <param name="addr">Endereços nos quais estão sendo escritos para CPU.</param>
        /// <param name="data">Dados a serem passados para a CPU.</param>
        /// <returns>Retorna os dados nos quais estão sendo escritos na CPU.</returns>
        public bool cpuWrite(int addr, byte data)
        {
            if (pMapper != null && pMapper.cpuMapWrite(addr, out int mapped_addr, data))
            {
                if (mapped_addr >= 0 && mapped_addr < vPRGMemory.Length)
                {
                    vPRGMemory[mapped_addr] = data;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Controla a leitura dos dados do cartucho vindos da CPU.
        /// </summary>
        /// <param name="addr">Endereços de retorno da CPU.</param>
        /// <param name="data">Os dados que chegaram da CPU.</param>
        /// <returns>Retorna os dados tratados da CPU pelo cartucho.</returns>
        public bool cpuRead(int addr, out byte data)
        {
            data = 0;
            if (pMapper != null && pMapper.cpuMapRead(addr, out int mapped_addr))
            {
                if (mapped_addr >= 0 && mapped_addr < vPRGMemory.Length)
                {
                    data = vPRGMemory[mapped_addr];
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Controla o acesso à memória CHR do cartucho, realizando a leitura dos dados vndos da PPU.
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool ppuRead(ushort addr, out byte data)
        {
            data = 0;
            if (pMapper != null && pMapper.ppuMapRead(addr, out int mapped_addr))
            {
                if (mapped_addr >= 0 && mapped_addr < vCHRMemory.Length)
                {
                    data = vCHRMemory[mapped_addr];
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Controla o acesso à memoria CHR do cartucho, realizandoa  escrita dos dados para a ppu.
        /// </summary>
        /// <param name="addr">Endereços a serem passados para a ppu.</param>
        /// <param name="data">Dados a serem passados para a ppu.</param>
        /// <returns>Retorna o tratamento desses dados a serem enviados para a ppu.</returns>
        public bool ppuWrite(ushort addr, byte data)
        {
            if (pMapper != null && pMapper.ppuMapWrite(addr, out int mapped_addr))
            {
                // Permite escrita apenas se a ROM usa CHR RAM (nCHRBanks == 0)
                if (nCHRBanks == 0 && mapped_addr >= 0 && mapped_addr < vCHRMemory.Length)
                {
                    vCHRMemory[mapped_addr] = data;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Não é responsável pelo reset do cartucho mas sim da leitura do mapper.
        /// </summary>
        public void Reset()
        {
            // Resetar o mapper, se existir
            pMapper?.reset();
        }
    }
}

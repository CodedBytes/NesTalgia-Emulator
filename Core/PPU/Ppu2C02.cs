using NesTalgia_EMU.Core.ROM;
using SDL2;
namespace NesTalgia_EMU.Core.PPU
{
    /// <summary>
    /// Classe responsável por gerenciar o PPU (Picture Processing Unit) do NES.
    /// </summary>
    public class Ppu
    {
        private readonly IntPtr renderer;
        private IntPtr textureScreen;
        private IntPtr textureNameTable0;
        private IntPtr textureNameTable1;
        public IntPtr texturePatternTable0;
        public IntPtr texturePatternTable1;
        int frameCount = 0;
        uint timer = SDL.SDL_GetTicks();
        float fps = 0f;
        private IntPtr pixelsPtr = IntPtr.Zero;
        private int pitch;
        private bool textureLocked = false;
        public byte[,] tblName = new byte[2,1024];
	    public byte[,] tblPattern = new byte[2,4096];
	    public byte[] tblPalette = new byte[32];
        public Sprite GameScreen;
        private Sprite[] sprNameTable = new Sprite[2];
        private Sprite[] sprPatternTable = new Sprite[2];
        private LoopyRegister vram_addr = new LoopyRegister();
        private LoopyRegister tram_addr = new LoopyRegister();
        private Status ppuStatus = new Status();
        private Mask ppuMask = new Mask();
        private Control ppuCTRL = new Control();
        private byte fine_x = 0x00;
        private byte address_latch = 0x00;
        private ushort ppu_Address = 0x0000;
        private byte ppu_data_buffer = 0x00;
        private int scanline = 0;
        private int cycle = 0;
        private byte bg_next_tile_id = 0x00;
        private byte bg_next_tile_attrib = 0x00;
        private byte bg_next_tile_lsb = 0x00;
        private byte bg_next_tile_msb = 0x00;
        private ushort bg_shifter_pattern_lo = 0x0000;
        private ushort bg_shifter_pattern_hi = 0x0000;
        private ushort bg_shifter_attrib_lo = 0x0000;
        private ushort bg_shifter_attrib_hi = 0x0000;
        public bool frame_complete = false;
        private Random rand = new Random();
        public bool nmi = false;

        // Paleta de cores da NES, 64 cores (olc::Pixel = RGB)
        private readonly SDL.SDL_Color[] palScreen = new SDL.SDL_Color[64];

        private Cartridge? cart;
        public const int SCREEN_WIDTH = 256;
        public const int SCREEN_HEIGHT = 240;

        /// <summary>
        /// status do PPU, utilizado para verificar o estado do PPU.
        /// </summary>
        public class Status
        {
            private byte _reg;

            public byte reg
            {
                get => _reg;
                set => _reg = value;
            }

            public byte unused
            {
                get => (byte)((_reg >> 0) & 0x1F);
                set => _reg = (byte)((_reg & ~(0x1F << 0)) | ((value & 0x1F) << 0));
            }

            public byte sprite_overflow
            {
                get => (byte)((_reg >> 5) & 0x01);
                set => _reg = (byte)((_reg & ~(1 << 5)) | ((value & 0x01) << 5));
            }

            public byte sprite_zero_hit
            {
                get => (byte)((_reg >> 6) & 0x01);
                set => _reg = (byte)((_reg & ~(1 << 6)) | ((value & 0x01) << 6));
            }

            public byte vertical_blank
            {
                get => (byte)((_reg >> 7) & 0x01);
                set => _reg = (byte)((_reg & ~(1 << 7)) | ((value & 0x01) << 7));
            }
        }

        /// <summary>
        /// Máscara do PPU, utilizada para controlar o que é renderizado na tela.
        /// </summary>
        public class Mask
        {
            private byte _reg;

            public byte reg
            {
                get => _reg;
                set => _reg = value;
            }

            public byte grayscale
            {
                get => (byte)(_reg & 0x01);
                set => _reg = (byte)((_reg & ~0x01) | (value & 0x01));
            }

            public byte render_background_left
            {
                get => (byte)((_reg >> 1) & 0x01);
                set => _reg = (byte)((_reg & ~(1 << 1)) | ((value & 0x01) << 1));
            }

            public byte render_sprites_left
            {
                get => (byte)((_reg >> 2) & 0x01);
                set => _reg = (byte)((_reg & ~(1 << 2)) | ((value & 0x01) << 2));
            }

            public byte render_background
            {
                get => (byte)((_reg >> 3) & 0x01);
                set => _reg = (byte)((_reg & ~(1 << 3)) | ((value & 0x01) << 3));
            }

            public byte render_sprites
            {
                get => (byte)((_reg >> 4) & 0x01);
                set => _reg = (byte)((_reg & ~(1 << 4)) | ((value & 0x01) << 4));
            }

            public byte enhance_red
            {
                get => (byte)((_reg >> 5) & 0x01);
                set => _reg = (byte)((_reg & ~(1 << 5)) | ((value & 0x01) << 5));
            }

            public byte enhance_green
            {
                get => (byte)((_reg >> 6) & 0x01);
                set => _reg = (byte)((_reg & ~(1 << 6)) | ((value & 0x01) << 6));
            }

            public byte enhance_blue
            {
                get => (byte)((_reg >> 7) & 0x01);
                set => _reg = (byte)((_reg & ~(1 << 7)) | ((value & 0x01) << 7));
            }
        }

        /// <summary>
        /// Controle do PPU, utilizado para configurar o comportamento do PPU.
        /// </summary>
        public class Control
        {
            private byte _reg;

            public byte reg
            {
                get => _reg;
                set => _reg = value;
            }

            public byte nametable_x
            {
                get => (byte)(_reg & 0x01);
                set => _reg = (byte)((_reg & ~0x01) | (value & 0x01));
            }

            public byte nametable_y
            {
                get => (byte)((_reg >> 1) & 0x01);
                set => _reg = (byte)((_reg & ~(1 << 1)) | ((value & 0x01) << 1));
            }

            public byte increment_mode
            {
                get => (byte)((_reg >> 2) & 0x01);
                set => _reg = (byte)((_reg & ~(1 << 2)) | ((value & 0x01) << 2));
            }

            public byte pattern_sprite
            {
                get => (byte)((_reg >> 3) & 0x01);
                set => _reg = (byte)((_reg & ~(1 << 3)) | ((value & 0x01) << 3));
            }

            public byte pattern_background
            {
                get => (byte)((_reg >> 4) & 0x01);
                set => _reg = (byte)((_reg & ~(1 << 4)) | ((value & 0x01) << 4));
            }

            public byte sprite_size
            {
                get => (byte)((_reg >> 5) & 0x01);
                set => _reg = (byte)((_reg & ~(1 << 5)) | ((value & 0x01) << 5));
            }

            public byte slave_mode
            {
                get => (byte)((_reg >> 6) & 0x01);
                set => _reg = (byte)((_reg & ~(1 << 6)) | ((value & 0x01) << 6));
            }

            public byte enable_nmi
            {
                get => (byte)((_reg >> 7) & 0x01);
                set => _reg = (byte)((_reg & ~(1 << 7)) | ((value & 0x01) << 7));
            }
        }

        /// <summary>
        /// Loopy Register, utilizado para gerenciar o endereço de VRAM do PPU.
        /// </summary>
        public class LoopyRegister
        {
            private ushort _reg;

            public ushort reg
            {
                get => _reg;
                set => _reg = value;
            }

            public byte coarse_x
            {
                get => (byte)((_reg >> 0) & 0x1F);
                set => _reg = (ushort)((_reg & ~(0x1F << 0)) | ((value & 0x1F) << 0));
            }

            public byte coarse_y
            {
                get => (byte)((_reg >> 5) & 0x1F);
                set => _reg = (ushort)((_reg & ~(0x1F << 5)) | ((value & 0x1F) << 5));
            }

            public byte nametable_x
            {
                get => (byte)((_reg >> 10) & 0x01);
                set => _reg = (ushort)((_reg & ~(1 << 10)) | ((value & 0x01) << 10));
            }

            public byte nametable_y
            {
                get => (byte)((_reg >> 11) & 0x01);
                set => _reg = (ushort)((_reg & ~(1 << 11)) | ((value & 0x01) << 11));
            }

            public byte fine_y
            {
                get => (byte)((_reg >> 12) & 0x07);
                set => _reg = (ushort)((_reg & ~(0x07 << 12)) | ((value & 0x07) << 12));
            }

            public byte unused
            {
                get => (byte)((_reg >> 15) & 0x01);
                set => _reg = (ushort)((_reg & ~(1 << 15)) | ((value & 0x01) << 15));
            }
        }

        /// <summary>
        /// Constructor da PPU. Responsável por inicializar o PPU com o renderizador SDL e criar as texturas necessárias para a renderização da tela e das tabelas de padrões.
        /// </summary>
        /// <param name="sdlRenderer">Pointer para o render do SDL.</param>
        public Ppu(IntPtr sdlRenderer)
        {
            renderer = sdlRenderer;
            textureScreen = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_ARGB8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, SCREEN_WIDTH, SCREEN_HEIGHT);
            texturePatternTable0 = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_ARGB8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, 128, 128);
            texturePatternTable1 = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_ARGB8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, 128, 128);

            // Iniciando os sprites da nametable e da patterntable
            GameScreen = new Sprite(256, 240, renderer);
            sprNameTable[0] = new Sprite(256, 240, renderer);
            sprNameTable[1] = new Sprite(256, 240, renderer);
            sprPatternTable[0] = new Sprite(128, 128, renderer);
            sprPatternTable[1] = new Sprite(128, 128, renderer);
            InitPalette();
        }

        /// <summary>
        /// Responsável por inicializar a paleta de cores do PPU com as 64 cores padrão do NES.
        /// </summary>
        private void InitPalette()
        {
            palScreen[0x00] = new SDL.SDL_Color { r = 84, g = 84, b = 84, a = 255 };
            palScreen[0x01] = new SDL.SDL_Color { r = 0, g = 30, b = 116, a = 255 };
            palScreen[0x02] = new SDL.SDL_Color { r = 8, g = 16, b = 144, a = 255 };
            palScreen[0x03] = new SDL.SDL_Color { r = 48, g = 0, b = 136, a = 255 };
            palScreen[0x04] = new SDL.SDL_Color { r = 68, g = 0, b = 100, a = 255 };
            palScreen[0x05] = new SDL.SDL_Color { r = 92, g = 0, b = 48, a = 255 };
            palScreen[0x06] = new SDL.SDL_Color { r = 84, g = 4, b = 0, a = 255 };
            palScreen[0x07] = new SDL.SDL_Color { r = 60, g = 24, b = 0, a = 255 };
            palScreen[0x08] = new SDL.SDL_Color { r = 32, g = 42, b = 0, a = 255 };
            palScreen[0x09] = new SDL.SDL_Color { r = 8, g = 58, b = 0, a = 255 };
            palScreen[0x0A] = new SDL.SDL_Color { r = 0, g = 64, b = 0, a = 255 };
            palScreen[0x0B] = new SDL.SDL_Color { r = 0, g = 60, b = 0, a = 255 };
            palScreen[0x0C] = new SDL.SDL_Color { r = 0, g = 50, b = 60, a = 255 };
            palScreen[0x0D] = new SDL.SDL_Color { r = 0, g = 0, b = 0, a = 255 };
            palScreen[0x0E] = new SDL.SDL_Color { r = 0, g = 0, b = 0, a = 255 };
            palScreen[0x0F] = new SDL.SDL_Color { r = 0, g = 0, b = 0, a = 255 };

            palScreen[0x10] = new SDL.SDL_Color { r = 152, g = 150, b = 152, a = 255 };
            palScreen[0x11] = new SDL.SDL_Color { r = 8, g = 76, b = 196, a = 255 };
            palScreen[0x12] = new SDL.SDL_Color { r = 48, g = 50, b = 236, a = 255 };
            palScreen[0x13] = new SDL.SDL_Color { r = 92, g = 30, b = 228, a = 255 };
            palScreen[0x14] = new SDL.SDL_Color { r = 136, g = 20, b = 176, a = 255 };
            palScreen[0x15] = new SDL.SDL_Color { r = 160, g = 20, b = 100, a = 255 };
            palScreen[0x16] = new SDL.SDL_Color { r = 152, g = 34, b = 32, a = 255 };
            palScreen[0x17] = new SDL.SDL_Color { r = 120, g = 60, b = 0, a = 255 };
            palScreen[0x18] = new SDL.SDL_Color { r = 84, g = 90, b = 0, a = 255 };
            palScreen[0x19] = new SDL.SDL_Color { r = 40, g = 114, b = 0, a = 255 };
            palScreen[0x1A] = new SDL.SDL_Color { r = 8, g = 124, b = 0, a = 255 };
            palScreen[0x1B] = new SDL.SDL_Color { r = 0, g = 118, b = 40, a = 255 };
            palScreen[0x1C] = new SDL.SDL_Color { r = 0, g = 102, b = 120, a = 255 };
            palScreen[0x1D] = new SDL.SDL_Color { r = 0, g = 0, b = 0, a = 255 };
            palScreen[0x1E] = new SDL.SDL_Color { r = 0, g = 0, b = 0, a = 255 };
            palScreen[0x1F] = new SDL.SDL_Color { r = 0, g = 0, b = 0, a = 255 };

            palScreen[0x20] = new SDL.SDL_Color { r = 236, g = 238, b = 236, a = 255 };
            palScreen[0x21] = new SDL.SDL_Color { r = 76, g = 154, b = 236, a = 255 };
            palScreen[0x22] = new SDL.SDL_Color { r = 120, g = 124, b = 236, a = 255 };
            palScreen[0x23] = new SDL.SDL_Color { r = 176, g = 98, b = 236, a = 255 };
            palScreen[0x24] = new SDL.SDL_Color { r = 228, g = 84, b = 236, a = 255 };
            palScreen[0x25] = new SDL.SDL_Color { r = 236, g = 88, b = 180, a = 255 };
            palScreen[0x26] = new SDL.SDL_Color { r = 236, g = 106, b = 100, a = 255 };
            palScreen[0x27] = new SDL.SDL_Color { r = 212, g = 136, b = 32, a = 255 };
            palScreen[0x28] = new SDL.SDL_Color { r = 160, g = 170, b = 0, a = 255 };
            palScreen[0x29] = new SDL.SDL_Color { r = 116, g = 196, b = 0, a = 255 };
            palScreen[0x2A] = new SDL.SDL_Color { r = 76, g = 208, b = 32, a = 255 };
            palScreen[0x2B] = new SDL.SDL_Color { r = 56, g = 204, b = 108, a = 255 };
            palScreen[0x2C] = new SDL.SDL_Color { r = 56, g = 180, b = 204, a = 255 };
            palScreen[0x2D] = new SDL.SDL_Color { r = 60, g = 60, b = 60, a = 255 };
            palScreen[0x2E] = new SDL.SDL_Color { r = 0, g = 0, b = 0, a = 255 };
            palScreen[0x2F] = new SDL.SDL_Color { r = 0, g = 0, b = 0, a = 255 };

            palScreen[0x30] = new SDL.SDL_Color { r = 236, g = 238, b = 236, a = 255 };
            palScreen[0x31] = new SDL.SDL_Color { r = 168, g = 204, b = 236, a = 255 };
            palScreen[0x32] = new SDL.SDL_Color { r = 188, g = 188, b = 236, a = 255 };
            palScreen[0x33] = new SDL.SDL_Color { r = 212, g = 178, b = 236, a = 255 };
            palScreen[0x34] = new SDL.SDL_Color { r = 236, g = 174, b = 236, a = 255 };
            palScreen[0x35] = new SDL.SDL_Color { r = 236, g = 174, b = 212, a = 255 };
            palScreen[0x36] = new SDL.SDL_Color { r = 236, g = 180, b = 176, a = 255 };
            palScreen[0x37] = new SDL.SDL_Color { r = 228, g = 196, b = 144, a = 255 };
            palScreen[0x38] = new SDL.SDL_Color { r = 204, g = 210, b = 120, a = 255 };
            palScreen[0x39] = new SDL.SDL_Color { r = 180, g = 222, b = 120, a = 255 };
            palScreen[0x3A] = new SDL.SDL_Color { r = 168, g = 226, b = 144, a = 255 };
            palScreen[0x3B] = new SDL.SDL_Color { r = 152, g = 226, b = 180, a = 255 };
            palScreen[0x3C] = new SDL.SDL_Color { r = 160, g = 214, b = 228, a = 255 };
            palScreen[0x3D] = new SDL.SDL_Color { r = 160, g = 162, b = 160, a = 255 };
            palScreen[0x3E] = new SDL.SDL_Color { r = 0, g = 0, b = 0, a = 255 };
            palScreen[0x3F] = new SDL.SDL_Color { r = 0, g = 0, b = 0, a = 255 };
        }

        /// <summary>
        /// Responsável por pegar a textura da tela do PPU, adaptada para o SDL.
        /// </summary>
        /// <returns>Retorna a textura da PPU, adaptada para o SDL</returns>
        public IntPtr GetScreenTexture() => textureScreen;

        /// <summary>
        /// Responsável por pegar a textura da patterntable selecionada do PPU, adaptada para o SDL.
        /// </summary>
        /// <param name="i">O indice da pattern table que devo.</param>
        /// <param name="palette">Paleta de cores que sera usada no patern table, geralmente em forma de instrução.</param>
        /// <returns>Retorna a PatternTable montada com a paleta de cores correspondente.</returns>
        public Sprite GetPatternTable(byte i, byte palette)
        {
            for (ushort nTileY = 0; nTileY < 16; nTileY++)
            {
                for (ushort nTileX = 0; nTileX < 16; nTileX++)
                {
                    // Converte o tile 2D em um offset 1D na memória da pattern table.
                    ushort nOffset = (ushort)(nTileY * 256 + nTileX * 16);

                    // Dando looping em uma linha de 8 pixels
                    for (ushort row = 0; row < 8; row++)
                    {
                        byte tile_lsb = PpuRead((ushort)(i * 0x1000 + nOffset + row + 0));
                        byte tile_msb = PpuRead((ushort)(i * 0x1000 + nOffset + row + 8));

                        for (ushort col = 0; col < 8; col++)
                        {
                            byte pixel = (byte)((tile_lsb & 0x01) + (tile_msb & 0x01));
                            tile_lsb >>= 1; tile_msb >>= 1;

                            sprPatternTable[i].SetPixel(
                                nTileX * 8 + (7 - col),
                                nTileY * 8 + row,
                                GetColourFromPaletteRam(palette, pixel));
                        }
                    }
                }
            }
            return sprPatternTable[i];
        }

        /// <summary>
        /// Realiza o update da textura da Pattern Table selecionada, utilizando a paleta de cores fornecida.
        /// </summary>
        /// <param name="i">Index da pattern table a ser utilizada</param>
        /// <param name="palette">Paleta de cores a serem aplicadas, usualmente em forma de instrução.</param>
        public void UpdatePatternTableTexture(byte i, byte palette)
        {
            var sprite = GetPatternTable(i, palette);
            IntPtr pixelsPtr;
            int pitch; 
            IntPtr texture = i == 0 ? texturePatternTable0 : texturePatternTable1;

            // Fechando a textura do SDL e realizando o update 
            if (SDL.SDL_LockTexture(texture, IntPtr.Zero, out pixelsPtr, out pitch) == 0)
            {
                unsafe
                {
                    byte* dst = (byte*)pixelsPtr;
                    for (int y = 0; y < sprite.Height; y++)
                    {
                        for (int x = 0; x < sprite.Width; x++)
                        {
                            uint pixelColor = sprite.GetPixel(x, y);
                            byte a = (byte)((pixelColor >> 24) & 0xFF);
                            byte r = (byte)((pixelColor >> 16) & 0xFF);
                            byte g = (byte)((pixelColor >> 8) & 0xFF);
                            byte b = (byte)(pixelColor & 0xFF);
                            SDL.SDL_Color c = new SDL.SDL_Color { r = r, g = g, b = b, a = a };
                            int offset = y * pitch + x * 4;

                            dst[offset + 0] = c.b;
                            dst[offset + 1] = c.g;
                            dst[offset + 2] = c.r;
                            dst[offset + 3] = c.a;
                        }
                    }
                }
                SDL.SDL_UnlockTexture(texture);
            }
        }

        /// <summary>
        /// Realiza o update da textura baseada nos sprites direto na surface de texturas do SDL.
        /// </summary>
        public void UpdateScreenTextureFromSprite()
        {
            // Fecha a textura do SDL para realizar o Update.
            if (SDL.SDL_LockTexture(textureScreen, IntPtr.Zero, out pixelsPtr, out pitch) == 0)
            {
                unsafe
                {
                    byte* dst = (byte*)pixelsPtr;
                    for (int y = 0; y < GameScreen.Height; y++)
                    {
                        for (int x = 0; x < GameScreen.Width; x++)
                        {
                            uint pixelColor = GameScreen.GetPixel(x, y);
                            byte a = (byte)((pixelColor >> 24) & 0xFF);
                            byte r = (byte)((pixelColor >> 16) & 0xFF);
                            byte g = (byte)((pixelColor >> 8) & 0xFF);
                            byte b = (byte)(pixelColor & 0xFF);
                            int offset = y * pitch + x * 4;

                            dst[offset + 0] = b;
                            dst[offset + 1] = g;
                            dst[offset + 2] = r;
                            dst[offset + 3] = a;
                        }
                    }
                }
                SDL.SDL_UnlockTexture(textureScreen);
            }
        }

        /// <summary>
        /// Pega a paleta de cores de acordo com o endereço de paleta e pixel fornecido.
        /// </summary>
        /// <param name="palette">Paleta de cores.</param>
        /// <param name="pixel">Pixel do desenho</param>
        /// <returns>Retorna a cor referente da paleta de cores.</returns>
        public SDL.SDL_Color GetColourFromPaletteRam(byte palette, byte pixel)
        {
            ushort addr = (ushort)(0x3F00 + (palette << 2) + pixel);
            addr &= 0x001F;
            if (addr == 0x0010) addr = 0x0000;
            if (addr == 0x0014) addr = 0x0004;
            if (addr == 0x0018) addr = 0x0008;
            if (addr == 0x001C) addr = 0x000C;

            return palScreen[tblPalette[addr] & 0x3F];
        }

        /// <summary>
        /// Responsável por pegar a tabela de nomes (Name Table) do PPU, que contém os sprites e tiles renderizados na tela. (Foreground)
        /// </summary>
        /// <param name="i">Index a ser utilizado</param>
        /// <returns>Retorna a Nametable correspondente.</returns>
        public Sprite GetNameTable(byte i) => sprNameTable[i];

        /// <summary>
        /// Responsável por ler os dados do PPU a partir de um endereço específico.
        /// </summary>
        /// <param name="addr">Endereço necessário para a leitura</param>
        /// <param name="rdonly">Identifica se estamos lidando com leitura ou escrita. Ainda não implementado.</param>
        /// <returns>Retorna os dados processados pela CPU.</returns>
        public byte CpuRead(ushort addr, bool rdonly)
        {
            byte data = 0x00;

            switch (addr)
            {
                case 0x0000: // Control
                    break;
                case 0x0001: // Mask
                    break;
                case 0x0002: // Status
                    data = (byte)((ppuStatus.reg & 0xE0) | (ppu_data_buffer & 0x1F));
                    ppuStatus.vertical_blank = 0;
                    address_latch = 0;
                    break;
                case 0x0003: // OAM Address
                    break;
                case 0x0004: // OAM Data
                    break;
                case 0x0005: // Scroll
                    break;
                case 0x0006: // PPU Address
                    break;
                case 0x0007:
                    data = ppu_data_buffer;
                    ppu_data_buffer = PpuRead(vram_addr.reg); 
                    if (vram_addr.reg >= 0x3F00) data = ppu_data_buffer;

                    vram_addr.reg += (ushort)(ppuCTRL.increment_mode != 0 ? 32 : 1);
                    return data;

            }

            return data;
        }

        /// <summary>
        /// Responsável por escrever dados na PPU a partir de um endereço específico.
        /// </summary>
        /// <param name="addr">Endereço a ser escrito.</param>
        /// <param name="data">Dados correspondentes da escritura na CPU.</param>
        public void CpuWrite(ushort addr, byte data)
        {
            switch (addr)
            {
                case 0x0000: // Controle
                    ppuCTRL.reg = data;
                    tram_addr.nametable_x = ppuCTRL.nametable_x;
                    tram_addr.nametable_y = ppuCTRL.nametable_y;
                    break;
                case 0x0001: // Mask
                    ppuMask.reg = data;
                    break;
                case 0x0002: // Status (readonly)
                    break;
                case 0x0003: // OAM Address
                    break;
                case 0x0004: // OAM Data
                    break;
                case 0x0005: // Scroll
                    if (address_latch == 0)
                    {
                        fine_x = (byte)(data & 0x07);
                        tram_addr.coarse_x = (byte)(data >> 3);
                        address_latch = 1;
                    }
                    else
                    {
                        tram_addr.fine_y = (byte)(data & 0x07);
                        tram_addr.coarse_y = (byte)(data >> 3);
                        address_latch = 0;
                    }
                    break;
                case 0x0006: // PPUADDR
                    if (address_latch == 0)
                    {
                        tram_addr.reg = (ushort)((tram_addr.reg & 0x00FF) | (data << 8));
                        address_latch = 1;
                    }
                    else
                    {
                        tram_addr.reg = (ushort)((tram_addr.reg & 0xFF00) | data);
                        vram_addr.reg = tram_addr.reg;
                        address_latch = 0;
                    }
                    break;
                case 0x0007:
                    PpuWrite(vram_addr.reg, data);
                    vram_addr.reg += (ushort)(ppuCTRL.increment_mode != 0 ? 32 : 1);
                    break;

            }
        }

        // PPU read VRAM / pattern tables (endereços 0x0000 a 0x3FFF)
        /// <summary>
        /// Responsável por ler os dados do PPU a partir de um endereço específico.
        /// </summary>
        /// <param name="addr">Endereço de leitura</param>
        /// <param name="rdonly">Se estamos lidando com escrita ou apenas leitura. Ainda não implementado.</param>
        /// <returns></returns>
        public byte PpuRead(ushort addr, bool rdonly = false)
        {
            addr &= 0x3FFF;
            if (cart != null && cart.ppuRead(addr, out byte data))
                return data;
            else if (addr >= 0x0000 && addr <= 0x1FFF)
            {
                return tblPattern[(addr & 0x1000) >> 12, addr & 0x0FFF];
            }
            else if (addr >= 0x2000 && addr <= 0x3EFF)
            {
                addr &= 0x0FFF;

                if (cart != null && cart.mirror == Cartridge.MIRROR.VERTICAL)
                {
                    // Vertical
                    if (addr >= 0x0000 && addr <= 0x03FF)
                        return tblName[0, addr & 0x03FF];
                    if (addr >= 0x0400 && addr <= 0x07FF)
                        return tblName[1, addr & 0x03FF];
                    if (addr >= 0x0800 && addr <= 0x0BFF)
                        return tblName[0, addr & 0x03FF];
                    if (addr >= 0x0C00 && addr <= 0x0FFF)
                        return tblName[1, addr & 0x03FF];
                }
                else if (cart != null && cart.mirror == Cartridge.MIRROR.HORIZONTAL)
                {
                    // Horizontal
                    if (addr >= 0x0000 && addr <= 0x03FF)
                        return tblName[0, addr & 0x03FF];
                    if (addr >= 0x0400 && addr <= 0x07FF)
                        return tblName[0, addr & 0x03FF];
                    if (addr >= 0x0800 && addr <= 0x0BFF)
                        return tblName[1, addr & 0x03FF];
                    if (addr >= 0x0C00 && addr <= 0x0FFF)
                        return tblName[1, addr & 0x03FF];
                }
            }
            else if (addr >= 0x3F00 && addr <= 0x3FFF)
            {
                // Paleta de cores do NES, baseados nos endereços do cartucho.
                addr &= 0x001F;
                if (addr == 0x0010) addr = 0x0000;
                if (addr == 0x0014) addr = 0x0004;
                if (addr == 0x0018) addr = 0x0008;
                if (addr == 0x001C) addr = 0x000C;
                return (byte)(tblPalette[addr] & (ppuMask.grayscale != 0 ? 0x30 : 0x3F));
            }
            return 0;
        }

        // PPU write VRAM / pattern tables (endereços 0x0000 a 0x3FFF)
        /// <summary>
        /// Responsável por escrever os dados do PPU a partir de um endereço específico.
        /// </summary>
        /// <param name="addr">Endereço de escrita</param>
        /// <param name="data">Dados relacionados a escrita</param>
        public void PpuWrite(ushort addr, byte data)
        {
            addr &= 0xFFFF;// Limita a escrita até esse endereço

            if (cart != null && cart.ppuWrite(addr, data)) { /* Controlado pelo cartucho */ }
            else if (addr >= 0x0000 && addr <= 0x1FFF) tblPattern[(addr & 0x1000) >> 12, addr & 0x0FFF] = data;
            else if (addr >= 0x2000 && addr <= 0x3EFF)
            {
                addr &= 0x0FFF;
                if (cart != null && cart.mirror == Cartridge.MIRROR.VERTICAL)
                {
                    // Espelhamento Vertical
                    if (addr >= 0x0000 && addr <= 0x03FF)
                        tblName[0, addr & 0x03FF] = data;
                    if (addr >= 0x0400 && addr <= 0x07FF)
                        tblName[1, addr & 0x03FF] = data;
                    if (addr >= 0x0800 && addr <= 0x0BFF)
                        tblName[0, addr & 0x03FF] = data;
                    if (addr >= 0x0C00 && addr <= 0x0FFF)
                        tblName[1, addr & 0x03FF] = data;
                }
                else if (cart != null && cart.mirror == Cartridge.MIRROR.HORIZONTAL)
                {
                    // Espelhamento Horizontal
                    if (addr >= 0x0000 && addr <= 0x03FF) tblName[0, addr & 0x03FF] = data;
                    if (addr >= 0x0400 && addr <= 0x07FF) tblName[0, addr & 0x03FF] = data;
                    if (addr >= 0x0800 && addr <= 0x0BFF) tblName[1, addr & 0x03FF] = data;
                    if (addr >= 0x0C00 && addr <= 0x0FFF) tblName[1, addr & 0x03FF] = data;
                }
            }
            else if (addr >= 0x3F00 && addr <= 0x3FFF)
            {
                // paleta de cores do NES, baseados nos endereços doc artucho.
                addr &= 0x001F;
                if (addr == 0x0010) addr = 0x0000;
                if (addr == 0x0014) addr = 0x0004;
                if (addr == 0x0018) addr = 0x0008;
                if (addr == 0x001C) addr = 0x000C;
                tblPalette[addr] = data;
            }
        }

        /// <summary>
        /// Responsável por conectar o cartucho com a PPU.
        /// </summary>
        /// <param name="cartridge">cartucho com a ROM do jogo</param>
        public void ConnectCartridge(Cartridge cartridge) { this.cart = cartridge; }

        /// <summary>
        /// Inicia o frame da PPU, bloqueando a textura do SDL para realizar a escrita.
        /// </summary>
        public void StartFrame()
        {
            if (SDL.SDL_LockTexture(textureScreen, IntPtr.Zero, out pixelsPtr, out pitch) == 0) textureLocked = true;
            else textureLocked = false;
        }

        /// <summary>
        /// Finaliza o frame da PPU, desbloqueando a textura do SDL.
        /// </summary>
        public void EndFrame()
        {
            if (textureLocked)
            {
                SDL.SDL_UnlockTexture(textureScreen);
                textureLocked = false;
            }
        }
        
        /// <summary>
        /// Função responsável pelo funcionamento do clock da ppu.
        /// </summary>
        public void Clock()
        {
            // trava a textura e obtém pixelsPtr e pitch
            if (cycle == 0 && scanline == 0) StartFrame();

            // responsável pela logica de scroll horizontal do background
            void IncrementScrollX()
            {
                if (ppuMask.render_background != 0 || ppuMask.render_sprites != 0)
                {
                    if (vram_addr.coarse_x == 31)
                    {
                        vram_addr.coarse_x = 0;
                        vram_addr.nametable_x = (byte)~vram_addr.nametable_x;
                    }
                    else vram_addr.coarse_x++;
                }
            }

            // Responsável pela lógica de scroll vertical do background
            void IncrementScrollY()
            {
                if (ppuMask.render_background != 0 || ppuMask.render_sprites != 0)
                {
                    if (vram_addr.fine_y < 7) vram_addr.fine_y++;
                    else
                    {
                        vram_addr.fine_y = 0;
                        if (vram_addr.coarse_y == 29)
                        {
                            vram_addr.coarse_y = 0;
                            vram_addr.nametable_y = (byte)~vram_addr.nametable_y;
                        }
                        else if (vram_addr.coarse_y == 31) vram_addr.coarse_y = 0;
                        else vram_addr.coarse_y++;
                    }
                }
            }

            // Transferir o endereço de tram para vram horizontalmente
            void TransferAddressX()
            {
                if (ppuMask.render_background != 0 || ppuMask.render_sprites != 0)
                {
                    vram_addr.nametable_x = tram_addr.nametable_x;
                    vram_addr.coarse_x = tram_addr.coarse_x;
                }
            }

            // Trasnferir o endereço de tram para vram verticalmente
            void TransferAddressY()
            {
                if (ppuMask.render_background != 0 || ppuMask.render_sprites != 0)
                {
                    vram_addr.fine_y = tram_addr.fine_y;
                    vram_addr.nametable_y = tram_addr.nametable_y;
                    vram_addr.coarse_y = tram_addr.coarse_y;
                }
            }

            // Responsável por atualizar o endereço de vram_addr para tram_addr
            void LoadBackgroundShifters()
            {
                bg_shifter_pattern_lo = (ushort)((bg_shifter_pattern_lo & 0xFF00) | bg_next_tile_lsb);
                bg_shifter_pattern_hi = (ushort)((bg_shifter_pattern_hi & 0xFF00) | bg_next_tile_msb);

                bg_shifter_attrib_lo = (ushort)((bg_shifter_attrib_lo & 0xFF00) | (((bg_next_tile_attrib & 0b01) != 0) ? 0xFF : 0x00));
                bg_shifter_attrib_hi = (ushort)((bg_shifter_attrib_hi & 0xFF00) | (((bg_next_tile_attrib & 0b10) != 0) ? 0xFF : 0x00));
            }

            // Responsável por atualizar os shifters do background.
            void UpdateShifters()
            {
                if (ppuMask.render_background != 0)
                {
                    bg_shifter_pattern_lo <<= 1;
                    bg_shifter_pattern_hi <<= 1;
                    bg_shifter_attrib_lo <<= 1;
                    bg_shifter_attrib_hi <<= 1;
                }
            }

            // Responsável pela lógica de incremento do endereço de vram_addr e manipulação dos tiles
            if (scanline >= -1 && scanline < 240)
            {
                if ((cycle >= 2 && cycle < 258) || (cycle >= 321 && cycle < 338))
                {
                    UpdateShifters();
                    switch ((cycle - 1) % 8)
                    {
                        case 0:
                            LoadBackgroundShifters();
                            
                            bg_next_tile_id = PpuRead((ushort)(0x2000 | (vram_addr.reg & 0x0FFF)));
                            // Os 12 bits inferiores do registrador Loopy são usados como índice para acessar qualquer tile dentro das 4 Name Tables do NES.
                            // Então cada nametable é uma matriz de 32x32 tiles
                            // Como existem 4 nametables no total, isso forma uma grade igual a essa:
                            //   0                1
                            // 0 +----------------+----------------+
                            //   |                |                |
                            //   |    (32x32)     |    (32x32)     |
                            //   |                |                |
                            // 1 +----------------+----------------+
                            //   |                |                |
                            //   |    (32x32)     |    (32x32)     |
                            //   |                |                |
                            //   +----------------+----------------+
                            // Isso dá exatamente 2¹² = 4096, ou seja, os 12 bits do registrador são suficientes para cobrir todas as posições possíveis.
                            break;

                        case 2:
                            // Aqui pegamos o atributo do proximo tile do background.
                            // Cada nametable do NES tem uma parte especial (no final dela) que não armazena tiles, mas sim informações de cor (atributos)
                            //
                            // De forma resumida, todos os aributos começam em 0x03C0 junto ao nametable,
                            // então fazemos uma operação binaria "OU" para selecionar a nametable, e o atributo do offset,
                            // Finalmente fazemos o operador "OU" com 0x2000 para o offset dentro do endereço da nametable na MemoryMap.
                            bg_next_tile_attrib = PpuRead((ushort)(0x23C0 |
                                ((vram_addr.nametable_y & 1) << 11) |
                                ((vram_addr.nametable_x & 1) << 10) |
                                ((vram_addr.coarse_y >> 2) << 3) |
                                (vram_addr.coarse_x >> 2)));

                            // O byte de atributo indica quais cores usar em cada bloco de 2x2 tiles (cada tile = 8x8 pixels).
                            // São divididos assim: BR = Bottom Right(76), BL = Bottom Left(54), TR = Top Right(32), TL = Top Left(10)
                            //
                            // +----+----+             +----+----+
                            // | TL | TR |     vira    | ID | ID |   ← ID = índice de paleta (2 bits)
                            // +----+----+             +----+----+
                            // | BL | BR |             | ID | ID |
                            // +----+----+             +----+----+
                            //
                            // Da pra saber em qual parte do byte pegar os 2 bits certos, olhando os últimos bits das coordenadas coarse_x e coarse_y
                            // Então sabemos se estamos pedindo TL, TR, BL ou BR, e então usamos shift para extrair os 2 bits certos do byte.
                            if ((vram_addr.coarse_y & 0x02) != 0) bg_next_tile_attrib >>= 4;
                            if ((vram_addr.coarse_x & 0x02) != 0) bg_next_tile_attrib >>= 2;
                            bg_next_tile_attrib &= 0x03;
                            break;

                        case 4:
                            // Pega o proximo bit menos importante do tile do background do pattern de tiles na memoria.
                            // Aqui, o ID do tile ja foi lida pela nametable.
                            // A gente usa esse id gerado pela memoria para achar o sprite correto (isso aqui assume que o sprite
                            // tem uma dimenção de 8x8 pixels na memoria, o que pode ser feito mesmo se tiver uma dimenção de 8x16 pixels também,
                            // igual acontece com o Mario quando esta com cogumelo, ficando 8x16 pixels, mas o background é sempre 8x8 pixels).
                            bg_next_tile_lsb = PpuRead((ushort)((ppuCTRL.pattern_background << 12) + (bg_next_tile_id << 4) + vram_addr.fine_y));
                            break;

                        case 6:
                            // Pega o proximo bit mais importante do tile do background do pattern de tiles na memoria.
                            // É a mesma coisa que o que acontece ali em cima, mas tem um offset de +8 para selecionar o proximo bit.
                            bg_next_tile_msb = PpuRead((ushort)((ppuCTRL.pattern_background << 12) + (bg_next_tile_id << 4) + vram_addr.fine_y + 8));
                            break;

                        case 7:
                            // Aqui incrementamos o "ponteiro" do tile do background para o proximo tile horizontal
                            // na memoria da name table. Olhando o nesdev você pode notar que isso aqui pode 
                            // ultrapassar os limites da name table, mas isso é essencial pra fazer o scroll da cemera no eixo X.
                            IncrementScrollX();
                            break;
                    }
                }

                // Aqui é o final da scanline visivel, onde a gente incrementa o scroll Y,
                // resetando a posição X e prepara o endereço para o próximo inicio de ciclos da PPU.
                if (cycle == 256) IncrementScrollY();
                if (cycle == 257)
                {
                    LoadBackgroundShifters();
                    TransferAddressX();
                }

                // Aqui lemos o próximo tile do background no final da scanline.
                if (cycle == 338 || cycle == 340) bg_next_tile_id = PpuRead((ushort)(0x2000 | (vram_addr.reg & 0x0FFF)));

                // Aqui é o final do vertical blank, 
                // onde a gente prepara o endereço para o próximo ciclo, resetando o endereço Y 
                // e preparando para renderizar novamente.
                if (scanline == -1 && cycle >= 280 && cycle < 305) TransferAddressY();
            }

            // Por algum motivo, que ainda preciso estudar, pela documentação do nesdev, 
            // isso aqui é encarado como "pós renderização" ... e não faz nada... ¯\_('v')_/¯
            if (scanline == 240) { }

            // Aqui vamos lidar com a ativação do vertical blank.
            // É uma flag bem importante para o mundo dos emuladores, jogos e funcionamentos de GPU.
            // Pro emulador, é o momento onde a PPU terminou de desenhar os pixels e se prepara para rinciar o processo.
            // Mas pra quem joga bastante jogos de computador, conhece o vertical blank pelo famoso vSync.
            if (scanline >= 241 && scanline < 261)
            {
                if (scanline == 241 && cycle == 1)
                {
                    ppuStatus.vertical_blank = 1;
                    if (ppuCTRL.enable_nmi != 0) nmi = true;
                }
            }

            // Aqui temos a composição do background.
            // Notamos que aqui é para interesse apenas do background, então essa sessão é dedicada para isso.
            byte bg_pixel = 0x00;
	        byte bg_palette = 0x00;
            if (ppuMask.render_background != 0)
            {
                // Controla a seleção de pixel ao selecionar o bit mais relevante dependendo
                // do scroll da variavel fine x.
                // Isso tem um efeito de aplicar um offset em todo o background do jogo
                // por um numero de pixels fazendo com que o scroll seja mais "liso".
                ushort bit_mux = (ushort)(0x8000 >> fine_x);

                // Seleciona os pixels por extrair da posição desejada do shifter.
                byte p0 = (byte)((bg_shifter_pattern_lo & bit_mux) > 0 ? 1 : 0);
                byte p1 = (byte)((bg_shifter_pattern_hi & bit_mux) > 0 ? 1 : 0);

                // Combinando o pixel retornando a index dele.
                bg_pixel = (byte)((p1 << 1) | p0);

                // Pegando a paleta de cores de acordo com os bits.
                byte pal0 = (byte)((bg_shifter_attrib_lo & bit_mux) > 0 ? 1 : 0);
                byte pal1 = (byte)((bg_shifter_attrib_hi & bit_mux) > 0 ? 1 : 0);
                bg_palette = (byte)((pal1 << 1) | pal0);
            }

            // Aqui a gente pega a cor do pixel final gerado pelo codigo acima,
            // e pegamos também a cor para a paleta de cores definidas pela ROM para essa scanline.
            // Após realizar essa definição, aplicamos as cores corretas para a surface do jogo (SDL).
            SDL.SDL_Color renderColor = GetColourFromPaletteRam(bg_palette, bg_pixel);
            GameScreen.SetPixel(cycle - 1, scanline, renderColor);

            // Executa o avanço do render, basicamente a base do clock da PPU.
            cycle++;
            if (cycle >= 341)
            {
                // A partir do ciclo 341 (ultimo ciclo da rodada da PPU) resetamos o ciclo.
                cycle = 0;
                scanline++;

                // A partir da scanline 261 já consideramos como fim de render visual.
                if (scanline >= 261)
                {
                    scanline = -1;
                    frame_complete = true;
                    EndFrame();
                }
            }
        }

        /// <summary>
        /// Reseta todos os vetores, estados e variaveis da PPU.
        /// </summary>
        public void reset()
        {
            fine_x = 0x00;
            address_latch = 0x00;
            ppu_data_buffer = 0x00;
            scanline = 0;
            cycle = 0;
            bg_next_tile_id = 0x00;
            bg_next_tile_attrib = 0x00;
            bg_next_tile_lsb = 0x00;
            bg_next_tile_msb = 0x00;
            bg_shifter_pattern_lo = 0x0000;
            bg_shifter_pattern_hi = 0x0000;
            bg_shifter_attrib_lo = 0x0000;
            bg_shifter_attrib_hi = 0x0000;
            ppuStatus.reg = 0x00;
            ppuMask.reg = 0x00;
            ppuCTRL.reg = 0x00;
            vram_addr.reg = 0x0000;
            tram_addr.reg = 0x0000;
        }
    }
}

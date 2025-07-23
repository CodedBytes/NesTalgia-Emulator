using SDL2;
using System;
using System.Runtime.InteropServices;

namespace NesTalgia_EMU.Core.PPU
{
    /// <summary>
    /// Classe Adicional para complementar o SDL, responsavel pela renderização de sprites em texturas.
    /// </summary>
    public unsafe class Sprite : IDisposable
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public IntPtr Surface { get; private set; }
        private IntPtr texture;
        private IntPtr renderer;

        /// <summary>
        /// Construtor da classe de Sprites. Responsável por inicializar a surface do SDL e a Textura necessaria para os sprites.
        /// </summary>
        /// <param name="width">Largura da surface.</param>
        /// <param name="height">Altura da surface.</param>
        /// <param name="renderer">Render do SDL.</param>
        /// <exception cref="Exception">Retorna falha no SDL caso não a surface seja invalida.</exception>
        public Sprite(int width, int height, IntPtr renderer)
        {
            Width = width;
            Height = height;
            this.renderer = renderer;

            // Cria uma surface ARGB8888
            Surface = SDL.SDL_CreateRGBSurfaceWithFormat(0, width, height, 32, SDL.SDL_PIXELFORMAT_ARGB8888);
            if (Surface == IntPtr.Zero)
                throw new Exception("Falha ao criar SDL_Surface: " + SDL.SDL_GetError());

            // Cria uma textura persistente correspondente
            texture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_ARGB8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, width, height);
            if (texture == IntPtr.Zero)
                throw new Exception("Falha ao criar SDL_Texture: " + SDL.SDL_GetError());
        }

        /// <summary>
        /// Responsável por fazer o desenho de pixels na surface do SDL utilizando as 64 cores passadas do NES.
        /// </summary>
        /// <param name="x">Posição X do pixel</param>
        /// <param name="y">Posição Y do pixel</param>
        /// <param name="color">Cor de acordo com a paleta de cores do NES.</param>
        /// <exception cref="Exception">Retorna uma exception caso o trvamento da surface do SDL falhe.</exception>
        public void SetPixel(int x, int y, SDL.SDL_Color color)
        {
            if (Surface == IntPtr.Zero || x < 0 || y < 0 || x >= Width || y >= Height)
                return;

            if (SDL.SDL_LockSurface(Surface) != 0)
                throw new Exception("Falha ao travar surface: " + SDL.SDL_GetError());

            SDL.SDL_Surface* surf = (SDL.SDL_Surface*)Surface;
            byte* pixels = (byte*)surf->pixels;
            int pitch = surf->pitch;

            uint mapped = SDL.SDL_MapRGBA(surf->format, color.r, color.g, color.b, color.a);
            uint* row = (uint*)(pixels + y * pitch);
            row[x] = mapped;

            SDL.SDL_UnlockSurface(Surface);
        }

        /// <summary>
        /// Repsonsável por puxar oa cor do pixel correto do desenho nas posições X e Y passadas.
        /// </summary>
        /// <param name="x">Posição X do pixel.</param>
        /// <param name="y">Posição Y do pixel.</param>
        /// <returns>Retorna a cor do pixel daquela posição.</returns>
        /// <exception cref="Exception">Retorna uma exception caso a trava da surface do SDL falhe.</exception>
        public uint GetPixel(int x, int y)
        {
            if (Surface == IntPtr.Zero || x < 0 || y < 0 || x >= Width || y >= Height)
                return 0;

            if (SDL.SDL_LockSurface(Surface) != 0)
                throw new Exception("Falha ao travar surface: " + SDL.SDL_GetError());

            SDL.SDL_Surface* surf = (SDL.SDL_Surface*)Surface;
            byte* pixels = (byte*)surf->pixels;
            int pitch = surf->pitch;

            uint* row = (uint*)(pixels + y * pitch);
            uint color = row[x];

            SDL.SDL_UnlockSurface(Surface);
            return color;
        }

        /// <summary>
        /// Responsavel por "se livrar" da surface e da textura do SDL. Vai fazer mais sentido quando desenharmos o Foreground Na PPU.
        /// </summary>
        public void Dispose()
        {
            if (Surface != IntPtr.Zero)
            {
                SDL.SDL_FreeSurface(Surface);
                Surface = IntPtr.Zero;
            }

            if (texture != IntPtr.Zero)
            {
                SDL.SDL_DestroyTexture(texture);
                texture = IntPtr.Zero;
            }
        }
    }
}

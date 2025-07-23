using NesTalgia_EMU.Core.CPU;
using NesTalgia_EMU.Core.Memory;
using SDL2;
using NesTalgia_EMU.Core.ROM;
using NesTalgia_EMU.Core.PPU;
using NesTalgia_EMU.Core.Input;
using System.Diagnostics;
namespace NesTalgia_EMU
{
    public partial class Form1 : Form
    {
        static IntPtr window;
        static IntPtr renderer;
        static IntPtr font;
        static bool isRunning = true;
        static Cpu cpu;
        static Ppu ppu;
        static MemoryMap nes;
        static Cartridge cart;
        static Joypad joy;
        static byte nSelectedPalette = 0x00;
        const int targetFPS = 60;
        const int frameDelay = 1000 / targetFPS;
        static FpsCounter fpsCounter = new FpsCounter();
        Stopwatch frameTimer = new Stopwatch();
        static Dictionary<ushort, string> mapAsm = new Dictionary<ushort, string>();

        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Responsável por realizar a renderização do frame na tela do SDL.
        /// </summary>
        static void Render()
        {
            SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
            SDL.SDL_RenderClear(renderer);

            fpsCounter.FrameRendered();
            SDL.SDL_SetWindowTitle(window, $"NesTalgia Emulator - FPS: {fpsCounter.CurrentFps}");

            // Pega a textura do frame do PPU
            IntPtr texture = ppu.GetScreenTexture();
            ppu.UpdateScreenTextureFromSprite();
            if (texture != IntPtr.Zero)
            {
                SDL.SDL_RenderSetLogicalSize(renderer, 256, 240);
                SDL.SDL_RenderCopy(renderer, texture, IntPtr.Zero, IntPtr.Zero);
            }

            #region LOGS DA CPU, PALETA DE CORES, FLAGS DA CPU, PATTERN TABLE, SPRITE TABLE
            // // Placeholder dos quadrados das Pattern Tables
            // int tileW = 128;
            // int tileH = 128;

            // SDL.SDL_Rect rect0 = new SDL.SDL_Rect { x = 520, y = 650, w = tileW, h = tileH };
            // SDL.SDL_Rect rect1 = new SDL.SDL_Rect { x = 660, y = 650, w = tileW, h = tileH };

            // // Fundo cinza para mostrar área reservada
            // SDL.SDL_SetRenderDrawColor(renderer, 60, 60, 60, 255);
            // SDL.SDL_RenderFillRect(renderer, ref rect0);
            // SDL.SDL_RenderFillRect(renderer, ref rect1);

            // // Bordas
            // SDL.SDL_SetRenderDrawColor(renderer, 200, 200, 200, 255);
            // SDL.SDL_RenderDrawRect(renderer, ref rect0);
            // SDL.SDL_RenderDrawRect(renderer, ref rect1);
            // // Títulos
            // DrawText("Pattern Table 0", rect0.x, rect0.y - 18);
            // DrawText("Pattern Table 1", rect1.x, rect1.y - 18);
            // // Atualizar texturas
            // ppu.UpdatePatternTableTexture(0, nSelectedPalette);
            // ppu.UpdatePatternTableTexture(1, nSelectedPalette);

            // // Renderizar
            // SDL.SDL_RenderCopy(renderer, ppu.texturePatternTable0, IntPtr.Zero, ref rect0);
            // SDL.SDL_RenderCopy(renderer, ppu.texturePatternTable1, IntPtr.Zero, ref rect1);

            // // --- STATUS e Flags ---
            // DrawText("STATUS:", 610, 10);
            // DrawFlag("N", (cpu.Status & (byte)Cpu.StatusFlags.N) != 0, 675, 10);
            // DrawFlag("V", (cpu.Status & (byte)Cpu.StatusFlags.V) != 0, 690, 10);
            // DrawFlag("U", (cpu.Status & (byte)Cpu.StatusFlags.U) != 0, 705, 10);
            // DrawFlag("B", (cpu.Status & (byte)Cpu.StatusFlags.B) != 0, 720, 10);
            // DrawFlag("D", (cpu.Status & (byte)Cpu.StatusFlags.D) != 0, 735, 10);
            // DrawFlag("I", (cpu.Status & (byte)Cpu.StatusFlags.I) != 0, 750, 10);
            // DrawFlag("Z", (cpu.Status & (byte)Cpu.StatusFlags.Z) != 0, 765, 10);
            // DrawFlag("C", (cpu.Status & (byte)Cpu.StatusFlags.C) != 0, 780, 10);

            // // --- Registradores ---
            // DrawText($"PC: ${cpu.PC:X4}", 520, 30);
            // DrawText($"A:  ${cpu.A:X2}", 520, 50);
            // DrawText($"X:  ${cpu.X:X2}", 520, 70);
            // DrawText($"Y:  ${cpu.Y:X2}", 520, 90);
            // DrawText($"SP: ${cpu.SP:X2}", 520, 110);

            // // --- Disassembly com destaque na instrução atual ---
            // DrawCode(520, 150, 26);
            #endregion

            // Renderiza isso tudo no render do SDL.
            SDL.SDL_RenderPresent(renderer);
        }

        /// <summary>
        /// Responsável por realizar o desenho das flags no SDL [Quando em modo Debug]
        /// </summary>
        /// <param name="name">Nome da flag</param>
        /// <param name="on">Se esta ligada ou não</param>
        /// <param name="x">Posição de desenho no eixo X</param>
        /// <param name="y">Posição de desenho no eixo Y</param>
        static void DrawFlag(string name, bool on, int x, int y)
        {
            // Verde se ligado, vermelho se desligado
            DrawText(name, x, y, on ? (byte)0 : (byte)255, on ? (byte)255 : (byte)0, 0);
        }

        /// <summary>
        /// Responsável por desenhar o texto na tela do SDL. [quando em modo Debug]
        /// </summary>
        /// <param name="text">Texto a ser desenhado na tela.</param>
        /// <param name="x">Posição X</param>
        /// <param name="y">Posição Y</param>
        /// <param name="r">Cor vermelha</param>
        /// <param name="g">Cor Verde</param>
        /// <param name="b">Cor azul</param>
        static void DrawText(string text, int x, int y, byte r = 255, byte g = 255, byte b = 255)
        {
            IntPtr surface = SDL_ttf.TTF_RenderText_Solid(font, text, new SDL.SDL_Color { r = r, g = g, b = b, a = 255 });
            IntPtr texture = SDL.SDL_CreateTextureFromSurface(renderer, surface);

            SDL.SDL_QueryTexture(texture, out _, out _, out int w, out int h);
            SDL.SDL_Rect dstRect = new SDL.SDL_Rect { x = x, y = y, w = w, h = h };
            SDL.SDL_RenderCopy(renderer, texture, IntPtr.Zero, ref dstRect);

            SDL.SDL_FreeSurface(surface);
            SDL.SDL_DestroyTexture(texture);
        }

        /// <summary>
        /// Responsável por desenhar o código de máquina em torno da instrução atual, relacionado a engenharia reversa do codigo da CPU que fiz. [Quando em modo debug]
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="nLines"></param>
        static void DrawCode(int x, int y, int nLines)
        {
            int midLine = nLines / 2;
            int lineHeight = 18;

            var keys = mapAsm.Keys.OrderBy(k => k).ToList();
            int pcIndex = keys.IndexOf(cpu.LastExecutedPC);

            // Se PC não encontrado no dicionário, desenha linha de erro e sai
            if (pcIndex == -1)
            {
                DrawText($"${cpu.PC:X4}: ???", x, y + midLine * lineHeight, 255, 0, 0);
                return;
            }

            // Desenha a instrução atual em ciano
            if (!mapAsm.TryGetValue(cpu.LastExecutedPC, out var inst))
                inst = $"${cpu.LastExecutedPC:X4}: ???";
            DrawText(inst, x, y + midLine * lineHeight, 0, 255, 255);

            // Desenha linhas abaixo da instrução atual
            for (int i = 1; i < nLines - midLine; i++)
            {
                int idx = pcIndex + i;
                if (idx >= keys.Count) break;

                ushort addr = keys[idx];
                if (!mapAsm.TryGetValue(addr, out var line))
                    line = $"${addr:X4}: ???";

                DrawText(line, x, y + (midLine + i) * lineHeight);
            }

            // Desenha linhas acima da instrução atual
            for (int i = 1; i <= midLine; i++)
            {
                int idx = pcIndex - i;
                if (idx < 0) break;

                ushort addr = keys[idx];
                if (!mapAsm.TryGetValue(addr, out var line))
                    line = $"${addr:X4}: ???";

                DrawText(line, x, y + (midLine - i) * lineHeight);
            }
        }

        /// <summary>
        /// Mapeamento de teclas do teclado para botões do joypad.
        /// </summary>
        Dictionary<SDL.SDL_Keycode, JoypadButton> keyMap = new()
        {
            [SDL.SDL_Keycode.SDLK_z] = JoypadButton.A,
            [SDL.SDL_Keycode.SDLK_x] = JoypadButton.B,
            [SDL.SDL_Keycode.SDLK_RETURN] = JoypadButton.Start,
            [SDL.SDL_Keycode.SDLK_RSHIFT] = JoypadButton.Select,
            [SDL.SDL_Keycode.SDLK_UP] = JoypadButton.Up,
            [SDL.SDL_Keycode.SDLK_DOWN] = JoypadButton.Down,
            [SDL.SDL_Keycode.SDLK_LEFT] = JoypadButton.Left,
            [SDL.SDL_Keycode.SDLK_RIGHT] = JoypadButton.Right,
        };

        /// <summary>
        /// Responsável por carregar a ROM do NES e iniciar o emulador.
        /// </summary>
        /// <param name="path">Caminho onde a ROM está.</param>
        private void LoadRom(string path)
        {
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            SDL_ttf.TTF_Init();

            // Inicnando a janela e o renderizador do SDL
            window = SDL.SDL_CreateWindow($"NESTalgia - FPS: {fpsCounter.CurrentFps}",
                SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED,
                800, 850, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);

            renderer = SDL.SDL_CreateRenderer(window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
            font = SDL_ttf.TTF_OpenFont("C:/Windows/Fonts/consola.ttf", 14);

            // Inicnando componentes do NES.
            ppu = new Ppu(renderer);
            joy = new Joypad();
            nes = new MemoryMap(ppu, joy);
            cart = new Cartridge(path);
            nes.InsertCartridge(cart);
            cpu = nes.cpu;
            nes.reset();

            // Iniciando o disassembly da CPU para debug
            mapAsm = cpu.Disassemble(0x0000, 0xFFFF);

            // Iniciando a emulação.
            const int clocksPerFrame = 341 * 262;
            while (isRunning)
            {
                uint frameStart = SDL.SDL_GetTicks();
                while (SDL.SDL_PollEvent(out SDL.SDL_Event evt) != 0)
                {
                    if (evt.type == SDL.SDL_EventType.SDL_QUIT)
                    {
                        isRunning = false;
                        break;
                    }
                    if (evt.type == SDL.SDL_EventType.SDL_KEYDOWN || evt.type == SDL.SDL_EventType.SDL_KEYUP)
                    {
                        bool pressed = evt.type == SDL.SDL_EventType.SDL_KEYDOWN;
                        if (keyMap.TryGetValue(evt.key.keysym.sym, out var btn)) joy.SetButtonState(0, btn, pressed);// controle 1
                        if (keyMap.TryGetValue(evt.key.keysym.sym, out var btn1)) joy.SetButtonState(1, btn1, pressed);// controle 2
                        switch ((SDL.SDL_Keycode)evt.key.keysym.sym)
                        {
                            case SDL.SDL_Keycode.SDLK_r:// Resetar o emulador de forma manual.
                                cpu.Reset();
                                mapAsm = cpu.Disassemble(0x0000, 0xFFFF);
                                break;
                        }
                    }
                }

                // Emulando clock do NES
                int clocksRun = 0;
                while (!ppu.frame_complete && clocksRun < clocksPerFrame)
                { 
                    nes.Clock();
                    clocksRun++;
                }

                // Se o frame estiver completo, renderiza na tela.
                if (ppu.frame_complete)
                {
                    ppu.frame_complete = false;
                    Render();
                }

                // Controla o FPS do emulador em 60fps;
                uint frameTime = SDL.SDL_GetTicks() - frameStart;
                if (frameTime < 16)
                    SDL.SDL_Delay(16 - frameTime);
            }

            // Limpa os recursos do SDL.
            SDL.SDL_DestroyRenderer(renderer);
            SDL.SDL_DestroyWindow(window);
            SDL_ttf.TTF_CloseFont(font);
            SDL_ttf.TTF_Quit();
            SDL.SDL_Quit();
        }

        // Abre o menu para abrir ROMs do NES.
        private void openRomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "NES Files (*.nes)|*.nes";
            openFileDialog.Title = "Abrir arquivo NES";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;

                // Rodar o emulador em outra thread pra não travar a UI
                System.Threading.Thread emuThread = new System.Threading.Thread(() => LoadRom(filePath));
                emuThread.IsBackground = true; // fecha junto com app
                emuThread.Start();
            }
        }
    }
}

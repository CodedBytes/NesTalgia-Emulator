using System;
using System.Diagnostics;
using SDL2;

namespace NesTalgia_EMU.Core.PPU
{
    /// <summary>
    /// REspons�vel por contar os frames por segundo (FPS) do emulador.
    /// </summary>
    public class FpsCounter
    {
        private int frameCount = 0;
        private double elapsedTime = 0;
        private Stopwatch stopwatch = new Stopwatch();
        public int CurrentFps { get; private set; } = 0;

        /// <summary>
        /// Constructor da classe FpsCounter, respons�vel por iniciar o cron�metro.
        /// </summary>
        public FpsCounter()
        {
            stopwatch.Start();
        }

        /// <summary>
        /// Respons�vel por registrar um frame renderizado.
        /// </summary>
        public void FrameRendered()
        {
            frameCount++;
            elapsedTime += stopwatch.Elapsed.TotalSeconds;
            stopwatch.Restart();

            if (elapsedTime >= 1.0)
            {
                CurrentFps = frameCount;
                frameCount = 0;
                elapsedTime = 0;
            }
        }
    }
}
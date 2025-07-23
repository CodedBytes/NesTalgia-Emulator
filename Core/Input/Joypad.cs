using System;

namespace NesTalgia_EMU.Core.Input
{
    /// <summary>
    /// Classe responsável pelos controles do NES.
    /// </summary>
    public class Joypad
    {
        private byte strobe;
        private byte[] buttonState = new byte[2];
        private byte[] shiftRegister = new byte[2];
        private int[] shiftCount = new int[2];

        /// <summary>
        /// Constructor da classe de controles do NES, atualmente apenas resetando os estados dos botões e registradores de shift.
        /// </summary>
        public Joypad()
        {
            Reset(); // Zera tudo no início
        }

        /// <summary>
        /// Responsável pela escrita de valores nos endereços de controle do NES.
        /// </summary>
        /// <param name="address">Endereço da escrita passada pela BUS</param>
        /// <param name="value">Valor passado pela BUS</param>
        public void Write(ushort address, byte value)
        {
            byte newStrobe = (byte)(value & 1);
            if ((value & 1) == 1)
            {
                for (int i = 0; i < 2; i++)
                {
                    shiftRegister[i] = buttonState[i];
                    shiftCount[i] = 0;
                }
            }

            strobe = newStrobe;
        }

        /// <summary>
        /// Responsável pela leitura dos endereços de controle do NES.
        /// </summary>
        /// <param name="address">Endreço vindo do BUS</param>
        /// <returns>Retorna o bite final do controle</returns>
        public byte Read(ushort address)
        {
            int player = (address == 0x4016) ? 0 : 1;
            byte result;

            if (strobe == 1) result = (byte)(buttonState[player] & 0x01);
            else if (shiftCount[player] < 8)
            {
                result = (byte)(shiftRegister[player] & 0x01);
                shiftRegister[player] >>= 1;
                shiftCount[player]++;
            }
            else result = 1;

            byte final = (byte)(result | 0x40);
            return final;
        }

        /// <summary>
        /// Responsável por definir o estado dos botões do controle.
        /// </summary>
        /// <param name="player">O player no qual o controle vai atuar</param>
        /// <param name="button">O botão a ser mudado o estado</param>
        /// <param name="pressed">Se esta pressionado ou não</param>
        public void SetButtonState(int player, JoypadButton button, bool pressed)
        {
            if (pressed) buttonState[player] |= (byte)(1 << (int)button);
            else buttonState[player] &= (byte)~(1 << (int)button);

            if ((strobe & 1) == 1)
            {
                shiftRegister[player] = buttonState[player];
                shiftCount[player] = 0;
            }
        }

        /// <summary>
        /// Realiza o reset dos controles, zerando os estados dos botões e registradores de shift.
        /// </summary>
        public void Reset()
        {
            strobe = 0;
            for (int i = 0; i < 2; i++)
            {
                buttonState[i] = 0;
                shiftRegister[i] = 0;
                shiftCount[i] = 0;
            }
        }
    }

    /// <summary>
    /// Enumeração dos botões do Joypad do NES.
    /// </summary>
    public enum JoypadButton : int
    {
        A = 0,
        B = 1,
        Select = 2,
        Start = 3,
        Up = 4,
        Down = 5,
        Left = 6,
        Right = 7
    }
}

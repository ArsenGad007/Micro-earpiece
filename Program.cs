using NAudio.Wave;

using static Micro_earpiece.Config;
using static System.Console;

namespace Micro_earpiece
{
    internal class Program
    {
        static void Main()
        {
            Funcs.InitSettings();

            var waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(SampleRate, 1),
                BufferMilliseconds = (int)((double)BufferSize / SampleRate * 1000.0)
            };

            waveIn.DataAvailable += Funcs.OnDataAvailable;
            waveIn.StartRecording();

            WriteLine("Слушаю микрофон... Нажми Enter для выхода.");
            ReadLine();

            waveIn.StopRecording();
            waveIn.Dispose();
        }
    }
}



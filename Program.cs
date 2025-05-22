using static Micro_earpiece.Config;
using static Micro_earpiece.Funcs;
using static System.Console;

using NAudio.Wave;

namespace Micro_earpiece
{
    internal class Program
    {
        static void Main()
        {
            CheckAudioFiles();

            var waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(SampleRate, 1),
                BufferMilliseconds = (int)((double)BufferSize / SampleRate * 1000.0)
            };

            waveIn.DataAvailable += OnDataAvailable;
            waveIn.StartRecording();

            WriteLine("Слушаю микрофон... Нажми Enter для выхода.");
            ReadLine();

            waveIn.StopRecording();
            waveIn.Dispose();
        }
    }
}



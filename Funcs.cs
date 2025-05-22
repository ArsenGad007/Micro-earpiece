using static Micro_earpiece.Config;
using static System.Console;

using MathNet.Numerics.IntegralTransforms;
using System.Numerics;
using NAudio.Wave;

namespace Micro_earpiece
{
    class Funcs
    {
        /// <summary>
        /// Путь к главной папке, где хранятся аудиофайлы и подпапки
        /// </summary>
        private static string audioFolder;
    
        /// <summary>
        /// Список флагов, хранящий результаты последних проверок на наличие писка.
        /// </summary>
        private static List<bool> analyze_beeps = [];

        /// <summary>
        /// Список всех аудиофайлов в текущей директории
        /// </summary>
        private static List<string> audioFiles = [];

        /// <summary>
        /// Флаг на воспроизведение аудио
        /// </summary>
        private static bool isPlaying = false;

        /// <summary>
        /// Проверяет правильность названий всех папок
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public static void CheckAudioFiles()
        {
            audioFolder = Path.Combine(
                Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName,
                MainFold);

            string[] folders = Directory.GetDirectories(audioFolder, "*", SearchOption.AllDirectories);
            foreach (string folder in folders)
            {
                var name = Path.GetFileName(folder);
                string[] arr = Directory.GetFiles(Directory.GetParent(folder).ToString());
                if (!arr.Any(x => Path.GetFileName(x)[..^4] == name))
                    throw new ArgumentException("Неверно выстроены пути аудиофайлов");
            }
        }

        /// <summary>
        /// Первоначальные настройки
        /// </summary>
        public static void InitSettings()
        {
            string[] formats = { "*.mp3", "*.wav", "*.m4a" };
            // Сохраняет все аудиозаписи в список
            foreach (string format in formats)
            {
                string[] found = Directory.GetFiles(audioFolder, format);
                audioFiles.AddRange(found);
            }
            AudioPlay($"{audioFolder}/3) Необходимое условие сходимости ряда.m4a");
        }

        /// <summary>
        /// Считывает Гц микрофона и обрабатывает её
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            // Преобразуем байты в сэмплы
            int samples = e.BytesRecorded / 2;
            var buffer = new float[BufferSize];
            for (int i = 0; i < samples && i < BufferSize; i++)
            {
                short sample = BitConverter.ToInt16(e.Buffer, i * 2);
                buffer[i] = sample / 32768f; // Нормализуем
            }

            // Преобразуем в комплексный массив
            Complex[] fftBuffer = new Complex[BufferSize];
            for (int i = 0; i < BufferSize; i++)
                fftBuffer[i] = new Complex(buffer[i], 0);

            // Выполняем FFT
            Fourier.Forward(fftBuffer, FourierOptions.Matlab);

            // Поиск частоты с максимальной амплитудой
            double maxMag = 0;
            int maxIndex = 0;

            for (int i = 0; i < fftBuffer.Length / 2; i++)
            {
                double magnitude = fftBuffer[i].Magnitude;
                if (magnitude > maxMag)
                {
                    maxMag = magnitude;
                    maxIndex = i;
                }
            }

            double freq = (double)maxIndex * SampleRate / BufferSize;
            WriteLine($"Частота: {freq:f1} Гц");

            // Проверка на писк
            if (freq > BeepHz - RangeBeepHz && freq < BeepHz + RangeBeepHz)
                analyze_beeps.Add(true);
            else
                analyze_beeps.Add(false);

            // Проверка писка на длительность cnt_analyse_beeps
            if (analyze_beeps.Count >= cnt_analyse_beeps)
            {
                if (analyze_beeps.All(x => x))
                    BeepDetected();
                analyze_beeps.Clear();
            }

            if (isPlaying) return;
        }

        /// <summary>
        /// Вызывается при уверенном обнаружении писка заданной частоты
        /// </summary>
        private static void BeepDetected()
        {
            WriteLine("Обнаружен писк!");
        }

        /// <summary>
        /// Воспроизведение аудиозаписи
        /// </summary>
        /// <param name="fname"></param>
        private static async void AudioPlay(string fname)
        {
            using (var audioFile = new AudioFileReader(fname))
            using (var outputDevice = new WaveOutEvent())
            {
                outputDevice.Init(audioFile);
                outputDevice.Play();

                isPlaying = true;

                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    await Task.Delay(1000);
                }

                isPlaying = false;
            }
        }
    }
}

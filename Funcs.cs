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
        /// Путь к папке, откуда берутся аудиофайлы
        /// </summary>
        private static string audioFolder;
    
        /// <summary>
        /// Список флагов, хранящий результаты последних проверок на наличие писка.
        /// </summary>
        private static List<bool> analyzeBeeps = [];

        /// <summary>
        /// Список всех путей аудиофайлов в текущей директории
        /// </summary>
        private static List<string> audioFilesPath = [];

        /// <summary>
        /// Текущий путь к аудиофайлу
        /// </summary>
        public static string curlFilePath;

        /// <summary>
        /// Флаг на воспроизведение аудио
        /// </summary>
        private static bool isPlaying = false;

        /// <summary>
        /// Токен чтобы прервать поток AudioPlay
        /// </summary>
        private static CancellationTokenSource cancelToken;

        /// <summary>
        /// Первоначальные настройки
        /// </summary>
        public static void InitSettings()
        {
            audioFolder = Path.Combine(
                Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName,
                MainFold);

            CheckAudioFiles();

            string[] formats = { "*.mp3", "*.wav", "*.m4a" };
            // Сохраняет все аудиозаписи в список
            foreach (string format in formats)
            {
                string[] found = Directory.GetFiles(audioFolder, format);
                audioFilesPath.AddRange(found);
            }

            curlFilePath = audioFilesPath[0];
        }

        /// <summary>
        /// Проверяет правильность названий всех папок
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public static void CheckAudioFiles()
        {
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
                analyzeBeeps.Add(true);
            else
                analyzeBeeps.Add(false);

            // Проверка писка на длительность cnt_analyse_beeps
            if (analyzeBeeps.Count >= CountValidityBeeps)
            {
                if (analyzeBeeps.All(x => x))
                    BeepDetected();
                analyzeBeeps.Clear();
            }

            if (isPlaying) return;

            AudioPlay(curlFilePath);
        }

        /// <summary>
        /// Вызывается при уверенном обнаружении писка заданной частоты
        /// </summary>
        private static void BeepDetected()
        {
            WriteLine("Обнаружен писк!");

            string check = Path.Combine(audioFolder, Path.GetFileNameWithoutExtension(curlFilePath));

            if (!Directory.Exists(check))
                return;

            audioFolder = check;
           
            string[] formats = { "*.mp3", "*.wav", "*.m4a" };
            audioFilesPath = [];
            // Сохраняет все аудиозаписи в список
            foreach (string format in formats)
            {
                string[] found = Directory.GetFiles(audioFolder, format);
                audioFilesPath.AddRange(found);
            }

            curlFilePath = audioFilesPath[0];
            AudioStop();
        }

        /// <summary>
        /// Воспроизведение аудиозаписи
        /// </summary>
        /// <param name="fname"></param>
        public static async void AudioPlay(string path)
        {
            cancelToken = new CancellationTokenSource();
            isPlaying = true;

            using (var audioFile = new AudioFileReader(path))
            using (var outputDevice = new WaveOutEvent())
            {
                outputDevice.Init(audioFile);
                outputDevice.Play();

                try
                {
                    while (outputDevice.PlaybackState == PlaybackState.Playing)
                    {
                        // Проверяем отмену
                        cancelToken.Token.ThrowIfCancellationRequested();
                        await Task.Delay(1000, cancelToken.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Останавливаем воспроизведение
                    outputDevice.Stop();             
                }
            }

            isPlaying = false;

            NextAudio();
        }

        /// <summary>
        /// Переключает на следующий аудиофайл
        /// </summary>
        private static void NextAudio()
        {
            int curlInd = audioFilesPath.IndexOf(curlFilePath);

            if (curlInd == audioFilesPath.Count - 1)
                curlFilePath = audioFilesPath[0];
            else
                curlFilePath = audioFilesPath[curlInd + 1];
        }

        /// <summary>
        /// Останавливает текущую аудиозапись
        /// </summary>
        public static void AudioStop()
        {   
            cancelToken.Cancel();
        }
    }
}

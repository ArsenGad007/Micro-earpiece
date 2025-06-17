using MathNet.Numerics.IntegralTransforms;
using System.Numerics;
using NAudio.Wave;

using static Micro_earpiece.Config;
using Micro_earpiece_solo;

namespace Micro_earpiece
{
    /// <summary>
    /// Хранит основные функции для работы с микронаушником
    /// </summary>
    class Funcs
    {
        #region Инициализация полей
        
        /// <summary>
        /// Путь к папке, откуда берутся аудиофайлы
        /// </summary>
        private static string folderPath = "";
    
        /// <summary>
        /// Список всех путей аудиофайлов в текущей директории
        /// </summary>
        private static List<string> audioPaths = [];

        /// <summary>
        /// Текущий путь к аудиофайлу   
        /// </summary>
        public static string curlAudioPath = "";

        /// <summary>
        /// Список флагов, хранящий результаты последних проверок на наличие писка.
        /// </summary>
        private static List<bool> analyzeBeeps = [];

        /// <summary>
        /// Флаг на воспроизведение аудио
        /// </summary>
        private static bool isPlaying = false;

        /// <summary>
        /// Задержка после обнаружения писка
        /// </summary>
        private static DateTime beepDelay = DateTime.Now;

        /// <summary>
        /// Токен чтобы прервать поток AudioPlay
        /// </summary>
        private static CancellationTokenSource cancelToken;

        #endregion

        /// <summary>
        /// Первоначальные настройки
        /// </summary>
        public static void InitSettings()
        {
            folderPath = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, MainFold);

            CheckAudioFiles();

            audioPaths = [];
            foreach (string format in Formats)
            {
                string[] found = Directory.GetFiles(folderPath, format);
                audioPaths.AddRange(found);
            }

            curlAudioPath = audioPaths[0];
        }

        /// <summary>
        /// Проверяет правильность названий всех папок
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        private static void CheckAudioFiles()
        {
            string[] folders = Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories);
            foreach (string folder in folders)
            {
                var name = Path.GetFileName(folder);
                string[] arr = Directory.GetFiles(Directory.GetParent(folder).ToString());
                if (!arr.Any(x => Path.GetFileNameWithoutExtension(x) == name))
                    Logging.ErrorWriteLog(new ArgumentException("Неверно выстроены пути аудиофайлов"));
            }
        }

        /// <summary>
        /// Считывает звук микрофона и обрабатывает его
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            /// Преобразуем байты в сэмплы
            int samples = e.BytesRecorded / 2;
            var buffer = new float[BufferSize];
            for (int i = 0; i < samples && i < BufferSize; i++)
            {
                short sample = BitConverter.ToInt16(e.Buffer, i * 2);
                buffer[i] = sample / 32768f; // Нормализуем
            }

            /// Преобразуем в комплексный массив
            Complex[] fftBuffer = new Complex[BufferSize];
            for (int i = 0; i < BufferSize; i++)
                fftBuffer[i] = new Complex(buffer[i], 0);

            /// Выполняем FFT
            Fourier.Forward(fftBuffer, FourierOptions.Matlab);

            /// Поиск частоты с максимальной амплитудой
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
            Console.WriteLine($"Частота: {freq:f1} Гц");

            /// Проверка на писк
            if (freq > BeepHz - RangeBeepHz && freq < BeepHz + RangeBeepHz)
                analyzeBeeps.Add(true);
            else
                analyzeBeeps.Add(false);

            /// Проверка писка на длительность cnt_analyse_beeps
            if (analyzeBeeps.Count >= CountValidityBeeps)
            {
                if (analyzeBeeps.All(x => x))
                    BeepDetected();
                analyzeBeeps.Clear();
            }

            if (isPlaying) return;

            AudioPlay(curlAudioPath);
        }

        /// <summary>
        /// Вызывается при уверенном обнаружении писка заданной частоты
        /// </summary>
        private static void BeepDetected()
        {
            Logging.WriteLog("Обнаружен писк!");

            if (DateTime.Now < beepDelay)
            {
                Logging.WriteLog("Задержка");
                return;
            }

            beepDelay = DateTime.Now.AddSeconds(2);

            string check = Path.Combine(folderPath, Path.GetFileNameWithoutExtension(curlAudioPath));
            // WriteLine($"Check: {check},\nfolderPath: {folderPath}");
            if (!Directory.Exists(check))
            {
                // WriteLine("Flag1");
                if (folderPath != Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, MainFold))
                {
                    InitSettings();
                    AudioStop();
                    // WriteLine("Flag2");
                }

                return;
            }

            folderPath = check;
           
            audioPaths = [];
            foreach (string format in Formats)
            {
                string[] found = Directory.GetFiles(folderPath, format);
                audioPaths.AddRange(found);
            }

            curlAudioPath = audioPaths[0];
            AudioStop();
        }

        /// <summary>
        /// Воспроизведение аудиозаписи
        /// </summary>
        /// <param name="fname"></param>
        private static async void AudioPlay(string path)
        {
            Logging.WriteLog($"Воспроизводиться: {path}");

            cancelToken = new CancellationTokenSource();
            isPlaying = true;

            using (var audioFile = new AudioFileReader(path))
            using (var outputDevice = new WaveOutEvent())
            {
                outputDevice.Init(audioFile);
                outputDevice.Play();

                try
                {
                    /// Проверяем отмену воспроизведения аудиозаписи
                    while (outputDevice.PlaybackState == PlaybackState.Playing)
                        await Task.Delay(1000, cancelToken.Token);
                }
                catch (OperationCanceledException)
                {
                    /// Останавливаем воспроизведение
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
            if (audioPaths == new List<string>())
                Logging.ErrorWriteLog(new ArgumentException("Список audioPaths - пустой"));
                
            int curlInd = audioPaths.IndexOf(curlAudioPath);

            if (curlInd == audioPaths.Count - 1)
                curlAudioPath = audioPaths[0];
            else
                curlAudioPath = audioPaths[curlInd + 1];
        }

        /// <summary>
        /// Останавливает текущую аудиозапись
        /// </summary>
        private static void AudioStop()
        {
            if (cancelToken == null)
                throw new ArgumentException("Нет ссылки cancelToken");
            cancelToken.Cancel();
        }
    }
}

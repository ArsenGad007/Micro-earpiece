namespace Micro_earpiece
{
    class Config
    {
        /// <summary>
        /// Частота дискретизации
        /// </summary>
        public const int SampleRate = 44100;

        /// <summary>
        /// Размер буфера
        /// </summary>
        public const int BufferSize = 2048;

        /// <summary>
        /// Опорная частота в герцах, используемая для обнаружения писка
        /// </summary>
        public const int BeepHz = 1119;

        /// <summary>
        /// Допустимое отклонение частоты писка от опорной (в герцах)
        /// </summary>
        public const int RangeBeepHz = 100;

        /// <summary>
        /// Количество последовательных обнаружений писка для подтверждения его валидности
        /// </summary>
        public const int CountValidityBeeps = 15;

        /// <summary>
        /// Название главной папки, где хранятся аудиофайлы и подпапки
        /// </summary>
        public const string MainFold = "AudioFiles";

        /// <summary>
        /// Доступные форматы аудиофайлов
        /// </summary>
        public static readonly string[] Formats = { "*.mp3", "*.wav", "*.m4a" };

        /// <summary>
        /// Создаёт конфиг файл
        /// </summary>
        /// <param name="fname"></param>
        public static void CreateConfigFile(string fname)
        {
            string path_config = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, fname);

            if (File.Exists(path_config))
                return;

            using (StreamWriter sw = new StreamWriter(File.Create(path_config)))
            {
                sw.WriteLine($"SampleRate = {SampleRate}");
                sw.WriteLine($"BufferSize = {BufferSize}");
                sw.WriteLine($"BeepHz = {BeepHz}");
                sw.WriteLine($"RangeBeepHz = {RangeBeepHz}");
                sw.WriteLine($"CountValidityBeeps = {CountValidityBeeps}");
                sw.WriteLine($"MainFold = {MainFold}");
            }
        }

        /// <summary>
        /// Читает конфиг файл
        /// </summary>
        public static void ReadConfig()
        {

        }
    }
}

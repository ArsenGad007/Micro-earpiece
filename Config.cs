namespace Micro_earpiece
{
    class Config
    {
        /// <summary>
        /// Частота дискретизации
        /// </summary>
        public static int SampleRate { get; private set; } = 44100;

        /// <summary>
        /// Размер буфера
        /// </summary>
        public static int BufferSize { get; private set; } = 2048;

        /// <summary>
        /// Опорная частота в герцах, используемая для обнаружения писка
        /// </summary>
        public static int BeepHz { get; private set; } = 1119;

        /// <summary>
        /// Допустимое отклонение частоты писка от опорной (в герцах)
        /// </summary>
        public static int RangeBeepHz { get; private set; } = 100;

        /// <summary>
        /// Количество последовательных обнаружений писка для подтверждения его валидности
        /// </summary>
        public static int CountValidityBeeps { get; private set; } = 15;

        /// <summary>
        /// Название главной папки, где хранятся аудиофайлы и подпапки
        /// </summary>
        public static string MainFold { get; private set; } = "AudioFiles";

        /// <summary>
        /// Доступные форматы аудиофайлов
        /// </summary>
        public static string[] Formats { get; } = { "*.mp3", "*.wav", "*.m4a" };

        /// <summary>
        /// Путь к конфиг файлу
        /// </summary>
        private static string pathConfig = "";

        /// <summary>
        /// Читает конфиг файл
        /// </summary>
        public static void ReadConfig()
        {
            if (!File.Exists(pathConfig))
                CreateConfig("config.txt");

            string[][] read = File.ReadAllLines(pathConfig)
                .Select(line => line.Split(new char[] { ' ', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToArray())
                .ToArray();

            if (read.Length != 6)
            {
                PrintError();
                return;
            }

            string[] settings = { "SampleRate", "BufferSize", "BeepHz", "RangeBeepHz", "CountValidityBeeps", "MainFold" };

            for (int i = 0; i < read.Length; i++)
                if (read[i][0] != settings[i] || read[i].Length != 3)
                {
                    PrintError();
                    return;
                }

            if (int.TryParse(read[0][2], out int sr) &&
                int.TryParse(read[1][2], out int bs) &&
                int.TryParse(read[2][2], out int bh) &&
                int.TryParse(read[3][2], out int rbh) &&
                int.TryParse(read[4][2], out int cvb))
            {
                SampleRate = sr;
                BufferSize = bs;
                BeepHz = bh;
                RangeBeepHz = rbh;
                CountValidityBeeps = cvb;
            }
            else
            {
                PrintError();
                return;
            }

            MainFold = read[5][2];
        }

        /// <summary>
        /// Создаёт конфиг файл
        /// </summary>
        /// <param name="fname"></param>
        private static void CreateConfig(string fname)
        {
            pathConfig = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, fname);

            if (File.Exists(pathConfig))
                return;

            using (StreamWriter sw = new StreamWriter(File.Create(pathConfig)))
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
        /// Печать ошибки
        /// </summary>
        private static void PrintError() => Console.WriteLine("Ошибка прочтения конфиг файла. Используются заводские настройки.");
    }
}

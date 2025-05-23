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
    }
}

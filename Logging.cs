
namespace Micro_earpiece_solo
{
    class Logging
    {
        /// <summary>
        /// Логирование типа Message
        /// </summary>
        /// <param name="logMessage"></param>
        public static void WriteLog(string logMessage)
        {
            using (var sw = new StreamWriter("log.txt", true))
                sw.WriteLine($"{DateTime.Now} Message: {logMessage}");
            Console.WriteLine($"{DateTime.Now} Message: {logMessage}");
        }

        /// <summary>
        /// Логирование типа Error
        /// </summary>
        /// <param name="logError"></param>
        public static void ErrorWriteLog(string logError)
        {
            using (var sw = new StreamWriter("log.txt", true))
                sw.WriteLine($"{DateTime.Now} Error: {logError}");
            Console.WriteLine($"{DateTime.Now} Error: {logError}");
        }

        /// <summary>
        /// Логирование типа Error с выбросом исключения
        /// </summary>
        /// <param name="logMessage"></param>
        public static void ErrorWriteLog(Exception e)
        {
            ErrorWriteLog(e.Message);
            throw e;
        }
    }
}

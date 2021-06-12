using System.ServiceProcess;

namespace Server
{

    static class Program
    {

        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        static void Main ()
        {
            ServiceBase [] ServicesToRun;

            // Инициализация массива базовых классов "ServicesToRun" (можно запускать нескольких
            // служб).
            ServicesToRun = new ServiceBase []
            {
                new Server ()
            };

            // Регистрация исполняемого файла "ServicesToRun" в "Службе" с помощью "SCM".
            ServiceBase.Run (ServicesToRun);
        }
    }
}

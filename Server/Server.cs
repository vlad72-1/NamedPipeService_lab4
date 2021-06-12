using System;
using System.IO;
using System.IO.Pipes;
using System.ServiceProcess;
using System.Text;
using System.Runtime.InteropServices;                                                  // Требуется для классов "StructLayout", "LayoutKind", "DllImport" и "SetLastError". //
using System.Threading.Tasks;

namespace Server
{

    // Перечисление ServiceState необходимо для инициализации объекта "dwCurrentState" (в
    // структуре "ServiceStatus").
    public enum ServiceState
    {
        SERVICE_STOPPED = 0x00000001,                                                  // 0x00000001 = 0x1 - код присваиваемый, каждой операции (присваиваются по умолчанию). //
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007,
    }

    // Структура "ServiceStatus" определяет состояние службы.
    [StructLayout (LayoutKind.Sequential)]                                             // "Sequential" - поля структуры располагаются последовательно (в том порядке, в котором они экспортируются в память). //
    public struct ServiceStatus
    {
        public int dwServiceType;                                                      // Используется самой службой. //
        public ServiceState dwCurrentState;                                            // Объявление перечисления ServiceState. //
        public int dwControlsAccepted;                                                 // Используется самой службой. //
        public int dwWin32ExitCode;                                                    // Используется самой службой. //
        public int dwServiceSpecificExitCode;                                          // Используется самой службой. //
        public int dwCheckPoint;                                                       // Используется самой службой. Контрольная точка (служба его периодически увеличивает на +1, чтобы сообщить о своем прогрессе во время длительного запуска, остановки, паузы или продолжения работы). //
        public int dwWaitHint;                                                         // Пауза (в милисекундах, по умолчанию 30 сек). Дается для того чтобы служба выполнила другую оперцию (отложенного запуска, остановки, приостановки или продолжения). Если dwCheckPoint в этот период не увеличится, то ОС считает, что произошла ошибка и закроет службу. //
    };

    public partial class Server : ServiceBase
    {

        private Task serverTask;                                                       // Объявление асинхронного объекта "serverTask". //
        private bool work;                                                             // Флаг работы программы. //

        // Прототип функции "SetServiceStatus".
        [DllImport ("advapi32.dll", SetLastError = true)]                              // "advapi32.dll" - Win32 API. Изменение статуса службы в "Диспетчере задач". //
        private static extern bool SetServiceStatus (IntPtr handle, ref ServiceStatus serviceStatus);     // Статус определяется с помощью структуры "ServiceStatus". //

        // Инициализация конструктора по умолчанию.
        public Server ()
        {
            InitializeComponent ();
        }

        // Основной блок программы.
        private void DoWork ()
        {
            try
            {
                while (work)
                {
                    using (NamedPipeServerStream serverStream = new NamedPipeServerStream ("pipe", PipeDirection.In))
                    {
                        serverStream.WaitForConnection();
                        byte[] clientData = new byte[1024 * 5000];
                        int receivedBytesLen = serverStream.Read(clientData, 0, clientData.Length);
                        int fileNameLen = BitConverter.ToInt32(clientData, 0);
                        string fileName = Encoding.ASCII.GetString(clientData, 4, fileNameLen);
                        fileName = Path.GetFileName(fileName);
                        BinaryWriter bWrite = new BinaryWriter(File.Open(@"C:\111\" + fileName, FileMode.Create)); 
                        bWrite.Write(clientData, 4 + fileNameLen, receivedBytesLen - 4 - fileNameLen);
                        bWrite.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine (ex.ToString ());
            }
        }

        // Запуск. Точка входа в службу.
        protected override void OnStart (string [] args)
        {
            // Механизм опроса (запуск службы).
            ServiceStatus serviceStatus = new ServiceStatus ();

            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;         // Полю "dwCurrentState" присваивается код 0x2 (ожидание запуска). //
            serviceStatus.dwWaitHint = 1000;                                           // Пауза (по умолчанию 100000 милисекунд). //
            work = true;
            serverTask = Task.Run (() => DoWork ());                                   // Объект "serverTask" вызывает функцию "DoWork". //

            // Механизм опроса (служба запущена).
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;               // Полю "dwCurrentState" присваивается код 0x4. //
            SetServiceStatus (ServiceHandle, ref serviceStatus);                       // Передача статуса службы (работает) в Win32 API ("advapi32.dll"). //
        }

        // Остановка.
        protected override void OnStop ()
        {
            // Механизм опроса (служба останавливается).
            ServiceStatus serviceStatus = new ServiceStatus ();

            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;          // Полю "dwCurrentState" присваивается код 0x3 (ожидание остановки). //
            serviceStatus.dwWaitHint = 1000;                                           // Пауза (по умолчанию 100000 милисекунд). //

            SetServiceStatus (ServiceHandle, ref serviceStatus);                       // Передача статуса службы (ожидание остановки) в Win32 API. //
            work = false;
            serverTask.Wait ();                                                        // Приостановка объекта "serverTask". //

            // Механизм опроса (служба остановлена).
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;               // Полю "dwCurrentState" присваивается код 0x1 (остановлен). //
            SetServiceStatus (ServiceHandle, ref serviceStatus);                       // Передача статуса службы (остановлена) в Win32 API. //
        }

        // Возобновление после приостановки.
        protected override void OnContinue ()
        {
            OnStart (new string [0]);
        }
    }
}

using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Windows.Forms;

namespace Client
{

    public partial class Form1 : Form
    {

        OpenFileDialog openFile;

        // Конструктор по умолчанию.
        public Form1 ()
        {
            InitializeComponent ();                                  // Инициализация конструктора по умолчанию. Обязательная функция. //

            button2.Enabled = false;                                 // Выключение кнопки "Отправить" чтобы пользователь вначале выбрал файл для отправки. //

            openFile = new OpenFileDialog ();                        // Окно открытия файла. //

            button1.Click += button1_Click;                          // Инициализация кнопки "Выбрать файл" (определения события при нажатии пользователем на эту кнопку). //
            button2.Click += button2_Click;                          // Инициализация кнопки "Отправить файл" (определения события при нажатии пользователем на эту кнопку). //
        }

        private void button1_Click (object sender, EventArgs e)      // Обработчик события кнопки "Выбрать файл". //
        {
            openFile.Filter = "Text|*.txt|Pdf|*.pdf|All|*.*";
            if (openFile.ShowDialog () == DialogResult.Cancel)       // Обработчик события кнопки "Cancel". //
                return;

            button1.Enabled = false;                                 // Выключение кнопки "Выбрать файл" после выбора файла. //
            button2.Enabled = true;                                 // Включение кнопки "Отправить файл" после выбора файла. //
        }

        private void button2_Click (object sender, EventArgs e)      // Обработчик события кнопки "Отправить файл". //
        {
            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "pipe", PipeDirection.Out))
            {
                pipeClient.Connect();

                byte[] fileNameByte = Encoding.ASCII.GetBytes(openFile.FileName);
                byte[] fileNameLen = BitConverter.GetBytes(fileNameByte.Length);
                byte[] fileData = File.ReadAllBytes(openFile.FileName);
                byte[] clientData = new byte[4 + fileNameByte.Length + fileData.Length];

                fileNameLen.CopyTo(clientData, 0);
                fileNameByte.CopyTo(clientData, 4);
                fileData.CopyTo(clientData, 4 + fileNameByte.Length);

                pipeClient.Write(clientData, 0, clientData.Length);
            }
            button2.Enabled = false;                                 // Выключение кнопки "Отправить файл" после отправки файла. //
        }

        private void button1_Click_1(object sender, EventArgs e)
        {

        }
    }
}

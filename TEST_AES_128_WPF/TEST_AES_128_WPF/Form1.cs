using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Security.Cryptography;

namespace TEST_AES_128_WPF
{
    public partial class Form1 : Form
    {
        string path = "none";
        string size = "none";
        int fileSize = 0;
        int fw_size = 0;
        List<byte> list = new List<byte>();

        public byte[] WBF_encrypt;

        List<byte> data_bin = new List<byte>();

        public byte[] ReadFile;
        public delegate void MyDelegate();      //Для доступа к элементам из другого потока с передачей параметров

        static BackgroundWorker File_Open;

        static byte[] AES_FW_KEY = { 0xB4, 0xEF, 0x74, 0x56, 0x7A, 0x87, 0xC0, 0xF2, 0x5A, 0x8F, 0xE5, 0x4A, 0x88, 0xD6, 0x1C, 0x2C };
        static byte[] AES_IV = { 0xA6, 0x52, 0xB6, 0xDC, 0x92, 0x22, 0x15, 0x07, 0x25, 0x8B, 0x76, 0x14, 0x67, 0x80, 0x7D, 0xA7 };

        public Form1()
        {
            InitializeComponent();

            File_Open = new BackgroundWorker();
            File_Open.WorkerReportsProgress = true;
            File_Open.DoWork += File_Open_DoWork;
            File_Open.ProgressChanged += File_Open_ProgressChanged;
            File_Open.RunWorkerCompleted += File_Open_RunWorkerCompleted;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.Text += "Выберите файл прошивки - .bin." + Environment.NewLine;
        }

        void Push()     
        {
            read_binary_file();
            progressBar1.Minimum = 0;
            progressBar1.Maximum = fileSize;
            File_Open.RunWorkerAsync(null);
        }
        void read_binary_file()     
        {
            path = Open_dialog();
            if (path != "none")
            {
                fileSize = (int)new FileInfo(path).Length;
                fw_size = (fileSize * 2);

                if (fileSize < 1024)
                {
                    size = fileSize.ToString() + "B";
                }
                else
                {
                    size = (fileSize / 1024).ToString() + "Kb";
                }
                textBox1.Clear();
                TIME();
                OPEN_BinaryFile();
            }
        }

        string Open_dialog()
        {
            string filename = "none";
            openFileDialog1.Filter = "bin files (*.bin)|*.bin";
            openFileDialog1.FileName = "Firmware";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                filename = openFileDialog1.FileName;
            }
            return filename;
        }
        void File_Open_DoWork(object sender, DoWorkEventArgs e)
        {
            byte ReadByte = 0;
            int Progress = 0;

            try
            {
                using (BinaryReader firmware = new BinaryReader(File.Open(path, FileMode.Open)))
                {
                    while (firmware.BaseStream.Position != firmware.BaseStream.Length)
                    {
                        ReadByte = firmware.ReadByte();
                        list.Add(ReadByte);
                        File_Open.ReportProgress(Progress++);
                    }

                    ReadFile = new byte[list.Count];
                    list.CopyTo(ReadFile);
                    System.Threading.Thread.Sleep(1000);
                }
            }
            catch (IOException)
            {
                DialogResult result;
                System.Threading.Thread.Sleep(500);
                result = MessageBox.Show("Файл занят другим приложением," + Environment.NewLine + "закройте все приложения ипользующие файл" + Environment.NewLine +
                                         "и повторите попытку.", "Ошибка открытия файла", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (result == DialogResult.OK)
                {
                    BeginInvoke(new MyDelegate(Close_WPF));
                }
            }
            BeginInvoke(new MyDelegate(TIME));
            e.Result = "Шифрование - завершено...";
        }

        void File_Open_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
                textBox1.Text += "Interrupted by user" + Environment.NewLine;
            else if (e.Error != null)
                textBox1.Text += "Interrupted" + Environment.NewLine;
            else
            {
                textBox1.Text += e.Result + Environment.NewLine;
            }
            progressBar1.Value = 0;
            write_binary_file();
        }

        void File_Open_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        void write_binary_file()
        {
            WBF_encrypt = encryptdata(ReadFile, AES_FW_KEY, AES_IV);

            path = Save_dialog();

            Thread WBF = new Thread(new ThreadStart(WBFile));           //Создаем новый объект потока (Thread)
            WBF.IsBackground = true;                                    //Поток является фоновым
            WBF.Start();                                                //запускаем поток
        }

        void WBFile()
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.OpenOrCreate)))
            {
                foreach (byte b in WBF_encrypt)
                {
                    writer.Write(b);
                }
            }
            if (path != "none")
            {
                BeginInvoke(new MyDelegate(TIME));
                BeginInvoke(new MyDelegate(SAVE_WBinaryFile));
            }
        }
        //Code to encrypt Data :   
        public byte[] encryptdata(byte[] bytearraytoencrypt, byte[] key, byte[] iv)
        {
            AesCryptoServiceProvider dataencrypt = new AesCryptoServiceProvider();
            //Block size : Gets or sets the block size, in bits, of the cryptographic operation.  
            dataencrypt.BlockSize = 128;
            //KeySize: Gets or sets the size, in bits, of the secret key  
            dataencrypt.KeySize = 128;
            //Key: Gets or sets the symmetric key that is used for encryption and decryption.  
            dataencrypt.Key = key;//System.Text.Encoding.UTF8.GetBytes(key);
            //IV : Gets or sets the initialization vector (IV) for the symmetric algorithm  
            dataencrypt.IV = iv;//System.Text.Encoding.UTF8.GetBytes(iv);
            //Padding: Gets or sets the padding mode used in the symmetric algorithm  
            dataencrypt.Padding = PaddingMode.Zeros;
            //Mode: Gets or sets the mode for operation of the symmetric algorithm  
            dataencrypt.Mode = CipherMode.CBC;
            //Creates a symmetric AES encryptor object using the current key and initialization vector (IV).  
            ICryptoTransform crypto1 = dataencrypt.CreateEncryptor(dataencrypt.Key, dataencrypt.IV);
            //TransformFinalBlock is a special function for transforming the last block or a partial block in the stream.   
            //It returns a new array that contains the remaining transformed bytes. A new array is returned, because the amount of   
            //information returned at the end might be larger than a single block when padding is added.  
            byte[] encrypteddata = crypto1.TransformFinalBlock(bytearraytoencrypt, 0, bytearraytoencrypt.Length);
            crypto1.Dispose();
            //return the encrypted data  
            return encrypteddata;
        }

        //code to decrypt data
        private byte[] decryptdata(byte[] bytearraytodecrypt, string key, string iv)
        {

            AesCryptoServiceProvider keydecrypt = new AesCryptoServiceProvider();
            keydecrypt.BlockSize = 128;
            keydecrypt.KeySize = 128;
            keydecrypt.Key = System.Text.Encoding.UTF8.GetBytes(key);
            keydecrypt.IV = System.Text.Encoding.UTF8.GetBytes(iv);
            keydecrypt.Padding = PaddingMode.PKCS7;
            keydecrypt.Mode = CipherMode.CBC;
            ICryptoTransform crypto1 = keydecrypt.CreateDecryptor(keydecrypt.Key, keydecrypt.IV);

            byte[] returnbytearray = crypto1.TransformFinalBlock(bytearraytodecrypt, 0, bytearraytodecrypt.Length);
            crypto1.Dispose();
            return returnbytearray;
        }
        void SAVE_WBinaryFile()
        {
            textBox1.Text += "Сохранение завершено..." + Environment.NewLine;
        }

        string Save_dialog()
        {
            string filename = "none";
            saveFileDialog1.Filter = "bin files (*.bin)|*.bin";
            saveFileDialog1.FileName = "firmware";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                filename = saveFileDialog1.FileName;
            }

            return filename;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Push();
        }

        private void button4_Click(object sender, EventArgs e)
        {

        }
        void Close_WPF()
        {
            this.Close();
        }
        void TIME()
        {
            DateTime ThToday = DateTime.Now;
            string ThData = ThToday.ToString("HH:mm:ss" + "  --->  ");
            textBox1.Text += ThData;
        }
        void OPEN_BinaryFile()
        {
            textBox1.Text += "Файл открыт: " + size + Environment.NewLine;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult result;
            //System.Threading.Thread.Sleep(500);
            result = MessageBox.Show("Данная программа предназначена для шифрования. " + Environment.NewLine +
                                     "бинарных файлов прошивки по алгоритму AES128 CBC Zeros." + Environment.NewLine +
                                     "Для шифрования необходимо:" + Environment.NewLine +
                                     "1.Нажать кнопку <Encrypt>, Выбрать бинарный файл для шифрования." + Environment.NewLine +
                                     "2.Сохранить зашифрованный бинарный файл прошивки.", "Описание программы", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (result == DialogResult.OK)
            {
                result = MessageBox.Show("Хотите получить ключ шифрования?", "Ключ шифрования", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    textBox1.Clear();
                    textBox1.Text +="KEY:";
                    for (int i = 0; i < 16; i++)
                    {
                        textBox1.Text += " " + AES_FW_KEY[i].ToString("X2");
                    }
                    textBox1.Text += Environment.NewLine + "IV:";
                    for (int i = 0; i < 16; i++)
                    {
                        textBox1.Text += " " + AES_IV[i].ToString("X2");
                    }
                    textBox1.Text += Environment.NewLine;
                }
                if (result == DialogResult.No)
                {
                    textBox1.Clear();
                    textBox1.Text += "Выберите файл прошивки - .bin." + Environment.NewLine;
                }
            }
        }
    }
}

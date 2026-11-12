using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Diagnostics.Eventing.Reader;

namespace CekmeCihaziUygulamasi
{
    public partial class Form1 : Form
    {
        long min = 0;  // Grafik için X ekseni başlangıç değeri
        long maks = 10; // Grafik için X ekseni bitiş değeri
        string[] portlar = SerialPort.GetPortNames(); // Bilgisayarda mevcut olan seri portları alır

        public Form1()
        {
            InitializeComponent(); // Form bileşenlerini başlatır
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            button2.Enabled = false; // Başta durdurma butonu devre dışı
            foreach (string port in portlar) // Bulunan seri portları ComboBox'a ekle
            {
                comboBox1.Items.Add(port);
                comboBox1.SelectedIndex = 0; // İlk portu seçili yap
            }

            serialPort1.DataReceived += new SerialDataReceivedEventHandler(serialPort1_DataReceived); // Veri geldiğinde tetiklenecek olay
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false; // Başlat butonunu devre dışı bırak
            button2.Enabled = true;  // Durdur butonunu aktif yap

            if (!serialPort1.IsOpen)
            {
                serialPort1.PortName = comboBox1.Text; // Seçilen portu ayarla
                try
                {
                    serialPort1.Open(); // Portu aç
                    timer1.Start();     // Zamanlayıcıyı başlat
                }
                catch (Exception ex)
                {
                    // Port açılmazsa hata mesajı göster
                    MessageBox.Show("Bağlantı hatası: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    button1.Enabled = false;
                    button2.Enabled = false;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button1.Enabled = true;
            button2.Enabled = false;

            if (serialPort1.IsOpen)
            {
                serialPort1.DiscardInBuffer(); // Portta kalan verileri temizle
                timer1.Stop();                 // Zamanlayıcıyı durdur
                serialPort1.Close();           // Portu kapat
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                if (serialPort1.IsOpen && serialPort1.BytesToRead > 0)
                {
                    string sonuc = serialPort1.ReadLine(); // Seri porttan bir satır oku
                    if (!string.IsNullOrEmpty(sonuc))
                    {
                        string[] cekme = sonuc.Split('*'); // Veriyi * karakterine göre ayır
                        if (cekme.Length >= 2)
                        {
                            // Kuvvet ve mesafe değerlerini al
                            string kuvvet = cekme[0];
                            string mesafe = cekme[1];

                            // String değerleri sayıya çevir
                            double mesafeDegeri = double.Parse(mesafe);
                            double kuvvetDegeri = double.Parse(kuvvet);
                            string alan = textBox1.Text;
                            string ilkUzunluk = textBox2.Text;
                            double alanDegeri = double.Parse(alan);
                            double ilkUzunlukDegeri = double.Parse(ilkUzunluk);

                            // Uzama, gerilim, şekil değiştirme hesapları
                            double uzamaDegeri = mesafeDegeri - ilkUzunlukDegeri;
                            double gerilimDegeri = kuvvetDegeri / alanDegeri;
                            double sekilDegistirmeDegeri = uzamaDegeri / ilkUzunlukDegeri;

                            // Etiketlere verileri yaz
                            label2.Text = gerilimDegeri.ToString();
                            label7.Text = kuvvetDegeri.ToString();
                            label8.Text = uzamaDegeri.ToString();

                            // Grafik eksen ayarları
                            chart1.ChartAreas[0].AxisX.Minimum = min;
                            chart1.ChartAreas[0].AxisX.Maximum = maks;
                            chart1.ChartAreas[0].AxisY.Minimum = -1000;
                            chart1.ChartAreas[0].AxisY.Maximum = 1000;
                            chart1.ChartAreas[0].AxisX.ScaleView.Zoom(min, maks);

                            chart2.ChartAreas[0].AxisX.Minimum = min;
                            chart2.ChartAreas[0].AxisX.Maximum = maks;
                            chart2.ChartAreas[0].AxisY.Minimum = -100;
                            chart2.ChartAreas[0].AxisY.Maximum = 100;
                            chart2.ChartAreas[0].AxisX.ScaleView.Zoom(min, maks);

                            // Grafiklere veri ekle
                            chart3.Series[0].Points.AddXY(sekilDegistirmeDegeri, gerilimDegeri);
                            chart1.Series[0].Points.AddXY(min, kuvvetDegeri);
                            chart2.Series[0].Points.AddXY(min, uzamaDegeri);

                            maks++; // X eksenini artır
                            serialPort1.DiscardInBuffer(); // Porttaki okunmamış verileri temizle
                        }
                    }
                }
            }
            catch (IOException ioEx)
            {
                // Seri port ile ilgili hata olursa
                MessageBox.Show("Seri port bağlantı hatası: " + ioEx.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                timer1.Stop();
                if (serialPort1.IsOpen) serialPort1.Close();
                button1.Enabled = true;
                button2.Enabled = false;
            }
            catch (Exception ex)
            {
                // Diğer tüm hatalar için
                MessageBox.Show("Beklenmeyen hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // Bu kısım kullanılmıyor ama veri geldiğinde tetikleniyor
        }

        private void chart1_Click(object sender, EventArgs e)
        {
            // Grafik tıklama olayı (şu an boş)
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            // ComboBox değiştiğinde tetiklenir (şu an boş)
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Programdan çıkış işlemi
            if (serialPort1.IsOpen)
            {
                serialPort1.Close(); // Port açıksa kapat
                this.Close();        // Formu kapat
            }
            else
            {
                this.Close();        // Port kapalıysa sadece formu kapat
            }
        }
    }
}

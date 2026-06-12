using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Linq;
using System.Data.SqlClient;

namespace Gelir_Gider_Projesi
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;

            // DateTimePicker başlangıç yılı 2012 olarak ayarlandı
            dateTimePicker1.MaxDate = DateTime.Now;
            dateTimePicker1.MinDate = new DateTime(1900, 1, 1);
            dateTimePicker1.Value = new DateTime(2012, 1, 1);

            // İller ve ilçeler ComboBox'lara dolduruluyor
            LoadIller();
        }

        private void LoadIller()
        {
            try
            {
                using (SqlConnection baglanti = Database.GetConnection())
                {
                    SqlCommand komut = new SqlCommand("SELECT il_adi FROM İller", baglanti);
                    baglanti.Open();

                    using (SqlDataReader reader = komut.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            comboBox1.Items.Add(reader["il_adi"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("İller yüklenirken hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool IsValidEmail(string email)
        {
            return email.EndsWith("@gmail.com") && Regex.IsMatch(email, "^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}$");
        }

        private bool IsStrongPassword(string password)
        {
            return password.Length >= 8 && password.Any(char.IsUpper) && password.Any(char.IsDigit);
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            // Panel1 boyama işlemleri
        }

        private void panel6_Paint(object sender, PaintEventArgs e)
        {
            // Panel6 boyama işlemleri
        }

        

       

        

        

       
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox2.Items.Clear(); // İlçe ComboBox'ını temizle

            try
            {
                using (SqlConnection baglanti = Database.GetConnection())
                {
                    SqlCommand komut = new SqlCommand(
                        "SELECT ilce_adi FROM İlçeler WHERE il_id = (SELECT il_id FROM İller WHERE il_adi = @il)",
                        baglanti
                    );
                    komut.Parameters.AddWithValue("@il", comboBox1.SelectedItem.ToString());

                    baglanti.Open();

                    using (SqlDataReader reader = komut.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            comboBox2.Items.Add(reader["ilce_adi"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("İlçeler yüklenirken hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string ad = textBox1.Text;
            string soyad = textBox2.Text;
            DateTime dogumTarihi = dateTimePicker1.Value;
            string il = comboBox1.Text;
            string ilce = comboBox2.Text;
            string tcKimlik = maskedTextBox1.Text;
            string email = textBox3.Text;
            string sifre = textBox4.Text;

            if ((DateTime.Now.Year - dogumTarihi.Year) < 14)
            {
                MessageBox.Show("Yaşınız minimum 14 olmalıdır.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(il) || string.IsNullOrWhiteSpace(ilce))
            {
                MessageBox.Show("Lütfen il ve ilçe seçin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!IsValidTcKimlik(tcKimlik))
            {
                MessageBox.Show("Geçersiz TC Kimlik numarası.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!IsValidEmail(email))
            {
                MessageBox.Show("Geçersiz e-posta adresi. Lütfen @gmail.com ile biten bir adres girin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!IsStrongPassword(sifre))
            {
                MessageBox.Show("Şifre kurallara uygun değil. Şifre en az 8 karakter olmalı, bir büyük harf ve bir sayı içermelidir.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                using (SqlConnection baglanti = new SqlConnection(Database.connectionString))
                {
                    baglanti.Open();

                    // Aynı TC kimlik numarasına sahip kullanıcı kontrolü
                    SqlCommand kontrolKomut = new SqlCommand("SELECT COUNT(*) FROM Kullanicilar WHERE kullanicikimliknumarasi = @tcKimlik", baglanti);
                    kontrolKomut.Parameters.AddWithValue("@tcKimlik", tcKimlik);

                    int mevcutKullanici = (int)kontrolKomut.ExecuteScalar();
                    if (mevcutKullanici > 0)
                    {
                        MessageBox.Show("Bu TC Kimlik numarası ile kayıtlı bir kullanıcı zaten mevcut.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Kullanıcı kaydı
                    SqlCommand komut = new SqlCommand(
                        "INSERT INTO Kullanicilar (kullanici_ismi, kullanici_soyisim, eposta, kullanicikimliknumarasi, kullanici_sifre, il_id, rol, dogum_yili) " +
                        "VALUES (@ad, @soyad, @eposta, @tcKimlik, @sifre, (SELECT il_id FROM İller WHERE il_adi = @il), @rol, @dogumYili)",
                        baglanti
                    );

                    komut.Parameters.AddWithValue("@ad", ad);
                    komut.Parameters.AddWithValue("@soyad", soyad);
                    komut.Parameters.AddWithValue("@eposta", email);
                    komut.Parameters.AddWithValue("@tcKimlik", tcKimlik);
                    komut.Parameters.AddWithValue("@sifre", sifre);
                    komut.Parameters.AddWithValue("@il", il);
                    komut.Parameters.AddWithValue("@rol", 1); // Varsayılan rol: 1 (kullanıcı)
                    komut.Parameters.AddWithValue("@dogumYili", dogumTarihi.Year);

                    komut.ExecuteNonQuery();

                    MessageBox.Show("Kayıt başarılı! Giriş ekranına yönlendiriliyorsunuz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    Girisformu girisFormu = new Girisformu();
                    girisFormu.Show();
                    this.Hide();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kayıt işleminde hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool IsValidTcKimlik(string tc)
        {
            if (tc.Length != 11 || !long.TryParse(tc, out _)) return false;

            int toplam = 0;
            for (int i = 0; i < 10; i++) toplam += int.Parse(tc[i].ToString());

            return toplam % 10 == int.Parse(tc[10].ToString());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Button1 tıklama işlemleri
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Form1 yükleme işlemleri
        }

        
    }
}



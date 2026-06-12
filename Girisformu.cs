using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OfficeOpenXml;

namespace Gelir_Gider_Projesi
{
    public partial class Girisformu : Form
    {
#pragma warning disable 0169
        int progress;
#pragma warning restore 0169

        public Girisformu()
        {
            InitializeComponent();
            AddPlaceholderToTextBox(textBox1, "E-posta giriniz");
            textBox2.PasswordChar = '*'; // Şifreyi gizleme

            this.StartPosition = FormStartPosition.CenterScreen;

            // Varsayılan olarak @gmail.com ekle
            textBox1.Text = "@gmail.com";
            textBox1.ForeColor = Color.Gray;

            textBox1.GotFocus += (sender, e) =>
            {
                if (textBox1.Text == "@gmail.com")
                {
                    textBox1.Text = "";
                    textBox1.ForeColor = Color.Black;
                }
            };

            textBox1.LostFocus += (sender, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBox1.Text))
                {
                    textBox1.Text = "@gmail.com";
                    textBox1.ForeColor = Color.Gray;
                }
            };

            // Sabit bir resim ayarla
            string basePath = "C:\\Users\\HP\\source\\repos\\Gelir-Gider Projesi\\Resimler\\";
            pictureBox1.Image = Image.FromFile(basePath + "girisresim.jpg");
        }

        private int girisYapanKullanciID;

        // küçük sınıf
        private class LoginInfo
        {
            public int Rol { get; set; }
            public int KullaniciId { get; set; }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Kullanıcıyı Form1'e yönlendir
            Form1 kayitFormu = new Form1();
            kayitFormu.Show();
            this.Hide();
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            try
            {
                Form1 frm = new Form1();
                frm.Show();
                this.Hide();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Giriş formu açılamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            

            label7.Visible = true;

            string email = textBox1.Text;
            string sifre = textBox2.Text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(sifre) || !email.EndsWith("@gmail.com"))
            {
                MessageBox.Show("Lütfen tüm alanları doldurun ve geçerli bir @gmail.com adresi girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (SqlConnection baglanti = new SqlConnection(Database.connectionString))
                {
                    SqlCommand komut = new SqlCommand("SELECT kullanici_id, rol FROM Kullanicilar WHERE eposta=@p1 AND kullanici_sifre=@p2", baglanti);
                    komut.Parameters.AddWithValue("@p1", email);
                    komut.Parameters.AddWithValue("@p2", sifre);

                    baglanti.Open();
                    using (SqlDataReader dr = komut.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            int id = Convert.ToInt32(dr["kullanici_id"]);
                            int rol = Convert.ToInt32(dr["rol"]);

                            girisYapanKullanciID = id;

                            if (rol == 0)
                            {
                                Admin adminForm = new Admin();
                                adminForm.Show();
                                this.Hide();
                            }
                            else if (rol == 1)
                            {
                                Kullanıcıkısmı kullaniciForm = new Kullanıcıkısmı(id);
                                kullaniciForm.Show();
                                this.Hide();
                            }
                            else
                            {
                                MessageBox.Show("Bilinmeyen rol değeri.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            return;
                        }
                    }

                    MessageBox.Show("E-posta veya şifre yanlış.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Giriş işleminde hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();

            var info = timer1.Tag as LoginInfo;
            if (info != null)
            {
                int rol = info.Rol;
                int kullaniciId = info.KullaniciId;

                if (rol == 1)
                {
                    Kullanıcıkısmı frm = new Kullanıcıkısmı(kullaniciId);
                    frm.Show();
                    this.Hide();
                }
                else if (rol == 0)
                {
                    Admin frm = new Admin();
                    frm.Show();
                    this.Hide();
                }
                else
                {
                    MessageBox.Show("Bilinmeyen rol değeri: " + rol, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Giriş bilgileri alınamadı (Tag beklenen türde değil).", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int currentImageIndex = 0;
        private List<Image> images = new List<Image>();

        private void LoadImages()
        {
            try
            {
                // Update paths to use the Resimler directory
                string basePath = "C:\\Users\\HP\\source\\repos\\Gelir-Gider Projesi\\Resimler\\";
                images.Add(Image.FromFile(basePath + "ekonomi4.jpg"));
                images.Add(Image.FromFile(basePath + "ekonomi2.jpg"));
                images.Add(Image.FromFile(basePath + "ekonomo1.jpg"));
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Resim yüklenirken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StartImageRotation()
        {
            Timer imageTimer = new Timer();
            imageTimer.Interval = 4000; // 4 seconds
            imageTimer.Tick += (s, e) =>
            {
                if (images.Count > 0)
                {
                    currentImageIndex = (currentImageIndex + 1) % images.Count;
                    pictureBox1.Image = images[currentImageIndex];
                }
            };
            imageTimer.Start();
        }

        private void Girisformu_Load(object sender, EventArgs e)
        {
            

           

            
            LoadImages();
            StartImageRotation();

            // Eğer kayıt hemen öncesinde kullanıcı oluşturulduysa email'i otomatik doldur
            if (!string.IsNullOrEmpty(Session.PendingRegisteredEmail))
            {
                textBox1.Text = Session.PendingRegisteredEmail;
                textBox1.ForeColor = Color.Black;
            }
            else
            {
                textBox1.Text = "E-postanız";
                textBox1.ForeColor = Color.Gray;
                textBox2.Text = "şifreniz";
                textBox2.ForeColor = Color.Gray;
            }

            button1.FlatAppearance.MouseOverBackColor = Color.ForestGreen;  //fare üzerinde iken 
            button1.FlatAppearance.MouseDownBackColor = Color.SeaGreen; //tıklayınca

            label7.Visible = false;
            label7.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            label7.ForeColor = Color.DarkBlue;

        }
        

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            // Show confirmation popup
            DialogResult result = MessageBox.Show(
                "Tüm 12 ayın gelir-gider verileri analiz için hazırlanacak ve sizi analiz sitesine yönlendireceğiz. Devam etmek ister misiniz?",
                "Analiz Onayı",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                try
                {
                    // Collect data for all 12 months
                    DataTable data = new DataTable();
                    data.Columns.Add("Ay", typeof(string));
                    data.Columns.Add("Gelir", typeof(decimal));
                    data.Columns.Add("Gider", typeof(decimal));

                    using (SqlConnection conn = new SqlConnection(Database.connectionString))
                    {
                        conn.Open();
                        for (int month = 1; month <= 12; month++)
                        {
                            string query = @"SELECT 
                                ISNULL(SUM(CASE WHEN k.kategori_tip = 1 THEN g.tutar ELSE 0 END), 0) AS Gelir,
                                ISNULL(SUM(CASE WHEN k.kategori_tip = 0 THEN g.tutar ELSE 0 END), 0) AS Gider
                            FROM GelirGiderler_4 g
                            JOIN Kategori k ON g.kategori_id = k.kategori_id
                            WHERE MONTH(g.Tarih) = @month AND YEAR(g.Tarih) = @year AND g.kullanici_id = @userId";

                            using (SqlCommand cmd = new SqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@month", month);
                                cmd.Parameters.AddWithValue("@year", DateTime.Now.Year);
                                cmd.Parameters.AddWithValue("@userId", girisYapanKullanciID);

                                using (SqlDataReader reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        decimal gelir = reader.GetDecimal(0);
                                        decimal gider = reader.GetDecimal(1);
                                        string ayAdi = DateTimeFormatInfo.CurrentInfo.GetMonthName(month);
                                        data.Rows.Add(ayAdi, gelir, gider);
                                    }
                                }
                            }
                        }
                    }

                   
                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.Filter = "Excel Dosyası|*.xlsx|CSV Dosyası|*.csv";
                    sfd.FileName = "12AyHarcamaAnalizi";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        string filePath = sfd.FileName;

                        if (filePath.EndsWith(".csv"))
                        {
                            using (StreamWriter sw = new StreamWriter(filePath))
                            {
                                
                                sw.WriteLine("Ay,Gelir,Gider");

                                
                                foreach (DataRow row in data.Rows)
                                {
                                    sw.WriteLine($"{row["Ay"]},{row["Gelir"]},{row["Gider"]}");
                                }
                            }
                        }
                        else if (filePath.EndsWith(".xlsx"))
                        {
                            using (var package = new OfficeOpenXml.ExcelPackage())
                            {
                                var worksheet = package.Workbook.Worksheets.Add("12 Ay Analizi");
                                worksheet.Cells[1, 1].Value = "Ay";
                                worksheet.Cells[1, 2].Value = "Gelir";
                                worksheet.Cells[1, 3].Value = "Gider";

                                int row = 2;
                                foreach (DataRow dataRow in data.Rows)
                                {
                                    worksheet.Cells[row, 1].Value = dataRow["Ay"];
                                    worksheet.Cells[row, 2].Value = dataRow["Gelir"];
                                    worksheet.Cells[row, 3].Value = dataRow["Gider"];
                                    row++;
                                }

                                package.SaveAs(new FileInfo(filePath));
                            }
                        }

                        
                        MessageBox.Show("12 ayın verileri hazırlanıldı! Analiz sitesine yönlendiriliyorsunuz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        
                        System.Diagnostics.Process.Start("https://datastudio.google.com");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Bir hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void AddPlaceholderToTextBox(TextBox textBox, string placeholder)
        {
            textBox.Text = placeholder;
            textBox.ForeColor = Color.Gray;

            textBox.GotFocus += (sender, e) =>
            {
                if (textBox.Text == placeholder)
                {
                    textBox.Text = "";
                    textBox.ForeColor = Color.Black;
                }
            };

            textBox.LostFocus += (sender, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = placeholder;
                    textBox.ForeColor = Color.Gray;
                }
            };
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            // Kullanıcıyı Form1'e yönlendir
            Form1 kayitFormu = new Form1();
            kayitFormu.Show();
            this.Hide();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_MouseEnter(object sender, EventArgs e)
        {
            button1.BackColor = Color.Green;
        }

        private void button1_MouseLeave(object sender, EventArgs e)
        {
            button1.BackColor=Color.FromArgb(34, 197, 94);
        }

        private void button2_MouseEnter(object sender, EventArgs e)
        {
            button2.BackColor = Color.FromArgb(37, 99, 235);
        }

        private void button2_MouseLeave(object sender, EventArgs e)
        {
            button2.BackColor=Color.FromArgb(59, 130,246);
        }
    }
    
}

using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Gelir_Gider_Projesi
{
    public partial class Admin : Form
    {
        public Admin()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void Admin_Load(object sender, EventArgs e)
        {
            try
            {
                // ComboBox1'e roller ekleniyor
                comboBox1.Items.Add("Kullanıcı");
                comboBox1.Items.Add("Admin");

                // Kullanıcıları listele
                LoadUsers();

                // Kategori tiplerini comboBox5'e ekle
                comboBox5.Items.Add("Gider");
                comboBox5.Items.Add("Gelir");

                // dataGridView2 verilerini yükle
                LoadCategories();

                // dataGridView3 kullanıcı bilgilerini yükle
                LoadUserDetails();
                // dataGridView3'te seçilen satırı textbox'lara yansıtmak için olay ata
                dataGridView3.CellClick += dataGridView3_CellClick;

                // comboBox2 ödeme türlerini yükle
                LoadPaymentTypes();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Admin yüklenirken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StyleDataGridView(DataGridView dv)
        {
            dv.BackgroundColor = Color.White;
            dv.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            dv.DefaultCellStyle.ForeColor = Color.Black;
        }

        private void LoadUsers()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(Database.connectionString))
                {
                    conn.Open();
                    string sql = "SELECT kullanici_id, kullanici_ismi AS [Kullanıcı Adı], kullanici_soyisim AS [Soyadı], eposta AS [E-Posta], kullanicikimliknumarasi AS [TC], rol FROM Kullanicilar";
                    using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        dataGridView1.DataSource = dt;
                        if (dataGridView1.Columns.Contains("kullanici_id")) dataGridView1.Columns["kullanici_id"].Visible = false;
                        dataGridView1.ClearSelection();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kullanıcılar yüklenirken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadCategories()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(Database.connectionString))
                {
                    conn.Open();
                    string sql = "SELECT kategori_id_2, kategori_adi, kategori_tip FROM Kategori2";
                    using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        dataGridView2.DataSource = dt;
                        if (dataGridView2.Columns.Contains("kategori_id_2")) dataGridView2.Columns["kategori_id_2"].Visible = false;
                        dataGridView2.ClearSelection();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kategoriler yüklenirken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadUserDetails()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(Database.connectionString))
                {
                    conn.Open();
                    string sql = "SELECT kullanici_ismi AS [İsim], kullanici_soyisim AS [Soyadı], eposta AS [E-Posta], kullanicikimliknumarasi AS [TC No] FROM Kullanicilar";
                    using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        dataGridView3.DataSource = dt;
                        dataGridView3.ClearSelection();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kullanıcı bilgileri yüklenirken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadPaymentTypes()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(Database.connectionString))
                {
                    conn.Open();
                    string sql = "SELECT odeme_adi FROM OdemeTuru2";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            comboBox2.Items.Clear();
                            while (reader.Read())
                            {
                                comboBox2.Items.Add(reader["odeme_adi"].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ödeme türleri yüklenirken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            // dataGridView3'te seçilen kullanıcıyı güncelle
            if (dataGridView3.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen bir kullanıcı seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Seçili kullanıcının bilgilerini al
            int rowIndex = dataGridView3.SelectedRows[0].Index;

            // Varsayılan olarak dataGridView3'te ID kolonu olmayabilir. Burada e-posta veya isimle eşleştirme yapacağız.
            string isim = textBox3.Text.Trim();
            string soyisim = textBox4.Text.Trim();
            string eposta = textBox5.Text.Trim();
            string tc = maskedTextBox1.Text.Trim();

            if (string.IsNullOrWhiteSpace(isim) || string.IsNullOrWhiteSpace(soyisim) || string.IsNullOrWhiteSpace(eposta) || string.IsNullOrWhiteSpace(tc))
            {
                MessageBox.Show("Lütfen tüm alanları doldurun.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(Database.connectionString))
                {
                    conn.Open();

                    // Güncelleme sorgusu: eposta alanı benzersiz ise e-postaya göre güncelleme yap
                    string updateSql = "UPDATE Kullanicilar SET kullanici_ismi = @isim, kullanici_soyisim = @soyisim, eposta = @eposta, kullanicikimliknumarasi = @tc WHERE eposta = @oldEposta";
                    using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                    {
                        // Eski eposta değeri dataGridView3'te seçilen satırın eposta hücresinden alınmalı
                        string oldEposta = dataGridView3.SelectedRows[0].Cells["E-Posta"].Value.ToString();

                        cmd.Parameters.AddWithValue("@isim", isim);
                        cmd.Parameters.AddWithValue("@soyisim", soyisim);
                        cmd.Parameters.AddWithValue("@eposta", eposta);
                        cmd.Parameters.AddWithValue("@tc", tc);
                        cmd.Parameters.AddWithValue("@oldEposta", oldEposta);

                        int affected = cmd.ExecuteNonQuery();
                        if (affected > 0)
                        {
                            MessageBox.Show("Kullanıcı başarıyla güncellendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadUserDetails();
                            LoadUsers();
                        }
                        else
                        {
                            MessageBox.Show("Güncelleme başarısız. Kullanıcı bulunamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kullanıcı güncellenirken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void bbtnkullanici_sil(object sender, EventArgs e)
        {
           
        
            // Seçili kullanıcıyı sil
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen bir kullanıcı seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            object idObj = dataGridView1.SelectedRows[0].Cells["kullanici_id"].Value;
            if (idObj == null || !int.TryParse(idObj.ToString(), out int selectedUserId))
            {
                MessageBox.Show("Seçilen kullanıcının ID'si alınamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var dr = MessageBox.Show("Seçili kullanıcıyı silmek istediğinize emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr != DialogResult.Yes) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(Database.connectionString))
                {
                    conn.Open();
                    string deleteSql = "DELETE FROM Kullanicilar WHERE kullanici_id = @id";
                    using (SqlCommand cmd = new SqlCommand(deleteSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", selectedUserId);
                        int affected = cmd.ExecuteNonQuery();
                        if (affected > 0)
                        {
                            MessageBox.Show("Kullanıcı başarıyla silindi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadUsers();
                            LoadUserDetails();
                        }
                        else
                        {
                            MessageBox.Show("Kullanıcı bulunamadı veya silinemedi.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kullanıcı silinirken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        
        }

        private void button9_Click_1(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen bir kullanıcı seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedUserId = Convert.ToInt32(dataGridView1.SelectedRows[0].Cells["kullanici_id"].Value);
            int selectedRole = comboBox1.SelectedItem.ToString() == "Kullanıcı" ? 1 : 0;

            try
            {
                using (SqlConnection conn = new SqlConnection(Database.connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("UPDATE Kullanicilar SET rol = @rol WHERE kullanici_id = @id", conn);
                    cmd.Parameters.AddWithValue("@rol", selectedRole);
                    cmd.Parameters.AddWithValue("@id", selectedUserId);
                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Rol başarıyla güncellendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadUsers();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Rol güncellenirken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button7_Click_1(object sender, EventArgs e)
        {
            // Seçili ödeme türünü silme işlemi
            if (comboBox2.SelectedItem == null)
            {
                MessageBox.Show("Lütfen silinecek bir ödeme türü seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string odemeTuru = comboBox2.SelectedItem.ToString();

            var dr = MessageBox.Show($"'{odemeTuru}' ödeme türünü silmek istediğinize emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr != DialogResult.Yes) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(Database.connectionString))
                {
                    conn.Open();

                    string deleteSql = "DELETE FROM OdemeTuru2 WHERE odeme_adi = @odeme";
                    using (SqlCommand cmd = new SqlCommand(deleteSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@odeme", odemeTuru);
                        int affected = cmd.ExecuteNonQuery();
                        if (affected > 0)
                        {
                            MessageBox.Show("Ödeme türü başarıyla silindi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            comboBox2.SelectedIndex = -1;
                        }
                        else
                        {
                            MessageBox.Show("Ödeme türü bulunamadı veya silinemedi.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }

                // ComboBox ve kaynakları güncelle
                LoadPaymentTypes();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ödeme türü silinirken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            // AdminPanel2 formuna geçiş
            var panel2Form = new AdminPanel2();
            panel2Form.StartPosition = FormStartPosition.CenterScreen;
            panel2Form.Show();
            this.Hide();
        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            string yeniOdemeTuru = textBox2.Text;

            if (string.IsNullOrWhiteSpace(yeniOdemeTuru))
            {
                MessageBox.Show("Lütfen bir ödeme türü girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(Database.connectionString))
                {
                    conn.Open();
                    string insertPaymentTypeSql = "INSERT INTO OdemeTuru2 (odeme_adi) VALUES (@odeme)";
                    using (SqlCommand cmd = new SqlCommand(insertPaymentTypeSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@odeme", yeniOdemeTuru);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Ödeme türü başarıyla eklendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadPaymentTypes();
                textBox2.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ödeme türü eklenirken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // dataGridView2 üzerinden bir satır seçilip seçilmediğini kontrol et
            if (dataGridView2.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen bir kategori seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Seçilen kategori ID'sini al
            int selectedCategoryId = Convert.ToInt32(dataGridView2.SelectedRows[0].Cells["kategori_id_2"].Value);

            try
            {
                using (SqlConnection conn = new SqlConnection(Database.connectionString))
                {
                    conn.Open();

                    // GelirGiderler_4 tablosundaki ilgili kayıtları sil
                    string deleteRelatedRecordsSql = "DELETE FROM GelirGiderler_4 WHERE kategori_id_2 = @id";
                    using (SqlCommand deleteRelatedCmd = new SqlCommand(deleteRelatedRecordsSql, conn))
                    {
                        deleteRelatedCmd.Parameters.AddWithValue("@id", selectedCategoryId);
                        deleteRelatedCmd.ExecuteNonQuery();
                    }

                    // Kategori2 tablosundan kategoriyi sil
                    string deleteCategorySql = "DELETE FROM Kategori2 WHERE kategori_id_2 = @id";
                    using (SqlCommand deleteCategoryCmd = new SqlCommand(deleteCategorySql, conn))
                    {
                        deleteCategoryCmd.Parameters.AddWithValue("@id", selectedCategoryId);
                        deleteCategoryCmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Kategori başarıyla silindi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadCategories();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kategori silinirken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // DataGridView2 hücre tıklama işlemleri
        }

        private void dataGridView3_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // Satır başlığına veya geçersiz hücreye tıklanmışsa çık
            if (e.RowIndex < 0) return;

            var row = dataGridView3.Rows[e.RowIndex];
            // Kolon isimleri LoadUserDetails sorgusundaki alias'lara göre
            textBox3.Text = row.Cells["İsim"].Value?.ToString() ?? string.Empty;
            textBox4.Text = row.Cells["Soyadı"].Value?.ToString() ?? string.Empty;
            textBox5.Text = row.Cells["E-Posta"].Value?.ToString() ?? string.Empty;
            maskedTextBox1.Text = row.Cells["TC No"].Value?.ToString() ?? string.Empty;

            // Satırı seçili hale getir
            row.Selected = true;
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            string kategoriAdi = textBox1.Text;
            int kategoriTipi = comboBox5.SelectedIndex; // 0: Gider, 1: Gelir

            if (string.IsNullOrWhiteSpace(kategoriAdi) || kategoriTipi < 0)
            {
                MessageBox.Show("Lütfen tüm alanları doldurun.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(Database.connectionString))
                {
                    conn.Open();
                    string insertCategorySql = "INSERT INTO Kategori2 (kategori_adi, kategori_tip) VALUES (@adi, @tip)";
                    using (SqlCommand cmd = new SqlCommand(insertCategorySql, conn))
                    {
                        cmd.Parameters.AddWithValue("@adi", kategoriAdi);
                        cmd.Parameters.AddWithValue("@tip", kategoriTipi);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Kategori başarıyla eklendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadCategories();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kategori eklenirken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        private void maskedTextBox1_Leave(object sender, EventArgs e)
        {
            string tcKimlik = maskedTextBox1.Text;
            if (!IsValidTcKimlik(tcKimlik))
            {
                MessageBox.Show("Geçersiz TC Kimlik numarası. Lütfen 11 haneli ve Türkiye kurallarına uygun bir numara girin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                maskedTextBox1.Focus();
            }
        }

        private void textBox3_Leave(object sender, EventArgs e)
        {
            if (!textBox3.Text.EndsWith("@gmail.com"))
            {
                textBox3.Text = "@gmail.com";
                textBox3.ForeColor = Color.Gray;
            }
        }

        private void textBox4_Leave(object sender, EventArgs e)
        {
            string sifre = textBox4.Text;
            if (!IsStrongPassword(sifre))
            {
                MessageBox.Show("Şifre kurallara uygun değil. Şifre en az 8 karakter olmalı, bir büyük harf ve bir sayı içermelidir.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox4.Focus();
            }
        }

        private bool IsValidTcKimlik(string tc)
        {
            if (tc.Length != 11 || !long.TryParse(tc, out _)) return false;

            int toplam = 0;
            for (int i = 0; i < 10; i++) toplam += int.Parse(tc[i].ToString());

            return toplam % 10 == int.Parse(tc[10].ToString());
        }

        private bool IsStrongPassword(string password)
        {
            return password.Length >= 8 && password.Any(char.IsUpper) && password.Any(char.IsDigit);
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Hücre tıklama işlemleri burada yapılabilir
            MessageBox.Show("Bir hücreye tıklandı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            // Kullanıcıkısmı formuna git
            var kullaniciForm = new Kullanıcıkısmı();
            kullaniciForm.StartPosition = FormStartPosition.CenterScreen;
            kullaniciForm.Show();
            this.Hide();
        }

        private void btnkullanici_sil_Click(object sender, EventArgs e)
        {
            // Mevcut kullanıcı silme mantığını yeniden kullan
            bbtnkullanici_sil(sender, e);
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            // Mevcut kategori silme mantığını yeniden kullan
            button1_Click(sender, e);
        }
    }
}

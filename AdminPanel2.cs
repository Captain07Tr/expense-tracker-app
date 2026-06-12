using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace Gelir_Gider_Projesi
{
    public partial class AdminPanel2 : Form
    {
        public AdminPanel2()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void AdminPanel2_Load(object sender, EventArgs e)
        {
            try
            {
                // İstatistikleri yükle
                GetStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show("İstatistikler yüklenirken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GetStatistics()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(Database.connectionString))
                {
                    conn.Open();

                    // En çok harcama yapılan kategori (gider)
                    string mostSpentCategorySql = @"SELECT TOP 1 k.kategori_adi, SUM(g.tutar) AS total
FROM GelirGiderler_4 g
JOIN Kategori2 k ON g.kategori_id_2 = k.kategori_id_2
WHERE k.kategori_tip = 0
GROUP BY k.kategori_adi
ORDER BY total DESC";
                    using (SqlCommand cmd = new SqlCommand(mostSpentCategorySql, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            label5.Text = reader[0]?.ToString() ?? "Yok";
                        }
                        else label5.Text = "Yok";
                    }

                    // En az harcama yapılan kategori (gider)
                    string leastSpentCategorySql = @"SELECT TOP 1 k.kategori_adi, SUM(g.tutar) AS total
FROM GelirGiderler_4 g
JOIN Kategori2 k ON g.kategori_id_2 = k.kategori_id_2
WHERE k.kategori_tip = 0
GROUP BY k.kategori_adi
ORDER BY total ASC";
                    using (SqlCommand cmd = new SqlCommand(leastSpentCategorySql, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read()) label9.Text = reader[0]?.ToString() ?? "Yok";
                        else label9.Text = "Yok";
                    }

                    // En çok tercih edilen ödeme türü (OdemeTuru2 tablosundan isim al)
                    string mostPaymentSql = @"SELECT TOP 1 o.odeme_adi, COUNT(*) AS cnt
FROM GelirGiderler_4 g
LEFT JOIN OdemeTuru2 o ON g.odeme_tur_id_2 = o.odeme_tur_id_2
GROUP BY o.odeme_adi
ORDER BY cnt DESC";
                    using (SqlCommand cmd = new SqlCommand(mostPaymentSql, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read()) label11.Text = reader[0]?.ToString() ?? "Yok";
                        else label11.Text = "Yok";
                    }

                    // En tasarruflu kullanıcı (gelir - gider en yüksek) - adminler hariç (rol = 0 admin)
                    string mostSavingUserSql = @"SELECT TOP 1 u.kullanici_ismi + ' ' + u.kullanici_soyisim AS name,
ISNULL(SUM(CASE WHEN k.kategori_tip = 1 THEN g.tutar ELSE 0 END),0) - ISNULL(SUM(CASE WHEN k.kategori_tip = 0 THEN g.tutar ELSE 0 END),0) AS saving
FROM Kullanicilar u
LEFT JOIN GelirGiderler_4 g ON u.kullanici_id = g.kullanici_id
LEFT JOIN Kategori2 k ON g.kategori_id_2 = k.kategori_id_2
WHERE u.rol = 1
GROUP BY u.kullanici_ismi, u.kullanici_soyisim
ORDER BY saving DESC";
                    using (SqlCommand cmd = new SqlCommand(mostSavingUserSql, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read()) label10.Text = reader[0]?.ToString() ?? "Yok";
                        else label10.Text = "Yok";
                    }

                    // Sistemdeki tüm kullanıcıların toplam geliri (sadece normal kullanıcılar rol=1)
                    string totalIncomeSql = @"SELECT ISNULL(SUM(g.tutar),0) FROM GelirGiderler_4 g
JOIN Kategori2 k ON g.kategori_id_2 = k.kategori_id_2
JOIN Kullanicilar u ON g.kullanici_id = u.kullanici_id
WHERE k.kategori_tip = 1 AND u.rol = 1";
                    using (SqlCommand cmd = new SqlCommand(totalIncomeSql, conn))
                    {
                        var val = cmd.ExecuteScalar();
                        if (val != null && val != DBNull.Value)
                            label13.Text = Convert.ToDecimal(val).ToString("N2");
                        else
                            label13.Text = "0";
                    }

                    // Sistemdeki tüm kullanıcıların toplam gideri (sadece normal kullanıcılar rol=1)
                    string totalExpenseSql = @"SELECT ISNULL(SUM(g.tutar),0) FROM GelirGiderler_4 g
JOIN Kategori2 k ON g.kategori_id_2 = k.kategori_id_2
JOIN Kullanicilar u ON g.kullanici_id = u.kullanici_id
WHERE k.kategori_tip = 0 AND u.rol = 1";
                    using (SqlCommand cmd = new SqlCommand(totalExpenseSql, conn))
                    {
                        var val = cmd.ExecuteScalar();
                        if (val != null && val != DBNull.Value)
                            label15.Text = Convert.ToDecimal(val).ToString("N2");
                        else
                            label15.Text = "0";
                    }

                    // En çok harcama yapan kullanıcı - adminler hariç
                    string topSpenderSql = @"SELECT TOP 1 u.kullanici_ismi + ' ' + u.kullanici_soyisim AS name, ISNULL(SUM(CASE WHEN k.kategori_tip=0 THEN g.tutar ELSE 0 END),0) AS totalExpense
FROM Kullanicilar u
LEFT JOIN GelirGiderler_4 g ON u.kullanici_id = g.kullanici_id
LEFT JOIN Kategori2 k ON g.kategori_id_2 = k.kategori_id_2
WHERE u.rol = 1
GROUP BY u.kullanici_ismi, u.kullanici_soyisim
ORDER BY totalExpense DESC";
                    using (SqlCommand cmd = new SqlCommand(topSpenderSql, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read()) label17.Text = reader[0]?.ToString() ?? "Yok";
                        else label17.Text = "Yok";
                    }

                    // En çok gelir elde eden kullanıcı - adminler hariç
                    string topIncomeUserSql = @"SELECT TOP 1 u.kullanici_ismi + ' ' + u.kullanici_soyisim AS name, ISNULL(SUM(CASE WHEN k.kategori_tip=1 THEN g.tutar ELSE 0 END),0) AS totalIncome
FROM Kullanicilar u
LEFT JOIN GelirGiderler_4 g ON u.kullanici_id = g.kullanici_id
LEFT JOIN Kategori2 k ON g.kategori_id_2 = k.kategori_id_2
WHERE u.rol = 1
GROUP BY u.kullanici_ismi, u.kullanici_soyisim
ORDER BY totalIncome DESC";
                    using (SqlCommand cmd = new SqlCommand(topIncomeUserSql, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read()) label19.Text = reader[0]?.ToString() ?? "Yok";
                        else label19.Text = "Yok";
                    }

                    // Toplam kullanıcı sayısı (sadece normal kullanıcılar)
                    string userCountSql = "SELECT COUNT(*) FROM Kullanicilar WHERE rol = 1";
                    using (SqlCommand cmd = new SqlCommand(userCountSql, conn))
                    {
                        var val = cmd.ExecuteScalar();
                        label21.Text = val != null ? val.ToString() : "0";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("İstatistikler alınırken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // DataGridView1'e kullanıcıların toplam gelir ve giderini getir
            try
            {
                using (SqlConnection conn = new SqlConnection(Database.connectionString))
                {
                    conn.Open();
                    // Sadece normal kullanıcılar (rol = 1) gösterilsin
                    string sql = @"SELECT u.kullanici_ismi + ' ' + u.kullanici_soyisim AS Kullanici,
ISNULL(SUM(CASE WHEN k.kategori_tip = 1 THEN g.tutar ELSE 0 END),0) AS ToplamGelir,
ISNULL(SUM(CASE WHEN k.kategori_tip = 0 THEN g.tutar ELSE 0 END),0) AS ToplamGider
FROM Kullanicilar u
LEFT JOIN GelirGiderler_4 g ON u.kullanici_id = g.kullanici_id
LEFT JOIN Kategori2 k ON g.kategori_id_2 = k.kategori_id_2
WHERE u.rol = 1
GROUP BY u.kullanici_ismi, u.kullanici_soyisim";
                    using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        // Daha düzenli görünüm için sütun başlıklarını ayarla
                        dataGridView1.DataSource = dt;
                        dataGridView1.Columns[0].HeaderText = "Kullanıcı";
                        dataGridView1.Columns[1].HeaderText = "Toplam Gelir";
                        dataGridView1.Columns[2].HeaderText = "Toplam Gider";
                        dataGridView1.Columns[1].DefaultCellStyle.Format = "N2";
                        dataGridView1.Columns[2].DefaultCellStyle.Format = "N2";
                        dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gelir/Gider yüklenirken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Admin formuna geçiş
            var adminForm = new Admin();
            adminForm.StartPosition = FormStartPosition.CenterScreen;
            adminForm.Show();
            this.Hide();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // ListView1'e tüm kullanıcıların kişisel bilgilerini ekle
            try
            {
                listView1.Clear();
                listView1.View = View.Details;
                listView1.Columns.Clear();
                listView1.Columns.Add("ID", 60);
                listView1.Columns.Add("İsim", 120);
                listView1.Columns.Add("Soyadı", 120);
                listView1.Columns.Add("E-Posta", 180);
                listView1.Columns.Add("TC", 120);
                listView1.FullRowSelect = true;
                listView1.GridLines = true;

                using (SqlConnection conn = new SqlConnection(Database.connectionString))
                {
                    conn.Open();
                    // sadece normal kullanıcılar (rol = 1)
                    string sql = "SELECT kullanici_id, kullanici_ismi, kullanici_soyisim, eposta, kullanicikimliknumarasi FROM Kullanicilar WHERE rol = 1";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new ListViewItem(reader["kullanici_id"].ToString());
                            item.SubItems.Add(reader["kullanici_ismi"].ToString());
                            item.SubItems.Add(reader["kullanici_soyisim"].ToString());
                            item.SubItems.Add(reader["eposta"].ToString());
                            item.SubItems.Add(reader["kullanicikimliknumarasi"].ToString());
                            listView1.Items.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kullanıcılar yüklenirken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // DataGridView1 hücre tıklama işlemleri
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // ListView1 seçim değişikliği işlemleri
        }
    }
}

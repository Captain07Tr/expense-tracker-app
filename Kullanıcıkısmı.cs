using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using RestSharp;

namespace Gelir_Gider_Projesi
{
    public partial class Kullanıcıkısmı : Form
    {
        private int girisYapanKullanciID;

        private Font originalLabel15Font; 
        private Timer refreshTimer;


        
        private Label labelNetInfo;
        private Label labelBudgetWarning;

        private readonly object chartLock = new object();

        public class ComboboxItem
        {
            public string Text { get; set; }   // ComboBox’ta görünecek isim
            public string Value { get; set; }  // ID
            public string Extra { get; set; }  // Tip veya ekstra bilgi
            public override string ToString()
            {
                return Text; // ComboBox’ta sadece Text görünür
            }
        }
        /*harcama geçmişi başlangıç*/

        void HarcamaGetir()
        {
            SqlConnection baglanti = new SqlConnection(Database.connectionString);

            string sorgu = "SELECT Kategori2.kategori_adi as Kategori, g.tutar as Tutar " +
                           "FROM GelirGiderler_4 g " +
                            "INNER JOIN Kategori2 ON g.kategori_id_2 = Kategori2.kategori_id_2 " +
                           "WHERE g.kullanici_id = @kullaniciID ";

            SqlDataAdapter da = new SqlDataAdapter(sorgu, baglanti);
            da.SelectCommand.Parameters.AddWithValue("@kullaniciID", girisYapanKullanciID);

            DataTable dt = new DataTable();
            da.Fill(dt);

            dataGridView1.DataSource = dt;
        }

        /*harcama geçmişi bitiş*/






        /*Yüzdelik yazma start*/

        void YuzdeHesapla()
        {
            SqlConnection baglanti = new SqlConnection(Database.connectionString);

            // Toplam gelir (tip = 1)
            string gelirSorgu = "SELECT ISNULL(SUM(g.tutar), 0) FROM GelirGiderler_4 g " +
                 "INNER JOIN Kategori2 ON g.kategori_id_2 = Kategori2.kategori_id_2 " +
                "WHERE g.kullanici_id = @kullaniciID AND Kategori2.kategori_tip = 1";
            SqlCommand cmdGelir = new SqlCommand(gelirSorgu, baglanti);
            cmdGelir.Parameters.AddWithValue("@kullaniciID", girisYapanKullanciID);

            // Toplam gider (tip = 0)
            string giderSorgu = "SELECT ISNULL(SUM(g.tutar), 0) " +
                "FROM GelirGiderler_4 g INNER JOIN Kategori2 ON " +
                 "g.kategori_id_2 = Kategori2.kategori_id_2 " +
                "WHERE g.kullanici_id = @kullaniciID AND Kategori2.kategori_tip = 0";
            SqlCommand cmdGider = new SqlCommand(giderSorgu, baglanti);
            cmdGider.Parameters.AddWithValue("@kullaniciID", girisYapanKullanciID);

            baglanti.Open();
            decimal toplamGelir = Convert.ToDecimal(cmdGelir.ExecuteScalar());
            decimal toplamGider = Convert.ToDecimal(cmdGider.ExecuteScalar());
            baglanti.Close();

            decimal toplam = toplamGelir + toplamGider;
            if (toplam == 0) toplam = 1; // sıfıra bölme hatası

            decimal gelirYuzde = (toplamGelir / toplam) * 100;
            decimal giderYuzde = (toplamGider / toplam) * 100;

            label21.Text = gelirYuzde.ToString("0.##") + " %";
            label23.Text = giderYuzde.ToString("0.##") + " %";
        }


        /*Yüzdelik hesaplama stoppp*/





        //ödeme türü yükleme başlangıç 
        private void YukleOdemeTuru()
        {
            try
            {
                using (SqlConnection baglanti = new SqlConnection(Database.connectionString))
                {
                    
                    SqlCommand komut = new SqlCommand("SELECT * FROM OdemeTuru2", baglanti);
                    baglanti.Open();
                    using (SqlDataReader dr = komut.ExecuteReader())
                    {
                        
                        string idCol = null;
                        var schema = dr.GetSchemaTable();
                        if (schema != null)
                        {
                            
                            foreach (DataRow r in schema.Rows)
                            {
                                string colName = (r["ColumnName"] ?? string.Empty).ToString();
                                if (string.Equals(colName, "odeme_tur_id_2", StringComparison.OrdinalIgnoreCase))
                                {
                                    idCol = colName;
                                    break;
                                }
                            }
                            if (idCol == null)
                            {
                                foreach (DataRow r in schema.Rows)
                                {
                                    string colName = (r["ColumnName"] ?? string.Empty).ToString();
                                    if (string.Equals(colName, "odeme_tur_id", StringComparison.OrdinalIgnoreCase))
                                    {
                                        idCol = colName;
                                        break;
                                    }
                                }
                            }

                            
                            if (idCol == null)
                            {
                                foreach (DataRow r in schema.Rows)
                                {
                                    var dataType = r["DataType"] as Type;
                                    if (dataType == typeof(int) || dataType == typeof(long) || dataType == typeof(short) || dataType == typeof(byte))
                                    {
                                        idCol = (r["ColumnName"] ?? string.Empty).ToString();
                                        break;
                                    }
                                }
                            }
                        }

                        if (idCol == null)
                        {
                            MessageBox.Show("Ödeme türleri tablosunda uygun bir ID sütunu bulunamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        comboBox2.Items.Clear();

                        while (dr.Read())
                        {
                            string idColValue = dr[idCol]?.ToString() ?? string.Empty;
                            string display = dr["Odeme_adi"]?.ToString() ?? dr[idCol]?.ToString() ?? idColValue;

                            comboBox2.Items.Add(new ComboboxItem
                            {
                                Text = display,
                                Value = idColValue,
                                Extra = ""
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ödeme türleri yüklenirken hata: " + ex.Message);
            }
        }

        //ödeme türü yükleme 


        public Kullanıcıkısmı()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;

           


            labelNetInfo = new Label();
            labelNetInfo.AutoSize = true;
            labelNetInfo.Font = new Font("Segoe UI", 7F, FontStyle.Regular);
            labelNetInfo.Location = new Point(6, 142);
            labelNetInfo.Text = "Gelir: -  Gider: -";
            labelNetInfo.MaximumSize = new Size(380, 0);
            labelNetInfo.AutoEllipsis = true;
            panel5.Controls.Add(labelNetInfo);

            labelBudgetWarning = new Label();
            labelBudgetWarning.AutoSize = true;
            labelBudgetWarning.Font = new Font("Segoe UI", 7F, FontStyle.Bold);
            labelBudgetWarning.Location = new Point(6, 158);
            labelBudgetWarning.ForeColor = Color.DarkRed;
            labelBudgetWarning.Text = string.Empty;
            labelBudgetWarning.MaximumSize = new Size(380, 0);
            labelBudgetWarning.AutoEllipsis = true;
            panel5.Controls.Add(labelBudgetWarning);


        }

        private void ButtonUpdateChart_Click(object sender, EventArgs e)
        {


           

        }

        //  giriş yapan kullanıcı id'sini alır
        public Kullanıcıkısmı(int kullaniciId) : this()
        {
            this.girisYapanKullanciID = kullaniciId;
        }

        private void Kullanıcıkısmı_Load(object sender, EventArgs e)
        {
            /*aylık yüzde 
             * 
             * */

            YuzdeHesapla();

            /*
             * 
             * 
             */
            /*datagridview harcama geçmişini getirme başlangıç*/

            HarcamaGetir();

            //tasarım
            dataGridView1.BorderStyle = BorderStyle.None;
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.Black;
            dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            /*harcama geçmişi bitiş*/



            chart1.Series.Clear();
            chart1.Series.Add("Series1");
            chart1.Series["Series1"].ChartType = SeriesChartType.Column;
            chart1.Series["Series1"].IsValueShownAsLabel = true;
            chart1.Series["Series1"].LabelForeColor = Color.Black;
            chart1.Series["Series1"].Font = new Font("Segoe UI", 10, FontStyle.Bold);
            chart1.Series["Series1"].ToolTip = "#VALY# TL";

            // Chart alanı tasarımı
            chart1.ChartAreas[0].BackColor = Color.WhiteSmoke;
            chart1.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.LightGray;
            chart1.ChartAreas[0].AxisY.Title = "Tutar (TL)";
            chart1.ChartAreas[0].AxisY.TitleFont = new Font("Segoe UI", 11, FontStyle.Bold);

            // Başlık
            chart1.Titles.Clear();
            chart1.Titles.Add("Seçilen Ay Gelir-Gider Grafiği");
            chart1.Titles[0].Font = new Font("Segoe UI", 14, FontStyle.Bold);

            // Legend kaldır
            chart1.Legends.Clear();

            // Örnek barlar ve X etiketleri
            chart1.Series["Series1"].Points.Clear();

            var gelirPoint = chart1.Series["Series1"].Points.AddXY("Gelir", 500); // Gelir


            var giderPoint = chart1.Series["Series1"].Points.AddXY("Gider", 300); // Gider




            // ComboBox4 tasarım
            comboBox4.DropDownStyle = ComboBoxStyle.DropDownList; // Sadece listeden seçim
              // Modern mavi arka plan
            comboBox4.ForeColor = Color.Black;                     // Yazı rengi beyaz
            comboBox4.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            comboBox4.FlatStyle = FlatStyle.Flat;

            // Örnek: İşlem Tipi seçenekleri (Gelir / Gider)
            comboBox4.Items.Clear();
            comboBox4.Items.Add(new ComboboxItem() { Text = "Gelir", Value = "1" });
            comboBox4.Items.Add(new ComboboxItem() { Text = "Gider", Value = "0" });

            comboBox4.SelectedIndex = 0; // Varsayılan olarak Gelir seçili






            /*grafik başlangıç */
            comboBox4.Items.Clear();
            comboBox4.Items.Add(new ComboboxItem() { Text = "Ocak", Value = "1" });
            comboBox4.Items.Add(new ComboboxItem() { Text = "Şubat", Value = "2" });
            comboBox4.Items.Add(new ComboboxItem() { Text = "Mart", Value = "3" });
            comboBox4.Items.Add(new ComboboxItem() { Text = "Nisan", Value = "4" });
            comboBox4.Items.Add(new ComboboxItem() { Text = "Mayıs", Value = "5" });
            comboBox4.Items.Add(new ComboboxItem() { Text = "Haziran", Value = "6" });
            comboBox4.Items.Add(new ComboboxItem() { Text = "Temmuz", Value = "7" });
            comboBox4.Items.Add(new ComboboxItem() { Text = "Ağustos", Value = "8" });
            comboBox4.Items.Add(new ComboboxItem() { Text = "Eylül", Value = "9" });
            comboBox4.Items.Add(new ComboboxItem() { Text = "Ekim", Value = "10" });
            comboBox4.Items.Add(new ComboboxItem() { Text = "Kasım", Value = "11" });
            comboBox4.Items.Add(new ComboboxItem() { Text = "Aralık", Value = "12" });


            /*grafik bitiş */

            comboBox3.Items.Clear();
            comboBox3.Items.Add(new ComboboxItem { Text = "Gelir", Value = "1" });
            comboBox3.Items.Add(new ComboboxItem { Text = "Gider", Value = "0" });

            comboBox3.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox3.SelectedIndex = 0;

            // sade tarih görünümü (eski haline yakın)
            dateTimePicker1.Format = DateTimePickerFormat.Custom;
            dateTimePicker1.CustomFormat = "dd.MM.yyyy"; // tarih
            dateTimePicker1.ShowUpDown = false;
            dateTimePicker1.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            dateTimePicker1.Width = 184;
            dateTimePicker1.CalendarForeColor = Color.DarkBlue;
            dateTimePicker1.CalendarMonthBackground = Color.LightYellow;


            //kategorileri yükleme başlangıç 

            //ilgili comcox1 tasarımı 
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;  // kullanıcı sadece seçim yapabilir
            comboBox1.FlatStyle = FlatStyle.Flat;                 // modern görünüm
            
            comboBox1.ForeColor = Color.Black;                   // yazı rengi
            comboBox1.Font = new Font("Segoe UI", 8, FontStyle.Regular);  // okunaklı font
            comboBox1.ItemHeight = 25;                           // satır yüksekliği
            comboBox1.DropDownHeight = 150;                      // açılır pencere yüksekliği
            comboBox1.MaxDropDownItems = 5;                      // kaç tane gösterilsin

            comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox2.FlatStyle = FlatStyle.Flat;
           
            comboBox2.ForeColor = Color.Black;
            comboBox2.Font = new Font("Segoe UI", 8, FontStyle.Regular);
            comboBox2.ItemHeight = 25;
            comboBox2.DropDownHeight = 150;
            comboBox2.MaxDropDownItems = 5;

            comboBox3.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox3.FlatStyle = FlatStyle.Flat;
           
            comboBox3.ForeColor = Color.Black;
            comboBox3.Font = new Font("Segoe UI", 8, FontStyle.Regular);
            comboBox3.ItemHeight = 25;
            comboBox3.DropDownHeight = 150;
            comboBox3.MaxDropDownItems = 5;


                 
            richTextBox1.ForeColor = Color.Black;             // yazı rengi
            richTextBox1.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            richTextBox1.BorderStyle = BorderStyle.FixedSingle; // modern çerçeve
            richTextBox1.ScrollBars = RichTextBoxScrollBars.Vertical; // dikey scroll
            richTextBox1.MaxLength = 500;

            //------------------------------//

            try
            {
                SqlConnection baglanti = new SqlConnection(Database.connectionString); // kendi connection string
                SqlCommand komut = new SqlCommand("select kategori_id_2, kategori_adi, kategori_tip from Kategori2", baglanti);

                baglanti.Open();
                SqlDataReader dr = komut.ExecuteReader();

                comboBox1.Items.Clear();
                while (dr.Read())
                {
                    comboBox1.Items.Add(new ComboboxItem
                    {
                        Text = dr["kategori_adi"].ToString(),
                        Value = dr["kategori_id_2"].ToString(),
                        Extra = dr["kategori_tip"].ToString()
                    });
                }

                baglanti.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kategori yüklenirken hata: " + ex.Message);
            }


            //kategori bitiş

            //-------------------------------//

            //Ödeme Türü başlangıç 

            YukleOdemeTuru();


            //Ödeme türü Bitiş//

            //tutar giriş kısmı //

                 
            textBox1.ForeColor = Color.Black;             // okunaklı yazı rengi
            textBox1.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            textBox1.TextAlign = HorizontalAlignment.Right; // sayı gibi sağa hizala

            // varsayılan tutar görüntüsü 
            textBox1.Text = string.Empty;

            
            try
            {
                originalLabel15Font = label15.Font;
            }
            catch
            {
                originalLabel15Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            }

            // Başlangıçta özet etiketlerini gizle; kullanıcı "Güncelle" dediğinde gösterilecek
            try
            {
                label9.Visible = false;
                label11.Visible = false;
                label13.Visible = false;
                label15.Visible = false;
            }
            catch { }

           
            refreshTimer = new Timer();
            refreshTimer.Interval = 3000; // 3 seconds
            refreshTimer.Tick += (s, ea) =>
            {
                refreshTimer.Stop();


                UpdateMonthlySummary();

            
                try
                {
                    label9.Visible = true;
                    label11.Visible = true;
                    label13.Visible = true;
                    label15.Visible = true;
                }
                catch { }
            };

    
            try
            {

                int yil = DateTime.Now.Year;
                using (SqlConnection conn = new SqlConnection(Database.connectionString))
                {
                    conn.Open();
                    string sqlMonths = @"SELECT DISTINCT MONTH(tarih) AS m FROM GelirGiderler_4 WHERE YEAR(tarih) = @y";
                    if (girisYapanKullanciID > 0) sqlMonths += " AND kullanici_id = @uid";
                    sqlMonths += " ORDER BY m";

                    using (SqlCommand cmd = new SqlCommand(sqlMonths, conn))
                    {
                        cmd.Parameters.AddWithValue("@y", yil);
                        if (girisYapanKullanciID > 0) cmd.Parameters.AddWithValue("@uid", girisYapanKullanciID);
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                int m = 0;
                                int.TryParse(dr["m"].ToString(), out m);
                                if (m >= 1 && m <= 12)
                                {
                                    string name = DateTimeFormatInfo.CurrentInfo.MonthNames[m - 1];

                                }
                            }
                        }
                    }
                }
               
            }
            catch { }

            
        }

        // Aylık bazda gelir/gider grafiğini güncelle (varsayılan: seçili ay veya cari ay)
        private void UpdateMonthlyChart(int year = 0, int month = 0, int month2 = 0)
        {
            try
            {
                int kullaniciId = this.girisYapanKullanciID;
                if (year <= 0) year = DateTime.Now.Year;
                if (month <= 0) month = DateTime.Now.Month;

                
                var kategoriList = new List<string>();
                var gelirList = new List<decimal>();
                var giderList = new List<decimal>();

                using (SqlConnection conn = new SqlConnection(Database.connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT k.kategori_adi,
                                    SUM(CASE WHEN k.kategori_tip = 1 THEN g.tutar ELSE 0 END) AS gelir,
                                    SUM(CASE WHEN k.kategori_tip = 0 THEN g.tutar ELSE 0 END) AS gider
                            FROM GelirGiderler_4 g
                            JOIN Kategori2 k ON g.kategori_id_2 = k.kategori_id_2
                            WHERE YEAR(g.tarih) = @y AND MONTH(g.tarih) = @m";

                    if (kullaniciId > 0)
                        sql += " AND g.kullanici_id = @uid";

                    sql += " GROUP BY k.kategori_adi ORDER BY (SUM(CASE WHEN k.kategori_tip = 1 THEN g.tutar ELSE 0 END) + SUM(CASE WHEN k.kategori_tip = 0 THEN g.tutar ELSE 0 END)) DESC";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@y", year);
                        cmd.Parameters.AddWithValue("@m", month);
                        if (kullaniciId > 0) cmd.Parameters.AddWithValue("@uid", kullaniciId);

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                kategoriList.Add(dr["kategori_adi"].ToString());
                                decimal g1 = 0, g2 = 0;
                                decimal.TryParse(dr["gelir"].ToString(), out g1);
                                decimal.TryParse(dr["gider"].ToString(), out g2);
                                gelirList.Add(g1);
                                giderList.Add(g2);
                            }
                        }
                    }
                }

                
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => RenderChart(kategoriList, gelirList, giderList, month, year)));
                }
                else
                {
                    RenderChart(kategoriList, gelirList, giderList, month, year);
                }

               
                try
                {
                    decimal toplamGelir = gelirList.Sum();
                    decimal toplamGider = giderList.Sum();
                    decimal net = toplamGelir - toplamGider;
                    labelNetInfo.Text = $"Gelir: {toplamGelir:C}  Gider: {toplamGider:C}  Net: {net:C}";
                    if (net < 0)
                    {
                        labelBudgetWarning.Text = "Bütçe aşıldı — negatif bakiye";
                        labelBudgetWarning.ForeColor = Color.DarkRed;
                    }
                    else if (net == 0)
                    {
                        labelBudgetWarning.Text = "Bütçe dengede";
                        labelBudgetWarning.ForeColor = Color.DarkOrange;
                    }
                    else
                    {
                        labelBudgetWarning.Text = "Bütçe uygun — pozitif net";
                        labelBudgetWarning.ForeColor = Color.DarkGreen;
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Grafik güncellenirken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        
        private void RenderChart(List<string> categories, List<decimal> gelir, List<decimal> gider, int month, int year)
        {
            try
            {
                if (chart1 == null) return;

                chart1.Series.Clear();
                chart1.ChartAreas.Clear();

                var area = new ChartArea("Default");
                chart1.ChartAreas.Add(area);

                var sGelir = new Series("Gelir") { ChartType = SeriesChartType.Column, XValueType = ChartValueType.String };
                var sGider = new Series("Gider") { ChartType = SeriesChartType.Column, XValueType = ChartValueType.String };

                sGelir.Color = Color.SeaGreen;
                sGider.Color = Color.IndianRed;

                chart1.Series.Add(sGelir);
                chart1.Series.Add(sGider);

                for (int i = 0; i < categories.Count; i++)
                {
                    string cat = categories[i];
                    decimal g = gelir.Count > i ? gelir[i] : 0M;
                    decimal gd = gider.Count > i ? gider[i] : 0M;

                    sGelir.Points.AddXY(cat, (double)g);
                    sGider.Points.AddXY(cat, (double)gd);
                }

                chart1.Legends.Clear();
                chart1.Legends.Add(new Legend("Legend"));
                chart1.ChartAreas[0].AxisX.LabelStyle.Angle = -45;
                chart1.ChartAreas[0].RecalculateAxesScale();
                chart1.Invalidate();
            }
            catch { }
        }

        // Güncelle butonu sadece özet label'ları güncellesin, grafiği etkilemesin
        private void buttonRefreshSummary_Click(object sender, EventArgs e)
        {

            label9.Visible = true;
            label11.Visible = true;
            label13.Visible = true;
            label15.Visible = true;
            int kullaniciID = girisYapanKullanciID; // login’den gelen ID

            using (SqlConnection baglanti = new SqlConnection(Database.connectionString))
            {
                baglanti.Open();

                // 1️⃣ En çok harcama yapılan kategori
                SqlCommand cmdEnCok = new SqlCommand(@"
            SELECT TOP 1 k.kategori_adi
            FROM GelirGiderler_4 g
            INNER JOIN Kategori2 k ON g.kategori_id_2 = k.kategori_id_2
            WHERE g.kullanici_id = @kullaniciID
              AND k.kategori_tip = 0  -- sadece gider
            GROUP BY k.kategori_adi
            ORDER BY SUM(g.tutar) DESC", baglanti);
                cmdEnCok.Parameters.AddWithValue("@kullaniciID", kullaniciID);

                object sonucKategori = cmdEnCok.ExecuteScalar();
                label9.Text = sonucKategori != null ? sonucKategori.ToString() : "Yok";

                // 2️⃣ Toplam gelir
                SqlCommand cmdGelir = new SqlCommand(@"
            SELECT ISNULL(SUM(g.tutar),0)
            FROM GelirGiderler_4 g
            INNER JOIN Kategori2 k ON g.kategori_id_2 = k.kategori_id_2
            WHERE g.kullanici_id = @kullaniciID
              AND k.kategori_tip = 1", baglanti); // gelir
                cmdGelir.Parameters.AddWithValue("@kullaniciID", kullaniciID);

                decimal toplamGelir = Convert.ToDecimal(cmdGelir.ExecuteScalar());
                label11.Text = toplamGelir.ToString("C");

                // 3️⃣ Toplam gider
                SqlCommand cmdGider = new SqlCommand(@"
            SELECT ISNULL(SUM(g.tutar),0)
            FROM GelirGiderler_4 g
            INNER JOIN Kategori2 ON g.kategori_id_2 = Kategori2.kategori_id_2
            WHERE g.kullanici_id = @kullaniciID
              AND Kategori2.kategori_tip = 0", baglanti); // gider
                cmdGider.Parameters.AddWithValue("@kullaniciID", kullaniciID);

                decimal toplamGider = Convert.ToDecimal(cmdGider.ExecuteScalar());
                label13.Text = toplamGider.ToString("C");

                // 4️⃣ Kalan para
                decimal kalan = toplamGelir - toplamGider;
                label15.Text = kalan.ToString("C");

                baglanti.Close();
            }

        }

        private void UpdateMonthlySummary()
        {
            try
            {
                decimal toplamGelir = 0M;
                decimal toplamGider = 0M;
                string enCokKategori = "(yok)";

                int kullaniciId = this.girisYapanKullanciID;
                int ay = DateTime.Now.Month;
                int yil = DateTime.Now.Year;

                using (SqlConnection conn = new SqlConnection(Database.connectionString))
                {
                    conn.Open();

                    // Toplam gelir
                    using (SqlCommand cmd = new SqlCommand(@"SELECT ISNULL(SUM(g.tutar),0) FROM GelirGiderler_4 g JOIN Kategori2 k ON g.kategori_id_2 = k.kategori_id_2 WHERE g.kullanici_id=@uid AND k.kategori_tip=1 AND MONTH(g.tarih)=@m AND YEAR(g.tarih)=@y", conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", kullaniciId);
                        cmd.Parameters.AddWithValue("@m", ay);
                        cmd.Parameters.AddWithValue("@y", yil);
                        object o = cmd.ExecuteScalar();
                        if (o != null && o != DBNull.Value) decimal.TryParse(o.ToString(), out toplamGelir);
                    }

                    // Toplam gider
                    using (SqlCommand cmd2 = new SqlCommand(@"SELECT ISNULL(SUM(g.tutar),0) FROM GelirGiderler_4 g JOIN Kategori2 k ON g.kategori_id_2 = k.kategori_id_2 WHERE g.kullanici_id=@uid AND k.kategori_tip=0 AND MONTH(g.tarih)=@m AND YEAR(g.tarih)=@y", conn))
                    {
                        cmd2.Parameters.AddWithValue("@uid", kullaniciId);
                        cmd2.Parameters.AddWithValue("@m", ay);
                        cmd2.Parameters.AddWithValue("@y", yil);
                        object o2 = cmd2.ExecuteScalar();
                        if (o2 != null && o2 != DBNull.Value) decimal.TryParse(o2.ToString(), out toplamGider);
                    }

                    // En çok harcama yapılan kategori
                    using (SqlCommand cmd3 = new SqlCommand(@"SELECT TOP 1 k.kategori_adi, ISNULL(SUM(g.tutar),0) AS toplam FROM GelirGiderler_4 g JOIN Kategori2 k ON g.kategori_id_2 = k.kategori_id_2 WHERE g.kullanici_id=@uid AND k.kategori_tip=0 AND MONTH(g.tarih)=@m AND YEAR(g.tarih)=@y GROUP BY k.kategori_adi ORDER BY toplam DESC", conn))
                    {
                        cmd3.Parameters.AddWithValue("@uid", kullaniciId);
                        cmd3.Parameters.AddWithValue("@m", ay);
                        cmd3.Parameters.AddWithValue("@y", yil);
                        using (SqlDataReader dr = cmd3.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                enCokKategori = dr["kategori_adi"].ToString();
                            }
                        }
                    }
                }

                decimal kalan = toplamGelir - toplamGider;
                if (label9 != null) label9.Text = enCokKategori;
                if (label11 != null) label11.Text = toplamGelir.ToString("C");
                if (label13 != null) label13.Text = toplamGider.ToString("C");
                if (label15 != null)
                {
                    label15.Text = kalan.ToString("C");
                    try
                    {
                        label15.ForeColor = kalan < 0 ? Color.DarkRed : Color.Black;
                        label15.Font = kalan < 0 ? new Font(originalLabel15Font.FontFamily, originalLabel15Font.Size + 2, FontStyle.Bold) : originalLabel15Font;
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Özet güncellenirken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            
            if (comboBox1.SelectedItem == null) return;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
           
            if (comboBox2.SelectedItem == null) return;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            {
                e.Handled = true;
            }

            if (e.KeyChar == '.' && textBox1.Text.Contains("."))
            {
                e.Handled = true;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
            if (button1.Tag != null && button1.Tag is bool && (bool)button1.Tag == true)
                return;
            button1.Tag = true;
            button1.Enabled = false;

            try
            {
                
                if (!decimal.TryParse(textBox1.Text, out decimal tutar))
                {
                    MessageBox.Show("Harcamalar kısmını doldurduğunuzdan emin olun.", "Geçersiz Giriş", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (comboBox1.SelectedItem == null)
                {
                    MessageBox.Show("Lütfen kategori seçin.", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (comboBox2.SelectedItem == null)
                {
                    MessageBox.Show("Lütfen ödeme türü seçin.", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (girisYapanKullanciID <= 0)
                {
                    MessageBox.Show("Kullanıcı bilgisi bulunamadı. Lütfen tekrar giriş yapın.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var kategoriItem = (ComboboxItem)comboBox1.SelectedItem;
                var odemeItem = (ComboboxItem)comboBox2.SelectedItem;

                if (string.IsNullOrWhiteSpace(kategoriItem.Value))
                {
                    MessageBox.Show("Seçilen kategori ID boş.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (string.IsNullOrWhiteSpace(odemeItem.Value))
                {
                    MessageBox.Show("Seçilen ödeme türü ID boş.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!int.TryParse(kategoriItem.Value, out int kategoriId))
                {
                    MessageBox.Show("Kategori ID geçersiz.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!int.TryParse(odemeItem.Value, out int odemeId))
                {
                    MessageBox.Show("Ödeme türü ID geçersiz.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string aciklama = richTextBox1.Text;
                DateTime tarih = dateTimePicker1.Value;

                using (SqlConnection baglanti = new SqlConnection(Database.connectionString))
                {
                    baglanti.Open();

                    
                    string GetExistingTable(SqlConnection conn, string[] candidates)
                    {
                        foreach (var t in candidates)
                        {
                            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @t", conn))
                            {
                                cmd.Parameters.AddWithValue("@t", t);
                                int exists = Convert.ToInt32(cmd.ExecuteScalar());
                                if (exists > 0) return t;
                            }
                        }
                        return null;
                    }

                    string GetExistingColumn(SqlConnection conn, string tableName, string[] candidates)
                    {
                        if (string.IsNullOrEmpty(tableName)) return null;
                        foreach (var c in candidates)
                        {
                            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME=@t AND COLUMN_NAME=@c", conn))
                            {
                                cmd.Parameters.AddWithValue("@t", tableName);
                                cmd.Parameters.AddWithValue("@c", c);
                                int exists = Convert.ToInt32(cmd.ExecuteScalar());
                                if (exists > 0) return c;
                            }
                        }
                        return null;
                    }

                    
                    string[] tableCandidates = new[] { "GelirGiderler_4", "IncomeExpenses_new", "GelirGiderler_4", "IncomeExpenses" };
                    string transTable = GetExistingTable(baglanti, tableCandidates) ?? "GelirGiderler_4"; // fallback

                    
                    string odemeCol = GetExistingColumn(baglanti, transTable, new[] { "odeme_tur_id_2", "odeme_tur_id" }) ?? "odeme_tur_id_2";
                    string kategoriCol = GetExistingColumn(baglanti, transTable, new[] { "kategori_id_2", "kategori_id" }) ?? "kategori_id_2";

                   
                    
                    string insertSql = $"INSERT INTO GelirGiderler_4 (tutar, aciklama, tarih, kullanici_id, {odemeCol}, {kategoriCol}) VALUES (@tutar, @aciklama, @Tarih, @kullanici_id, @odemeId, @kategoriId)";

                    using (SqlCommand komut = new SqlCommand(insertSql, baglanti))
                    {
                        komut.Parameters.AddWithValue("@tutar", tutar);
                        komut.Parameters.AddWithValue("@aciklama", string.IsNullOrWhiteSpace(aciklama) ? (object)DBNull.Value : aciklama);
                        komut.Parameters.AddWithValue("@Tarih", tarih);
                        komut.Parameters.AddWithValue("@kullanici_id", girisYapanKullanciID);
                        komut.Parameters.AddWithValue("@odemeId", odemeId);
                        komut.Parameters.AddWithValue("@kategoriId", kategoriId);

                        int rows = komut.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            MessageBox.Show("Kayıt başarılı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            textBox1.Text = string.Empty;
                            richTextBox1.Clear();
                            comboBox1.SelectedIndex = 0;
                            comboBox2.SelectedIndex = 0;
                            dateTimePicker1.Value = DateTime.Now;

                            // refresh view
                            HarcamaGetir();
                        }
                        else
                        {
                            MessageBox.Show("Kayıt yapılamadı. Lütfen tekrar deneyin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kaydetme hatası: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                button1.Tag = false;
                button1.Enabled = true;
            }
        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox4.SelectedItem == null) return;

            ComboboxItem secilenAy = (ComboboxItem)comboBox4.SelectedItem;
            int ay = Convert.ToInt32(secilenAy.Value);

            // Grafiği temizle
            chart1.Series["Series1"].Points.Clear();

            using (SqlConnection baglanti = new SqlConnection(Database.connectionString))
            {
                string sql = @"
    SELECT k.kategori_tip, SUM(g.tutar) AS Toplam
    FROM GelirGiderler_4 g
    INNER JOIN Kategori2 k ON g.kategori_id_2 = k.kategori_id_2
    WHERE g.kullanici_id = @userId AND MONTH(g.tarih) = @Ay
    GROUP BY k.kategori_tip";

                SqlCommand cmd = new SqlCommand(sql, baglanti);
                cmd.Parameters.AddWithValue("@userId", girisYapanKullanciID);
                cmd.Parameters.AddWithValue("@Ay", ay);

                baglanti.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    int islemTipi = Convert.ToInt32(dr["kategori_tip"]); // 1 = Gelir, 2 = Gider
                    decimal toplam = Convert.ToDecimal(dr["Toplam"]);
                    string label = (islemTipi == 1) ? "Gelir" : "Gider";

                    chart1.Series["Series1"].Points.AddXY(label, toplam);
                }

                baglanti.Close();
            }
        }

        

            /*harcama geçmişini arama*/
           /* string aranacak = textBox2.Text.ToLower();

            foreach (DataGridViewRow satir in dataGridView1.Rows)
            {
                if (satir.Cells[0].Value != null)
                {
                    if (satir.Cells[0].Value.ToString().ToLower().Contains(aranacak))
                    {
                        satir.Selected = true;

                        dataGridView1.FirstDisplayedScrollingRowIndex = satir.Index;
                        break;
                    }
                }
            }*/
        

        private void button3_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "CSV Dosyası|*.csv";
            sfd.FileName = "HarcamaRaporu.csv";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter sw = new StreamWriter(sfd.FileName))
                {
                    // 1️⃣ Label verilerini yaz
                    sw.WriteLine("\"En Çok Harcama Kategorisi\",\"" + (label9?.Text ?? "") + "\"");
                    sw.WriteLine("\"Bu Ay Toplam Gelir\",\"" + (label11?.Text ?? "") + "\"");
                    sw.WriteLine("\"Bu Ay Toplam Gider\",\"" + (label13?.Text ?? "") + "\"");
                    sw.WriteLine("\"Kalan Para\",\"" + (label15?.Text ?? "") + "\"");
                    sw.WriteLine("\"Aylık Gelir Yüzdesi\",\"" + (label21?.Text ?? "") + "\"");
                    sw.WriteLine("\"Aylık Gider Yüzdesi\",\"" + (label23?.Text ?? "") + "\"");

                    // 2️⃣ DataGridView başlıklarını yaz
                    for (int i = 0; i < dataGridView1.Columns.Count; i++)
                    {
                        sw.Write("\"" + dataGridView1.Columns[i].HeaderText + "\"");
                        if (i < dataGridView1.Columns.Count - 1)
                            sw.Write(",");
                    }
                    sw.WriteLine();

                    // 3️⃣ DataGridView verilerini yaz
                    for (int i = 0; i < dataGridView1.Rows.Count; i++)
                    {
                        for (int j = 0; j < dataGridView1.Columns.Count; j++)
                        {
                            sw.Write("\"" + dataGridView1.Rows[i].Cells[j].Value?.ToString() + "\"");
                            if (j < dataGridView1.Columns.Count - 1)
                                sw.Write(",");
                        }
                        sw.WriteLine();
                    }
                }

                MessageBox.Show("CSV dosyası başarıyla oluşturuldu!", "CSV DOSYASINI EXCELDEN AÇABİLİRSİNİZ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
          
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
                    JOIN Kategori2 k ON g.kategori_id_2 = k.kategori_id_2
                    WHERE MONTH(g.tarih) = @month AND YEAR(g.tarih) = @year AND g.kullanici_id = @userId";

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
                    sfd.Filter = "CSV Dosyası|*.csv";
                    sfd.FileName = "12AyHarcamaAnalizi.csv";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        string filePath = sfd.FileName;

                        using (StreamWriter sw = new StreamWriter(filePath))
                        {
                          
                            sw.WriteLine("Ay,Gelir,Gider");

                           
                            foreach (DataRow row in data.Rows)
                            {
                                sw.WriteLine($"{row["Ay"]},{row["Gelir"]},{row["Gider"]}");
                            }
                        }

                        
                        MessageBox.Show("12 ayın verileri hazır! Analiz sitesine yönlendiriliyorsunuz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        
                        System.Diagnostics.Process.Start("https://datastudio.google.com");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Bir hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void button5_Click_1(object sender, EventArgs e)
        {
           
            DialogResult result = MessageBox.Show(
                "Tüm 12 ayın gelir-gider verileri analiz için hazırlanacak ve sizi Zoho Analytics'e yönlendireceğiz. Devam etmek ister misiniz?",
                "Analiz Onayı",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                try
                {
                 
                    DataTable data = new DataTable();
                    data.Columns.Add("Ay", typeof(string));
                    data.Columns.Add("Gelir", typeof(int));
                    data.Columns.Add("Gider", typeof(int));

                    using (SqlConnection conn = new SqlConnection(Database.connectionString))
                    {
                        conn.Open();
                        for (int month = 1; month <= 12; month++)
                        {
                            string query = @"SELECT 
                        ISNULL(SUM(CASE WHEN k.kategori_tip = 1 THEN g.tutar ELSE 0 END), 0) AS Gelir,
                        ISNULL(SUM(CASE WHEN k.kategori_tip = 0 THEN g.tutar ELSE 0 END), 0) AS Gider
                    FROM GelirGiderler_4 g
                    JOIN Kategori2 k ON g.kategori_id_2 = k.kategori_id_2
                    WHERE MONTH(g.tarih) = @month AND YEAR(g.tarih) = @year AND g.kullanici_id = @userId";

                            using (SqlCommand cmd = new SqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@month", month);
                                cmd.Parameters.AddWithValue("@year", DateTime.Now.Year);
                                cmd.Parameters.AddWithValue("@userId", girisYapanKullanciID);

                                using (SqlDataReader reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        int gelir = Convert.ToInt32(reader.GetDecimal(0));
                                        int gider = Convert.ToInt32(reader.GetDecimal(1));
                                        string ayAdi = DateTimeFormatInfo.CurrentInfo.GetMonthName(month);
                                        data.Rows.Add(ayAdi, gelir, gider);
                                    }
                                }
                            }
                        }
                    }

                  
                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.Filter = "CSV Dosyası|*.csv";
                    sfd.FileName = "12AyHarcamaAnalizi.csv";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        string filePath = sfd.FileName;

                        using (StreamWriter sw = new StreamWriter(filePath))
                        {
                           
                            sw.WriteLine("Ay,Gelir,Gider");

                            foreach (DataRow row in data.Rows)
                            {
                                sw.WriteLine($"{row["Ay"]},{row["Gelir"]},{row["Gider"]}");
                            }
                        }

                       
                        MessageBox.Show("12 ayın verileri hazır! Zoho Analytics'e yönlendiriliyorsunuz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        
                        System.Diagnostics.Process.Start("https://www.zoho.com/analytics/");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Bir hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            
            string enCokHarcamaKategori = label9.Text;
            string aylikGelirYuzdesiText = label21.Text.Replace("%", "").Trim();
            string aylikGiderYuzdesiText = label23.Text.Replace("%", "").Trim();

            if (!decimal.TryParse(aylikGelirYuzdesiText, out decimal aylikGelirYuzdesi))
            {
                aylikGelirYuzdesi = 0;
            }

            if (!decimal.TryParse(aylikGiderYuzdesiText, out decimal aylikGiderYuzdesi))
            {
                aylikGiderYuzdesi = 0;
            }

            
            if (aylikGiderYuzdesi > 50)
            {
                string uyarıMesajı = "Harcamalarınız olağanüstü seviyede. En çok harcama yaptığınız kategori: " + enCokHarcamaKategori + ".\n" +
                                     "Tavsiyemiz: Harcamalarınızı acilen gözden geçirin ve özellikle " + enCokHarcamaKategori + " kategorisinden uzak durmaya çalışın.";
                MessageBox.Show(uyarıMesajı, "Acil Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (aylikGiderYuzdesi > 40)
            {
                string uyarıMesajı = "Harcamalarınız çok yüksek. En çok harcama yaptığınız kategori: " + enCokHarcamaKategori + ".\n" +
                                     "Tavsiyemiz: Harcamalarınızı azaltmaya çalışın ve bütçenizi dengeleyin.";
                MessageBox.Show(uyarıMesajı, "Ciddi Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (aylikGiderYuzdesi > 30)
            {
                string uyarıMesajı = "Harcamalarınız dikkat edilmesi gereken bir seviyede. En çok harcama yaptığınız kategori: " + enCokHarcamaKategori + ".\n" +
                                     "Tavsiyemiz: Harcamalarınızı kontrol altında tutmaya devam edin.";
                MessageBox.Show(uyarıMesajı, "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (aylikGiderYuzdesi > 20)
            {
                string bilgilendirmeMesajı = "Harcamalarınız artıyor. En çok harcama yaptığınız kategori: " + enCokHarcamaKategori + ".\n" +
                                             "Bilgilendirme: Harcamalarınızı kontrol altında tutmaya devam edin.";
                MessageBox.Show(bilgilendirmeMesajı, "Bilgilendirme", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (aylikGiderYuzdesi > 10)
            {
                string bilgilendirmeMesajı = "Harcamalarınız düşük seviyede. En çok harcama yaptığınız kategori: " + enCokHarcamaKategori + ".\n" +
                                             "Bilgilendirme: Harcamalarınız dengeli görünüyor.";
                MessageBox.Show(bilgilendirmeMesajı, "Bilgilendirme", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                string tavsiyeMesajı = "Harcamalarınız dengeli, herhangi bir uyarı bulunmamaktadır.\n" +
                                       "Tavsiyemiz: Gelirinizin bir kısmını birikim hesabına aktararak gelecekteki ihtiyaçlarınız için hazırlık yapabilirsiniz.";
                MessageBox.Show(tavsiyeMesajı, "Birikim Tavsiyesi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        
        private void ShowLoginError(string message)
        {
            MessageBox.Show(message, "Giriş Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            
            this.Hide();
            Girisformu girisFormu = new Girisformu();
            girisFormu.Show();
        }

        private void button10_Click(object sender, EventArgs e)
        {
           
            Application.Exit();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(Database.connectionString))
                {
                    conn.Open();

                    
                    string expenseQuery = @"SELECT TOP 3 k.kategori_adi, SUM(g.tutar) AS toplam 
                                    FROM GelirGiderler_4 g
                                    JOIN Kategori2 k ON g.kategori_id_2 = k.kategori_id_2
                                    WHERE g.kullanici_id = @userId AND k.kategori_tip = 0
                                    GROUP BY k.kategori_adi
                                    ORDER BY toplam DESC";

                    SqlCommand expenseCmd = new SqlCommand(expenseQuery, conn);
                    expenseCmd.Parameters.AddWithValue("@userId", girisYapanKullanciID);

                    List<string> topExpenses = new List<string>();
                    using (SqlDataReader reader = expenseCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string category = reader["kategori_adi"].ToString();
                            decimal amount = Convert.ToDecimal(reader["toplam"]);
                            topExpenses.Add($"{category}: {amount:C}");
                        }
                    }

                   
                    string incomeQuery = @"SELECT TOP 2 k.kategori_adi, SUM(g.tutar) AS toplam 
                                    FROM GelirGiderler_4 g
                                    JOIN Kategori2 k ON g.kategori_id_2 = k.kategori_id_2
                                    WHERE g.kullanici_id = @userId AND k.kategori_tip = 1
                                    GROUP BY k.kategori_adi
                                    ORDER BY toplam DESC";

                    SqlCommand incomeCmd = new SqlCommand(incomeQuery, conn);
                    incomeCmd.Parameters.AddWithValue("@userId", girisYapanKullanciID);

                    List<string> topIncomes = new List<string>();
                    using (SqlDataReader reader = incomeCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string category = reader["kategori_adi"].ToString();
                            decimal amount = Convert.ToDecimal(reader["toplam"]);
                            topIncomes.Add($"{category}: {amount:C}");
                        }
                    }

                   
                    string message = "Öneri: \n\n";

                    if (topExpenses.Count > 0)
                    {
                        message += "En çok harcama yaptığınız ilk 3 kategori:\n";
                        message += string.Join("\n", topExpenses);
                        message += "\n\n";
                    }

                    if (topIncomes.Count > 0)
                    {
                        message += "En çok gelir elde ettiğiniz ilk 2 kategori:\n";
                        message += string.Join("\n", topIncomes);
                        message += "\n\n";
                    }

                    message += "Tavsiyemiz: Gelir elde ettiğiniz kategorilere odaklanarak harcamalarınızı azaltabilirsiniz.";

                   
                    MessageBox.Show(message, "Öneri", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Öneri oluşturulurken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button9_Click_1(object sender, EventArgs e)
        {
            // Navigate to Girisformu
            this.Hide();
            Girisformu girisFormu = new Girisformu();
            girisFormu.Show();
        }

        private void button10_Click_1(object sender, EventArgs e)
        {
            // Close the application
            Application.Exit();
        }

        private void button8_Click_1(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(Database.connectionString))
                {
                    conn.Open();

                    // Get top 3 expense categories
                    string expenseQuery = @"SELECT TOP 3 k.kategori_adi, SUM(g.tutar) AS toplam 
                                    FROM GelirGiderler_4 g
                                    JOIN Kategori2 k ON g.kategori_id_2 = k.kategori_id_2
                                    WHERE g.kullanici_id = @userId AND k.kategori_tip = 0
                                    GROUP BY k.kategori_adi
                                    ORDER BY toplam DESC";

                    SqlCommand expenseCmd = new SqlCommand(expenseQuery, conn);
                    expenseCmd.Parameters.AddWithValue("@userId", girisYapanKullanciID);

                    List<string> topExpenses = new List<string>();
                    using (SqlDataReader reader = expenseCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string category = reader["kategori_adi"].ToString();
                            decimal amount = Convert.ToDecimal(reader["toplam"]);
                            topExpenses.Add($"{category}: {amount:C}");
                        }
                    }

                   
                    string incomeQuery = @"SELECT TOP 2 k.kategori_adi, SUM(g.tutar) AS toplam 
                                    FROM GelirGiderler_4 g
                                    JOIN Kategori2 k ON g.kategori_id_2 = k.kategori_id_2
                                    WHERE g.kullanici_id = @userId AND k.kategori_tip = 1
                                    GROUP BY k.kategori_adi
                                    ORDER BY toplam DESC";

                    SqlCommand incomeCmd = new SqlCommand(incomeQuery, conn);
                    incomeCmd.Parameters.AddWithValue("@userId", girisYapanKullanciID);

                    List<string> topIncomes = new List<string>();
                    using (SqlDataReader reader = incomeCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string category = reader["kategori_adi"].ToString();
                            decimal amount = Convert.ToDecimal(reader["toplam"]);
                            topIncomes.Add($"{category}: {amount:C}");
                        }
                    }

                   
                    string message = "Öneri: \n\n";

                    if (topExpenses.Count > 0)
                    {
                        message += "En çok harcama yaptığınız ilk 3 kategori:\n";
                        message += string.Join("\n", topExpenses);
                        message += "\n\n";
                    }

                    if (topIncomes.Count > 0)
                    {
                        message += "En çok gelir elde ettiğiniz ilk 2 kategori:\n";
                        message += string.Join("\n", topIncomes);
                        message += "\n\n";
                    }

                    message += "Tavsiyemiz: Gelir elde ettiğiniz kategorilere odaklanarak harcamalarınızı azaltabilirsiniz.";

                   
                    MessageBox.Show(message, "Öneri", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Öneri oluşturulurken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                // Veritabanından son 3 ayın finansal verilerini çek
                DataTable financialData = new DataTable();
                using (SqlConnection conn = new SqlConnection(Database.connectionString))
                {
                    conn.Open();
                    string query = @"SELECT kategori_adi, tutar, tarih, aciklama 
                         FROM GelirGiderler_4 g
                         JOIN Kategori2 k ON g.kategori_id_2 = k.kategori_id_2
                         WHERE g.kullanici_id = @userId AND tarih >= DATEADD(MONTH, -3, GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", girisYapanKullanciID);
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(financialData);
                        }
                    }
                }

                // Verileri prompt formatına dönüştür
                StringBuilder promptBuilder = new StringBuilder();
                promptBuilder.AppendLine("Bütün Aylık Kullanıcı Harcamaları ve Açıklamaları:Harcamalar hakkında detaylı bilgi almak istiyorum");
                foreach (DataRow row in financialData.Rows)
                {
                    promptBuilder.AppendLine($"Kategori: {row["kategori_adi"]}, Tutar: {row["tutar"]}, Tarih: {Convert.ToDateTime(row["tarih"]).ToString("yyyy-MM-dd")}, Açıklama: {row["aciklama"]}");
                }

                string prompt = promptBuilder.ToString();

                // Kullanıcıya promptu kopyalaması için özel bir form göster
                System.Windows.Forms.Form copyForm = new System.Windows.Forms.Form();
                copyForm.Text = "Son 3 Ayın Verileri";
                copyForm.Size = new System.Drawing.Size(600, 400);
                copyForm.StartPosition = FormStartPosition.CenterScreen;
                copyForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                copyForm.MaximizeBox = false;
                copyForm.MinimizeBox = false;

                System.Windows.Forms.TextBox textBox = new System.Windows.Forms.TextBox();
                textBox.Multiline = true;
                textBox.ReadOnly = true;
                textBox.Text = prompt;
                textBox.Dock = System.Windows.Forms.DockStyle.Fill;
                textBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;

                System.Windows.Forms.Button copyButton = new System.Windows.Forms.Button();
                copyButton.Text = "Kopyala ve ChatGPT'ye Git";
                copyButton.Dock = System.Windows.Forms.DockStyle.Bottom;
                copyButton.Height = 40;
                copyButton.Click += (s, args) =>
                {
                    System.Windows.Forms.Clipboard.SetText(prompt);
                    System.Diagnostics.Process.Start("https://chatgpt.com");
                };

                copyForm.Controls.Add(textBox);
                copyForm.Controls.Add(copyButton);

                copyForm.ShowDialog();
            }
            catch (Exception ex)
            {
                // Genel hata durumunda kullanıcıya bilgi ver
                System.Windows.Forms.MessageBox.Show("Bir hata oluştu: " + ex.Message, "Hata", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                
                int month = DateTime.Now.Month;
                int year = DateTime.Now.Year;

                decimal toplamGelir = 0M;
                decimal toplamGider = 0M;

                using (SqlConnection conn = new SqlConnection(Database.connectionString))
                {
                    conn.Open();
                    string q = @"SELECT 
                                    ISNULL(SUM(CASE WHEN k.kategori_tip = 1 THEN g.tutar ELSE 0 END),0) AS Gelir,
                                    ISNULL(SUM(CASE WHEN k.kategori_tip = 0 THEN g.tutar ELSE 0 END),0) AS Gider
                                FROM GelirGiderler_4 g
                                JOIN Kategori2 k ON g.kategori_id_2 = k.kategori_id_2
                                WHERE g.kullanici_id = @uid AND MONTH(g.tarih) = @m AND YEAR(g.tarih) = @y";

                    using (SqlCommand cmd = new SqlCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", girisYapanKullanciID);
                        cmd.Parameters.AddWithValue("@m", month);
                        cmd.Parameters.AddWithValue("@y", year);

                        using (var rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                if (!rdr.IsDBNull(0)) toplamGelir = rdr.GetDecimal(0);
                                if (!rdr.IsDBNull(1)) toplamGider = rdr.GetDecimal(1);
                            }
                        }
                    }
                }

                decimal kalan = toplamGelir - toplamGider;
                string ayAdi = DateTimeFormatInfo.CurrentInfo.GetMonthName(month);

                string mesaj = $"{ayAdi} {year} için özet:\n\nToplam Gelir: {toplamGelir:C}\nToplam Gider: {toplamGider:C}\nKalan: {kalan:C}\n\nGrafik etkilenmedi; sadece bilgilendirme gösteriliyor.";

                MessageBox.Show(mesaj, "Aylık Özet", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Aylık özet alınırken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

            /*harcama geçmişini arama*/
            string aranacak = textBox2.Text.ToLower();

            foreach (DataGridViewRow satir in dataGridView1.Rows)
            {
                if (satir.Cells[0].Value != null)
                {
                    if (satir.Cells[0].Value.ToString().ToLower().Contains(aranacak))
                    {
                        satir.Selected = true;

                        dataGridView1.FirstDisplayedScrollingRowIndex = satir.Index;
                        break;
                    }
                }
            }
        }
    }
}

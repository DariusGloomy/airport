using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Configuration;

namespace airport
{
    public partial class Tablo : Form
    {
        string connection = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        int currentPage = 0;
        int itemsPerPage = 4;
        int totalItems = 0;

        public Tablo()
        {
            InitializeComponent();
        }

        private void Tablo_Load(object sender, EventArgs e)
        {
            // Сначала считаем, сколько всего рейсов
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Flights", conn);
                totalItems = (int)cmd.ExecuteScalar();
            }

            LoadBoard(); // Загружаем первую страницу
        }

        private void LoadBoard()
        {
            flowLayoutPanel1.Controls.Clear(); // Очищаем панель

            using (SqlConnection conn = new SqlConnection(connection))
            {
                try
                {
                    conn.Open();

                    // ЗАПРОС С ПАГИНАЦИЕЙ (LIMIT и OFFSET)
                    string query = @"
                        SELECT
                            f.FlightNumber,
                            a.AirlineName,
                            c1.CityName as DepartureCity,
                            c2.CityName as ArrivalCity,
                            f.DepartureDateTime,
                            fs.StatusName
                        FROM Flights f
                        JOIN Airlines a ON f.AirlineID = a.AirlineID
                        JOIN Cities c1 ON f.DepartureCityID = c1.CityID
                        JOIN Cities c2 ON f.ArrivalCityID = c2.CityID
                        JOIN FlightStatuses fs ON f.FlightStatusID = fs.FlightStatusID
                        ORDER BY f.DepartureDateTime
                        OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY";
                    // OFFSET - пропускаем уже просмотренные, FETCH NEXT - берем нужное количество

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        // Вычисляем, сколько строк пропустить
                        int offset = currentPage * itemsPerPage;

                        cmd.Parameters.AddWithValue("@offset", offset);
                        cmd.Parameters.AddWithValue("@limit", itemsPerPage);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                CreateCard(
                                    reader["FlightNumber"].ToString(),
                                    reader["AirlineName"].ToString(),
                                    reader["DepartureCity"].ToString(),
                                    reader["ArrivalCity"].ToString(),
                                    Convert.ToDateTime(reader["DepartureDateTime"]),
                                    reader["StatusName"].ToString()
                                );
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message, "Ошибка базы данных");
                }
            }
        }

        private void CreateCard(string flightNum, string airline, string fromCity, string toCity, DateTime time, string status)
        {
            // 1. Сама карточка
            Panel card = new Panel();
            card.Size = new Size(700, 100);
            card.BackColor = Color.FromArgb(50, 50, 50);
            card.Margin = new Padding(15);

            // --- НОВЫЙ БЛОК: ДОБАВЛЯЕМ КАРТИНКУ ---
            PictureBox pic = new PictureBox();
            pic.Size = new Size(80, 80);
            pic.Location = new Point(10, 10);
            pic.SizeMode = PictureBoxSizeMode.Zoom;

            // Программа ищет файл: (папка с exe) \ (Имя Авиакомпании).png
            string imagePath = Path.Combine(Application.StartupPath, airline + ".png");

            try
            {
                if (File.Exists(imagePath))
                {
                    pic.Image = Image.FromFile(imagePath);
                }
            }
            catch { /* Если картинка битая, программа не упадет */ }

            card.Controls.Add(pic); // Добавляем картинку на карточку
            // ---------------------------------------

            // 2. Номер рейса (СДВИГАЕМ ТЕКСТ ВПРАВО, чтобы не наложился на картинку)
            Label lblFlight = new Label();
            lblFlight.Text = flightNum;
            lblFlight.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            lblFlight.ForeColor = Color.White;
            lblFlight.Location = new Point(100, 10); // Сдвинули X с 20 на 100
            lblFlight.AutoSize = true;
            card.Controls.Add(lblFlight);

            // 3. Города (ТОЖЕ СДВИГАЕМ)
            Label lblCities = new Label();
            lblCities.Text = $"{fromCity} ➔ {toCity}";
            lblCities.Font = new Font("Segoe UI", 14, FontStyle.Regular);
            lblCities.ForeColor = Color.LightGray;
            lblCities.Location = new Point(100, 50); // Сдвинули X с 20 на 100
            lblCities.AutoSize = true;
            card.Controls.Add(lblCities);

            // 4. Время
            Label lblTime = new Label();
            lblTime.Text = time.ToString("HH:mm");
            lblTime.Font = new Font("Segoe UI", 24, FontStyle.Bold);
            lblTime.ForeColor = Color.White;
            lblTime.Location = new Point(500, 10);
            lblTime.AutoSize = true;
            card.Controls.Add(lblTime);

            // 5. Статус
            Label lblStatus = new Label();
            lblStatus.Text = status;
            lblStatus.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblStatus.Location = new Point(500, 55);
            lblStatus.AutoSize = true;

            if (status == "Задержан" || status == "Отменён") lblStatus.ForeColor = Color.Red;
            else lblStatus.ForeColor = Color.LightGreen;

            card.Controls.Add(lblStatus);

            // Добавляем готовую карточку на панель
            flowLayoutPanel1.Controls.Add(card);
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnPrev_Click(object sender, EventArgs e)
        {
            if (currentPage > 0)
            {
                currentPage--;
                LoadBoard();
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if ((currentPage + 1)*itemsPerPage < totalItems)
            {
                currentPage++;
                LoadBoard();
            }
        }

        private void btnBack_Click_1(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.Show();
            this.Close();
        }
    }
}

using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;

namespace airport
{
    public partial class RegistrationForm : Form
    {
        string connection = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        string lastTicket = "";
        string lastPassengerName = "";
        public RegistrationForm()
        {
            InitializeComponent();
        }

        private void RegistrationForm_Load(object sender, EventArgs e)
        {
            LoadFlights();
            cmbGender.Items.Add("М");
            cmbGender.Items.Add("Ж");
            cmbGender.SelectedIndex = 0;
        }
        private void LoadFlights()
        {
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                //Берем рейсы, где время вылета больше текущего, и статус не "Отменен"
                string query = @"
                    SELECT f.FlightID, f.FlightNumber + ' | ' + c1.CityName + ' -> ' + c2.CityName + ' | ' + CONVERT(varchar, f.DepartureDateTime, 104) as DisplayText
                    FROM Flights f
                    JOIN Cities c1 ON f.DepartureCityID = c1.CityID
                    JOIN Cities c2 ON f.ArrivalCityID = c2.CityID
                    WHERE f.FlightStatusID != 5";

                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);

                cmbFlight.DataSource = dt;
                cmbFlight.DisplayMember = "DisplayText";
                cmbFlight.ValueMember = "FlightID";
            }
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            if (cmbFlight.SelectedValue == null || string.IsNullOrWhiteSpace(txtSurname.Text) || string.IsNullOrWhiteSpace(txtPassport.Text))
            {
                MessageBox.Show("Заполните ФИО и паспортные данные!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbFlight.SelectedValue == null || !(cmbFlight.SelectedValue is int))
            {
                MessageBox.Show("Пожалуйста, выберите рейс из списка!");
                return;
            }
            int flightId = (int)cmbFlight.SelectedValue;
            string passport = txtPassport.Text.Trim();

            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();

                // Ищем пассажира по паспорту или создаем нового
                int passengerId = 0;
                string checkPassengerQuery = "SELECT PassengerID FROM Passengers WHERE DocumentNumber = @pass";

                using (SqlCommand cmd = new SqlCommand(checkPassengerQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@pass", passport);
                    object result = cmd.ExecuteScalar();

                    if (result != null)
                    {
                        passengerId = Convert.ToInt32(result); // Если нашли существующего
                    }
                    else
                    {
                        // Создаем нового пассажира
                        string insertPassenger = @"INSERT INTO Passengers (LastName, FirstName, MiddleName, Gender, BirthDate, DocumentType, DocumentSeries, DocumentNumber)
                                                   OUTPUT INSERTED.PassengerID
                                                   VALUES (@surname, @name, @patr, @gender, @birth, 'Паспорт', 'РФ', @pass)";
                        using (SqlCommand insertCmd = new SqlCommand(insertPassenger, conn))
                        {
                            insertCmd.Parameters.AddWithValue("@surname", txtSurname.Text);
                            insertCmd.Parameters.AddWithValue("@name", txtName.Text);
                            insertCmd.Parameters.AddWithValue("@patr", txtPatronymic.Text);
                            insertCmd.Parameters.AddWithValue("@gender", cmbGender.SelectedItem.ToString());
                            // Если дата не введена, ставим обычную
                            insertCmd.Parameters.AddWithValue("@birth", string.IsNullOrWhiteSpace(txtBirthDate.Text) ? (object)DBNull.Value : Convert.ToDateTime(txtBirthDate.Text));
                            insertCmd.Parameters.AddWithValue("@pass", passport);

                            passengerId = (int)insertCmd.ExecuteScalar(); // Получаем айди нового пассажира
                        }
                    }
                }

                // Проверяем, есть ли свободные места на рейс и считаем, сколько мест уже занято
                string countQuery = "SELECT COUNT(*) FROM Registrations WHERE FlightID = @flightId";
                int occupiedSeats = 0;
                using (SqlCommand countCmd = new SqlCommand(countQuery, conn))
                {
                    countCmd.Parameters.AddWithValue("@flightId", flightId);
                    occupiedSeats = (int)countCmd.ExecuteScalar();
                }

                // Получаем максимальную вместимость самолета
                string capacityQuery = @"SELECT a.PassengerCapacity FROM Flights f JOIN Aircraft a ON f.AircraftID = a.AircraftID WHERE f.FlightID = @flightId";
                int maxCapacity = 0;
                using (SqlCommand capCmd = new SqlCommand(capacityQuery, conn))
                {
                    capCmd.Parameters.AddWithValue("@flightId", flightId);
                    maxCapacity = (int)capCmd.ExecuteScalar();
                }

                if (occupiedSeats >= maxCapacity)
                {
                    MessageBox.Show("На этот рейс больше нет свободных мест!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Генерируем место и номер билета
                string seatNumber = (occupiedSeats + 1).ToString("D2") + "A";
                string ticketNumber = "TK-" + new Random().Next(10000, 99999).ToString();

                // Сохраняем регистрацию
                string insertRegQuery = @"INSERT INTO Registrations (PassengerID, FlightID, TicketNumber, SeatNumber, EmployeeID, RegistrationDateTime)
                                          VALUES (@passId, @flightId, @ticket, @seat, 2, GETDATE())"; //
                using (SqlCommand regCmd = new SqlCommand(insertRegQuery, conn))
                {
                    regCmd.Parameters.AddWithValue("@passId", passengerId);
                    regCmd.Parameters.AddWithValue("@flightId", flightId);
                    regCmd.Parameters.AddWithValue("@ticket", ticketNumber);
                    regCmd.Parameters.AddWithValue("@seat", seatNumber);

                    regCmd.ExecuteNonQuery();
                }

                lblInfo.Text = $"Пассажир зарегистрирован!\nБилет: {ticketNumber}\nМесто: {seatNumber}";
                lblInfo.ForeColor = Color.Green;
                lastPassengerName = $"{txtSurname.Text} {txtName.Text} {txtPatronymic.Text}";
                lastTicket = ticketNumber;
                MessageBox.Show("Регистрация прошла успешно!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Log.Information("Был зарегистрирован новый пассажир");

                // Очистка полей
                txtSurname.Clear(); txtName.Clear(); txtPatronymic.Clear(); txtPassport.Clear(); txtBirthDate.Clear();
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnShowList_Click(object sender, EventArgs e)
        {
            if (cmbFlight.SelectedValue == null)
            {
                MessageBox.Show("Сначала выберите рейс!");
                return;
            }

            int flightId = Convert.ToInt32(cmbFlight.SelectedValue);
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                string query = @"
            SELECT p.LastName + ' ' + p.FirstName + ' ' + ISNULL(p.MiddleName, '') as [ФИО],
                   r.SeatNumber as [Место],
                   r.TicketNumber as [Билет]
            FROM Registrations r
            JOIN Passengers p ON r.PassengerID = p.PassengerID
            WHERE r.FlightID = @flightId";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@flightId", flightId);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("На этот рейс еще никто не зарегистрирован.", "Список пуст");
                    return;
                }

                string resultText = "Зарегистрированные пассажиры:\n\n";
                foreach (DataRow row in dt.Rows)
                {
                    resultText += $"Место {row["Место"]} | {row["Билет"]} | {row["ФИО"]}\n";
                }

                MessageBox.Show(resultText, "Список пассажиров", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnPrintTicket_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(lastTicket))
            {
                MessageBox.Show("Сначала зарегистрируйте пассажира, чтобы сформировать билет!", "Ошибка");
                return;
            }

            // Получаем данные
            string fio = lastPassengerName;
            string flight = cmbFlight.Text;

            // Достаем место из строки (хитрым способом)
            string seat = "";
            string[] infoLines = lblInfo.Text.Split('\n');
            if (infoLines.Length >= 3) seat = infoLines[2].Replace("Место: ", "").Trim();

            string ticket = lastTicket;
            string date = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

            // Достаем города из текста рейса (например, "SU101 | Москва -> Сочи")
            string routeText = cmbFlight.Text;
            string fromCity = "";
            string toCity = "";
            if (routeText.Contains("->"))
            {
                string[] parts = routeText.Split(new string[] { "->" }, StringSplitOptions.None);
                // Берем последнее слово из левой части (город) и первое из правой
                fromCity = parts[0].Trim().Split('|').Last().Trim();
                toCity = parts[1].Trim().Split('|').First().Trim();
            }

            // Открываем форму билета
            TicketForm ticketForm = new TicketForm(fio, flight, fromCity, toCity, seat, ticket, date);
            ticketForm.ShowDialog();
            Log.Information("Был сформирован билет");
        }
        private void cmbFlight_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Проверяем, что выбранный элемент - это ID рейса, а не пустота
            if (cmbFlight.SelectedValue == null || !(cmbFlight.SelectedValue is int))
            {
                if (lblFreeSeats != null) lblFreeSeats.Text = "Выберите рейс";
                return;
            }

            int flightId = (int)cmbFlight.SelectedValue;

            using (SqlConnection conn = new SqlConnection(connection))
            {
                try
                {
                    conn.Open();
                    string query = @"
                SELECT
                    a.PassengerCapacity,
                    (SELECT COUNT(*) FROM Registrations r WHERE r.FlightID = f.FlightID) as Booked
                FROM Flights f
                JOIN Aircraft a ON f.AircraftID = a.AircraftID
                WHERE f.FlightID = @id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", flightId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int total = Convert.ToInt32(reader["PassengerCapacity"]);
                                int booked = Convert.ToInt32(reader["Booked"]);
                                int free = total - booked;

                                // Выводим текст в лейбл (убедись, что он называется lblFreeSeats)
                                lblFreeSeats.Text = $"Свободно мест: {free} из {total}";

                                if (free == 0)
                                {
                                    lblFreeSeats.ForeColor = Color.Red;
                                    lblFreeSeats.Text = "МЕСТ НЕТ!";
                                }
                                else
                                {
                                    lblFreeSeats.ForeColor = Color.Green;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при подсчете мест: " + ex.Message);
                    Log.Information("Программа выдала ошибку при подсчете мест");
                }
            }
        }
    }
}







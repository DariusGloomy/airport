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
    public partial class AddFlightForm : Form
    {
        string connection = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        public AddFlightForm()
        {
            InitializeComponent();
        }

        private void AddFlightForm_Load(object sender, EventArgs e)
        {
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();

                // Авиакомпании
                DataTable dtAir = new DataTable();
                new SqlDataAdapter("SELECT AirlineID, AirlineName FROM Airlines", conn).Fill(dtAir);
                cmbAirline.DataSource = dtAir;
                cmbAirline.DisplayMember = "AirlineName";
                cmbAirline.ValueMember = "AirlineID";

                // Самолеты
                DataTable dtPlane = new DataTable();
                new SqlDataAdapter("SELECT AircraftID, Model FROM Aircraft", conn).Fill(dtPlane);
                cmbAircraft.DataSource = dtPlane;
                cmbAircraft.DisplayMember = "Model";
                cmbAircraft.ValueMember = "AircraftID";

                // Города (для вылета и прилета)
                DataTable dtCity = new DataTable();
                new SqlDataAdapter("SELECT CityID, CityName FROM Cities", conn).Fill(dtCity);

                cmbDepCity.DataSource = dtCity.Copy(); // .Copy() нужен, чтобы два ComboBox не были жестко связаны
                cmbDepCity.DisplayMember = "CityName";
                cmbDepCity.ValueMember = "CityID";

                cmbArrCity.DataSource = dtCity;
                cmbArrCity.DisplayMember = "CityName";
                cmbArrCity.ValueMember = "CityID";

                // Терминалы
                DataTable dtTerm = new DataTable();
                new SqlDataAdapter("SELECT TerminalID, TerminalNumber FROM Terminals", conn).Fill(dtTerm);
                cmbTerminal.DataSource = dtTerm;
                cmbTerminal.DisplayMember = "TerminalNumber";
                cmbTerminal.ValueMember = "TerminalID";

                // Гейты
                DataTable dtGate = new DataTable();
                new SqlDataAdapter("SELECT GateID, GateNumber FROM BoardingGates", conn).Fill(dtGate);
                cmbGate.DataSource = dtGate;
                cmbGate.DisplayMember = "GateNumber";
                cmbGate.ValueMember = "GateID";
            }

        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFlightNumber.Text))
            {
                MessageBox.Show("Введите номер рейса!");
                return;
            }

            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                // FlightStatusID = 1 (По расписанию) ставим по умолчанию для нового рейса
                string query = @"INSERT INTO Flights
                    (FlightNumber, AirlineID, AircraftID, DepartureDateTime, ArrivalDateTime, DepartureCityID, ArrivalCityID, FlightStatusID, TerminalID, GateID)
                    VALUES (@num, @air, @plane, @depTime, @arrTime, @depCity, @arrCity, 1, @term, @gate)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@num", txtFlightNumber.Text);
                    cmd.Parameters.AddWithValue("@air", cmbAirline.SelectedValue);
                    cmd.Parameters.AddWithValue("@plane", cmbAircraft.SelectedValue);
                    cmd.Parameters.AddWithValue("@depTime", dtpDeparture.Value);
                    cmd.Parameters.AddWithValue("@arrTime", dtpArrival.Value);
                    cmd.Parameters.AddWithValue("@depCity", cmbDepCity.SelectedValue);
                    cmd.Parameters.AddWithValue("@arrCity", cmbArrCity.SelectedValue);
                    cmd.Parameters.AddWithValue("@term", cmbTerminal.SelectedValue);
                    cmd.Parameters.AddWithValue("@gate", cmbGate.SelectedValue);

                    try
                    {
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Рейс успешно добавлен!");
                        Log.Information("Был добавлен новый рейс");
                        this.DialogResult = DialogResult.OK; // Говорим главной форме, что мы всё сохранили
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка: " + ex.Message);
                    }
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
    }


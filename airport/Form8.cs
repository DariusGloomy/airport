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
    public partial class EditFlightForm : Form
    {
        string connection = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        private int currentFlightID;
        public EditFlightForm(int flightId)
        {
            InitializeComponent();
            currentFlightID = flightId;

        }

        

        private void EditFlightForm_Load(object sender, EventArgs e)
        {
            LoadComboBoxes();
            // 2. Загружаем данные конкретного рейса в поля
            LoadFlightData();
        }
        private void LoadComboBoxes()
        {
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();

                DataTable dtAir = new DataTable();
                new SqlDataAdapter("SELECT AirlineID, AirlineName FROM Airlines", conn).Fill(dtAir);
                cmbAirline.DataSource = dtAir;
                cmbAirline.DisplayMember = "AirlineName";
                cmbAirline.ValueMember = "AirlineID";

                DataTable dtPlane = new DataTable();
                new SqlDataAdapter("SELECT AircraftID, Model FROM Aircraft", conn).Fill(dtPlane);
                cmbAircraft.DataSource = dtPlane;
                cmbAircraft.DisplayMember = "Model";
                cmbAircraft.ValueMember = "AircraftID";

                DataTable dtCity = new DataTable();
                new SqlDataAdapter("SELECT CityID, CityName FROM Cities", conn).Fill(dtCity);
                cmbDepCity.DataSource = dtCity.Copy();
                cmbDepCity.DisplayMember = "CityName";
                cmbDepCity.ValueMember = "CityID";

                cmbArrCity.DataSource = dtCity;
                cmbArrCity.DisplayMember = "CityName";
                cmbArrCity.ValueMember = "CityID";

                DataTable dtTerm = new DataTable();
                new SqlDataAdapter("SELECT TerminalID, TerminalNumber FROM Terminals", conn).Fill(dtTerm);
                cmbTerminal.DataSource = dtTerm;
                cmbTerminal.DisplayMember = "TerminalNumber";
                cmbTerminal.ValueMember = "TerminalID";

                DataTable dtGate = new DataTable();
                new SqlDataAdapter("SELECT GateID, GateNumber FROM BoardingGates", conn).Fill(dtGate);
                cmbGate.DataSource = dtGate;
                cmbGate.DisplayMember = "GateNumber";
                cmbGate.ValueMember = "GateID";

                // Загружаем статусы!
                DataTable dtStatus = new DataTable();
                new SqlDataAdapter("SELECT FlightStatusID, StatusName FROM FlightStatuses", conn).Fill(dtStatus);
                cmbStatus.DataSource = dtStatus;
                cmbStatus.DisplayMember = "StatusName";
                cmbStatus.ValueMember = "FlightStatusID";
            }
        }

        private void LoadFlightData()
        {
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                string query = "SELECT * FROM Flights WHERE FlightID = @id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", currentFlightID);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            txtFlightNumber.Text = reader["FlightNumber"].ToString();
                            cmbAirline.SelectedValue = reader["AirlineID"];
                            cmbAircraft.SelectedValue = reader["AircraftID"];
                            dtpDeparture.Value = Convert.ToDateTime(reader["DepartureDateTime"]);
                            dtpArrival.Value = Convert.ToDateTime(reader["ArrivalDateTime"]);
                            cmbDepCity.SelectedValue = reader["DepartureCityID"];
                            cmbArrCity.SelectedValue = reader["ArrivalCityID"];
                            cmbStatus.SelectedValue = reader["FlightStatusID"];
                            cmbTerminal.SelectedValue = reader["TerminalID"];
                            cmbGate.SelectedValue = reader["GateID"];
                        }
                    }
                }
            }
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                string query = @"UPDATE Flights SET
                    FlightNumber = @num,
                    AirlineID = @air,
                    AircraftID = @plane,
                    DepartureDateTime = @depTime,
                    ArrivalDateTime = @arrTime,
                    DepartureCityID = @depCity,
                    ArrivalCityID = @arrCity,
                    FlightStatusID = @status,
                    TerminalID = @term,
                    GateID = @gate
                    WHERE FlightID = @id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@num", txtFlightNumber.Text);
                    cmd.Parameters.AddWithValue("@air", cmbAirline.SelectedValue);
                    cmd.Parameters.AddWithValue("@plane", cmbAircraft.SelectedValue);
                    cmd.Parameters.AddWithValue("@depTime", dtpDeparture.Value);
                    cmd.Parameters.AddWithValue("@arrTime", dtpArrival.Value);
                    cmd.Parameters.AddWithValue("@depCity", cmbDepCity.SelectedValue);
                    cmd.Parameters.AddWithValue("@arrCity", cmbArrCity.SelectedValue);
                    cmd.Parameters.AddWithValue("@status", cmbStatus.SelectedValue);
                    cmd.Parameters.AddWithValue("@term", cmbTerminal.SelectedValue);
                    cmd.Parameters.AddWithValue("@gate", cmbGate.SelectedValue);
                    cmd.Parameters.AddWithValue("@id", currentFlightID);

                    try
                    {
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Рейс обновлен!");
                        Log.Information("Были обновлены данные рейса");
                        this.DialogResult = DialogResult.OK;
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


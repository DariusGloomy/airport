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
    public partial class CitiesForm : Form
    {
        string connection = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        public CitiesForm()
        {
            InitializeComponent();
        }

        private void CitiesForm_Load(object sender, EventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                string query = "SELECT CityID as [ID], CityName as [Название города] FROM Cities";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    DataTable table = new DataTable();
                    table.Columns.Add("ID", typeof(int));
                    table.Columns.Add("Название города", typeof(string));

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            table.Rows.Add(reader["ID"], reader["Название города"]);
                        }
                    }

                    dataGridView1.DataSource = table;
                    dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                    foreach (DataGridViewColumn col in dataGridView1.Columns)
                    {
                        col.SortMode = DataGridViewColumnSortMode.NotSortable;
                    }
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCityName.Text))
            {
                MessageBox.Show("Введите название города!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                string query = "INSERT INTO Cities (CityName) VALUES (@name)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@name", txtCityName.Text);
                    cmd.ExecuteNonQuery();
                }
            }

            MessageBox.Show("Город успешно добавлен!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Log.Information("Добавлен новый город в справочник городов");
            txtCityName.Clear();
            LoadData();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите город для удаления!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult result = MessageBox.Show("Вы уверены, что хотите удалить этот город?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.No) return;

            int id = Convert.ToInt32(dataGridView1.SelectedRows[0].Cells[0].Value);

            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                string query = "DELETE FROM Cities WHERE CityID = @id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    try
                    {
                        cmd.ExecuteNonQuery();
                        Log.Information("Удален город из справочника городов");
                    }
                    catch (SqlException)
                    {
                        MessageBox.Show("Невозможно удалить город, так как он используется в рейсах!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            LoadData();
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            Form5 form5 = new Form5();
            form5.Show();
            this.Close();
        }
    }
}
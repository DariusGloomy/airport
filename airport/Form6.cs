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
    public partial class Form6 : Form
    {
        string connection = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        public Form6()
        {
            InitializeComponent();
        }

        private void Form6_Load(object sender, EventArgs e)
        {
            LoadData();

        }

        private void LoadData()
        {
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                string query = "SELECT TerminalID as [ID], TerminalNumber as [Номер], TerminalName as [Название] FROM Terminals";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // Создаем таблицу вручную
                    DataTable table = new DataTable();
                    table.Columns.Add("ID", typeof(int));
                    table.Columns.Add("Номер", typeof(string));
                    table.Columns.Add("Название", typeof(string));

                    // Читаем данные
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            table.Rows.Add(reader["ID"], reader["Номер"], reader["Название"]);
                        }
                    }

                    // Привязываем
                    dataGridView1.DataSource = table;
                    dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                    foreach (DataGridViewColumn col in dataGridView1.Columns)
                    {
                        col.SortMode = DataGridViewColumnSortMode.NotSortable;
                    }
                }
            }
        }
        private void button8_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text) || string.IsNullOrWhiteSpace(textBox2.Text))
            {
                MessageBox.Show("Заполните оба поля!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                string query = "INSERT INTO Terminals (TerminalNumber, TerminalName) VALUES (@num, @name)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // Используем параметры
                    cmd.Parameters.AddWithValue("@num", textBox1.Text);
                    cmd.Parameters.AddWithValue("@name", textBox2.Text);
                    cmd.ExecuteNonQuery();
                }
            }

            MessageBox.Show("Успешно добавлено!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Log.Information("Добавлен терминал в справочнике терминалов");
            textBox1.Clear();
            textBox2.Clear();
            LoadData(); // Обновляем таблицу
        }
        

        private void button1_Click(object sender, EventArgs e)
        {
            {
                if (dataGridView1.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Выберите строку для удаления!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int id = Convert.ToInt32(dataGridView1.SelectedRows[0].Cells[0].Value);

                DialogResult result = MessageBox.Show("Точно удалить?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.No) return;

                using (SqlConnection conn = new SqlConnection(connection))
                {
                    conn.Open();
                    string query = "DELETE FROM Terminals WHERE TerminalID = @id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);

                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch (SqlException)
                        {
                            MessageBox.Show("Нельзя удалить! К этому терминалу привязаны рейсы или гейты.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                Log.Information("Удален терминал в справочнике тернминалов");
                LoadData(); // Обновляем таблицу
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Form5 form5 = new Form5();
            form5.Show();
            this.Hide();

        }


    }
}

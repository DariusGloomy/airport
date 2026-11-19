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
    public partial class AnalyticsForm : Form
    {
        string connection = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        public AnalyticsForm()
        {
            InitializeComponent();
        }

        private void AnalyticsForm_Load(object sender, EventArgs e)
        {
            LoadTerminals();
            LoadData();
        }
        private void LoadTerminals()
        {
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                DataTable dt = new DataTable();
                new SqlDataAdapter("SELECT TerminalID, TerminalNumber FROM Terminals", conn).Fill(dt);
                cmbTerminal.DataSource = dt;
                cmbTerminal.DisplayMember = "TerminalNumber";
                cmbTerminal.ValueMember = "TerminalID";
            }
        }

        // Загрузка истории расчетов в таблицу
        private void LoadData()
        {
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                string query = @"
                    SELECT
                        c.CalculationID as [ID],
                        c.CalculationDateTime as [Дата расчета],
                        t.TerminalNumber as [Терминал],
                        c.NumberOfCounters as [Кол-во стоек],
                        c.ArrivalRatePerHour as [Пассажиров/час],
                        c.AverageServiceTimeMinutes as [Время обслуж. (мин)],
                        c.TotalServiceCapacityPerHour as [Пропускная способность],
                        CAST(c.Utilization * 100 AS DECIMAL(5,1)) as [Загрузка (%)],
                        CAST(c.AverageQueueLength AS DECIMAL(5,2)) as [Средняя очередь (чел)],
                        c.AverageWaitMinutes as [Ожидание (мин)]
                    FROM CapacityCalculations c
                    JOIN Terminals t ON c.TerminalID = t.TerminalID
                    ORDER BY c.CalculationDateTime DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    DataTable table = new DataTable();
                    table.Columns.Add("ID", typeof(int));
                    table.Columns.Add("Дата расчета", typeof(DateTime));
                    table.Columns.Add("Терминал", typeof(string));
                    table.Columns.Add("Кол-во стоек", typeof(int));
                    table.Columns.Add("Пассажиров/час", typeof(int));
                    table.Columns.Add("Время обслуж. (мин)", typeof(int));
                    table.Columns.Add("Пропускная способность", typeof(int));
                    table.Columns.Add("Загрузка (%)", typeof(decimal));
                    table.Columns.Add("Средняя очередь (чел)", typeof(decimal));
                    table.Columns.Add("Ожидание (мин)", typeof(decimal));

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            table.Rows.Add(
                                reader["ID"], reader["Дата расчета"], reader["Терминал"],
                                reader["Кол-во стоек"], reader["Пассажиров/час"], reader["Время обслуж. (мин)"],
                                reader["Пропускная способность"], reader["Загрузка (%)"],
                                reader["Средняя очередь (чел)"], reader["Ожидание (мин)"]
                            );
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

        private void btnCalculate_Click(object sender, EventArgs e)
        {
            // Проверка на пустые поля
            if (string.IsNullOrWhiteSpace(txtCounters.Text) || string.IsNullOrWhiteSpace(txtArrivalRate.Text) || string.IsNullOrWhiteSpace(txtServiceTime.Text))
            {
                MessageBox.Show("Заполните все поля для расчета!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                
                int counters = Convert.ToInt32(txtCounters.Text); // Количество стоек
                double arrivalRate = Convert.ToDouble(txtArrivalRate.Text); // Интенсивность входа
                double serviceTime = Convert.ToDouble(txtServiceTime.Text); // Время обслуживания (мин)
                //Интенсивность обслуживания одной стойки - сколько человек в час обслуживает одна стойка
                double serviceRatePerCounter = 60.0 / serviceTime;
                //Общая пропускная способность всех стоек
                double totalCapacity = counters * serviceRatePerCounter;
                //Коэффициент загрузки
                double utilization = arrivalRate / totalCapacity;
                // Проверка на перегрузку (если пассажиров больше, чем могут обслужить)
                if (utilization >= 1.0)
                {
                    MessageBox.Show("Внимание! Терминал перегружен. Очередь будет расти бесконечно!\nУвеличьте количество стоек или уменьшите время обслуживания.", "Перегрузка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Log.Information("Программа выдала предупреждение о перегрузке терминала");
                }
                //Вероятность ожидания
                double probabilityOfWaiting = utilization;
                //Средняя длина очереди
                double averageQueueLength = (utilization * utilization) / (1 - utilization);
                if (double.IsInfinity(averageQueueLength) || averageQueueLength < 0) averageQueueLength = 0;

                //Среднее время ожидания в очереди в минутах
                double averageWaitMinutes = (averageQueueLength / arrivalRate) * 60;
                // Сохраняем
                SaveCalculation(Convert.ToInt32(cmbTerminal.SelectedValue), counters, arrivalRate, serviceTime,
                                (int)serviceRatePerCounter, (int)totalCapacity, utilization, probabilityOfWaiting,
                                averageQueueLength, averageWaitMinutes);

                MessageBox.Show("Расчет выполнен успешно! Данные добавлены в таблицу.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Очищаем поля и обновляем таблицу
                txtCounters.Clear();
                txtArrivalRate.Clear();
                txtServiceTime.Clear();
                LoadData();
                Log.Information("Рассчитано СПО");
            }
            catch (FormatException)
            {
                MessageBox.Show("Вводите только числа!", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Вспомогательный метод для сохранения в БД
        private void SaveCalculation(int terminalId, int counters, double arrivalRate, double serviceTime,
                                     int serviceRatePerCounter, int totalCapacity, double utilization, double probWaiting,
                                     double avgQueue, double avgWait)
        {
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                string query = @"INSERT INTO CapacityCalculations
                    (CalculationDateTime, TerminalID, NumberOfCounters, ArrivalRatePerHour, AverageServiceTimeMinutes,
                     ServiceRatePerCounterPerHour, TotalServiceCapacityPerHour, Utilization, ProbabilityOfWaiting,
                     AverageQueueLength, AverageWaitMinutes, QueueModel)
                    VALUES
                    (@date, @term, @count, @arr, @time, @servRate, @total, @util, @prob, @queue, @wait, 'M')";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@date", DateTime.Now);
                    cmd.Parameters.AddWithValue("@term", terminalId);
                    cmd.Parameters.AddWithValue("@count", counters);
                    cmd.Parameters.AddWithValue("@arr", arrivalRate);
                    cmd.Parameters.AddWithValue("@time", serviceTime);
                    cmd.Parameters.AddWithValue("@servRate", serviceRatePerCounter);
                    cmd.Parameters.AddWithValue("@total", totalCapacity);
                    cmd.Parameters.AddWithValue("@util", utilization);
                    cmd.Parameters.AddWithValue("@prob", probWaiting);
                    cmd.Parameters.AddWithValue("@queue", avgQueue);
                    cmd.Parameters.AddWithValue("@wait", avgWait);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnBack_Click_1(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.Show();
            this.Close();
        }
    }
}
        
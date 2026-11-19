using Microsoft.Data.SqlClient;
using OfficeOpenXml;
using OfficeOpenXml.Export.HtmlExport.StyleCollectors.StyleContracts;
using PdfSharp;
using PdfSharp.Drawing;   // Подключаем пространства имен PdfSharp
using PdfSharp.Pdf;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using System.Configuration;

namespace airport
{
    public partial class Form3 : Form
    {
        string connection = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        public Form3()
        {
            InitializeComponent();
        }


        private void button1_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.Show();
            this.Hide();
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            LoadData();
            LoadFilters();
        }

        private void LoadData(string searchQuery = "")
        {
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();

                // Базовый запрос, который объединяет 6 таблиц!
                string query = @"
                    SELECT
                        f.FlightID as [ID],
                        f.FlightNumber as [Номер рейса],
                        a.AirlineName as [Авиакомпания],
                        ac.Model as [Модель самолета],
                        c1.CityName as [Город отправления],
                        c2.CityName as [Город прибытия],
                        f.DepartureDateTime as [Время вылета],
                        f.ArrivalDateTime as [Время прилета],
                        fs.StatusName as [Статус],
                        t.TerminalNumber as [Терминал],
                        bg.GateNumber as [Пос. выход],
                        ac.PassengerCapacity as [Всего мест],
                        (SELECT COUNT(*) FROM Registrations r WHERE r.FlightID = f.FlightID) as [Занято]
                    FROM Flights f
                    JOIN Airlines a ON f.AirlineID = a.AirlineID
                    JOIN Aircraft ac ON f.AircraftID = ac.AircraftID
                    JOIN Cities c1 ON f.DepartureCityID = c1.CityID
                    JOIN Cities c2 ON f.ArrivalCityID = c2.CityID
                    JOIN FlightStatuses fs ON f.FlightStatusID = fs.FlightStatusID
                    JOIN Terminals t ON f.TerminalID = t.TerminalID
                    JOIN BoardingGates bg ON f.GateID = bg.GateID";

                // Если передан поисковый запрос, добавляем WHERE
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    query += " WHERE " + searchQuery;
                }

                query += " ORDER BY f.DepartureDateTime";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    DataTable table = new DataTable();
                    table.Columns.Add("ID", typeof(int));
                    table.Columns.Add("Номер рейса", typeof(string));
                    table.Columns.Add("Авиакомпания", typeof(string));
                    table.Columns.Add("Модель самолета", typeof(string));
                    table.Columns.Add("Город отправления", typeof(string));
                    table.Columns.Add("Город прибытия", typeof(string));
                    table.Columns.Add("Время вылета", typeof(DateTime));
                    table.Columns.Add("Время прилета", typeof(DateTime));
                    table.Columns.Add("Статус", typeof(string));
                    table.Columns.Add("Терминал", typeof(string));
                    table.Columns.Add("Пос. выход", typeof(string));
                    table.Columns.Add("Всего мест", typeof(int));
                    table.Columns.Add("Занято", typeof(int));

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            table.Rows.Add(
                                reader["ID"], reader["Номер рейса"], reader["Авиакомпания"],
                                reader["Модель самолета"], reader["Город отправления"],
                                reader["Город прибытия"], reader["Время вылета"],
                                reader["Время прилета"], reader["Статус"],
                                reader["Терминал"], reader["Пос. выход"],
                                reader["Всего мест"], reader["Занято"]
                            );
                        }
                    }
                    dataGridView1.DataSource = table;
                    dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                    // Отключаем сортировку для предотвращения ошибок
                    foreach (DataGridViewColumn col in dataGridView1.Columns)
                    {
                        col.SortMode = DataGridViewColumnSortMode.NotSortable;
                    }
                }
            }
        }
        private void LoadFilters()
        {
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();

                // Заполняем ComboBox статусов
                SqlDataAdapter daStatus = new SqlDataAdapter("SELECT FlightStatusID, StatusName FROM FlightStatuses", conn);
                DataTable dtStatus = new DataTable();
                daStatus.Fill(dtStatus);
                cmbFilterStatus.DataSource = dtStatus;
                cmbFilterStatus.DisplayMember = "StatusName";
                cmbFilterStatus.ValueMember = "FlightStatusID";
                cmbFilterStatus.SelectedIndex = -1; // Сбрасываем выбор

                // Заполняем ComboBox направлений (городов)
                SqlDataAdapter daCity = new SqlDataAdapter("SELECT CityID, CityName FROM Cities", conn);
                DataTable dtCity = new DataTable();
                daCity.Fill(dtCity);
                cmbFilterDirection.DataSource = dtCity;
                cmbFilterDirection.DisplayMember = "CityName";
                cmbFilterDirection.ValueMember = "CityID";
                cmbFilterDirection.SelectedIndex = -1;
            }
        }

        private void txtFilterDate_TextChanged(object sender, EventArgs e)
        {

        }

        private void cmbFilterStatus_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void dtpFilterDate_ValueChanged(object sender, EventArgs e)
        {

        }

        private void cmbFilterDirection_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string whereClause = "";

            // Фильтр по номеру рейса
            if (!string.IsNullOrWhiteSpace(txtFilterNumber.Text))
            {
                whereClause += $"f.FlightNumber LIKE '%{txtFilterNumber.Text}%' ";
            }
            if (cmbFilterStatus.SelectedValue != null && cmbFilterStatus.SelectedIndex != -1)
            {
                if (whereClause != "") whereClause += " AND ";
                whereClause += $"f.FlightStatusID = {cmbFilterStatus.SelectedValue} ";
            }

            // Фильтр по направлению (городу)
            if (cmbFilterDirection.SelectedValue != null && cmbFilterDirection.SelectedIndex != -1)
            {
                if (whereClause != "") whereClause += " AND ";
                // Ищем либо в городе вылета, либо в городе прилета
                whereClause += $"(f.DepartureCityID = {cmbFilterDirection.SelectedValue} OR f.ArrivalCityID = {cmbFilterDirection.SelectedValue}) ";
            }

            // Фильтр по дате (сравниваем только день)
            if (dtpFilterDate.Checked) // Если пользователь выбрал дату
            {
                if (whereClause != "") whereClause += " AND ";
                string dateStr = dtpFilterDate.Value.ToString("yyyy-MM-dd");
                whereClause += $"CAST(f.DepartureDateTime AS DATE) = '{dateStr}' ";
            }

            LoadData(whereClause);
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            txtFilterNumber.Clear();
            cmbFilterStatus.SelectedIndex = -1;
            cmbFilterDirection.SelectedIndex = -1;
            LoadData(); // Загружаем всё заново
        }

        private void button2_Click(object sender, EventArgs e)
        {
            AddFlightForm addFlightForm = new AddFlightForm();
            addFlightForm.Show();
        }
        private void btnEditFlight_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Проверяем, выбрал ли пользователь рейс
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите рейс для редактирования!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Берем ID рейса из скрытой первой колонки
            int flightId = Convert.ToInt32(dataGridView1.SelectedRows[0].Cells[0].Value);

            // Открываем форму редактирования и передаем ей ID
            EditFlightForm editForm = new EditFlightForm(flightId);

            // Если сохранили — обновляем таблицу
            if (editForm.ShowDialog() == DialogResult.OK)
            {
                LoadData();
                Log.Information("Были изменены данные рейса");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Пожалуйста, выберите рейс, который хотите удалить.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. Спрашиваем подтверждение (чтобы случайно не удалить)
            DialogResult result = MessageBox.Show("Вы уверены, что хотите удалить этот рейс?", "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.No) return;

            // 3. Берем ID рейса из первой колонки выделенной строки
            int flightId = Convert.ToInt32(dataGridView1.SelectedRows[0].Cells[0].Value);

            // 4. Отправляем запрос в базу
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                string query = "DELETE FROM Flights WHERE FlightID = @id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", flightId);

                    try
                    {
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Рейс успешно удален!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Log.Information("Рейс был удален");
                        LoadData(); // Обновляем таблицу, чтобы удаленный рейс исчез
                    }
                    catch (SqlException)
                    {
                        // Вот эта проверка очень важна!
                        // Если в базе настроены Foreign Keys, база не даст удалить рейс,
                        // на который уже есть записи в таблице Registrations (куплены билеты).
                        MessageBox.Show("Невозможно удалить рейс! На него уже зарегистрированы пассажиры.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Диалоговое окно для сохранения файла
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Excel Files|*.xlsx";
            sfd.FileName = "Отчет_Рейсы_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm");

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // Настройка лицензии для EPPlus 5+ (обязательно для новых версий)
                    ExcelPackage.License.SetNonCommercialPersonal("КФЭК");

                    using (ExcelPackage excelPackage = new ExcelPackage())
                    {
                        // 1. Создаем новый лист
                        ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets.Add("Рейсы");

                        // 2. Записываем заголовки (берем из DataGridView)
                        for (int i = 1; i <= dataGridView1.Columns.Count; i++)
                        {
                            worksheet.Cells[1, i].Value = dataGridView1.Columns[i - 1].HeaderText;

                            // Делаем заголовки жирными и с серым фоном
                            worksheet.Cells[1, i].Style.Font.Bold = true;
                            worksheet.Cells[1, i].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            worksheet.Cells[1, i].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                        }

                        // 3. Записываем данные
                        for (int i = 0; i < dataGridView1.Rows.Count; i++)
                        {
                            for (int j = 0; j < dataGridView1.Columns.Count; j++)
                            {
                                worksheet.Cells[i + 2, j + 1].Value = dataGridView1.Rows[i].Cells[j].Value?.ToString();
                            }
                        }

                        // 4. Автоматически настраиваем ширину колонок (чтобы текст не обрезался)
                        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                        // 5. Сохраняем файл
                        FileInfo fi = new FileInfo(sfd.FileName);
                        excelPackage.SaveAs(fi);
                    }

                    MessageBox.Show("Отчет успешно выгружен в Excel!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Опционально: открыть файл сразу после сохранения
                    System.Diagnostics.Process.Start(sfd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при экспорте: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnExportPdf_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "PDF Files|*.pdf";
            sfd.FileName = "Отчет_Рейсы_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm");

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    PdfSharpLicense.SetAgplLicense();
                    PdfSharp.Fonts.GlobalFontSettings.UseWindowsFontsUnderWindows = true;
                    // Создаем документ
                    PdfDocument document = new PdfDocument();
                    document.Info.Title = "Отчет по рейсам";

                    // Добавляем страницу
                    PdfPage page = document.AddPage();
                    XGraphics gfx = XGraphics.FromPdfPage(page);

                    // Настраиваем шрифты (Arial для русского языка)
                    // Мы используем шрифт Windows, чтобы не подключать файлы .ttf
                    XFont fontTitle = new XFont("Arial", 16, XFontStyleEx.Bold);
                    XFont fontHeader = new XFont("Arial", 10, XFontStyleEx.Bold);
                    XFont fontBody = new XFont("Arial", 9, XFontStyleEx.Regular);

                    int yPoint = 40; // Текущая высота (Y) для рисования

                    // Рисуем заголовок
                    gfx.DrawString("Отчет по рейсам", fontTitle, XBrushes.Black, new XRect(0, yPoint, page.Width, page.Height), XStringFormats.TopCenter);
                    yPoint += 40;

                    // Рисуем шапку таблицы (упрощенный вариант)
                    // Мы не будем рисовать сетку полностью, чтобы не усложнять код,
                    // сделаем 5 основных колонок для красоты.

                    gfx.DrawString("Номер рейса", fontHeader, XBrushes.Black, 30, yPoint);
                    gfx.DrawString("Авиакомпания", fontHeader, XBrushes.Black, 120, yPoint);
                    gfx.DrawString("Откуда", fontHeader, XBrushes.Black, 240, yPoint);
                    gfx.DrawString("Куда", fontHeader, XBrushes.Black, 340, yPoint);
                    gfx.DrawString("Время вылета", fontHeader, XBrushes.Black, 440, yPoint);
                    gfx.DrawString("Кол-во пасс.", fontHeader, XBrushes.Black, 530, yPoint);

                    yPoint += 20;
                    gfx.DrawLine(XPens.Black, 30, yPoint, 570, yPoint); // Линия под шапкой
                    yPoint += 10;

                    // Заполняем данными из DataGridView
                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        if (row.Cells[1].Value != null) // Проверка, что строка не пустая
                        {
                            // Если страница заканчивается - создаем новую
                            if (yPoint > page.Height - 40)
                            {
                                page = document.AddPage();
                                gfx = XGraphics.FromPdfPage(page);
                                yPoint = 40;
                            }

                            // Достаем данные (индексы колонок могут отличаться, проверь!)
                            // 1 = Номер рейса, 2 = Авиакомпания, 4 = Откуда, 5 = Куда, 6 = Время вылета
                            string flightNum = row.Cells[1].Value?.ToString() ?? "";
                            string airline = row.Cells[2].Value?.ToString() ?? "";
                            string fromCity = row.Cells[4].Value?.ToString() ?? "";
                            string toCity = row.Cells[5].Value?.ToString() ?? "";
                            string depTime = row.Cells[6].Value?.ToString() ?? "";
                            
                            string bookedSeats = row.Cells[12].Value?.ToString() ?? "";

                            gfx.DrawString(flightNum, fontBody, XBrushes.Black, 30, yPoint);
                            gfx.DrawString(airline, fontBody, XBrushes.Black, 120, yPoint);
                            gfx.DrawString(fromCity, fontBody, XBrushes.Black, 240, yPoint);
                            gfx.DrawString(toCity, fontBody, XBrushes.Black, 340, yPoint);
                            gfx.DrawString(depTime, fontBody, XBrushes.Black, 440, yPoint);
                           
                            gfx.DrawString(bookedSeats, fontBody, XBrushes.Black, 530, yPoint);

                            yPoint += 20; // Сдвиг вниз для следующей строки
                        }
                    }

                    // Сохраняем документ
                    document.Save(sfd.FileName);

                    MessageBox.Show("PDF успешно сохранен!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    System.Diagnostics.Process.Start(sfd.FileName); // Открываем файл
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при создании PDF: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
    
  


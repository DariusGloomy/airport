using Microsoft.Data.SqlClient;
using OfficeOpenXml;
using PdfSharp.Drawing;
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
using System.Configuration;

namespace airport
{
    public partial class ReportsForm : Form
    {
        string connection = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        public ReportsForm()
        {
            InitializeComponent();

            // Настройка лицензий (обязательно для экспорта)
            ExcelPackage.License.SetNonCommercialPersonal("Валерия");
            PdfSharp.PdfSharpLicense.SetAgplLicense();
            PdfSharp.Fonts.GlobalFontSettings.UseWindowsFontsUnderWindows = true;
        }

        // ==========================================
        // 1. ОТЧЕТ ПО РЕЙСАМ
        // ==========================================

        private DataTable GetFlightsData()
        {
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                string query = @"
                    SELECT
                        f.FlightNumber as [Номер рейса],
                        a.AirlineName as [Авиакомпания],
                        ac.Model as [Модель самолета],
                        c1.CityName as [Откуда],
                        c2.CityName as [Куда],
                        f.DepartureDateTime as [Время вылета],
                        (SELECT COUNT(*) FROM Registrations r WHERE r.FlightID = f.FlightID) as [Занято]
                    FROM Flights f
                    JOIN Airlines a ON f.AirlineID = a.AirlineID
                    JOIN Aircraft ac ON f.AircraftID = ac.AircraftID
                    JOIN Cities c1 ON f.DepartureCityID = c1.CityID
                    JOIN Cities c2 ON f.ArrivalCityID = c2.CityID
                    ORDER BY f.DepartureDateTime";

                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private void btnFlightsExcel_Click(object sender, EventArgs e)
        {
            SaveToExcel(GetFlightsData(), "Отчет_Рейсы");
            Log.Information("Сохранен отчет о рейсах в Excel");
        }

        private void btnFlightsPdf_Click(object sender, EventArgs e)
        {
            SaveToPdf(GetFlightsData(), "Отчет_Рейсы", "Отчет по рейсам аэропорта");
            Log.Information("Сохранен отчет о рейсах в Pdf");
        }

        // ==========================================
        // 2. ОТЧЕТ ПО ПАССАЖИРАМ
        // ==========================================

        private DataTable GetPassengersData()
        {
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                string query = @"
                    SELECT
                        p.LastName + ' ' + p.FirstName + ' ' + ISNULL(p.MiddleName, '') as [ФИО],
                        p.DocumentNumber as [Паспорт],
                        f.FlightNumber as [Рейс],
                        r.TicketNumber as [Билет],
                        r.SeatNumber as [Место],
                        r.RegistrationDateTime as [Дата регистрации]
                    FROM Registrations r
                    JOIN Passengers p ON r.PassengerID = p.PassengerID
                    JOIN Flights f ON r.FlightID = f.FlightID";

                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private void btnPassExcel_Click(object sender, EventArgs e)
        {
            SaveToExcel(GetPassengersData(), "Отчет_Пассажиры");
            Log.Information("Сохранен отчет о пассажирах в Excel");
        }

        private void btnPassPdf_Click(object sender, EventArgs e)
        {
            SaveToPdf(GetPassengersData(), "Отчет_Пассажиры", "Отчет по зарегистрированным пассажирам");
            Log.Information("Сохранен отчет о пассажирах в Pdf");
        }

        // ==========================================
        // 3. ОТЧЕТ ПО СМО (ПРОПУСКНАЯ СПОСОБНОСТЬ)
        // ==========================================

        private DataTable GetSmoData()
        {
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                string query = @"
                    SELECT
                        t.TerminalNumber as [Терминал],
                        c.NumberOfCounters as [Стоек],
                        c.ArrivalRatePerHour as [Пассажиров в час],
                        c.Utilization as [Загрузка],
                        c.AverageQueueLength as [Очередь],
                        c.CalculationDateTime as [Дата расчета]
                    FROM CapacityCalculations c
                    JOIN Terminals t ON c.TerminalID = t.TerminalID
                    ORDER BY c.CalculationDateTime DESC";

                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private void btnSmoExcel_Click(object sender, EventArgs e)
        {
            SaveToExcel(GetSmoData(), "Отчет_СМО");
            Log.Information("Сохранен отчет о Расчете СПО в Pdf");
        }

        private void btnSmoPdf_Click(object sender, EventArgs e)
        {
            SaveToPdf(GetSmoData(), "Отчет_СМО", "Отчет по пропускной способности терминалов");
        }

        // ==========================================
        // УНИВЕРСАЛЬНЫЕ ФУНКЦИИ СОХРАНЕНИЯ
        // ==========================================

        // --- СОХРАНЕНИЕ В EXCEL ---
        private void SaveToExcel(DataTable dataTable, string defaultFileName)
        {
            if (dataTable.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для отчета!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Excel Files|*.xlsx";
            sfd.FileName = defaultFileName + "_" + DateTime.Now.ToString("dd-MM-yyyy");

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (ExcelPackage excelPackage = new ExcelPackage())
                    {
                        ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets.Add("Отчет");

                        // Заголовки
                        for (int i = 1; i <= dataTable.Columns.Count; i++)
                        {
                            worksheet.Cells[1, i].Value = dataTable.Columns[i - 1].ColumnName;
                            worksheet.Cells[1, i].Style.Font.Bold = true;
                            worksheet.Cells[1, i].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            worksheet.Cells[1, i].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                        }

                        // Данные
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            for (int j = 0; j < dataTable.Columns.Count; j++)
                            {
                                worksheet.Cells[i + 2, j + 1].Value = dataTable.Rows[i][j].ToString();
                            }
                        }

                        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                        FileInfo fi = new FileInfo(sfd.FileName);
                        excelPackage.SaveAs(fi);
                    }

                    MessageBox.Show("Отчет успешно сохранен в Excel!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Log.Information("Сохранен отчет о Расчете СПО в Excel");
                    System.Diagnostics.Process.Start(sfd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message);
                }
            }
        }

        // --- СОХРАНЕНИЕ В PDF ---


        private void SaveToPdf(DataTable dataTable, string defaultFileName, string reportTitle)
        {
            if (dataTable.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для отчета!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "PDF Files|*.pdf";
            sfd.FileName = defaultFileName + "_" + DateTime.Now.ToString("dd-MM-yyyy");

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    PdfDocument document = new PdfDocument();
                    document.Info.Title = reportTitle;
                    PdfPage page = document.AddPage();
                    XGraphics gfx = XGraphics.FromPdfPage(page);

                    XFont fontTitle = new XFont("Arial", 16, XFontStyleEx.Bold);
                    XFont fontHeader = new XFont("Arial", 10, XFontStyleEx.Bold);
                    XFont fontBody = new XFont("Arial", 9, XFontStyleEx.Regular);

                    int yPoint = 40;

                    // Заголовок
                    gfx.DrawString(reportTitle, fontTitle, XBrushes.Black, new XRect(0, yPoint, page.Width, page.Height), XStringFormats.TopCenter);
                    yPoint += 40;

                    // Рассчитываем ширину колонок (равномерно по странице)
                    int colWidth = (int)(page.Width.Value - 60) / dataTable.Columns.Count;

                    // Рисуем шапку таблицы
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        gfx.DrawString(dataTable.Columns[i].ColumnName, fontHeader, XBrushes.Black, 30 + (i * colWidth), yPoint);
                    }

                    yPoint += 20;
                    gfx.DrawLine(XPens.Black, 30, yPoint, page.Width.Value - 30, yPoint);
                    yPoint += 10;
                    // Рисуем данные
                    foreach (DataRow row in dataTable.Rows)
                    {
                        if (yPoint > page.Height - 40) // Если места нет, создаем новую страницу
                        {
                            page = document.AddPage();
                            gfx = XGraphics.FromPdfPage(page);
                            yPoint = 40;
                        }

                        for (int i = 0; i < dataTable.Columns.Count; i++)
                        {
                            string text = row[i].ToString();
                            // Если текст слишком длинный, обрезаем его
                            if (dataTable.Columns[i].ColumnName == "Время вылета" && row[i] is DateTime dt)
                            {
                                text = dt.ToString("dd.MM.yyyy HH:mm");
                            }

                            // Умное обрезание текста (чтобы колонки не наезжали)
                            if (text.Length > 15) text = text.Substring(0, 12) + "...";

                            gfx.DrawString(text, fontBody, XBrushes.Black, 30 + (i * colWidth), yPoint);
                        }
                        yPoint += 20;
                    }

                    document.Save(sfd.FileName);
                    MessageBox.Show("PDF успешно сохранен!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    System.Diagnostics.Process.Start(sfd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при создании PDF: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.Show();
            this.Close();
        }

        private void btnFlightsXml_Click(object sender, EventArgs e)
        {
            SaveToXml(GetFlightsData(), "Отчет_Рейсы");
            Log.Information("Сохранен отчет о рейсах в XML");
        }

        private void btnPassXml_Click(object sender, EventArgs e)
        {
            SaveToXml(GetPassengersData(), "Отчет_Пассажиры");
            Log.Information("Сохранен отчет о  в XML");
            Log.Information("Сохранен отчет о пассажирах в XML");
        }

        private void btnSmoXml_Click(object sender, EventArgs e)
        {
            SaveToXml(GetSmoData(), "Отчет_СМО");
            Log.Information("Сохранен отчет о расчете СМО в XML");
        }
        private void SaveToXml(DataTable dataTable, string defaultFileName)
        {
            if (dataTable.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для отчета!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "XML Files|*.xml";
            sfd.FileName = defaultFileName + "_" + DateTime.Now.ToString("dd-MM-yyyy");

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    dataTable.TableName = "Отчет";
                    dataTable.WriteXml(sfd.FileName);

                    MessageBox.Show("Отчет успешно сохранен в XML!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    System.Diagnostics.Process.Start(sfd.FileName); 
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при создании XML: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}

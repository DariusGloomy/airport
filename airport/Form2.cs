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
    public partial class Form2 : Form
    {
        string connection = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        // Переменные для хранения данных пользователя
        int currentUserId;
        string currentUserRole;
        string currentUserLastName;
        string currentUserFirstName;
        public Form2()
        {
            InitializeComponent();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            AuthForm authForm = new AuthForm();
            authForm.Show();
            this.Hide();
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
            Log.Information("Пользователь вышел из системы.");


        }

        private void button3_Click(object sender, EventArgs e)
        {
            Form3 form3 = new Form3();
            form3.Show();
            this.Hide();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            RegistrationForm registrationForm = new RegistrationForm();
            registrationForm.Show();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            AnalyticsForm analyticsForm = new AnalyticsForm();
            analyticsForm.Show();
            this.Hide();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            ReportsForm reportsForm = new ReportsForm();
            reportsForm.Show();
            this.Hide();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Form5 form5 = new Form5();
            form5.Show();
            this.Hide();
        }

        private void Form2_Load(object sender, EventArgs e)
        {

            label1.Text = $"Добро пожаловать, {UserSession.FirstName} {UserSession.LastName}";

            string roleName = "";
            switch (UserSession.RoleId)
            {
                case "1": roleName = "Администратор"; break;
                case "2": roleName = "Агент по регистрации"; break;
                case "3": roleName = "Аналитик"; break;
                case "4": roleName = "Директор"; break;
                default: roleName = "Сотрудник"; break;
            }
            label2.Text = $"({roleName})";

            //роли

            // button3 - Рейсы
            // button4 - Регистрация
            // button5 - Аналитика
            // button6 - Отчеты
            // button8 - Справочники
            // button7 - Табло

            //Если Агент
            if (UserSession.RoleId == "2")
            {
                button5.Visible = false; // Скрываем аналитику
                button6.Visible = false; // Скрываем отчеты
                button8.Visible = false; // Скрываем справочники
            }
            // Если Аналитик (3)
            else if (UserSession.RoleId == "3")
            {
                button4.Visible = false; // Скрываем регистрацию
                button8.Visible = false; // Скрываем справочники
            }
            // Если Директор (4)
            else if (UserSession.RoleId == "4")
            {
                button3.Visible = false; // Скрываем рейсы
                button4.Visible = false; // Скрываем регистрацию
                button5.Visible = false; // Скрываем аналитику
               // button8.Visible = false; // Скрываем справочники
            }
            // Администратор видит всё, для него ничего не скрываем.
        }
        

        private void button7_Click(object sender, EventArgs e)
        {
            Tablo displayForm = new Tablo();
            displayForm.Show();
            this.Hide();
        }
    }
}

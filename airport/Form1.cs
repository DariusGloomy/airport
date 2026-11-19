using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;

namespace airport
{
    public partial class AuthForm : Form

    {
        string connection = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        public AuthForm()
        {
            InitializeComponent();
        }
        public string HashPassword(string password)
        {
            // Создаем объект
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Превращаем строку в массив байтов и хэшируем
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Конвертируем байты обратно в строку
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string login = textBox1.Text;
            string password = textBox2.Text;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Оба поля должны быть заполнены!", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string hashedPassword = HashPassword(password);

            using (SqlConnection sqlConnection = new SqlConnection(connection))
            {
                sqlConnection.Open();

                // Достаем данные пользователя
                string query = "SELECT UserID, RoleID, LastName, FirstName FROM Users WHERE Login = @login AND Password = @password";
                SqlCommand sqlCommand = new SqlCommand(query, sqlConnection);

                sqlCommand.Parameters.AddWithValue("@login", login);
                sqlCommand.Parameters.AddWithValue("@password", hashedPassword);

                using (SqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        UserSession.UserId = Convert.ToInt32(reader["UserID"]);
                        UserSession.RoleId = reader["RoleID"].ToString();
                        UserSession.LastName = reader["LastName"].ToString();
                        UserSession.FirstName = reader["FirstName"].ToString();

                        MessageBox.Show("Успешный вход!", "Отлично!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Log.Information($"Пользователь {login} успешно вошел в систему.");

                        Form2 form2 = new Form2();
                        form2.Show();
                        this.Hide();
                    }
                    else
                    {
                        MessageBox.Show("Вы указали неверные данные!", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void AuthForm_Load(object sender, EventArgs e)
        {

        }
    }
}
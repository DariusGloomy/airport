using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using System.Configuration;


namespace airport
{
    public partial class TicketForm : Form
    {
        // Конструктор принимает все данные билета
        public TicketForm(string fio, string flight, string fromCity, string toCity, string seat, string ticket, string date)
        {
            InitializeComponent();

            // Заполняем лейблы
            lblPassenger.Text = fio;
            lblFlight.Text = flight;
            lblCities.Text = $"{fromCity}  ➔  {toCity}";
            lblSeat.Text = seat;
            lblTicket.Text = ticket;
            lblDate.Text = date;
        }

        // Кнопка закрытия (крестик)
        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // Если рамки нет, нужно разрешить перетаскивание формы мышкой за фон
        private void TicketForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Магия перетаскивания формы без рамки
                // (Для этого нужно будет добавить пару строк, но для простоты можно просто убрать FormBorderStyle=None)
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}

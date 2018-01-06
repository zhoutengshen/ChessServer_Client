using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class JoinRoomForm : Form
    {
        public JoinRoomForm(string allRoms)
        {
            InitializeComponent();
            int i = 0;
            foreach (string item in allRoms.Split('|'))
            {
                listView1.Items.Add("Id="+item);
                listView1.Items[i].Tag = item;
            }

        }


        string slectRoomIp;
        private void button1_Click(object sender, EventArgs e)
        {
            slectRoomIp = listView1.SelectedItems[0].Tag.ToString();
        }
    }
}

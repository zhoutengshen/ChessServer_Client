using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace Client
{
    [Serializable]
    public partial class ClientForm : Form
    {
        public ClientForm()
        {
            InitializeComponent();
            TextBox.CheckForIllegalCrossThreadCalls = false;
        }

        Socket sokClient = null;//负责与服务端通信的套接字
        Thread threadClient = null;//负责 监听 服务端发送来的消息的线程
        bool isRec = true;//是否循环接收服务端数据

        private void btnConnect_Click(object sender, EventArgs e)
        {
            //实例化 套接字
            sokClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //创建 ip对象
            IPAddress address = IPAddress.Parse(txtIP.Text.Trim());
            //创建网络节点对象 包含 ip和port
            IPEndPoint endpoint = new IPEndPoint(address, int.Parse(txtPort.Text.Trim()));
            //连接 服务端监听套接字
            sokClient.Connect(endpoint);

            //创建负责接收 服务端发送来数据的 线程
            threadClient = new Thread(ReceiveMsg);
            threadClient.IsBackground = true;
            //如果在win7下要通过 某个线程 来调用 文件选择框的代码，就需要设置如下
            threadClient.SetApartmentState(ApartmentState.STA);
            threadClient.Start();

            RequestLoadAllRoom();
        }

        /// <summary>
        /// 接收服务端发送来的消息数据
        /// </summary>
        void ReceiveMsg()
        {
            while (isRec)
            {
                try
                {
                    byte[] msgArr = new byte[1024 * 1024 * 1];//接收到的消息的缓冲区
                    int length = 0;
                    //接收服务端发送来的消息数据
                    length = sokClient.Receive(msgArr);//Receive会阻断线程
                    Package pk = new Package();


                    pk.UnPack(msgArr);
                    string strMsg1 = System.Text.Encoding.UTF8.GetString(pk.ArryPackage, 0, length);
                    txtShow.Clear();
                    txtShow.AppendText(strMsg1 + "\r\n");


                    if (pk.ArrFlag == 0)//发送来的是文字
                    {
                        string strMsg = System.Text.Encoding.UTF8.GetString(pk.ArryPackage, 0, length);
                        txtShow.AppendText(strMsg + "\r\n");

                    }
                    else if (pk.ArrFlag == 1)
                    { //发送来的是文件
                        SaveFileDialog sfd = new SaveFileDialog();
                        //弹出文件保存选择框
                        if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            //创建文件流
                            using (FileStream fs = new FileStream(sfd.FileName, FileMode.OpenOrCreate))
                            {
                                fs.Write(pk.ArryPackage, 1, length - 1);
                            }
                        }
                    }
                    else if (pk.ArrFlag == 2)
                    {//发送来的是命令
                        string str = Encoding.UTF8.GetString(pk.ArryPackage, 0, length - 1);
                        string methodName = str.Split('|')[0];
                        MethodInfo mi = this.GetType().GetMethod(methodName);
                        mi.Invoke(this, new object[] { str });
                    }
                    else if (pk.ArrFlag == 3)
                    {
                        comboBox1.Items.Clear();
                        string strMsg = System.Text.Encoding.UTF8.GetString(pk.ArryPackage, 0, length - 1);
                        foreach (string item in strMsg.Split('|'))
                        {
                            if (item != string.Empty)
                            {
                                comboBox1.Items.Add(item);
                            }
                        }

                    }
                    else if (pk.ArrFlag == 4)
                    {
                        List<byte> lb = new List<byte>(msgArr);
                        byte[] newBuff = lb.GetRange(2, length - 2).ToArray();
                        using (MemoryStream ms = new MemoryStream(newBuff))
                        {
                            IFormatter iFormatter = new BinaryFormatter();
                            f1.manmager = iFormatter.Deserialize(ms) as Chess.GameManager;
                            f1.CurentBmp = f1.manmager.curentBmp.Clone() as Bitmap;
                            f1.panel2.CreateGraphics().DrawImage(f1.manmager.curentBmp, f1.panel2.ClientRectangle);
                            //MessageBox.Show(f1.manmager == null ? "yes" : "no");
                        }
                    }
                }
                catch (Exception)
                {
                    //MessageBox.Show("客户端异常"+i);
                    //throw;
                }

            }
        }



        /// <summary>
        /// 闪屏
        /// </summary>
        public void ShakeWindow(string str)
        {
            Random ran = new Random();
            //保存 窗体原坐标
            System.Drawing.Point point = this.Location;
            for (int i = 0; i < 30; i++)
            {
                //随机 坐标
                this.Location = new System.Drawing.Point(point.X + ran.Next(8), point.Y + ran.Next(8));
                System.Threading.Thread.Sleep(15);//休息15毫秒
                this.Location = point;//还原 原坐标(窗体回到原坐标)
                System.Threading.Thread.Sleep(15);//休息15毫秒
            }
        }

        //发送消息
        private void btnSend_Click(object sender, EventArgs e)
        {
            //byte[] arrMsg = System.Text.Encoding.UTF8.GetBytes(txtInput.Text.Trim());
            Package pk = new Package();
            byte[] arrMsg = pk.PackStr(joinRoom.roomId + "|" + txtInput.Text.Trim());
            sokClient.Send(arrMsg);
        }

        Chess.Form1 f1 = new Chess.Form1();
        string strCmd;
        //创建房间请求
        private void button3_Click(object sender, EventArgs e)
        {
            //1：创建一个房间
            CreateRoomForm crf = new CreateRoomForm();
            DialogResult dr = crf.ShowDialog();
            if (dr == DialogResult.OK)
            {
                strCmd = "CreateRoom|roomName=" + crf.textBox1.Text + "|roomId=" + crf.textBox2.Text + "|roomstate=0";
                this.Text = "房间：" + crf.textBox2.Text;
                //2：将创建的房间的名字反馈到服务器上；
                byte[] bstr = (new Package()).PackStrCmd(strCmd);
                sokClient.Send(bstr);
                crf.Close();

                f1.Show();

            }

        }
        //创建房间
        public void CreateRoom(string str)
        {
            string[] sstr = str.Split('|');
            Room room = new Room();
            room.roomName = sstr[1].Split('=')[1];
            room.roomId = Convert.ToInt32(sstr[2].Split('=')[1]);
            room.roomState = Convert.ToInt32(sstr[3].Split('=')[1]);
            listView1.Items.Add(room.roomName, room.roomState);
            listView1.Items[listView1.Items.Count - 1].Tag = room;

            HosterJoin(str);



        }

        //用户推出房请求
        public void RequestLeaveRoom()
        {
            string strCmd = "LeaveRoom";
            sokClient.Send((new Package()).PackStrCmd(strCmd));
        }
        //离开房间
        public void LeaveRoom()
        {
            MessageBox.Show("");
        }

        public void DelRoom(int roomId)
        {

            foreach (ListViewItem item in listView1.Items)
            {
                if (Convert.ToInt32(item.Tag) == roomId)
                {
                    listView1.Items.Remove(item);
                }
            }
        }

        //请求获取所有的房间
        public void RequestLoadAllRoom()
        {
            string rq = "LoadAllRoom";
            sokClient.Send((new Package().PackStrCmd(rq)));
        }
        //加载所有的房间
        public void LoadAllRoom(string strRooms)
        {
            listView1.Items.Clear();
            int index = 0;
            string[] rooms = strRooms.Split('|');
            for (int i = 1; i < rooms.Length;)
            {
                Room room = new Room();
                room.roomName = rooms[i++].Split('=')[1];
                room.roomId = Convert.ToInt32(rooms[i++].Split('=')[1]);
                room.roomState = Convert.ToInt32(rooms[i++].Split('=')[1]);
                listView1.Items.Add(room.roomName, room.roomState);
                listView1.Items[index].Tag = room;
                index++;
            }
        }
        /// <summary>
        /// 请求加入房间
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void button4_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                Room room = (listView1.SelectedItems[0].Tag) as Room;
                if (room.roomState == 1)
                    return;
                string strCmd = "JoinRoom|roomName=" + room.roomName +
                    "|roomId=" + room.roomId + "|roomState=" + 1;
                sokClient.Send((new Package()).PackStrCmd(strCmd));
                this.Text = "加入房间：" + room.roomId;
            }
            f1.Show();
        }
        //加入房间
        Room joinRoom = new Room();
        public void JoinRoom(string str)
        {
            string[] sstr = str.Split('|');
            Room room = new Room();
            room.roomName = sstr[1].Split('=')[1];
            room.roomId = Convert.ToInt32(sstr[2].Split('=')[1]);
            room.roomState = Convert.ToInt32(sstr[3].Split('=')[1]);
            for (int i = 0; i < listView1.Items.Count; i++)
            {
                joinRoom = listView1.Items[i].Tag as Room;
                if (room != null && joinRoom.roomId == room.roomId)
                {
                    listView1.Items[i].ImageIndex = room.roomState;

                    return;
                }
            }


        }

        public void HosterJoin(string roomStr)
        {
            string[] strs = roomStr.Split('|');
            joinRoom.roomName = strs[1].Split('=')[1];
            joinRoom.roomId = Convert.ToInt32(strs[2].Split('=')[1]);
            joinRoom.roomState = Convert.ToInt32(strs[3].Split('=')[1]);
        }

        public void PlayerJoin(string roomStr)
        {
            string[] strs = roomStr.Split('|');
            joinRoom.roomName = strs[1].Split('=')[1];
            joinRoom.roomId = Convert.ToInt32(strs[2].Split('=')[1]);
            joinRoom.roomState = Convert.ToInt32(strs[3].Split('=')[1]);
        }

        void SendToClient(Chess.Form1 f1)
        {
            byte[] buff;
            using (MemoryStream ms = new MemoryStream())
            {
                IFormatter iFormatter = new BinaryFormatter();
                iFormatter.Serialize(ms, f1.manmager);
                buff = ms.GetBuffer();
            }

            byte[] newBuff = new byte[buff.Length + 1];
            newBuff[0] = Convert.ToByte(joinRoom.roomId);
            buff.CopyTo(newBuff, 1);


            Package pk = new Package();


            this.sokClient.Send(pk.PackObj(newBuff));
        }

        private void ClientForm_Load(object sender, EventArgs e)
        {
            f1.SendToClient = SendToClient;
        }





        private void button5_Click(object sender, EventArgs e)
        {
            string str = "1";
            byte b = Convert.ToByte(str);
            MessageBox.Show(b + "");
        }
    }
}

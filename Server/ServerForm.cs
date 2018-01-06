using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server
{
    public partial class ServerForm : Form
    {
        public ServerForm()
        {
          
            InitializeComponent();
            TextBox.CheckForIllegalCrossThreadCalls = false;//关闭跨线程修改控件检查
        }


        Socket sokWatch = null;//负责监听 客户端段 连接请求的  套接字
        Thread threadWatch = null;//负责 调用套接字， 执行 监听请求的线程


        string AllRemoteEndPoint = "";

        //开启监听 按钮
        private void btnStartListen_Click(object sender, EventArgs e)
        {
            //实例化 套接字 （ip4寻址协议，流式传输，TCP协议）
            sokWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //创建 ip对象
            IPAddress address = IPAddress.Parse(txtIP.Text.Trim());
            //创建网络节点对象 包含 ip和port
            IPEndPoint endpoint = new IPEndPoint(address, int.Parse(txtPort.Text.Trim()));
            //将 监听套接字  绑定到 对应的IP和端口
            sokWatch.Bind(endpoint);
            //设置 监听队列 长度为10(同时能够处理 10个连接请求)
            sokWatch.Listen(10);
            threadWatch = new Thread(StartWatch);
            threadWatch.IsBackground = true;
            threadWatch.Start();
            txtShow.AppendText("启动服务器成功......\r\n");
        }

        //Dictionary<string, Socket> dictSocket = new Dictionary<string, Socket>();
        Dictionary<string, ConnectionClient> dictConn = new Dictionary<string, ConnectionClient>();

        bool isWatch = true;

        #region 1.被线程调用 监听连接端口
        /// <summary>
        /// 被线程调用 监听连接端口
        /// </summary>
        void StartWatch()
        {
            while (isWatch)
            {
                //threadWatch.SetApartmentState(ApartmentState.STA);
                //监听 客户端 连接请求，但是，Accept会阻断当前线程
                Socket sokMsg = sokWatch.Accept();//监听到请求，立即创建负责与该客户端套接字通信的套接字
                ConnectionClient connection = new ConnectionClient(sokMsg, ShowMsg, RemoveClientConnection,
                    CeateRoom, JoinRoom, LoadAllRoom, GetRoomByRoomId);
                //connection.actCreateRoom = new Action<Room>(CeateRoom);
                //connection.actJoinRoom = new Action<Room>(JoinRoom);
                //connection.funcLoadAllRoom = new Func<string>(LoadAllRoom);
                //connection.funcGetRoomByRoomId = new Func<int, Room>(GetRoomByRoomId);
                //将负责与当前连接请求客户端 通信的套接字所在的连接通信类 对象 装入集合
                dictConn.Add(sokMsg.RemoteEndPoint.ToString(), connection);
                //将 通信套接字 加入 集合，并以通信套接字的远程IpPort作为键
                //dictSocket.Add(sokMsg.RemoteEndPoint.ToString(), sokMsg);
                //将 通信套接字的 客户端IP端口保存在下拉框里
                cboClient.Items.Add(sokMsg.RemoteEndPoint.ToString());
                AllRemoteEndPoint += sokMsg.RemoteEndPoint.ToString() + "|";
                ShowMsg("接收连接成功......");
                //启动一个新线程，负责监听该客户端发来的数据
                //Thread threadConnection = new Thread(ReciveMsg);
                //threadConnection.IsBackground = true;
                //threadConnection.Start(sokMsg);

                foreach (string item in dictConn.Keys)
                {
                    //dictConn[item].SendAllClientPortToClient(AllRemoteEndPoint);
                }
            }
        }
        #endregion

        #region 发送消息 到指定的客户端 -btnSend_Click
        //发送消息 到指定的客户端
        private void btnSend_Click(object sender, EventArgs e)
        {
            string connectionSokKey = cboClient.Text;
            if (!string.IsNullOrEmpty(connectionSokKey))
            {
                dictConn[connectionSokKey].Send(txtInput.Text);
            }
            else
            {
                MessageBox.Show("请选择要发送的客户端～～");
            }
        }
        #endregion

        //发送闪屏！！
        private void btnShack_Click(object sender, EventArgs e)
        {
            string connectionSokKey = cboClient.Text;
            if (!string.IsNullOrEmpty(connectionSokKey))
            {
                dictConn[connectionSokKey].SendShake();
            }
            else
            {
                MessageBox.Show("请选择要发送的客户端～～");
            }
        }
        //群闪
        private void btnShackAll_Click(object sender, EventArgs e)
        {
            foreach (ConnectionClient conn in dictConn.Values)
            {
                conn.SendShake();
            }
        }

        #region 2 移除与指定客户端的连接 +void RemoveClientConnection(string key)
        /// <summary>
        /// 移除与指定客户端的连接
        /// </summary>
        /// <param name="key">指定客户端的IP和端口</param>
        public void RemoveClientConnection(string key)
        {
            if (dictConn.ContainsKey(key))
            {
                dictConn.Remove(key);
                cboClient.Items.Remove(key);
            }
        }
        #endregion

        //选择要发送的文件
        private void btnChooseFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtFilePath.Text = ofd.FileName;
            }
        }

        //发送文件
        private void btnSendFile_Click(object sender, EventArgs e)
        {
            //拿到下拉框中选中的客户端IPPORT
            string key = cboClient.Text;
            if (!string.IsNullOrEmpty(key))
            {
                dictConn[key].SendFile(txtFilePath.Text.Trim());
            }
        }

        #region 向文本框显示消息 -void ShowMsg(string msgStr)
        /// <summary>
        /// 向文本框显示消息
        /// </summary>
        /// <param name="msgStr">消息</param>
        public void ShowMsg(string msgStr)
        {
            txtShow.AppendText(msgStr + "\r\n");
        }
        #endregion

        private void btnSendMsgAll_Click(object sender, EventArgs e)
        {
            foreach (ConnectionClient conn in dictConn.Values)
            {
                conn.Send(txtInput.Text.Trim());
            }
        }


        private void txtShow_TextChanged(object sender, EventArgs e)
        {

        }

        //添加一个room
        public void CeateRoom(Room room)
        {

            listView1.Items.Add(room.roomName, 0);
            listView1.Items[listView1.Items.Count - 1].Tag = room;
            SenAllRoomsToAllClient(room);
        }

        public void JoinRoom(Room room)
        {
            for (int i = 0; i < listView1.Items.Count; i++)
            {
                Room roomJ = listView1.Items[i].Tag as Room;
                if (roomJ != null && roomJ.roomId == room.roomId)
                {
                    listView1.Items[i].ImageIndex = room.roomState;
                    roomJ.roomState = room.roomState;
                    roomJ.payerSock = room.payerSock;
                    Package pk = new Package();


                    roomJ.hostSock.Send(pk.PackStrCmd("PlayerJoin" +
                        "|roomName=" + roomJ.roomName +
                        "|roomId=" + roomJ.roomId +
                        "|roomState=" + roomJ.roomState));
                    return;
                }
            }
        }

        public string LoadAllRoom()
        {
            string roomStr = "LoadAllRoom";
            foreach (ListViewItem item in listView1.Items)
            {
                Room room = item.Tag as Room;
                if (room != null)
                {
                    roomStr += "|roomName=" + room.roomName
                         + "|roomId=" + room.roomId
                         + "|roomstate=" + room.roomState;
                }
            }
            return roomStr;
        }
        //更新客户端的房间
        public void SenAllRoomsToAllClient(Room room)
        {
            string strRooms = LoadAllRoom();
            Package pk = new Package();
            byte[] arrData = pk.PackStrCmd(strRooms);
            foreach (ConnectionClient conn in dictConn.Values)
            {
                if (room.hostSock != conn.SokMsg)
                {
                    conn.SokMsg.Send(arrData);
                }

            }
        }

        public Room GetRoomByRoomId(int roomId)
        {
            Room room = null;
            foreach (ListViewItem item in listView1.Items)
            {
                room = item.Tag as Room;
                if (room.roomId == roomId)
                {
                    return room;
                }
            }
            return null;
        }

        private void ServerForm_Load(object sender, EventArgs e)
        {
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            string strRooms = LoadAllRoom();
            Package pk = new Package();
            byte[] arrData = pk.PackStrCmd(strRooms);
            foreach (ConnectionClient conn in dictConn.Values)
            {
                conn.SokMsg.Send(arrData);
            }
        }
    }
}


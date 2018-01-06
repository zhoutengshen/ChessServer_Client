using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace Server
{
    /// <summary>
    /// 与客户端的 连接通信类(包含了一个 与客户端 通信的 套接字，和线程)
    /// </summary>
    public class ConnectionClient
    {

        Socket sokMsg;
        Action<string> dgShowMsg = null;//负责 向主窗体文本框显示消息的方法委托
        Action<string> dgRemoveConnection = null;// 负责 从主窗体 中移除 当前连接
        Thread threadMsg;

        public Action actInitAllDg = null;

        #region 构造函数
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sokMsg">通信套接字</param>
        /// <param name="dgShowMsg">向主窗体文本框显示消息的方法委托</param>
        public ConnectionClient(Socket sokMsg, Action<string> dgShowMsg, Action<string> dgRemoveConnection,
            Action<Room> actCreateRoom, Action<Room> actJoinRoom,
            Func<string> funcLoadAllRoom, Func<int, Room> funcGetRoomByRoomId)
        {
            this.actCreateRoom = actCreateRoom;
            this.actJoinRoom = actJoinRoom;
            this.funcLoadAllRoom = funcLoadAllRoom;
            this.funcGetRoomByRoomId = funcGetRoomByRoomId;
            this.sokMsg = sokMsg;
            this.dgShowMsg = dgShowMsg;
            this.dgRemoveConnection = dgRemoveConnection;
            this.threadMsg = new Thread(RecMsg);
            this.threadMsg.IsBackground = true;
            this.threadMsg.Start();
        }
        #endregion

        bool isRec = true;
        #region 02负责监听客户端发送来的消息
        byte[] arrMsg;
        void RecMsg()
        {
            while (isRec)
            {
                try
                {
                    arrMsg = new byte[1024 * 1024 * 2];
                    //接收 对应 客户端发来的消息
                    int length = sokMsg.Receive(arrMsg);

                    Package pk = new Package();
                    pk.UnPack(arrMsg);

                    if (pk.ArrFlag == 0)
                    {
                        //将接收到的消息数组里真实消息转成字符串
                        string strMsg = System.Text.Encoding.UTF8.GetString(pk.ArryPackage, 0, length - 1);
                        //发送到在同一房间的socket；
                        Transpond(strMsg);
                        //通过委托 显示消息到 窗体的文本框
                        dgShowMsg(strMsg);
                    }
                    if (pk.ArrFlag == 2)
                    {

                        //将接收到的消息数组里真实消息转成字符串
                        string strMsg = System.Text.Encoding.UTF8.GetString(pk.ArryPackage, 0, length - 1);
                        //通过委托 显示消息到 窗体的文本框

                        string[] str = strMsg.Split('|');
                        MethodInfo mi = this.GetType().GetMethod(str[0]);
                        object[] ss = new object[] { strMsg };
                        mi.Invoke(this, ss);
                        dgShowMsg(str[0]);
                        this.sokMsg.Send(arrMsg);


                    }
                    if (pk.ArrFlag == 4)
                    {
                        List<byte> lb = new List<byte>(arrMsg);
                        byte []newBuff = lb.GetRange(0, length).ToArray();
                        int id = pk.ArryPackage[0];
                        Room room = funcGetRoomByRoomId(Convert.ToInt32(id));
                        if (room.hostSock == this.sokMsg)
                        {
                            room.payerSock.Send(newBuff);
                        }
                        else
                        {
                            room.hostSock.Send(newBuff);
                        }


                    }
                }
                catch (Exception )
                {
                    isRec = false;
                    //从主窗体中 移除 下拉框中对应的客户端选择项，同时 移除 集合中对应的 ConnectionClient对象
                    dgRemoveConnection(sokMsg.RemoteEndPoint.ToString());


                }
            }
        }
        #endregion

        #region 03向客户端发送消息
        /// <summary>
        /// 向客户端发送消息
        /// </summary>
        /// <param name="strMsg"></param>
        public void Send(string strMsg)
        {
            //byte[] arrMsg = System.Text.Encoding.UTF8.GetBytes(strMsg);
            //byte[] arrMsgFinal = new byte[arrMsg.Length + 1];

            //arrMsgFinal[0] = 0;//设置 数据标识位等于0，代表 发送的是 文字
            //arrMsg.CopyTo(arrMsgFinal, 1);
            //sokMsg.Send(arrMsgFinal);
            Package pk = new Package();
            sokMsg.Send(pk.PackStr(strMsg));
        }
        #endregion

        #region 04向客户端发送文件数据 +void SendFile(string strPath)
        /// <summary>
        /// 04向客户端发送文件数据
        /// </summary>
        /// <param name="strPath">文件路径</param>
        public void SendFile(string strPath)
        {

            Package pk = new Package();
            sokMsg.Send(pk.PackFile(strPath));
        }
        #endregion

        #region 05向客户端发送闪屏
        /// <summary>
        /// 向客户端发送闪屏
        /// </summary>
        /// <param name="strMsg"></param>
        public void SendShake()
        {
            //byte[] arrMsgFinal = new byte[1];
            //arrMsgFinal[0] = 2;
            //sokMsg.Send(arrMsgFinal);
            Package pk = new Package();
            sokMsg.Send(pk.PackStrCmd("ShakeWindow"));
        }
        #endregion

        #region 06关闭与客户端连接
        /// <summary>
        /// 关闭与客户端连接
        /// </summary>
        public void CloseConnection()
        {
            isRec = false;
        }
        #endregion

        #region 返回所有客户端端口给用户建立通讯；
        public void SendAllClientPortToClient(string strMsg)
        {

            Package pk = new Package();
            sokMsg.Send(pk.PackAllIp(strMsg));

        }
        #endregion


        public Action<Room> actCreateRoom = null;
        public void CreateRoom(string strPar)
        {
            string[] str = strPar.Split('|');
            if (str.Length > 0)
            {
                Room room = new Room();
                room.roomName = str[1].Split('=')[1];
                room.roomId = Convert.ToInt32(str[2].Split('=')[1]);
                room.roomState = Convert.ToInt32(str[3].Split('=')[1]);
                room.hostSock = sokMsg;
                actCreateRoom(room);
            }
        }

        public Action<Room> actJoinRoom = null;
        public void JoinRoom(string strPar)
        {
            string[] str = strPar.Split('|');
            if (str.Length > 0)
            {
                Room room = new Room();
                room.roomName = str[1].Split('=')[1];
                room.roomId = Convert.ToInt32(str[2].Split('=')[1]);
                room.roomState = Convert.ToInt32(str[3].Split('=')[1]);
                room.payerSock = sokMsg;
                actJoinRoom(room);
            }
        }


        public Func<string> funcLoadAllRoom = null;

        public Socket SokMsg
        {
            get
            {
                return sokMsg;
            }

            set
            {
                sokMsg = value;
            }
        }

        public void LoadAllRoom(string strPar)
        {
            string strRooms = funcLoadAllRoom();
            Package pk = new Package();
            arrMsg = pk.PackStrCmd(strRooms);

        }


        public Func<int, Room> funcGetRoomByRoomId = null;
        public void Transpond(string clientStr)
        {
            Package pk = new Package();
            string[] str = clientStr.Split('|');
            Room room = funcGetRoomByRoomId(Convert.ToInt32(str[0]));
            if (room.hostSock == this.sokMsg)
            {
                byte[] byteStr = pk.PackStr(clientStr.Substring(str[0].Length - 1));
                room.payerSock.Send(byteStr);
            }
            else
            {
                byte[] byteStr = pk.PackStr(clientStr.Substring(str[0].Length - 1));
                room.hostSock.Send(byteStr);
            }
        }
    }
}
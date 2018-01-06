using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Room
    {
        //房间的id
        public int roomId;
        //房间的名字
        public string roomName;
        //房间的密码
        public string roomPwd;
        //房间的状态
        public int roomState;
        //房主的套接字
        public Socket hostSock;
        //玩家的套接字
       public Socket payerSock;
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Package
    {

        //内容
        byte[] arryPackage;
        //标记；
        byte arrFlag;
     

        public byte ArrFlag
        {
            get
            {
                return arrFlag;
            }

            set
            {
                arrFlag = value;
            }
        }

        public byte[] ArryPackage
        {
            get
            {
                return arryPackage;
            }

            set
            {
                arryPackage = value;
            }
        }

        /// <summary>
        /// 封装字符串
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public byte[] PackStr(string str)
        {
            byte[] arry = Encoding.UTF8.GetBytes(str);
            byte[] byteStr = new byte[arry.Length + 1];
            byteStr[0] = 0;
            arry.CopyTo(byteStr, 1);
            return byteStr;

        }

        public byte[] PackObj(byte[] objBuff)
        {
            byte[] newObjBuff = new byte[objBuff.Length + 1];
            newObjBuff[0] = 4;
            newObjBuff.CopyTo(newObjBuff, 1);
            return newObjBuff;
        }

        /// <summary>
        /// 反射命令
        /// </summary>
        /// <param name="strCmd"></param>
        /// <returns></returns>
        public byte[] PackStrCmd(string str)
        {
            byte[] arry = Encoding.UTF8.GetBytes(str);
            byte[] byteStr = new byte[arry.Length + 1];
            byteStr[0] = 2;
            arry.CopyTo(byteStr, 1);
            return byteStr;
        }

        /// <summary>
        /// 封装文件；
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public byte[] PackFile(string filePath)
        {
            //通过文件流 读取文件内容
            using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate))
            {
                byte[] arrFile = new byte[1024 * 1024 * 2];
                //读取文件内容到字节数组，并 获得 实际文件大小
                int length = fs.Read(arrFile, 0, arrFile.Length);
                //定义一个 新数组，长度为文件实际长度 +1
                byte[] arrFileFina = new byte[length + 1];
                arrFileFina[0] = 1;//设置 数据标识位等于1，代表 发送的是文件
                //将 文件数据数组 复制到 新数组中，下标从1开始
                //arrFile.CopyTo(arrFileFina, 1);
                Buffer.BlockCopy(arrFile, 0, arrFileFina, 1, length);
                arrFileFina.CopyTo(arryPackage, 0);
                return arrFileFina;
            }
        }

        /// <summary>
        /// 解包
        /// </summary>
        /// <param name="date"></param>
        public void UnPack(byte[] date)
        {
            arrFlag = date[0];
            ArryPackage = new byte[date.Length];
            for (int i = 1; i < date.Length; i++)
            {
                arryPackage[i-1] = date[i];
            }
        }

        public byte[] PackAllIp(string strMsg)
        {
            byte[] arrMsg = System.Text.Encoding.UTF8.GetBytes(strMsg);
            byte[] arrMsgFinal = new byte[arrMsg.Length + 1];

            arrMsgFinal[0] = 3;//设置 数据标识位等于0，代表 发送的是 文字
            arrMsg.CopyTo(arrMsgFinal, 1);
            return arrMsgFinal;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace XFileUpload
{
    public class ByteSocket
    {
        private const string BOUNDARY = "------ninesoftcrossplatformupfile\r\n";
        private const string CONTENTDISPOSITION = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n";
        private const string CONTENTTYPE = "Content-Type: {0}\r\n\r\n";
        private const string BOUNDARYEND = "\r\n------ninesoftcrossplatformupfile--\r\n";
        private const int SENDLENGTH = 1024;

        private string mUrl;
        private string mName;
        private string mFileName;
        private string mType;
        private int mContentLength;
        private string mPath;
        private Socket mSocket;

        public bool IsStop { get; set; }
        public event Action<Exception> OnException;
        public event Action<int, int> OnSend;
        public event Action<String> OnFinished;

        public ByteSocket(String url,string path, string name, string filename, string type)
        {
            this.mUrl = url;
            this.mName = name;
            this.mFileName = filename;
            this.mType = type;
            this.mPath = path;
        }

        public void Upload()
        {
            new TaskFactory().StartNew(() =>
            {
                FileStream fs = new FileStream(mPath, FileMode.Open);

                int readLength = 0;
                int totalLength = (int)fs.Length;
                mContentLength = totalLength;
                if(Start())
                {
                    while(totalLength > 0)
                    {
                        if(IsStop)
                        {
                            break;
                        }
                        byte[] readBytes = null;
                        if (totalLength > SENDLENGTH)
                        {
                            readBytes = new byte[SENDLENGTH];
                            readLength = fs.Read(readBytes, 0, SENDLENGTH);
                        }
                        else
                        {
                            readBytes = new byte[totalLength];
                            readLength = fs.Read(readBytes, 0, totalLength);
                        }
                        totalLength -= readLength;
                        SendByte(readBytes);
                    }
                    Stop();
                }
                fs.Close();
            });
        }

        protected bool Start()
        {
            try
            {
                Uri uri = new Uri(mUrl);
                IPHostEntry gist = Dns.GetHostEntry(uri.Host);
                IPAddress ip = gist.AddressList[0];
                IPEndPoint ipEnd = new IPEndPoint(ip, uri.Port);
                mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                mSocket.SendTimeout = 15000;
                mSocket.SendBufferSize = 1024 * 5;
                mSocket.Connect(ipEnd);
                byte[] headerBytes = GetHeadBytes(uri);
                mSocket.Send(headerBytes);
                mSocket.Send(GetBodyBeging());
            }
            catch(Exception ex)
            {
                if(OnException != null)
                {
                    OnException(ex);
                }
                return false;
            }
            return true;
        }

        protected void SendByte(byte[] bytes)
        {
            if (mSocket != null)
            {
                if(mSocket.Connected)
                {
                    try
                    {
                        mSocket.Send(bytes);
                        if(OnSend != null)
                        {
                            OnSend(mContentLength, bytes.Length);
                        }
                    }
                    catch(Exception ex)
                    {
                        IsStop = true;
                        if(OnException != null)
                        {
                            OnException(ex);
                        }
                    }
                }
            }
        }

        protected void Stop()
        {
            if (mSocket.Connected)
            {
                String strResult = null;
                if (!IsStop)
                {
                    
                    byte[] result = new byte[10240];
                    mSocket.Send(GetBodyEnd());
                    int length = mSocket.Receive(result);
                    strResult = Encoding.UTF8.GetString(result, 0, length);
                    Regex reg = new Regex(@"\s{4}(\d+)");

                    strResult = reg.Match(strResult).Groups[1].Value;
                }
                mSocket.Shutdown(SocketShutdown.Both);
                mSocket.Close();
                if(!IsStop)
                {
                    if (OnFinished != null)
                    {
                        OnFinished(strResult);
                    }
                }
            }
        }

        protected byte[] GetHeadBytes(Uri uri)
        {
            StringBuilder header = new StringBuilder();
            header.AppendFormat("POST {0} HTTP/1.1\r\n", mUrl);
            header.AppendFormat("Host: {0}\r\n", uri.Host);
            int head = GetBodyBeging().Length + GetBodyEnd().Length;
            //header.Append("Connection: Keep-Alive\r\n");
            header.AppendFormat("Content-Length: {0}\r\n", mContentLength + head);
            header.AppendFormat("Accept: application/json, text/javascript, */*; q=0.01\r\n");
            header.Append("Content-Type: multipart/form-data;boundary=----ninesoftcrossplatformupfile\r\n");
            header.Append("User-Agent: Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.89 Safari/537.36\r\n");
            header.Append("Accept-Language: zh-CN,zh;q=0.8\r\n\r\n");
            return Encoding.UTF8.GetBytes(header.ToString());
        }

        protected byte[] GetBodyBeging()
        {
            StringBuilder bodyBegin = new StringBuilder();
            bodyBegin.Append(BOUNDARY);
            bodyBegin.AppendFormat(CONTENTDISPOSITION, mName, mFileName);
            bodyBegin.AppendFormat(CONTENTTYPE, mType);
            return Encoding.UTF8.GetBytes(bodyBegin.ToString());
        }

        protected byte[] GetBodyEnd()
        {
            return Encoding.UTF8.GetBytes(BOUNDARYEND);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
    using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters;
using System.IO;
using System.Runtime.Remoting.Messaging;
namespace RemotingProtocolParser
{
    public class SampleTest
    {
        public void ReadRequestAndWriteResponse()
        {
            IPAddress[] addressList = Dns.GetHostEntry(Environment.MachineName).AddressList;
            var endpoint = new IPEndPoint(addressList[addressList.Length - 1], 9900);
            new TcpListener().Listen(endpoint);

            var url = string.Format("tcp://{0}/remote.rem", endpoint);
            var service = RemotingServices.Connect(typeof(ServiceClass), url) as ServiceClass;
            Console.WriteLine("Do Service Call, reutrn={0}", service.Do("Hi"));
        }

        public void WriteRequestAndReadResponse()
        {

        }

        public class ServiceClass : MarshalByRefObject
        {
            public string Do(string input) { return input; }
        }
    }

    //TODO:rewrite a server
    //buffer碎片管理
    //SocketAsyncEventArgs复用
    //reference:http://www.cnblogs.com/wsky/archive/2011/04/06/2007201.html
    //just a demo server
    public class TcpListener
    {
        private SocketAsyncEventArgs Args;
        private Socket ListenerSocket;
        private StringBuilder buffers;
        private byte[] totalBuffer = new byte[0];
        public TcpListener() { }
        public void Listen(EndPoint e)
        {
            //buffer
            buffers = new StringBuilder();
            //socket
            ListenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ListenerSocket.Bind(e);
            ListenerSocket.Listen(10);
            //异步socket事件
            Args = new SocketAsyncEventArgs();
            Args.Completed += new EventHandler<SocketAsyncEventArgs>(ProcessAccept);
            BeginAccept(Args);
            Console.WriteLine("server run at {0}", e.ToString());
        }

        //开始接受
        void BeginAccept(SocketAsyncEventArgs e)
        {
            e.AcceptSocket = null;
            if (!ListenerSocket.AcceptAsync(e))
                ProcessAccept(ListenerSocket, e);
        }
        //接受完毕 开始接收和发送
        void ProcessAccept(object sender, SocketAsyncEventArgs e)
        {
            Socket s = e.AcceptSocket;
            e.AcceptSocket = null;

            int bufferSize = 10;
            var args = new SocketAsyncEventArgs();
            args.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
            args.SetBuffer(new byte[bufferSize], 0, bufferSize);
            args.AcceptSocket = s;
            if (!s.ReceiveAsync(args))
                this.ProcessReceive(args);

            BeginAccept(e);
        }

        //IOCP回调
        void OnIOCompleted(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    this.ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    this.ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }

        //接收完毕
        void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0)
            {
                if (e.SocketError == SocketError.Success)
                {
                    totalBuffer = Combine(totalBuffer, e.Buffer);

                    if (e.AcceptSocket.Available == 0)
                    {
                        //.Net Remoting Protocol Parser
                        #region Read Request
                        Console.WriteLine("==== .Net Remoting Protocol Parser ====");
                        //1. Preamble, will be ".NET"
                        Console.WriteLine("Preamble: {0}", Encoding.ASCII.GetString(new byte[] { 
                            totalBuffer[0], 
                            totalBuffer[1], 
                            totalBuffer[2], 
                            totalBuffer[3] }));
                        //2. MajorVersion, will be 1
                        Console.WriteLine("MajorVersion: {0}", totalBuffer[4]);
                        //3. MinorVersion, will be 0
                        Console.WriteLine("MinorVersion: {0}", totalBuffer[5]);
                        //4. Operation, will be 5 (request,onewayrequest...)
                        Console.WriteLine("Operation: {0}", (UInt16)(totalBuffer[6] & 0xFF | totalBuffer[7] << 8));
                        //5. TcpContentDelimiter and ContentLength
                        var header = (UInt16)(totalBuffer[8] & 0xFF | totalBuffer[9] << 8);
                        if (header == 1)
                            Console.WriteLine("Chunked: {0}", true);
                        else
                            Console.WriteLine("ContentLength: {0}"
                                , (int)((totalBuffer[10] & 0xFF)
                                | totalBuffer[11] << 8
                                | totalBuffer[12] << 16
                                | totalBuffer[13] << 24));

                        #region 6. ReadHeaders ITransportHeaders
                        var index = header == 1 ? 9 : 13;
                        var headerType = ReadUInt16(ref index);
                        while (headerType != TcpHeaders.EndOfHeaders)
                        {
                            if (headerType == TcpHeaders.Custom)
                            {
                                Console.WriteLine("{0}: {1}", ReadCountedString(ref index), ReadCountedString(ref index));
                            }
                            else if (headerType == TcpHeaders.RequestUri)
                            {
                                Console.WriteLine("RequestUri-Format: {0}", ReadByte(ref index));
                                Console.WriteLine("RequestUri: {0}", ReadCountedString(ref index));
                            }
                            else if (headerType == TcpHeaders.StatusCode)
                            {
                                Console.WriteLine("StatusCode-Format: {0}", ReadByte(ref index));
                                var code = ReadUInt16(ref index);
                                Console.WriteLine("StatusCode: {0}", code);
                                //if (code != 0) error = true;
                            }
                            else if (headerType == TcpHeaders.StatusPhrase)
                            {
                                Console.WriteLine("StatusPhrase-Format: {0}", ReadByte(ref index));
                                Console.WriteLine("StatusPhrase: {0}", ReadCountedString(ref index));
                            }
                            else if (headerType == TcpHeaders.ContentType)
                            {
                                Console.WriteLine("ContentType-Format: {0}", ReadByte(ref index));
                                Console.WriteLine("ContentType: {0}", ReadCountedString(ref index));
                            }
                            else
                            {
                                var headerFormat = (byte)ReadByte(ref index);

                                switch (headerFormat)
                                {
                                    case TcpHeaderFormat.Void: break;
                                    case TcpHeaderFormat.CountedString: ReadCountedString(ref index); break;
                                    case TcpHeaderFormat.Byte: ReadByte(ref index); break;
                                    case TcpHeaderFormat.UInt16: ReadUInt16(ref index); break;
                                    case TcpHeaderFormat.Int32: ReadInt32(ref index); break;
                                    default:
                                        throw new RemotingException("Remoting_Tcp_UnknownHeaderType");
                                }
                            }

                            headerType = ReadUInt16(ref index);
                        }
                        #endregion

                        //7. RequestStream/Message
                        var requestStream = new byte[totalBuffer.Length - index - 1];
                        Buffer.BlockCopy(totalBuffer, index + 1, requestStream, 0, totalBuffer.Length - index - 1);
                        //using BinaryFormatterSink default
                        var requestMessage = BinaryFormatterHelper.DeserializeObject(requestStream) as MethodCall;
                        DumpMessage(requestMessage);
                        #endregion

                        //重置
                        buffers = new StringBuilder();
                        totalBuffer = new byte[0];

                        #region Write Response

                        //http://labs.developerfusion.co.uk/SourceViewer/browse.aspx?assembly=SSCLI&namespace=System.Runtime.Remoting
                        //else if (name.Equals("__Return"))
                        var responeMessage = new MethodResponse(new Header[] { new Header("__Return", "hi") }, requestMessage);

                        //responeMessage.ReturnValue//can not set
                        var responseStream = BinaryFormatterHelper.SerializeObject(responeMessage);
                        //1.Preamble
                        var preamble = Encoding.ASCII.GetBytes(".NET");
                        foreach (var b in preamble)
                            WriteByte(b);
                        //2.MajorVersion
                        WriteByte((byte)1);
                        //3.MinorVersion
                        WriteByte((byte)0);
                        //4.Operation
                        WriteUInt16(TcpOperations.Reply);
                        //5.TcpContentDelimiter and ContentLength
                        WriteUInt16(0);
                        WriteInt32(responseStream.Length);
                        //6.Headers
                        WriteUInt16(TcpHeaders.EndOfHeaders);
                        //7.ResponseStream/Message
                        foreach (var b in responseStream)
                            WriteByte(b);
                        #endregion

                        e.SetBuffer(totalBuffer, 0, totalBuffer.Length);
                        if (!e.AcceptSocket.SendAsync(e))
                        {
                            this.ProcessSend(e);
                        }
                    }
                    else if (!e.AcceptSocket.ReceiveAsync(e))
                    {
                        this.ProcessReceive(e);
                    }
                }
                else
                {
                    //this.ProcessError(e);
                }
            }
            else
            {
                //this.CloseClientSocket(e);
            }
        }
        //发送完毕
        void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                if (!e.AcceptSocket.ReceiveAsync(e))
                {
                    this.ProcessReceive(e);
                }
            }
            else
            {

            }
        }

        byte[] Combine(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }
        int ReadByte(ref int index)
        {
            return totalBuffer[++index];
        }
        UInt16 ReadUInt16(ref int index)
        {
            return (UInt16)(totalBuffer[++index] & 0xFF | totalBuffer[++index] << 8);
        }
        int ReadInt32(ref int index)
        {
            return (int)((totalBuffer[++index] & 0xFF)
                | totalBuffer[++index] << 8
                | totalBuffer[++index] << 16
                | totalBuffer[++index] << 24);
        }
        string ReadCountedString(ref int index)
        {
            var format = (byte)ReadByte(ref index);
            int size = ReadInt32(ref index);

            if (size > 0)
            {
                byte[] data = new byte[size];
                Buffer.BlockCopy(totalBuffer, index + 1, data, 0, size);
                index += size;

                switch (format)
                {
                    case TcpStringFormat.Unicode:
                        return Encoding.Unicode.GetString(data);

                    case TcpStringFormat.UTF8:
                        return Encoding.UTF8.GetString(data);

                    default:
                        throw new RemotingException("Remoting_Tcp_UnrecognizedStringFormat");
                }
            }
            else
            {
                return null;
            }
        }
        void DumpMessage(IMessage msg)
        {
            Console.WriteLine("");
            Console.WriteLine("==== Message Dump ====");
            Console.WriteLine("Type: {0}", msg);
            Console.WriteLine("---- Properties ----");
            var enm = msg.Properties.GetEnumerator();
            while (enm.MoveNext())
            {
                Console.WriteLine("{0}: {1}", enm.Key, enm.Value);
                var data = enm.Value as object[];
                if (data != null)
                    this.DumpArray(data);
            }

            Console.WriteLine("\n\n");
        }
        void DumpArray(object[] data)
        {
            Console.WriteLine("\t---- Array ----");
            for (var i = 0; i < data.Length; i++)
                Console.WriteLine("\t{0}: {1}", i, data[i]);
        }

        void WriteByte(byte data)
        {
            totalBuffer = Combine(totalBuffer, new byte[] { data });
        }
        void WriteUInt16(UInt16 data)
        {
            WriteByte((byte)data);
            WriteByte((byte)(data >> 8));
        }
        void WriteInt32(int data)
        {
            WriteByte((byte)data);
            WriteByte((byte)(data >> 8));
            WriteByte((byte)(data >> 16));
            WriteByte((byte)(data >> 24));
        }

        #region MS Remoting Sourcecode
        public class TcpHeaders
        {
            internal const ushort CloseConnection = 5;
            internal const ushort Custom = 1;
            internal const ushort ContentType = 6;
            internal const ushort EndOfHeaders = 0;
            internal const ushort RequestUri = 4;
            internal const ushort StatusCode = 2;
            internal const ushort StatusPhrase = 3;
        }
        public class TcpStringFormat
        {
            internal const byte Unicode = 0;
            internal const byte UTF8 = 1;
        }
        public class TcpHeaderFormat
        {
            internal const byte Byte = 2;
            internal const byte CountedString = 1;
            internal const byte Int32 = 4;
            internal const byte UInt16 = 3;
            internal const byte Void = 0;
        }
        public class TcpOperations
        {
            internal const ushort OneWayRequest = 1;
            internal const ushort Reply = 2;
            internal const ushort Request = 0;
        }
        #endregion

        internal static class BinaryFormatterHelper
        {
            private static readonly BinaryFormatter FormatterInstance = new BinaryFormatter
            {
                AssemblyFormat = FormatterAssemblyStyle.Simple,
                TypeFormat = FormatterTypeStyle.TypesWhenNeeded,
                FilterLevel = TypeFilterLevel.Full,
            };

            public static byte[] SerializeObject(object value)
            {
                if (value == null)
                    return null;

                using (var stream = new MemoryStream())
                {
                    FormatterInstance.Serialize(stream, value);
                    return stream.ToArray();
                }
            }

            public static object DeserializeObject(byte[] data)
            {
                if (data == null)
                    return null;

                using (var stream = new MemoryStream(data))
                {
                    return FormatterInstance.Deserialize(stream);
                }
            }
        }
    }
}
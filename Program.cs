using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Threading;
using Gps.Plugin.Tcp.GT808;
using Gps.Plugin.Common.Helpers;
using System.Configuration;

namespace GT808ClientTest
{
    class Program
    {
        protected static log4net.ILog log = null;
        private string ip;
        private int port;
        private string deviceId;
        Random r = new Random();
        private int interval;
        private int sendCountByOneDevice;
        private ProcessCounter processCounter;
        private static bool sendLogPrint;
        private static bool waitReceive;
        private static bool receiveLogPrint;

        static Program()
        {
            log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile));
            log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        }
        private double initLat = 38.01342;
        private double initLon = 114.560478;

        public Program(string ip, int port, string deviceId, int interval, int sendCountByOneDevice, ProcessCounter processCounter)
        {
            // TODO: Complete member initialization
            this.latlonBuilder = new LatlonBuilder(initLat, initLon, 31.802893, 39.300299, 104.941406, 117.861328);
            this.ip = ip;
            this.port = port;
            this.deviceId = deviceId;
            this.interval = interval;
            this.sendCountByOneDevice = sendCountByOneDevice;
            this.processCounter = processCounter;
        }

        private static byte[] stringtobytes(string str)
        {
            string[] strs = str.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            byte[] buffer = new byte[strs.Length];
            for (int i = 0; i < strs.Length; ++i)
            {
                buffer[i] = Convert.ToByte(strs[i], 16);
            }

            return buffer;
        }

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            //var headpack = new Gps.Plugin.Tcp.GT808.HeadPack();
            //byte[] packBytes = stringtobytes("7E-01-02-00-0E-00-20-47-05-52-32-00-00-32-30-31-32-30-31-31-39-30-38-31-30-34-37-0D-7E");
            //if (RawFormatter.Instance.Bytes2Struct(headpack, packBytes , 1, HeadPack.PackSize))
            //{
            //    Console.WriteLine("OK");
            //    Console.ReadKey();
            //}

            //MessageContext pack = new MessageContext();
            //byte[] packBytes = stringtobytes("68-68-25-00-00-00-00-00-00-00-12-34-56-00-18-10-DC-01-13-13-06-17-04-14-11-DC-0C-4A-7F-5C-1A-00-00-00-00-00-00-00-00-00-0A-0D");
            //if (RawFormatter.Instance.Bytes2Struct(pack, packBytes, BodyData.PackSize, MessageContext.PackSize))
            //{
            //    Console.WriteLine("OK");
            //    Console.ReadKey();
            //}

            sendLogPrint = Convert.ToBoolean(ConfigurationManager.AppSettings["sendLogPrint"]);
            waitReceive = Convert.ToBoolean(ConfigurationManager.AppSettings["waitReceive"]);
            receiveLogPrint = Convert.ToBoolean(ConfigurationManager.AppSettings["receiveLogPrint"]);

            string ip = "";
            int port = 0;
            while (port == 0 || string.IsNullOrEmpty(ip))
            {
                string line = "";
                string configValue = ConfigurationManager.AppSettings["remoteServerPort"];
                if (string.IsNullOrEmpty(configValue))
                {
                    Console.Write("请输入服务器IP和端口(113.31.92.200:8201):");
                    line = Console.ReadLine();
                }
                else
                {
                    line = configValue;
                }

                if (string.IsNullOrEmpty(line))
                    line = "113.31.92.200:8201";

                int pos = line.IndexOf(':');
                if (pos == -1)
                {
                    Console.WriteLine("正确格式：xx.xxx.xxx.xxx:xxxx");
                    continue;
                }

                ip = line.Substring(0, pos);
                string strPort = line.Substring(pos + 1);

                int.TryParse(strPort, out port);
            }

            int min = 0, max = 0;
            string deviceIdRangeValue = ConfigurationManager.AppSettings["deviceIdRange"];
            if (string.IsNullOrEmpty(deviceIdRangeValue))
            {
                Console.WriteLine("生成设备标识格式为:0000000000000000,0000000000000001....");
                Console.Write("依客户端网络情况输入要模拟的客户端数量(1-10000):");
                string strCount = Console.ReadLine();
                max = int.Parse(strCount);
            }
            else
            {
                string[] items = deviceIdRangeValue.Split('-');
                min = int.Parse(items[0]);
                max = int.Parse(items[1]);
            }

            int interval = 30;
            string strInterval = ConfigurationManager.AppSettings["interval"];
            if (string.IsNullOrEmpty(strInterval))
            {
                Console.Write("每个消息间隔（秒）：");
                strInterval = Console.ReadLine();
                interval = int.Parse(strInterval);
            }
            else
            {
                interval = int.Parse(strInterval);
            }

            int sendCountByOneDevice = 30;
            string strSendCountByOneDevice = ConfigurationManager.AppSettings["sendCountByOneDevice"];
            if (string.IsNullOrEmpty(strSendCountByOneDevice))
            {
                Console.Write("每个设备发送共多少个包后断开连接：");
                strSendCountByOneDevice = Console.ReadLine();
                sendCountByOneDevice = int.Parse(strSendCountByOneDevice);
            }
            else
            {
                sendCountByOneDevice = int.Parse(strSendCountByOneDevice);
            }

            ProcessCounter processCounter = new ProcessCounter((max - min + 1) * sendCountByOneDevice, pc =>
             {
                 Console.Title = pc.ToString();
             });

            for (int i = min; i <= max; ++i)
            {
                string deviceId = string.Concat("010000", i.ToString("00000"));

                Program p = new Program(ip, port, deviceId, interval, sendCountByOneDevice, processCounter);

                //p.Run();
                Thread iThred = new Thread(p.Run);
                iThred.Start();

                //Thread.SpinWait(2);
            }
            Console.ReadLine();
        }

        private void Run()
        {
            Console.WriteLine("Thread {0} Start {1}", Thread.CurrentThread.ManagedThreadId, DateTime.Now.ToString());

            Socket tcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                tcp.Connect(IPAddress.Parse(ip), port);

                try
                {
                    while (this.sendCountByOneDevice-- > 0)
                    {
                        if (SendPosition(tcp))
                        {
                            processCounter.IncrSuccess();
                        }
                        else
                        {
                            processCounter.IncrFailed();
                        }
                        Thread.Sleep(interval * 1000);
                        //Thread.Sleep(100);
                    }
                    /*
                    if (SendPosition(tcp))
                    {
                        processCounter.IncrSuccess();
                    }
                    else
                    {
                        processCounter.IncrFailed();
                    }

                    if (--this.sendCountByOneDevice <= 0)
                    {
                        //timer.Change(-1, -1);
                        //try
                        //{
                        //    tcp.Disconnect(true);
                        //    tcp.Dispose();
                        //}
                        //catch (Exception exp)
                        //{
                        //    log.Warn("断开连接出错:" + exp.Message);
                        //}
                    }
                    else
                    {
                        //SendPosition(tcp);
                        //timer.Change(Math.Max(1, interval * 1000 - watch.ElapsedMilliseconds), -1);
                    }
                    */
                }
                catch (SocketException exp)
                {
                    log.Debug("连接已经断开, 重连接中");
                    //timer.Change(Timeout.Infinite, Timeout.Infinite);
                    this.Run();
                }
                catch (Exception ex)
                {
                    log.Error(ex.Message, ex);
                    //timer.Change(Math.Max(1, interval * 1000 - watch.ElapsedMilliseconds), -1);
                }
            }
            catch (ThreadAbortException)
            {
                return;
            }
            catch (Exception exp)
            {
                log.Error(exp);

            }
            Console.WriteLine("Thread {0} End {1}", Thread.CurrentThread.ManagedThreadId, DateTime.Now.ToString());

        }

        ushort seqNum = 1;
        ushort GetNextSeqNum()
        {
            lock (this)
            {
                return ++seqNum;
            }
        }

        private byte[] bufferRecv = new byte[1024];
        private LatlonBuilder latlonBuilder;

        private bool SendPosition(Socket tcp)
        {
            HeadPack head = new HeadPack() { SeqNO = GetNextSeqNum(), MessageId = (ushort)MessageIds.PositionReport, BodyProp = (ushort)0 };
            head.SetDeviceId(this.deviceId);

            double lat;
            double lon;
            int speed = 10 + r.Next(90);
            latlonBuilder.GetNextLatlon(speed, out lat, out lon);

            PositionReportPack pack = new PositionReportPack()
            {
                AlermFlags = 0,
                Speed = (ushort)(speed * 10),
                State = 0,
                Latitude = Convert.ToUInt32(lat * 1000000),
                Longitude = Convert.ToUInt32(lon * 1000000),
                Altitude = 200,
                Direction = 0,
                Time = DateTime.Now.ToString("yyMMddHHmmss")
            };

            byte[] bytesSend = RawFormatter.Instance.Struct2Bytes(pack);

            BodyPropertyHelper.SetMessageLength(ref head.BodyProp, (ushort)bytesSend.Length);

            byte[] headBytes = RawFormatter.Instance.Struct2Bytes(head);
            byte[] fullBytes = headBytes.Concat(bytesSend).ToArray();
            byte checkByte = PackHelper.CalcCheckByte(fullBytes, 0, fullBytes.Length);

            bytesSend = (new byte[] { 0x7e }
            .Concat(PackHelper.EncodeBytes(fullBytes.Concat(new byte[] { checkByte })))
            .Concat(new byte[] { 0x7e })).ToArray();

            //Console.WriteLine("{0} {1}",head.SeqNO, bytesSend.ToHexString());

            //发送消息
            SendBytes(tcp, bytesSend);
            //控制台打印日志cpu占用太高
            if (sendLogPrint)
            {
                Console.WriteLine("{0} {1}, LatLon:{2:0.000000},{3:0.000000}", head.GetDeviceId(), DateTime.Now.ToString(), lat, lon);
            }
            //等待接收服务端返回值
            var success = true;
            if (waitReceive)
            {
                success = RecvBytes(tcp);
                //var success = true;
            }
            return success;
        }
        void SendBytes(Socket tcp, byte[] bytes)
        {
            lock (System.Reflection.MethodBase.GetCurrentMethod())
            {
                if (bytes.Length != tcp.Send(bytes))
                {
                    throw new SocketException((int)SocketError.ConnectionReset);
                }
            }
        }

        private bool RecvBytes(Socket tcp)
        {
            lock (bufferRecv)
            {
                byte[] buffer = bufferRecv;

                int received = tcp.Receive(buffer);
                //received = tcp.ReceiveAsync.Receive(buffer);

                byte[] bytesReceived = PackHelper.DecodeBytes(buffer, 1, received - 2);
                int rightSize = HeadPack.PackSize + ServerAnswerPack.PackSize + 1;
                if (bytesReceived.Length != rightSize)
                {
                    log.WarnFormat("返回消息长度不正确:" + bytesReceived.Length + "!=" + rightSize);
                }

                ServerAnswerPack pack = new ServerAnswerPack();
                RawFormatter.Instance.Bytes2Struct(pack, bytesReceived, HeadPack.PackSize, ServerAnswerPack.PackSize);

                if (pack.Result != 0)
                {
                    log.WarnFormat("发送失败:" + pack.Result.ToString());
                }
                if(waitReceive && receiveLogPrint)
                {
                    Console.WriteLine("SeqNO:{0} MessageId:{1} Result:{2}", pack.SeqNO,pack.MessageId,pack.Result);
                }
                return pack.Result == 0;
            }
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            log.Error(e.ExceptionObject);
        }
    }

    class LatlonBuilder
    {
        private double lat;
        private double lon;
        private double minLat;
        private double maxLat;
        private double minLon;
        private double maxLon;
        private int direction = 0;
        private Random r = new Random();
        public LatlonBuilder(double lat, double lon, double minLat, double maxlat, double minLon, double maxLon)
        {
            this.lat = lat;
            this.lon = lon;
            this.minLat = minLat;
            this.maxLat = maxlat;
            this.minLon = minLon;
            this.maxLon = maxLon;

            this.direction = r.Next(360);
        }

        public bool GetNextLatlon(int speed, out double lat, out double lon)
        {
            direction = (direction + (r.Next(30) - 15)) % 360;
            double angle = Math.PI * this.direction / 180.0;
            double latAdd = speed / 1000.0 * Math.Sin(angle);
            double lonAdd = speed / 1000.0 * Math.Cos(angle);

            this.lat = lat = this.lat + latAdd;
            this.lon = lon = this.lon + lonAdd;

            if (lat < minLat || lat > maxLat || lon < minLon || lon > maxLon)
            {
                direction = (direction + 180) % 360;
                return GetNextLatlon(speed, out lat, out lon);
            }


            return true;
        }
    }

    class ProcessCounter
    {
        private long current = 0, failedCounter = 0;
        private Thread thread;
        private AutoResetEvent evtUpdateUI = new AutoResetEvent(false);
        private long updatePerCount;
        private long lastUpdateVal;
        public ProcessCounter(long max, Action<ProcessCounter> updateUI)
        {
            this.Max = max;
            this.updatePerCount = max / 100;
            this.thread = new Thread(new ThreadStart(() =>
                {
                    while (true)
                    {
                        try
                        {
                            evtUpdateUI.WaitOne(500);
                            this.lastUpdateVal = Interlocked.Read(ref current);
                            updateUI(this);
                        }
                        catch (ThreadAbortException)
                        {
                            return;
                        }
                        catch (Exception exp)
                        {
                            Console.WriteLine(exp.Message);
                        }
                    }
                }));
            this.thread.IsBackground = true;
            this.thread.Start();

        }

        public long Max { get; set; }


        internal void IncrSuccess()
        {
            lock (this)
                Incr();
        }
        internal void IncrFailed()
        {
            lock (this)
            {
                Incr();
                Interlocked.Increment(ref failedCounter);
            }
        }

        private void Incr()
        {
            var val = Interlocked.Increment(ref current);
            if (val - lastUpdateVal > this.updatePerCount)
            {
                this.evtUpdateUI.Set();
            }

            if (val == Max)
            {
                Console.WriteLine("Done");
            }
        }

        public override string ToString()
        {
            long curr = Interlocked.Read(ref current);
            float g = Convert.ToSingle(curr / (Max / 100.0));
            string ret = string.Concat(curr, "/", Max, "=>", g.ToString("##.00"), "%");

            long failed = Interlocked.Read(ref failedCounter);
            if (failed > 0)
                ret = string.Concat(ret, "(Success:" + (curr - failed), ", Failed:", failed, ")");

            return ret;
        }


    }
}

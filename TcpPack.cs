using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics.Contracts;
using System.Net;
using Gps.Plugin.Common.Helpers;
using System.IO;
using System.Collections;

namespace Gps.Plugin.Tcp.GT808
{
    public class PackHelper
    {
        public static byte[] ToBytes(string deviceId, MessageIds messageId, ushort seqNO, byte[] bodyBytesReturn)
        {

            HeadPack head = new HeadPack()
            {
                BodyProp = 0,
                MessageId = (ushort)messageId,
                SeqNO = seqNO
            };

            head.SetDeviceId(deviceId);
            //BodyPropertyHelper.SetMessageLength(ref head.BodyProp,(ushort)bodyBytesReturn.Length);

            BodyPropertyHelper.SetMessageLength(ref head.BodyProp, (ushort)bodyBytesReturn.Length);


            return RawFormatter.Instance.Struct2Bytes(head).Concat(bodyBytesReturn).ToArray();

        }

        public static byte[] DecodeBytes(byte[] buffer)
        {
            return DecodeBytes(buffer, 0, buffer.Length);
        }

        public static byte[] DecodeBytes(byte[] bytes, int offset, int length)
        {
            int index = 0;
            int endOffset = Math.Min(length, bytes.Length - offset) + offset;
            for (int i = offset; i < endOffset; )
            {
                if (bytes[i] == 0x7d && bytes[i + 1] == 0x02)
                {
                    bytes[index++] = 0x7e;
                    i += 2;
                }
                else if (bytes[i] == 0x7d && bytes[i + 1] == 0x01)
                {
                    bytes[index++] = 0x7d;
                    i += 2;
                }
                else
                {
                    bytes[index++] = bytes[i++];
                }
            }

            return bytes.Take(index).ToArray();
        }


        public static byte[] EncodeBytes(IEnumerable<byte> bytes)
        {
            MemoryStream ms = new MemoryStream();
            foreach (var b in bytes)
            {
                if (b == 0x7e)
                {
                    ms.WriteByte(0x7d);
                    ms.WriteByte(0x02);
                }
                else if (b == 0x7d)
                {
                    ms.WriteByte(0x7d);
                    ms.WriteByte(0x01);
                }
                else
                {
                    ms.WriteByte(b);
                }

            }

            return ms.ToArray();
        }

        /// <summary>
        /// 计算校验位
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static byte CalcCheckByte(byte[] bytes, int offset, int count)
        {
            if (count == 0)
                return 0;

            byte ret = bytes[offset];
            for (int i = offset + 1; i < offset + count; ++i)
            {
                ret ^= bytes[i];
            }

            return ret;
        }

        /// <summary>
        /// 计算校验位
        /// </summary>
        /// <param name="bytes"></param>
        public static void CalcCheckByte(byte[] bytes)
        {
            if (bytes.Length == 0)
                return;

            bytes[bytes.Length - 1] = CalcCheckByte(bytes, 0, bytes.Length - 1);
        }

        
    }


    /*
   车载GPS定位器通信协议（GT808）(TCP_6004).pdf 
   P2
     * 
     * 7E-01-02-00-06-00-20-47-05-52-32-00-00-30-31-32-33-34-35-06-7E
    */

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public class HeadPack
    {
        public static byte FixPrefix = 0x7e;




        public static Int32 PackSize = Marshal.SizeOf(typeof(HeadPack));

        /// <summary>
        /// 消息 ID
        /// </summary>
        public BigEndianUInt16 MessageId;

        /// <summary>
        /// 消息体属性
        /// </summary>
        public BigEndianUInt16 BodyProp;

        /// <summary>
        /// 终端手机号
        /// </summary>
        protected BCD8421_6BytesString DeviceId;


        /// <summary>
        /// 自增一序列号
        /// </summary>
        public BigEndianUInt16 SeqNO;


        public string GetDeviceId()
        {
            //return string.Concat("460", DeviceId);
            return DeviceId;
        }

        public void SetDeviceId(string value)
        {
            if (value.Length > 12)
            {
                DeviceId = value.Substring(value.Length - 12, 12);
            }
            else
            {
                DeviceId = value.PadLeft(12, '0');
            }
        }

    }


    public static class BodyPropertyHelper
    {
        /// <summary>
        /// 9-0 BIT
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static Int16 GetMessageLength(BigEndianUInt16 val)
        {
            return (Int16)((UInt16)val & 0x03ff);
        }

        /// <summary>
        /// 9-0 BIT
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static void SetMessageLength(ref BigEndianUInt16 val, UInt16 length)
        {
            val = (UInt16)(((UInt16)val & (~0x03ff)) + length);
        }

        /// <summary>
        /// 12-10 BIT
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static Byte GetEncodeType(BigEndianUInt16 val)
        {
            return (Byte)((val >> 10) & 0x03);
        }


        public static void SetEncodeType(ref BigEndianUInt16 val, byte encType)
        {
            ushort v = val;

            v = (ushort)(v & ~(0x03 << 10) + encType << 10);

            val = v;
        }

        /// <summary>
        /// 13 BIT
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool IsMutliPack(this BigEndianUInt16 val)
        {
            return ((val >> 13) & 0x01) == 0x0001;
        }
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public class PackProperty
    {
        public static Int32 PackSize = Marshal.SizeOf(typeof(PackProperty));

        public BigEndianUInt16 Total;
        public BigEndianUInt16 Index;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public class ServerAnswerPack
    {
        public static Int32 PackSize = Marshal.SizeOf(typeof(ServerAnswerPack));


        /// <summary>
        /// 流水
        /// </summary>
        public BigEndianUInt16 SeqNO;

        /// <summary>
        /// 消息ID
        /// </summary>
        public BigEndianUInt16 MessageId;


        /// <summary>
        /// 结果 0:成功/确认  1:失败   2:消息有误  3:不支持  4:报警处理确认
        /// </summary>
        public byte Result;
    }

    /// <summary>
    /// 终端通用应答
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public class ClientAnswerPack
    {
        public static Int32 PackSize = Marshal.SizeOf(typeof(ServerAnswerPack));


        /// <summary>
        /// 流水
        /// </summary>
        public BigEndianUInt16 SeqNO;

        /// <summary>
        /// 消息ID
        /// </summary>
        public BigEndianUInt16 MessageId;


        /// <summary>
        /// 结果 0:成功/确认  1:失败   2:消息有误  3:不支持  
        /// </summary>
        public byte Result;
    }

    /// <summary>
    /// 终端注册 最后是GBK字串,车牌号
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public class ClientRegistPack
    {
        public static Int32 PackSize = Marshal.SizeOf(typeof(ClientRegistPack));

        /// <summary>
        /// 省
        /// </summary>
        public BigEndianUInt16 Province;

        /// <summary>
        /// 市
        /// </summary>
        public BigEndianUInt16 City;

        /// <summary>
        /// 制造商
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public byte[] Manufacturer;

        /// <summary>
        /// 终端型号
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] DeviceModel;


        /// <summary>
        /// 终端ID
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public byte[] DeviceId;


        /// <summary>
        /// 车牌颜色
        /// </summary>
        public byte CarColor;


        //最后是GBK字串,车牌号


    }


    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public class ClientRegistReturnPack
    {
        /// <summary>
        /// 自增一序列号
        /// </summary>
        public BigEndianUInt16 SeqNum;

        /// <summary>
        /// 0成功, 1车辆已经注册, 2数据库中无此车辆, 3终端已经被注册, 4数据库中无该终端
        /// </summary>
        public Byte Result;
    }

    /// <summary>
    /// 位置包汇报
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public class PositionReportPack
    {
        public static Int32 PackSize = Marshal.SizeOf(typeof(PositionReportPack));

        /// <summary>
        /// 报警标志
        /// </summary>
        public BigEndianUInt32 AlermFlags;


        /// <summary>
        /// 状态
        /// </summary>
        public BigEndianUInt32 State;



        /// <summary>
        /// 纬度 度*10^6
        /// </summary>
        public BigEndianUInt32 Latitude;

        /// <summary>
        /// 经度 度*10^6
        /// </summary>
        public BigEndianUInt32 Longitude;

        /// <summary>
        /// 高程 单位为米
        /// </summary>
        public BigEndianUInt16 Altitude;

        /// <summary>
        /// 速度  1/10KM/H
        /// </summary>
        public BigEndianUInt16 Speed;


        /// <summary>
        /// 方向 0~359 正北为0 顺时针
        /// </summary>
        public BigEndianUInt16 Direction;

        /// <summary>
        /// 时间
        /// </summary>
        public BCD8421_6BytesString Time;



    }

    /// <summary>
    /// 立即拍照命令
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public class CmdPhotographePack
    {
        /// <summary>
        /// 通道ID  > 0
        /// </summary>
        public Byte Channel;

        /// <summary>
        /// 拍摄命令 0表示停止拍摄；0xffff表示录像；其它表示拍摄张数
        /// </summary>
        public BigEndianUInt16 Cmd;

        /// <summary>
        /// 拍照间隔，录像时间。 单位为秒，0表示最小间隔拍照或者一直录像
        /// </summary>
        public BigEndianUInt16 Interval;

        /// <summary>
        /// 保存标志 1保存  0立即上传
        /// </summary>
        public byte Deal;

        /// <summary>
        /// 图像分辨率 0x01:320*240  0x02:640*480  0x03:800*600  0x04:1024*768  
        /// 0x05:176*144(Qcif) 0x06:352*288(Cif)  0x07:704*288(HALF D1)  0x08:704*546(D1) 
        /// </summary>
        public byte Resolution;

        /// <summary>
        /// 图像质量 1 ~ 10  1表示质量损失最小， 10表示压缩最大
        /// </summary>
        public byte Quality;

        /// <summary>
        /// 亮度 0 ~ 255
        /// </summary>
        public byte Brightness;

        /// <summary>
        /// 对比度   0 ~ 127 
        /// </summary>
        public byte Contrast;

        /// <summary>
        /// 饱和度 0 ~ 127 
        /// </summary>
        public byte Saturation;


        /// <summary>
        /// 色度 0 ~ 255
        /// </summary>
        public byte Chroma;



    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public class CmdSendTextPack
    {
        public byte Flag;


        /// <summary>
        /// 紧急
        /// </summary>
        public bool IsUrgent
        {
            get { return (Flag & 0x01) == 0x01; }
            set { Flag = (byte)(Flag | 0x01); }
        }

        /// <summary>
        /// 终端显示器显示
        /// </summary>
        public bool IsDisplay
        {
            get { return (Flag & 0x04) == 0x04; }
            set { Flag = (byte)(Flag | 0x04); }
        }
        /// <summary>
        /// 终端显示器显示
        /// </summary>
        public bool IsTTS
        {
            get { return (Flag & 0x08) == 0x08; }
            set { Flag = (byte)(Flag | 0x08); }
        }

        /// <summary>
        /// 终端显示器显示
        /// </summary>
        public bool IsAD
        {
            get { return (Flag & 0x10) == 0x10; }
            set { Flag = (byte)(Flag | 0x10); }
        }

        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0)]
        //public byte[] MessageBytes;
    }


    /// <summary>
    /// 多媒体数据上传
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public class GpsMultimediaUploadPack
    {
        public static Int32 PackSize = Marshal.SizeOf(typeof(GpsMultimediaUploadPack));

        /// <summary>
        /// 多媒体ID >0
        /// </summary>
        public BigEndianUInt32 ID;

        /// <summary>
        /// 多媒体类型  0：图像  1：音频   2：视频
        /// </summary>
        public Byte Type;

        /// <summary>
        /// 多媒体格式编码 0：JPEG   1：TIF   2：MP3  3：WAV  4：WMV
        /// </summary>
        public byte Format;

        /// <summary>
        /// 事件项编码 0：平台下发指令  1：定时动作  2：抢劫报警触发  3：碰撞侧翻触发报警  4：保留
        /// </summary>
        public byte EventCode;

        /// <summary>
        /// 通道ID  > 0
        /// </summary>
        public Byte Channel;



    }

    /// <summary>
    /// 多媒体数据上传
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public class GpsMultimediaReUploadPack
    {
        public static Int32 PackSize = Marshal.SizeOf(typeof(GpsMultimediaReUploadPack));

        /// <summary>
        /// 多媒体ID >0
        /// </summary>
        public BigEndianUInt32 ID;

        /// <summary>
        /// 多媒体类型  0：图像  1：音频   2：视频
        /// </summary>
        public Byte Count;

        ///// <summary>
        ///// 要求重发的下一个包
        ///// </summary>
        //public Byte NextIndex;



    }


    public class BitValueState
    {
        private BitArray bitArray;
        public BitValueState(ushort state)
        {
            this.bitArray = new BitArray(BitConverter.GetBytes(state));
        }

        /// <summary>
        /// ACC 开关
        /// </summary>
        bool ACC
        {
            get
            {
                return bitArray[0];
            }
            set
            {
                bitArray[0] = value;
            }
        }

        /// <summary>
        /// 是否定位
        /// </summary>
        //Position = 0x0002,
        bool Position
        {
            get
            {
                return bitArray[1];
            }
            set
            {
                bitArray[1] = value;
            }
        }

        /// <summary>
        /// 0北纬  1南纬
        /// </summary>
        //NorthOrSouth = 0x0004,
        bool IsSouth
        {
            get
            {
                return bitArray[2];
            }
            set
            {
                bitArray[2] = value;
            }
        }
        /// <summary>
        /// 0东经   1西经
        /// </summary>
        //EastOrWest = 0x0008,
        bool IsWest
        {
            get
            {
                return bitArray[3];
            }
            set
            {
                bitArray[3] = value;
            }
        }
        /// <summary>
        /// 0运营状态  1停运
        /// </summary>
        //OperationState = 0x0010,
        bool OperationStoped
        {
            get
            {
                return bitArray[4];
            }
            set
            {
                bitArray[4] = value;
            }
        }
        /// <summary>
        /// 经纬度是否加密
        /// </summary>
        //EncodeLongitudeLatitude = 0x0020,
        bool EncodeLongitudeLatitude
        {
            get
            {
                return bitArray[5];
            }
            set
            {
                bitArray[5] = value;
            }
        }
        /// <summary>
        /// 0车辆油路正常  1断开
        /// </summary>
        //CarOilchannel = 0x0400,
        bool CarOilchannelBreaked
        {
            get
            {
                return bitArray[10];
            }
            set
            {
                bitArray[10] = value;
            }
        }
        /// <summary>
        /// 0车辆电子正常  1断开
        /// </summary>
        //CarCircuit = 0x0800,
        bool CarCircuitBreaked
        {
            get
            {
                return bitArray[11];
            }
            set
            {
                bitArray[11] = value;
            }
        }
        /// <summary>
        /// 0车辆车门解锁  1加锁
        /// </summary>
        //CarDoorLock = 0x1000
        bool CarDoorLocked
        {
            get
            {
                return bitArray[12];
            }
            set
            {
                bitArray[12] = value;
            }
        }
    }

    public class BitValueAlerm
    {
        //private  uint data;
        private BitArray bitArray;
        public BitValueAlerm(UInt32 alerm)
        {
            //this.data = alerm;
            bitArray = new BitArray(new int[] { (int)alerm });
        }

        /// <summary>
        /// 紧急报警, 触动报警开关后触发 收到答应后清除
        /// </summary>
        bool Urgent
        {
            get
            {
                return bitArray[0];
            }
            set
            {
                bitArray[0] = value;
            }

        }

        /// <summary>
        /// 超速报警 标志维持到报警条件解除
        /// </summary>
        bool Speeding
        {
            get
            {
                return bitArray[1];
            }
            set
            {
                bitArray[1] = value;
            }

        }


        /// <summary>
        /// 疲劳驾驶 标志维持到报警条件解除
        /// </summary>
        bool Fatigue
        {
            get
            {
                return bitArray[2];
            }
            set
            {
                bitArray[2] = value;
            }

        }

        /// <summary>
        /// 预警 收到答应后清除
        /// </summary>
        bool Forewarning
        {
            get
            {
                return bitArray[3];
            }
            set
            {
                bitArray[3] = value;
            }

        }

        //TODO:未完, 以后用到添加

    }




    public enum MessageIds : ushort
    {

        /// <summary>
        /// 终端通用应答
        /// </summary>
        ClientAnswer = 0x0001,



        /// <summary>
        /// 终端心跳
        /// </summary>
        ClientPump = 0x0002,

        /// <summary>
        /// 终端注销
        /// </summary>
        ClientUnregist = 0x0003,


        /// <summary>
        /// 终端注册
        /// </summary>
        ClientRegist = 0x0100,


        /// <summary>
        /// 终端鉴权
        /// </summary>
        ClientAuth = 0x0102,

        /// <summary>
        /// 位置信息汇报
        /// </summary>
        PositionReport = 0x0200,

        /// <summary>
        /// 多媒体事件信息上传
        /// </summary>
        MultimediaEventUpload = 0x0800,

        /// <summary>
        /// 多媒体数据上传
        /// </summary>
        MultimediaUpload = 0x0801,

        /// <summary>
        /// 多媒体数据上传
        /// </summary>
        MultimediaUploadAnswer = 0x8800,

        /// <summary>
        /// 平台通用应答
        /// </summary>
        ServerAnswer = 0x8001,


        /// <summary>
        /// 终端注册应答
        /// </summary>
        ClientRegistReturn = 0x8100,

        /// <summary>
        /// 立即拍照
        /// </summary>
        Photographe = 0x8801,

        /// <summary>
        /// 下发文本
        /// </summary>
        SendTextMessage = 0x8300



    }





}

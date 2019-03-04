using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace EZ_Robot_Unity_DLL {

  public class ServoServerClient : DisposableBase {

    class ThreadStartParams {

      public int CNT;
      public string IPAddress;
      public int Port;
      public ThreadStartParams(int cnt, string ipAddress, int port) {

        CNT = cnt;
        IPAddress = ipAddress;
        Port = port;
      }
    }

    [Flags]
    public enum ServoPortEnum {
      D0 = 0,
      D1 = 1,
      D2 = 2,
      D3 = 3,
      D4 = 4,
      D5 = 5,
      D6 = 6,
      D7 = 7,
      D8 = 8,
      D9 = 9,
      D10 = 10,
      D11 = 11,
      D12 = 12,
      D13 = 13,
      D14 = 14,
      D15 = 15,
      D16 = 16,
      D17 = 17,
      D18 = 18,
      D19 = 19,
      D20 = 20,
      D21 = 21,
      D22 = 22,
      D23 = 23,
      NA = 24,
      V0 = 25,
      V1 = 26,
      V2 = 27,
      V3 = 28,
      V4 = 29,
      V5 = 30,
      V6 = 31,
      V7 = 32,
      V8 = 33,
      V9 = 34,
      V10 = 35,
      V11 = 36,
      V12 = 37,
      V13 = 38,
      V14 = 39,
      V15 = 40,
      V16 = 41,
      V17 = 42,
      V18 = 43,
      V19 = 44,
      V20 = 45,
      V21 = 46,
      V22 = 47,
      V23 = 48,
      V24 = 49,
      V25 = 50,
      V26 = 51,
      V27 = 52,
      V28 = 53,
      V29 = 54,
      V30 = 55,
      V31 = 56,
      V32 = 57,
      V33 = 58,
      V34 = 59,
      V35 = 60,
      V36 = 61,
      V37 = 62,
      V38 = 63,
      V39 = 64,
      V40 = 65,
      V41 = 66,
      V42 = 67,
      V43 = 68,
      V44 = 69,
      V45 = 70,
      V46 = 71,
      V47 = 72,
      V48 = 73,
      V49 = 74,
      V50 = 75,
      V51 = 76,
      V52 = 77,
      V53 = 78,
      V54 = 79,
      V55 = 80,
      V56 = 81,
      V57 = 82,
      V58 = 83,
      V59 = 84,
      V60 = 85,
      V61 = 86,
      V62 = 87,
      V63 = 88,
      V64 = 89,
      V65 = 90,
      V66 = 91,
      V67 = 92,
      V68 = 93,
      V69 = 94,
      V70 = 95,
      V71 = 96,
      V72 = 97,
      V73 = 98,
      V74 = 99,
      V75 = 100,
      V76 = 101,
      V77 = 102,
      V78 = 103,
      V79 = 104,
      V80 = 105,
      V81 = 106,
      V82 = 107,
      V83 = 108,
      V84 = 109,
      V85 = 110,
      V86 = 111,
      V87 = 112,
      V88 = 113,
      V89 = 114,
      V90 = 115,
      V91 = 116,
      V92 = 117,
      V93 = 118,
      V94 = 119,
      V95 = 120,
      V96 = 121,
      V97 = 122,
      V98 = 123,
      V99 = 124
    }

    /// <summary>
    /// Event risen for log data
    /// </summary>
    public event OnLogHandler OnLog;
    public delegate void OnLogHandler(DateTime time, string logTxt);

    /// <summary>
    /// Event raised when the image is ready.
    /// </summary>
    public event OnImageDataReadyHandler OnImageDataReady;
    public delegate void OnImageDataReadyHandler(byte[] imageData);

    /// <summary>
    /// Event raised when the JPEGStream has started
    /// </summary>
    public event OnStartHandler OnStart;
    public delegate void OnStartHandler();

    /// <summary>
    /// Event raised when the JPEGStream has stopped
    /// </summary>
    public event OnStopHandler OnStop;
    public delegate void OnStopHandler();

    int                   _imageSize       = 0;
    volatile int          _cnt             = 0;
    bool                  _isRunning       = false;
    EZTaskScheduler       _ts              = null;
    byte[]                _positions       = new byte[125];
    bool                  _sendUpdate      = false;
    bool                  _servosChanged   = false;

    readonly byte [] TAG_EZIMAGE = new byte[] { (byte)'E', (byte)'Z', (byte)'I', (byte)'M', (byte)'G' };

    readonly int BUFFER_SIZE = 1048576;

    /// <summary>
    /// Returns the size of the last camera image
    /// </summary>
    public int GetImageSize {
      get {
        return _imageSize;
      }
    }

    /// <summary>
    /// Returns the status of the camera streaming
    /// </summary>
    public bool IsRunning {
      get {
        return (_isRunning);
      }
    }

    public ServoServerClient() {

      _ts = new EZTaskScheduler("Servo Server");
      _ts.OnEventToRun += imageThreadWorker;
    }

    /// <summary>
    /// Connect and begin receiving the camera stream
    /// </summary>
    public void Start(string ipAddress, int port) {

      stopWorker();

      _cnt++;

      _ts.StartNew(new ThreadStartParams(_cnt, ipAddress, port));
    }

    /// <summary>
    /// Dispose of this object
    /// </summary>
    protected override void DisposeOverride() {

      _ts.Dispose();

      _ts = null;

      stopWorker();
    }

    private void stopWorker() {

      _cnt++;
    }

    /// <summary>
    /// Has any of the servo positions been changed?
    /// </summary>
    public bool HasServoChanged {
      get {
        return _servosChanged;
      }
    }

    /// <summary>
    /// Stop the camera from streaming and receiving frames
    /// </summary>
    public void Stop() {

      stopWorker();
    }

    /// <summary>
    /// Map the value within the inputMin/inputMax range to the outputMin/outputMax range.
    /// </summary>
    public float Map(float value, float inputMin, float inputMax, float outputMin, float outputMax) {

      value = System.Math.Min(value, inputMax);
      value = System.Math.Max(value, inputMin);

      return (value - inputMin) / (inputMax - inputMin) * (outputMax - outputMin) + outputMin;
    }

    /// <summary>
    /// specify a float between -1 and +1. This returns the position in degrees between 1-180 for servos
    /// </summary>
    public byte MapToByte(float val) {

      return (byte)Map(val, -1, 1, 1, 180);
    
        }

    /// <summary>
    /// Set the position of a servo but do not send it to the server.
    /// This merely caches the position, which you can send the cache later with SendCachedServoPositions()
    /// By caching, it allows you to effeciently use the communication by sending only servo updates.
    /// You would want to do this when there's a bunch of servo positions to set, and then send later.
    /// For example, in Unity you can send servo positions with every frame Update rather than send the updates separately.
    /// </summary>
    public void SetCachedServoPosition(ServoPortEnum servo, byte position) {

      _positions[(int)servo] = position;

      _servosChanged = true;
    }

    /// <summary>
    /// If you build a cache of servo positions with SetCachedServoPosition(), call this to send the cache of positions with the next camera frame.
    /// </summary>
    public void SendCachedServoPositions() {

      if (!_servosChanged)
        return;

      _servosChanged = false;

      _sendUpdate = true;
    }

    /// <summary>
    /// Set the position of a servo and send the cache to the server.
    /// If you are sending a bunch of servo position with a scheduled servo update, consider using SetCachedServoPosition() by caching, and calling SendCachedServoPositions() after.
    /// </summary>
    public void SetServoPositionAndSend(ServoPortEnum servo, byte position) {

      _positions[(int)servo] = position;

      _servosChanged = false;

      _sendUpdate = true;
    }

    void imageThreadWorker(int taskId, object o) {

      ThreadStartParams threadStartParams = (ThreadStartParams)o;

      if (_cnt != threadStartParams.CNT)
        return;

      List<byte> bufferImage = new List<byte>();
      byte[] bufferTmp = new byte[BUFFER_SIZE];

      try {

        _isRunning = true;

        using (var tcpClient = new TcpClient()) {

          IAsyncResult ar = tcpClient.BeginConnect(threadStartParams.IPAddress, threadStartParams.Port, null, null);

          if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3), false))
            throw new TimeoutException();

          tcpClient.EndConnect(ar);

          tcpClient.ReceiveBufferSize = BUFFER_SIZE;
          tcpClient.ReceiveTimeout = 5000;
          tcpClient.SendTimeout = 3000;

          if (_cnt != threadStartParams.CNT)
            return;

          if (OnStart != null)
            OnStart();

          using (NetworkStream ns = tcpClient.GetStream())
            while (_cnt == threadStartParams.CNT && tcpClient.Connected) {

              if (_sendUpdate) {

                _sendUpdate = false;

                ns.Write(_positions, 0, _positions.Length);
              }

              int read = ns.Read(bufferTmp, 0, BUFFER_SIZE);

              if (read == 0)
                throw new Exception("Client disconnected");

              bufferImage.AddRange(bufferTmp.Take(read));

              LOOP_AGAIN:
              int foundStart = -1;

              if (bufferImage.Count < TAG_EZIMAGE.Length)
                continue;

              for (int p = 0; p < bufferImage.Count - TAG_EZIMAGE.Length; p++)
                if (bufferImage[p] == TAG_EZIMAGE[0] &&
                  bufferImage[p + 1] == TAG_EZIMAGE[1] &&
                  bufferImage[p + 2] == TAG_EZIMAGE[2] &&
                  bufferImage[p + 3] == TAG_EZIMAGE[3] &&
                  bufferImage[p + 4] == TAG_EZIMAGE[4]) {

                  foundStart = p;

                  break;
                }

              if (foundStart == -1)
                continue;

              if (foundStart > 0)
                bufferImage.RemoveRange(0, foundStart);

              if (bufferImage.Count < TAG_EZIMAGE.Length + sizeof(UInt32))
                continue;

              int imageSize = (int)BitConverter.ToUInt32(bufferImage.GetRange(TAG_EZIMAGE.Length, sizeof(UInt32)).ToArray(), 0);

              if (bufferImage.Count <= imageSize + TAG_EZIMAGE.Length + sizeof(UInt32))
                continue;

              bufferImage.RemoveRange(0, TAG_EZIMAGE.Length + sizeof(UInt32));

              _imageSize = imageSize;

              try {

                if (OnImageDataReady != null)
                  OnImageDataReady(bufferImage.GetRange(0, imageSize).ToArray());
              } catch (Exception ex) {

                if (OnLog != null)
                  OnLog(DateTime.Now, string.Format("ezbv4 camera image render error: {0}", ex));
              }

              bufferImage.RemoveRange(0, imageSize);

              // If there's at least 5kb of data in the buffer, loop again and see if there's another image in the buffer
              // Without doing this, the there's a chance the buffer will fill with future images and we'll never catch up because image data is only processed when an image is available
              if (bufferImage.Count > 5000)
                goto LOOP_AGAIN;
            }
        }
      } catch (Exception ex) {

        if (OnLog != null)
          OnLog(DateTime.Now, string.Format("EZ-B v4 Camera Error: {0}", ex));
      } finally {

        _isRunning = false;

        if (OnStop != null)
          OnStop();
      }
    }
  }
}

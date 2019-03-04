using EZ_Robot_Unity_DLL;
using BioIK;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class CameraCubeRotate : MonoBehaviour {

    ServoServerClient _streamClient;
    bool _initialized = false;
    public float _speed = 0.5f;
    Texture2D _texture;
    float positionAbs;
    public GameObject D0;
    public GameObject D1;
    public GameObject D2;


    volatile byte [] _toDisplay = new byte[]{ };

        void Start() {

        _texture = new Texture2D(640, 480, TextureFormat.RGB24, false);

        _streamClient = new ServoServerClient();
        _streamClient.OnImageDataReady += _streamClient_OnImageDataReady;
        _streamClient.Start("127.0.0.1", 8282);
    }

      private void _streamClient_OnImageDataReady(byte[] imageData) {

        if (!_initialized)
          return;

        _toDisplay = imageData;
      }

      void OnDisable() {

        _streamClient.Stop();
      }

      void Update() {

        _initialized = true;

        if (Input.GetKey(KeyCode.Escape))
          Application.Quit();
    
        if (Input.GetKey(KeyCode.RightArrow)) {

          transform.Rotate(new Vector3(0, -_speed, 0));

          _streamClient.SetCachedServoPosition(ServoServerClient.ServoPortEnum.D1, _streamClient.MapToByte(transform.rotation.x));
        }

        if (Input.GetKey(KeyCode.LeftArrow)) {

          transform.Rotate(new Vector3(0, _speed, 0));

          _streamClient.SetCachedServoPosition(ServoServerClient.ServoPortEnum.D1, _streamClient.MapToByte(transform.rotation.x));
        }

        if (Input.GetKey(KeyCode.DownArrow)) {

          transform.Rotate(new Vector3(_speed, 0, 0));

          _streamClient.SetCachedServoPosition(ServoServerClient.ServoPortEnum.D0, _streamClient.MapToByte(transform.rotation.y));
        }

        if (Input.GetKey(KeyCode.UpArrow)) {

          transform.Rotate(new Vector3(-_speed, 0, 0));

          _streamClient.SetCachedServoPosition(ServoServerClient.ServoPortEnum.D0, _streamClient.MapToByte(transform.rotation.y));
        }

        // This will get the Rotations of the attached joints and will send them to the EZ-B ports D0, D1, D2

        // Extract the Z rotation of this joint
        BioJoint joint_D0 = D0.GetComponent<BioJoint>();
        double value_D0 = joint_D0.Z.GetTargetValue();
        int position_D0= Mathf.RoundToInt((float)value_D0);
        int positionAbs_D0 = Mathf.Abs(position_D0 - 90);
        //print(positionAbs_D0);

        // Extract the Y rotation of this joint
        BioJoint joint_D1 = D1.GetComponent<BioJoint>();
        double value_D1 = joint_D1.Y.GetTargetValue();
        int position_D1 = Mathf.RoundToInt((float)value_D1);
        int positionAbs_D1 = Mathf.Abs(180 - (position_D1 - 90));
        //print(positionAbs_D1);

        // Extract the Y rotation of this joint
        BioJoint joint_D2 = D2.GetComponent<BioJoint>();
        double value_D2 = joint_D2.Y.GetTargetValue();
        int position_D2 = Mathf.RoundToInt((float)value_D2);
        int positionAbs_D2 = Mathf.Abs(position_D2 - 90);
        //print(positionAbs_D2);

      
        _streamClient.SetCachedServoPosition(ServoServerClient.ServoPortEnum.D0, (byte)positionAbs_D0);
        _streamClient.SetCachedServoPosition(ServoServerClient.ServoPortEnum.D1, (byte)positionAbs_D1);
        _streamClient.SetCachedServoPosition(ServoServerClient.ServoPortEnum.D2, (byte)positionAbs_D2);

        // Send all the servo positions if there's been a change
        if (_streamClient.HasServoChanged)
        _streamClient.SendCachedServoPositions();

        if (_toDisplay.Length > 0) {

          _texture.LoadImage(_toDisplay);

          var material = GetComponent<Renderer>().material;
          material.mainTexture = _texture;
          material.mainTextureScale = new Vector2(1, -1);
        }
      }
    }

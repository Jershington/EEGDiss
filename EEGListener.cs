using System.Collections;
using System.Collections.Generic;
using System;

using UnityEngine;
using UnityEngine.UI;
using libStreamSDK;

public class EEGListener : MonoBehaviour
{
    public Text[] rawOut = new Text[13];
    private List<float> eegData;

    private Headset eegBand;
    private bool startRaw = false;
    private int connectionId;
    private float waitTimer;
    private string comPortName = "\\\\.\\COM3";
    private float connectionCheckTimer = 2;
    private float connectionCheckTimerDefault = 2;
    private NativeThinkgear.Baudrate focusBaud;

    private string TGCommPath;
    private System.Diagnostics.Process TGCommProcess;
    private int algorithmAttempts;

    //private string[] connectError = new string[5] {"Conneciton Successful", "Invalid Connection ID", "COM port could not be opened", "Invalid Baud rate value", "Invalid Data format"};

    // Use this for initialization
    void Start ()
    {
        Disconnect();
        RunTGConnector();
        algorithmAttempts = 0;
        //startRaw = ConnectionAlgorithm();
	}

    private void RunTGConnector()
    {
        TGCommPath = Application.dataPath + "/ThinkGear_Connector/ThinkGear_Connector/ThinkGearConnector.exe";//"/Scripts/TGComm.bat";
        TGCommProcess = new System.Diagnostics.Process();
        TGCommProcess.StartInfo.FileName = TGCommPath;
        //TGCommProcess.Start();
    }

    //Execution sequence for running the EEG connection protocols
    private bool ConnectionAlgorithm()
    {
        //Get a connection ID handle to ThinkGear
        if (AttemptConnect())
        {
            //Connect to TG Compatible headset
            if (ConnectToHeadset(connectionId, comPortName))
            {
                //Read Packet
                int packet = ReadPacket();
                Debug.Log("Packet read: " + packet);
                //Gets Value status and value
                if (GetValue(connectionId) != 0)
                {
                    eegBand = new Headset(connectionId, focusBaud);
                    return true;
                }
            }
            else Debug.Log("Connection Unsuccessful");
        }
        return false;
    }
    //Gets Value status and value
    private int GetValue(int cID)
    {
        int data_type = (int)NativeThinkgear.DataType.TG_DATA_RAW;
        int valueStatus = NativeThinkgear.TG_GetValueStatus(connectionId, data_type);
        Debug.Log("TG_GetValueStatus returned: " + valueStatus);
        if (NativeThinkgear.TG_GetValueStatus(cID, data_type) != 0)
        {
            string outMessage = "Raw: " + NativeThinkgear.TG_GetValue(connectionId, data_type);
            Debug.Log("Raw: " + outMessage);
        }
        else Debug.Log("Could not get value status");
        return valueStatus;
    }

    //Get a connection ID handle to ThinkGear
    private bool AttemptConnect() 
    {
        connectionId = 0;
        connectionId = NativeThinkgear.TG_GetNewConnectionId();
        if (connectionId < 0)
        {
            Debug.Log("Error: TG_GetNewConnectionId() returned Connection ID of: " + connectionId);
            return false;
        }
        else
        {
            Debug.Log("Connection ID: " + connectionId);
            return true;
        }
    }

    //Connect to TG Compatible headset
    private bool ConnectToHeadset(int cID, string cPN) 
    {
        int errCode = 0;
        errCode = NativeThinkgear.TG_Connect(cID, cPN, NativeThinkgear.Baudrate.TG_BAUD_57600, NativeThinkgear.SerialDataFormat.TG_STREAM_PACKETS);
        if (errCode < 0)
        {
            Debug.Log("ERROR: TG_Connect() returned: " + errCode);
            //Cycle through some COM ports
            if (errCode == -2)
            {
                Debug.Log("Attempting other COM Ports...");

                int comNum = 0;
                while (comNum <= 20 && errCode == -2)
                {
                    cPN = "\\\\.\\COM" + comNum;
                    errCode = NativeThinkgear.TG_Connect(cID, cPN, NativeThinkgear.Baudrate.TG_BAUD_57600, NativeThinkgear.SerialDataFormat.TG_STREAM_PACKETS);
                    comNum++;
                    Debug.Log("COM port attempted");
                }
                if (errCode != -2)
                {
                    Debug.Log("Successful COM port: " + comNum);
                    return true;
                }
                else Debug.Log("Could not Use any COM ports");
            }
            return false;
        }
        else
        {
            Debug.Log("Successfully connected to a headset on cID: " + cID);
            return true;
        }
    }

    //reads one packet from the connected headset
    private int ReadPacket() 
    {
        int packetsRead = 0;
        int count = 1;
        return packetsRead = NativeThinkgear.TG_ReadPackets(connectionId, count);
    }

	// Update is called once per frame
	void Update ()
    {
        
        while (startRaw == false)
        {
            
            startRaw = ConnectionAlgorithm();
        }
        if (startRaw)
        {
            DisplayEEGData_All();
            if (connectionCheckTimer > 0) connectionCheckTimer -= Time.deltaTime;
            else
            {
                if (IsDisconnected())
                {
                    ConnectionAlgorithm();
                }
            }
        }
    }

    //Continuously display Raw data every frame
    private void DisplayEEGData_All()
    {
        eegData = eegBand.GetData_All();
        foreach(float displayData in eegData)
        {
            rawOut[eegData.IndexOf(displayData)].text = displayData.ToString();
        }
    }

    //Connection check
    private bool IsDisconnected()
    {
        if (GetValue(connectionId) != 0) return false;
        else Disconnect();
        return true;
    }

    //Buttons
    public void Disconnect()
    {
        //NativeThinkgear.TG_Disconnect(connectionId);
        NativeThinkgear.TG_FreeConnection(connectionId);
        Debug.Log("Disconnected from Connection ID: " + connectionId);
    }

    void OnApplicationQuit()
    {
        Debug.Log("Application closing, disconnecting from Connection ID " + connectionId);
        Disconnect();
        //TGCommProcess.Kill();
        //TGCommProcess.CloseMainWindow();
    }
}


//Classes and enums
public class Headset
{
    public int connectionID;
    public NativeThinkgear.Baudrate serialBaudRate;
    //For Accurate data debugging
    public float poorSignal; //val 1
    public float attention;
    public float meditation;
    public float raw;
    public float delta;
    public float theta;
    public float alpha1;
    public float alpha2;
    public float beta1;
    public float beta2;
    public float gamma1;
    public float gamma2; //val 12
    public float dataFilterType; //val 49
    //For simple array-based data
    private List<float> dataList;

    public Headset(int cID, NativeThinkgear.Baudrate sBR)
    {
        connectionID = cID;
        serialBaudRate = sBR;
    }

    public void GetData_Debug()
    {
        poorSignal = NativeThinkgear.TG_GetValue(connectionID, 1);
        attention = NativeThinkgear.TG_GetValue(connectionID, 2);
        meditation = NativeThinkgear.TG_GetValue(connectionID, 3);
        raw = NativeThinkgear.TG_GetValue(connectionID, 4);
        delta = NativeThinkgear.TG_GetValue(connectionID, 5);
        theta = NativeThinkgear.TG_GetValue(connectionID, 6);
        alpha1 = NativeThinkgear.TG_GetValue(connectionID, 7);
        alpha2 = NativeThinkgear.TG_GetValue(connectionID, 8);
        beta1 = NativeThinkgear.TG_GetValue(connectionID, 9);
        beta2 = NativeThinkgear.TG_GetValue(connectionID, 10);
        gamma1 = NativeThinkgear.TG_GetValue(connectionID, 11);
        gamma2 = NativeThinkgear.TG_GetValue(connectionID, 12);
        dataFilterType = NativeThinkgear.TG_GetValue(connectionID, 49);
    }

    public List<float> GetData_All()
    {
        dataList.Clear();
        for (int i = 1; i < 14; i++)
        {
            if (i != 13) dataList.Add(NativeThinkgear.TG_GetValue(connectionID, i));
            else dataList.Add(NativeThinkgear.TG_GetValue(connectionID, 49));
        }
        return dataList;
    }

    public List<float> GetData_Essential()
    {
        dataList.Clear();
        for (int i = 1; i < 3; i++)
        {
            dataList.Add(NativeThinkgear.TG_GetValue(connectionID, i));
        }
        return dataList;
    }

    public float GetData_Specific(int index)
    {
        return NativeThinkgear.TG_GetValue(connectionID, index);
    }
}

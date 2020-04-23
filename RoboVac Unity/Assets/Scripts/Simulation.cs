﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Simulation : MonoBehaviour
{
    public Roomba roomba;
    public Button startStopButton;
    public Button pauseButton;
    public Button simSlowButton;
    public Button simFastButton;
    public Button settingsButton;

    public TMP_Text simSpeedText;
    public TMP_Text sqftText;

    public Sprite playImage;
    public Sprite stopImage;
    public Sprite pauseImage;
    public TrailRenderer trail;

    public bool hasErrors = false;

    private Button startStopBtn;
    private Button pauseBtn;
    private Button simSlowBtn;
    private Button simFastBtn;
    private Button settingsBtn;
    private TMP_Text simText;

    private PathType pathType = PathType.Snaking;
    private int simSpeed = 1;    
    private bool isPaused = false;
    private bool isPlaying = false;
    private bool isStopped = true;
    private bool usingAllPaths = false;

    private Queue<PathType> pathList;
    private RoombaSettingsScript roombaSettings;

    void Start() {
        startStopBtn = startStopButton.GetComponent<Button>();
        startStopBtn.onClick.AddListener(StartStopRoomba);

        pauseBtn = pauseButton.GetComponent<Button>();
        pauseBtn.onClick.AddListener(PauseRoomba);
        pauseBtn.interactable = false;

        simSlowBtn = simSlowButton.GetComponent<Button>();

        simSlowBtn.onClick.AddListener(SlowDown);
        simSlowBtn.interactable = false;

        simFastBtn = simFastButton.GetComponent<Button>();
        simFastBtn.onClick.AddListener(SpeedUp);
        simFastBtn.interactable = false;

        settingsBtn = settingsButton.GetComponent<Button>();
        
        simText = simSpeedText.GetComponent<TMP_Text>();
        trail.enabled = false;

        pathList = new Queue<PathType>();

        roombaSettings = GetComponentInParent<RoombaSettingsScript>();

        CreateDefaultFloorPlan();
    }

    void Update(){
        if(roomba.IsTimeLimitReached()){
            UserInputInformation.roombaStopGS = true;
            Debug.Log("Time limit has been reached");
            
            roomba.SetTimeLimitReached(false);
            SetDefaults();

            startStopBtn.GetComponent<Image>().sprite = playImage;
            startStopBtn.GetComponent<Image>().color = new Color(182, 214, 204, 255);
            
            if(pathList.Count != 0){
                pathType = pathList.Peek();
                Debug.Log("Next path type is: " + pathType);
                pathList.Dequeue();
                StartStopRoomba();
            } else {
                if(pathType == PathType.WallFollow && usingAllPaths){
                    usingAllPaths = false;
                }
            }
        }

        // Change the apperance of the sqft label
        sqftText.text = UserInputInformation.sqftGS.ToString();
        if (UserInputInformation.sqftGS > 8000)
        {
            // Inidicate that the current sqft is unacceptable
            sqftText.color = Color.red;
        }
        else
        {
            // Set label to the standard color
            Color newColor;
            newColor.r = 231;
            newColor.g = 236;
            newColor.b = 239;
            newColor.a = 255;

            sqftText.color = newColor;
        }

    }

    public void StartStopRoomba(){
        if (hasErrors && !isPlaying)
        {
            return;
        }

        simFastBtn.interactable = true;
        pauseBtn.interactable = true;

        if(pathType != roombaSettings.GetPathType() && !usingAllPaths){
            pathType = roombaSettings.GetPathType();
        }

        if(isPaused){   //Roomba is paused and needs to be resumed
            roomba.Resume();
            isPaused = false;
            isStopped = false;
            isPlaying = true;

            // Change the play button icon into a stop button icon
            startStopBtn.GetComponent<Image>().sprite = stopImage;
            startStopBtn.GetComponent<Image>().color = new Color(255, 0, 0, 255);

        } else if(isPlaying){
            SetDefaults();

            settingsBtn.interactable = true;
            pathList = new Queue<PathType>(); //Reset the queue if the stop button is clicked

            if(usingAllPaths){
                pathType = PathType.All;
            }

            // Change the stop button icon into a play button icon
            startStopBtn.GetComponent<Image>().sprite = playImage;
            startStopBtn.GetComponent<Image>().color = new Color(182, 214, 204, 255);

            OpenOutwardDoors();


        } else if(isStopped) {        //Roomba started for the first time  
            if(pathType == PathType.All){
                StartAllPaths();
            }
            settingsBtn.interactable = false;

            InitRoomba();

            // Change the play button icon into a stop button icon
            startStopBtn.GetComponent<Image>().sprite = stopImage;
            startStopBtn.GetComponent<Image>().color = new Color(255, 0, 0, 255);

            CloseOutwardDoors();

        }
    }

    void PauseRoomba(){
        roomba.Pause();
        isPaused = true;
        isPlaying = false;
        pauseBtn.interactable = false;

        // Change the stop button icon into a play button icon
        startStopBtn.GetComponent<Image>().sprite = playImage;
        startStopBtn.GetComponent<Image>().color = new Color(182, 214, 204, 255);

    }

    void SlowDown(){       
        if(simSpeed == 1){
            //Do nothing, min speed reached
        } else if(simSpeed == 25) {
            simSpeed = 1;
            simText.text = "1x";
            simSlowBtn.interactable = false;
            roomba.GetPath().SetVelocity(roombaSettings.GetRoombaSpeed());
        } else if(simSpeed == 50) {
            simSpeed = 25;
            simText.text = "25x";
            simFastBtn.interactable = true;
        } else {
            Debug.Log("Sim speed error. Reset to 1x speed.");
            simSpeed = 1;
            simText.text = "1x";
            simFastBtn.interactable = true;
        }

        roomba.SetSimSpeed(simSpeed);
    }

    void SpeedUp(){
        if(simSpeed == 1){
            simSpeed = 25;
            simText.text = "25x";
            simSlowBtn.interactable = true;
            roomba.GetPath().SetVelocity(1F);
        } else if(simSpeed == 25) {
            simSpeed = 50;
            simText.text = "50x";
            simFastBtn.interactable = false;
            roomba.GetPath().SetVelocity(1F);
        } else if(simSpeed == 50) {
            //Do nothing, max speed reached
        } else {
            Debug.Log("Sim speed error. Reset to 1x speed.");
            simSpeed = 1;
            simText.text = "1x";
            simSlowBtn.interactable = false;
        }

        roomba.SetSimSpeed(simSpeed);
    }

    private void InitRoomba()
    {
        trail.enabled = true;
        trail.time = roombaSettings.GetBatteryLife() * 60;
        roomba.Init(roombaSettings.GetRoombaSpeed(), roombaSettings.GetBatteryLife(), 
                    pathType, roombaSettings.GetVacuumEfficiency(), roombaSettings.GetWhiskerEfficiency());
        isStopped = false;
        isPlaying = true;
    }

    void SetDefaults(){
        simSpeed = 1;
        roomba.SetSimSpeed(simSpeed);
        simText.text = "1x";
        roomba.Stop();
        roomba.ResetRunTime();

        trail.Clear();
        trail.enabled = false;

        isPaused = false;
        isPlaying = false;
        isStopped = true;
        usingAllPaths = false;

        pauseBtn.interactable = false;
        simSlowBtn.interactable = false;
        simFastBtn.interactable = false;
        settingsBtn.interactable = true;
    }

    void StartAllPaths(){
        pathType = PathType.Random;
        pathList.Enqueue(PathType.Snaking);
        pathList.Enqueue(PathType.Spiral);
        pathList.Enqueue(PathType.WallFollow);

        usingAllPaths = true;
        Debug.Log("All paths chosen");
    }

    public bool IsPlaying()
    {
        return isPlaying;
    }

    public bool IsStopped()
    {
        return isStopped;
    }

    private void CloseOutwardDoors()
    {
        Door[] doors = UnityEngine.Object.FindObjectsOfType<Door>();

        foreach (Door door in doors)
        {
            if (door.touchingWalls.Count < 2)
            {
                door.CloseDoor();
            }

        }

    }

    private void OpenOutwardDoors()
    {
        Door[] doors = UnityEngine.Object.FindObjectsOfType<Door>();

        foreach (Door door in doors)
        {
            if (door.touchingWalls.Count < 2)
            {
                door.OpenDoor();
            }
        }

    }

    public GameObject room;
    public GameObject door;
    public GameObject chest;
    public GameObject chair;

    private void CreateDefaultFloorPlan()
    {
        CreateDefaultRoom(new Vector2(-32f, -16f), new Vector2(0f, 16f));
        CreateDefaultRoom(new Vector2(0f, -24f), new Vector2(24f, 0f));
        CreateDefaultRoom(new Vector2(0f, 0f), new Vector2(24f, 24f));

        CreateDefaultDoor(new Vector2(0f, -12f), true);
        CreateDefaultDoor(new Vector2(0f, 12f), true);
        CreateDefaultDoor(new Vector2(16f, 0f), false);

        CreateDefaultChair(new Vector2(14f, -20f), new Vector2(22f, -4f));

        CreateDefaultChest(new Vector2(-24f, -8f), new Vector2(-8f, 8f));
        Vector3 temp = new Vector3(10.0f,10.0f,0);
        roomba.transform.position = temp;
    }


    private void CreateDefaultRoom(Vector2 start, Vector2 stop)
    {
        GameObject new_room = Instantiate(room, new Vector3(0f, 0f, 0f), Quaternion.identity);
        new_room.GetComponent<Room>().LoadPositions(start, stop);
        UserInputInformation.AddRoom(new_room);
    }

    private void CreateDefaultDoor(Vector2 position, bool rotated)
    {
        GameObject new_door = Instantiate(door, new Vector3(position.x, position.y, 0f), Quaternion.identity);
        new_door.GetComponent<Door>().LoadPositions(position, rotated);
        UserInputInformation.AddDoor(new_door);
    }

    private void CreateDefaultChair(Vector2 start, Vector2 stop)
    {
        GameObject new_chair = Instantiate(chair, new Vector3(0f, 0f, 0f), Quaternion.identity);
        new_chair.GetComponentInChildren<Chair>().LoadPositions(start, stop);
        UserInputInformation.AddChair(new_chair);
    }

    private void CreateDefaultChest(Vector2 start, Vector2 stop)
    {
        GameObject new_chest = Instantiate(chest, new Vector3(0f, 0f, 0f), Quaternion.identity);
        new_chest.GetComponentInChildren<Chest>().LoadPositions(start, stop);
        UserInputInformation.AddChest(new_chest);
    }

}

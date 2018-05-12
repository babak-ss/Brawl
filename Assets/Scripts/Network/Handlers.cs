﻿using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Variables;
using Sfs2X.Requests;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Handlers : MonoBehaviour {

    private static Text log;
    private SmartFox sfs;
    private GameManager gm;

    void Start ()
    {
        gm = GetComponent<GameManager>();
        log = GameObject.Find("LogsObject/Log").GetComponent<Text>();

        gm.trace("Handlers script has been started!");
        if (!SmartFoxConnection.IsInitialized)
        {
            gm.trace("SFS not initialized! in Handlers");
            SceneManager.LoadScene("Connection");
            return;
        }

        sfs = SmartFoxConnection.Connection;

        if (sfs.IsConnected)
        {
            gm.trace("sfs is connected in Handlers ");

            sfs.AddEventListener(SFSEvent.USER_ENTER_ROOM, userEnterRoomHandler);
            sfs.AddEventListener(SFSEvent.USER_EXIT_ROOM, userExitRoomHandler);
            sfs.AddEventListener(SFSEvent.USER_VARIABLES_UPDATE, userVarUpdateHandler);
            sfs.AddEventListener(SFSEvent.PROXIMITY_LIST_UPDATE, proximityListUpdateHandler);
            sfs.AddEventListener(SFSEvent.PUBLIC_MESSAGE, publicMessageHandler);
            sfs.AddEventListener(SFSEvent.OBJECT_MESSAGE, messageHandler);
            sfs.AddEventListener(SFSEvent.CONNECTION_LOST, lostConnectionHandler);
        }

        if (gm.localPlayer == null)
        {
            gm.SpawnLocalPlayer();
        }
    }
	
	void Update () {
        if (sfs != null)
        {
            sfs.ProcessEvents();
        }
    }

    //----------------------------------------------------------
    // SmartFoxServer event listeners
    //----------------------------------------------------------

    private void userEnterRoomHandler(BaseEvent evt)
    {
        SFSUser user = (SFSUser)evt.Params["user"];
        gm.trace("(" + user.Name + ") Entered the room!");
        
        gm.SpawnRemotePlayer(user);

        // we update local player vars to let new entered user know where we are
        List<UserVariable> userVariables = new List<UserVariable>();
        userVariables.Add(new SFSUserVariable("px", (double) gm.localPlayer.transform.position.x));
        userVariables.Add(new SFSUserVariable("py", (double) gm.localPlayer.transform.position.y));
        sfs.Send(new SetUserVariablesRequest(userVariables));
    }

    private void userExitRoomHandler(BaseEvent evt)
    {
        SFSUser user = (SFSUser)evt.Params["user"];
        gm.trace("(" + user.Name + ") Exited the room!");
        if (gm.remotePlayers.ContainsKey(user))
        {
            Destroy(gm.remotePlayers[user]);
            gm.remotePlayers.Remove(user);
        }
    }

    private void userVarUpdateHandler(BaseEvent evt)
    {
        List<string> changedVars = (List<string>)evt.Params["changedVars"];

        SFSUser user = (SFSUser)evt.Params["user"];
        GameObject remotePlayer;
        bool remotePlayerDirection = true;

        if (user == sfs.MySelf) return;

        if (gm.remotePlayers.ContainsKey(user))
            remotePlayer = gm.remotePlayers[user];
        else
        {
            gm.SpawnRemotePlayer(user);
            remotePlayer = gm.remotePlayers[user];
        }

        //gm.trace("(" + user.Name + ") Changed its vars!");
        if (changedVars.Contains("px") || changedVars.Contains("py"))
            remotePlayer.transform.position = new Vector3((float)user.GetVariable("px").GetDoubleValue(), (float)user.GetVariable("py").GetDoubleValue(), 0);
        if (changedVars.Contains("vx"))
            remotePlayer.GetComponent<Rigidbody2D>().velocity = new Vector3((float)user.GetVariable("vx").GetDoubleValue(), (float)user.GetVariable("vy").GetDoubleValue(), 0);
        if (changedVars.Contains("vely"))
            remotePlayer.GetComponent<Rigidbody2D>().velocity = new Vector3((float)user.GetVariable("vx").GetDoubleValue(), (float)user.GetVariable("vy").GetDoubleValue(), 0);

        if (remotePlayer.GetComponent<Rigidbody2D>().velocity.x == 0 && remotePlayer.GetComponent<Rigidbody2D>().velocity.y == 0)
            remotePlayer.GetComponent<Animator>().SetBool("Running", false);
        else
        {
            remotePlayer.GetComponent<Animator>().SetBool("Running", true);
        }

        // Redirecting remote player direction
        if (user.GetVariable("vx").GetDoubleValue() != 0)
            remotePlayerDirection = (user.GetVariable("vx").GetDoubleValue() < 0) ? false : true;
        Vector3 trueDirection = new Vector3(remotePlayerDirection ? 1 : -1, 1, 1);
        remotePlayer.GetComponent<Transform>().localScale = trueDirection;
        remotePlayer.transform.FindChild("name").GetComponent<Transform>().localScale = trueDirection;
    }
    private void proximityListUpdateHandler(BaseEvent e)
    {
        var addedUsers = (List<User>)e.Params["addedUsers"];
        var removedUsers = (List<User>)e.Params["removedUsers"];

        // Handle new players
        foreach (var user in addedUsers)
        {
            gm.SpawnRemotePlayer(user);
        }

        // Handle removed players
        foreach (var user in removedUsers)
        {
            gm.remotePlayers.Remove(user);
        }
    }
    private void publicMessageHandler(BaseEvent e)
    {
        //Debug.Log("!!! New PublicMessage !!!");
        Room room = (Room)e.Params["room"];
        User sender = (User)e.Params["sender"];
        string msg = "[" + room.Name + "] " + sender.Name + ": " + e.Params["message"];
        gm.trace(msg);
    }

    private void lostConnectionHandler(BaseEvent evt)
    {
        gm.trace("Lost Connection");
        sfs.RemoveAllEventListeners();
        SceneManager.LoadScene("Connection");
    }

    private void messageHandler(BaseEvent evt)
    {
        gm.trace("Message Handler");
    }
}
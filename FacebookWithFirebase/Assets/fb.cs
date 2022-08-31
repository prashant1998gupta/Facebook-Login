using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Facebook.Unity;
//using System;
using UnityEngine.UI;

public class fb : MonoBehaviour
{
    public Text debug;
    private void Awake()
    {
        if(!FB.IsInitialized)
        {
            FB.Init(InitCallBack, OnHideUnity);
        }
        else
        {
            FB.ActivateApp();
        }
    }
    private void InitCallBack()
    {
        if(!FB.IsInitialized)
        {
            FB.ActivateApp();
        }
        else
        {
            debug.text=("Failed to initialize");
        }
    }
    private void OnHideUnity(bool isgameshown)
    {
        if(!isgameshown)
        {
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
        }
    }    

    public void Facebook_Login()
    {
        var permission = new List<string>() { "public_profile", "email" };
        FB.LogInWithReadPermissions(permission, AuthCallBack);
    }

    private void AuthCallBack(ILoginResult result)
    {
        if(FB.IsLoggedIn)
        {
            var aToken = Facebook.Unity.AccessToken.CurrentAccessToken;
            debug.text=(aToken.UserId);
        }
        else
        {
            debug.text=("User Cancelled login");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log($"this is start");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

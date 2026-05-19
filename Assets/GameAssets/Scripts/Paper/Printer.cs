using System.Collections.Generic;
using UnityEngine;

public class Printer : MonoBehaviour
{
    public class Notification
    {
        public Texture2D texture;
        public float time;
        public Notification(Texture2D texture, float time)
        {
            this.texture = texture;
            this.time = time;
        }
    }

    //Run time
    [HideInInspector]
    public List<Notification> notificationQueList = new List<Notification>();
    [HideInInspector]
    public Notification activeNotification;

    void Update()
    {
        UpdatePrinterCall();
    }
    public void UpdatePrinterCall()
    {
        if (activeNotification == null)
        {
            if (CanPlayNotification())
            {
                if (notificationQueList.Count > 0)
                {
                    //Begin Print
                    BeginPrint();
                }
                else
                {
                    DisablePrint();
                }
            }
        }
        else
        {
            activeNotification.time -= Time.deltaTime;
            if (activeNotification.time < 0f || OverrideClearTime())
            {
                ClearPrint();
            }
        }
    }

    //Begind showing notification
    public virtual void BeginPrint()
    {
        activeNotification = notificationQueList[0];
        notificationQueList.RemoveAt(0);
    }
    //Notification time has reached 0 or has been overriden
    public virtual void ClearPrint()
    {
        activeNotification = null;
    }
    //No notifications are in que. Disable render stuff if needed
    public virtual void DisablePrint()
    {

    }
    //Override the notificaton time
    public virtual bool OverrideClearTime()
    {
        return false;
    }
    //Override the can play expression
    public virtual bool CanPlayNotification()
    {
        return true;
    }
    public void ClearNotifications()
    {
        if (activeNotification != null) activeNotification.time = 0f;
        notificationQueList = new List<Notification>();
    }
    public void AddNotification(Texture2D texture, float length = 5f)
    {
        notificationQueList.Add(new Notification(texture, length));
    }
}

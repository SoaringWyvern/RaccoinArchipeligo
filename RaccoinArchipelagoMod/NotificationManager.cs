using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace RaccoinArchipelagoMod
{
    public class APNotificationManager : MonoBehaviour
    {
        private class Notification
        {
            public string Message;
            public float TimeLeft;
        }

        // Thread-safe queue to catch messages coming from the Archipelago network thread
        private static ConcurrentQueue<string> incomingMessages = new ConcurrentQueue<string>();
        
        // List of messages currently visible on screen
        private List<Notification> activeNotifications = new List<Notification>();
        
        // Settings
        private readonly float displayTime = 6.0f; 
        private readonly int maxMessages = 5;

        // Call this from ANYWHERE to pop a message on screen
        public static void SendNotification(string msg)
        {
            incomingMessages.Enqueue(msg);
        }

        void Update()
        {
            // Move network messages safely to the Unity main thread
            while (incomingMessages.TryDequeue(out string newMsg))
            {
                activeNotifications.Add(new Notification { Message = newMsg, TimeLeft = displayTime });
                if (activeNotifications.Count > maxMessages)
                {
                    activeNotifications.RemoveAt(0); // Remove oldest if we exceed the cap
                }
            }

            // Tick down timers and remove expired messages
            for (int i = activeNotifications.Count - 1; i >= 0; i--)
            {
                activeNotifications[i].TimeLeft -= Time.deltaTime;
                if (activeNotifications[i].TimeLeft <= 0)
                {
                    activeNotifications.RemoveAt(i);
                }
            }
        }

        void OnGUI()
        {
            if (activeNotifications.Count == 0) return;

            GUIStyle style = new GUIStyle();
            
            // Ensure the 'normal' state exists before we try to color it
            if (style.normal == null) 
            {
                style.normal = new GUIStyleState();
            }

            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;
            style.wordWrap = false;

            // Start drawing from the bottom left corner, going upwards
            int yOffset = Screen.height - 40 - (activeNotifications.Count * 25);

            foreach (var notif in activeNotifications)
            {
                // Draw a black drop-shadow slightly offset for readability
                style.normal.textColor = Color.black;
                GUI.Label(new Rect(22, yOffset + 2, Screen.width, 30), notif.Message, style);
                
                // Draw the actual white text on top
                style.normal.textColor = Color.white;
                GUI.Label(new Rect(20, yOffset, Screen.width, 30), notif.Message, style);
                
                yOffset += 25; // Spacing between messages
            }
        }
    }
}
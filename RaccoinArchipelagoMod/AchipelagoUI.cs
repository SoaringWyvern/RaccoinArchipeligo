using System;
using UnityEngine;

namespace RaccoinArchipelagoMod
{
    public class ArchipelagoUI : MonoBehaviour
    {
        public ArchipelagoUI(IntPtr handle) : base(handle) { }

        private bool showMenu = true;
        private int activeField = -1; 

        private string serverIp = "localhost";
        private string serverPort = "38281";
        private string slotName = "Player1";
        private string password = "";
        private string statusMessage = "";
        private Color statusColor = Color.white;
        private float statusTimer = 0f;

        public void Update()
        {
            if (statusTimer > 0f)
            {
                statusTimer -= Time.deltaTime;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.F3))
            {
                showMenu = !showMenu;
                activeField = -1; 
            }

            if (showMenu)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                if (activeField != -1)
                {
                    string input = UnityEngine.Input.inputString;
                    foreach (char c in input)
                    {
                        if (c == '\b') BackspaceActiveField();
                        else if (c == '\n' || c == '\r') activeField = -1;
                        else if (!char.IsControl(c)) AppendToActiveField(c);
                    }
                }
            }
        }

        private void BackspaceActiveField()
        {
            if (activeField == 0 && serverIp.Length > 0) serverIp = serverIp.Substring(0, serverIp.Length - 1);
            else if (activeField == 1 && serverPort.Length > 0) serverPort = serverPort.Substring(0, serverPort.Length - 1);
            else if (activeField == 2 && slotName.Length > 0) slotName = slotName.Substring(0, slotName.Length - 1);
            else if (activeField == 3 && password.Length > 0) password = password.Substring(0, password.Length - 1);
        }

        private void AppendToActiveField(char c)
        {
            if (activeField == 0) serverIp += c;
            else if (activeField == 1) serverPort += c;
            else if (activeField == 2) slotName += c;
            else if (activeField == 3) password += c;
        }

        public void OnGUI()
        {
            Event e = Event.current;

            // Draw the Notification
            // This ensures you can still see the success message after the menu hides itself.
            if (statusTimer > 0f)
            {
                Color originalContentColor = GUI.contentColor;

                GUI.contentColor = Color.black;
                GUI.Label(new Rect(11, 11, 800, 30), statusMessage);
                GUI.contentColor = statusColor;
                GUI.Label(new Rect(10, 10, 800, 30), statusMessage);

                GUI.contentColor = originalContentColor; 
            }

            // Stop drawing the rest of the UI if the menu is closed
            if (!showMenu) return;

            Rect ipRect = new Rect(30, 70, 280, 20);
            Rect portRect = new Rect(30, 115, 280, 20);
            Rect slotRect = new Rect(30, 160, 280, 20);
            Rect passRect = new Rect(30, 205, 280, 20);
            Rect connectRect = new Rect(30, 250, 280, 30);

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (ipRect.Contains(e.mousePosition)) activeField = 0;
                else if (portRect.Contains(e.mousePosition)) activeField = 1;
                else if (slotRect.Contains(e.mousePosition)) activeField = 2;
                else if (passRect.Contains(e.mousePosition)) activeField = 3;
                else if (connectRect.Contains(e.mousePosition))
                {
                    activeField = -1; 
                    
                    if (int.TryParse(serverPort.Trim(), out int port))
                    {
                        RaccoinPlugin.ModLogger.LogMessage($"Attempting to connect to {serverIp}:{port} as {slotName}...");
                        
                        // Handle the connection result
                        bool success = ArchipelagoManager.Connect(serverIp, port, slotName, password, out string errorMsg);

                        if (success)
                        {
                            statusMessage = "Archipelago Connection Successful!";
                            statusColor = Color.green;
                            statusTimer = 5f; // Show for 5 seconds
                            showMenu = false; // Hide the menu
                        }
                        else
                        {
                            statusMessage = $"Connection Failed: {errorMsg}";
                            statusColor = Color.red;
                            statusTimer = 8f; // Show errors a little longer
                            // do NOT hide the menu here, so they can fix their typo
                        }
                    }
                    else
                    {
                        statusMessage = "Connection Failed: Invalid Port Number!";
                        statusColor = Color.red;
                        statusTimer = 5f;
                    }
                }
                else
                {
                    activeField = -1; 
                }
            }

            // Background texture swap to prevent the menu being see-through
            Color originalBg = GUI.backgroundColor;
            Color originalContent = GUI.contentColor;
            Texture2D originalBoxTexture = GUI.skin.box.normal.background;

            GUI.skin.box.normal.background = Texture2D.whiteTexture;
            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f); 
            GUI.contentColor = Color.white;

            GUI.Box(new Rect(20, 20, 300, 320), "Archipelago Connection");

            GUI.skin.box.normal.background = originalBoxTexture;
            GUI.backgroundColor = originalBg;

            GUI.Label(new Rect(30, 50, 280, 20), "Server IP:");
            GUI.Label(new Rect(30, 95, 280, 20), "Port:");
            GUI.Label(new Rect(30, 140, 280, 20), "Slot Name:");
            GUI.Label(new Rect(30, 185, 280, 20), "Password (Leave blank if none):");
            GUI.Label(new Rect(30, 290, 280, 20), "Press F3 to toggle this menu.");

            GUI.Box(ipRect, serverIp + (activeField == 0 ? "_" : ""), GUI.skin.textField);
            GUI.Box(portRect, serverPort + (activeField == 1 ? "_" : ""), GUI.skin.textField);
            GUI.Box(slotRect, slotName + (activeField == 2 ? "_" : ""), GUI.skin.textField);
            
            string passDisplay = new string('*', password.Length) + (activeField == 3 ? "_" : "");
            GUI.Box(passRect, passDisplay, GUI.skin.textField);

            GUI.contentColor = originalContent;
            GUI.Button(connectRect, "Connect");

            // The Cursor
            Color preCursorBg = GUI.backgroundColor;
            Texture2D preCursorTex = GUI.skin.box.normal.background;

            GUI.skin.box.normal.background = Texture2D.whiteTexture;
            GUI.backgroundColor = Color.red;
            
            GUI.Box(new Rect(e.mousePosition.x, e.mousePosition.y, 8, 8), "");
            
            GUI.backgroundColor = preCursorBg; 
            GUI.skin.box.normal.background = preCursorTex;
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IconSystray
{
    /// <summary>
    /// Handles the systray icon
    /// </summary>
    static class NotifyIconSystray
    {
        private static NotifyIcon notifyIcon;
        public delegate void Status(bool status);
        private static string _name;
        public delegate void CallbackQuit();
        public static event CallbackQuit OnQuit;

        private static string _on_text = "on";
        private static string _on_image = "icon.ico";
        public static void setOnIcon(string text, string imageName)
        {
            _on_text = text;
            _on_image = imageName;
        }

        private static string _off_text = "off";
        private static string _off_image = "icon_off.ico";
        public static void setOffIcon(string text, string imageName)
        {
            _off_text = text;
            _off_image = imageName;
        }

        /// <summary>
        /// This method allows to change the state of icon and tooltip
        /// true = Log Active: the logger detected the client and is active.
        /// </summary>
        /// <param name="status"></param>
        public static void Status_DelegateMethod(bool status)
        {
            string text = String.Format("{0}\nstatus: {1}", _name, status ? _on_text : _off_text);

            string iconName = status ? _on_image : _off_image;

            setNotifyIcon(iconName, text);
        }

        /// <summary>
        /// This delegate allows us to call Status_DelegateMethod in the backgroundworker
        /// It changes the indicator that displays the state of the app.
        /// </summary>
        public static Status ChangeStatus = Status_DelegateMethod;

        /// <summary>
        /// set text and icon for the taskbar
        /// Icon must be in the project as embedded resource
        /// </summary>
        /// <param name="nameIcon"></param>
        /// <param name="text"></param>
        public static void setNotifyIcon(string iconName, string text)
        {
            //set text that support 128 char instead of 64
            Fixes.SetNotifyIconText(notifyIcon, text);

            //get icon by its name. Icon must be in the project as embedded resource
            Assembly assembly = Assembly.GetExecutingAssembly();
            string ns = assembly.EntryPoint.DeclaringType.Namespace;
            Stream iconStream = assembly.GetManifestResourceStream(string.Format("{0}.{1}", ns, iconName));
            notifyIcon.Icon = new Icon(iconStream);
        }


        /// <summary>
        /// add notification icon to system tray bar (near the clock)
        /// quit option is available by default
        /// </summary>
        /// <param name="name">name displayed on mouse hover</param>
        /// <param name="items">items to add to the context menu</param>
        public static void addNotifyIcon(String name, MenuItem[] items = null)
        {
            _name = name;
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Visible = true;

            Status_DelegateMethod(false); //set name and icon

            ContextMenu contextMenu1 = new ContextMenu();
            if (items != null)
            {
                contextMenu1.MenuItems.AddRange(items);
            }
            contextMenu1.MenuItems.Add(new MenuItem("Quit", (s, e) =>
            {
                if (OnQuit != null)
                {
                    OnQuit();
                }
                disposeNotifyIcon();
            }));
            notifyIcon.ContextMenu = contextMenu1;

        }

        public static void disposeNotifyIcon()
        {
            notifyIcon.Dispose();
            foreach (ProcessThread thread in Process.GetCurrentProcess().Threads)
            {
                thread.Dispose();
            }
            Process.GetCurrentProcess().Kill();
        }
    }

    public class Fixes
    {
        /// <summary>
        /// Set text tooltip to 128 char limit instead of 64
        /// http://stackoverflow.com/questions/579665/how-can-i-show-a-systray-tooltip-longer-than-63-chars
        /// </summary>
        /// <param name="ni"></param>
        /// <param name="text"></param>
        public static void SetNotifyIconText(NotifyIcon ni, string text)
        {
            if (text.Length >= 128) throw new ArgumentOutOfRangeException("Text limited to 127 characters");

            Type t = typeof(NotifyIcon);
            BindingFlags hidden = BindingFlags.NonPublic | BindingFlags.Instance;
            t.GetField("text", hidden).SetValue(ni, text);

            if ((bool)t.GetField("added", hidden).GetValue(ni))
                t.GetMethod("UpdateIcon", hidden).Invoke(ni, new object[] { true });
        }
    }
}

/*
Copyright 2010 Thomas W. Holtquist

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.IO;
using System.Threading;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using GHIElectronics.NETMF.System;

using Skewworks.Pyxis.Kernel;

namespace Skewworks.Pyxis
{
    public class Program
    {

        #region Variables

        private static bool bAniBoot;       // Loading while true
        private static PyxisAPI API;

        #endregion

        /// <summary>
        /// Determines which method to run based on Boot Mode & Device
        /// </summary>
        public static void Main()
        {
            // Create instance of API
            API = new PyxisAPI();

            // Handle Emulator
            if (API.ActiveDevice == PyxisAPI.DeviceType.Emulator)
            {
                BootProcess();
                Thread.Sleep(-1);
            }

            // Handle Boot Modes
            switch (SystemUpdate.GetMode())
            {
                case SystemUpdate.SystemUpdateMode.NonFormatted:

                    // Display IFU Prep Screen
                    bAniBoot = true;
                    Thread thBoot = new Thread(PrepBootScreen);
                    thBoot.Priority = ThreadPriority.Highest;
                    thBoot.Start();

                    SystemUpdate.EnableBootloader();
                    break;
                case SystemUpdate.SystemUpdateMode.Bootloader:
                    throw new Exception("Invalid Boot Mode!");
                case SystemUpdate.SystemUpdateMode.Application:
                    BootProcess();
                    break;
            }


            // Keep thread alive
            Thread.Sleep(-1);
        }

        /// <summary>
        /// Takes care of the actual startup of Pyxis
        /// </summary>
        static void BootProcess()
        {
            // Always set the boot logo
            AppearanceManager.SetBootLogo();

            // Display Boot Screen
            bAniBoot = true;
            Thread thBoot = new Thread(BootScreen);
            thBoot.Priority = ThreadPriority.Highest;
            thBoot.Start();

            // We only need to load settings outside the emulator
            if (API.ActiveDevice != PyxisAPI.DeviceType.Emulator)
            {

                // Check for null settings
                if (SettingsManager.LCDSettings == null && API.ActiveDevice == PyxisAPI.DeviceType.Cobra)
                {
                    bAniBoot = false;
                    API.SelectScreenSize();
                }

                // Calibrate if needed
                if (SettingsManager.LCDSettings.calibrateLCD == ScreenCalibration.Gather && API.ActiveDevice == PyxisAPI.DeviceType.Cobra)
                {
                    thBoot.Suspend();
                    API.CalibrateScreen();
                    thBoot.Resume();
                }
                else if (SettingsManager.LCDSettings.calibrateLCD == ScreenCalibration.Restore && API.ActiveDevice == PyxisAPI.DeviceType.Cobra)
                {
                    SettingsManager.RestoreLCDCalibration();
                }
            }

            // Allow drives time to load
            Thread.Sleep(1500);

            // Enable Network Devices
            if (SettingsManager.BootSettings.EnableDHCP) API.MyNetwork.AutoConnect = NetworkManager.AutoConnectType.DHCP;

            // Display Desktop
            bAniBoot = false;
            API.DisplayDesktop();

        }

        /// <summary>
        /// Run in its own thread this displays an animated boot screen
        /// </summary>
        static void BootScreen()
        {
            int i = 0;
            int xx = (int)(((float)AppearanceManager.ScreenWidth / 2) - 58);
            int x = xx;
            int y = AppearanceManager.ScreenHeight - 22;
            Bitmap bmpL0 = new Bitmap(Resources.GetBytes(Resources.BinaryResources.load0), Bitmap.BitmapImageType.Jpeg);
            Bitmap bmpL1 = new Bitmap(Resources.GetBytes(Resources.BinaryResources.load1), Bitmap.BitmapImageType.Jpeg);
            Bitmap bmpL2 = new Bitmap(Resources.GetBytes(Resources.BinaryResources.load2), Bitmap.BitmapImageType.Jpeg);
            Bitmap bmp = new Bitmap(AppearanceManager.ScreenWidth, AppearanceManager.ScreenHeight);

            switch (AppearanceManager.ScreenWidth)
            {
                case 320:
                    bmp.DrawImage(-80, -26, new Bitmap(Resources.GetBytes(Resources.BinaryResources.boot), Bitmap.BitmapImageType.Jpeg), 0, 0, 480, 272);
                    break;
                case 480:
                    bmp.DrawImage(0, 0, new Bitmap(Resources.GetBytes(Resources.BinaryResources.boot), Bitmap.BitmapImageType.Jpeg), 0, 0, 480, 272);
                    break;
                case 800:
                    break;
            }


            while (bAniBoot)
            {
                x = xx;

                bmp.DrawImage(x, y, (i == 1 || i == 3) ? bmpL1 : (i == 2) ? bmpL2 : bmpL0, 0, 0, 14, 14);
                x += 17;

                bmp.DrawImage(x, y, (i == 2 || i == 4) ? bmpL1 : (i == 3) ? bmpL2 : bmpL0, 0, 0, 14, 14);
                x += 17;

                bmp.DrawImage(x, y, (i == 3 || i == 5) ? bmpL1 : (i == 4) ? bmpL2 : bmpL0, 0, 0, 14, 14);
                x += 17;

                bmp.DrawImage(x, y, (i == 4 || i == 6) ? bmpL1 : (i == 5) ? bmpL2 : bmpL0, 0, 0, 14, 14);
                x += 17;

                bmp.DrawImage(x, y, (i == 5 || i == 7) ? bmpL1 : (i == 6) ? bmpL2 : bmpL0, 0, 0, 14, 14);
                x += 17;

                bmp.DrawImage(x, y, (i == 6 || i == 8) ? bmpL1 : (i == 7) ? bmpL2 : bmpL0, 0, 0, 14, 14);
                x += 17;

                bmp.DrawImage(x, y, (i == 7 || i == 9) ? bmpL1 : (i == 8) ? bmpL2 : bmpL0, 0, 0, 14, 14);

                i++;
                if (i > 9) i = 0;
                bmp.Flush();
                Thread.Sleep(150);
            }
        }

        /// <summary>
        /// Boot screen displayed while IFU region is being created
        /// </summary>
        static void PrepBootScreen()
        {
            int i = 0;
            int xx = (int)(((float)AppearanceManager.ScreenWidth / 2) - 58);
            int x = xx;
            int y = AppearanceManager.ScreenHeight - 22;
            Bitmap bmpL0 = new Bitmap(Resources.GetBytes(Resources.BinaryResources.load0), Bitmap.BitmapImageType.Jpeg);
            Bitmap bmpL1 = new Bitmap(Resources.GetBytes(Resources.BinaryResources.load1), Bitmap.BitmapImageType.Jpeg);
            Bitmap bmpL2 = new Bitmap(Resources.GetBytes(Resources.BinaryResources.load2), Bitmap.BitmapImageType.Jpeg);
            Bitmap bmp = new Bitmap(AppearanceManager.ScreenWidth, AppearanceManager.ScreenHeight);

            switch (AppearanceManager.ScreenWidth)
            {
                case 320:
                    bmp.DrawImage(-80, -26, new Bitmap(Resources.GetBytes(Resources.BinaryResources.boot), Bitmap.BitmapImageType.Jpeg), 0, 0, 480, 272);
                    break;
                case 480:
                    bmp.DrawImage(0, 0, new Bitmap(Resources.GetBytes(Resources.BinaryResources.boot), Bitmap.BitmapImageType.Jpeg), 0, 0, 480, 272);
                    break;
                case 800:
                    break;
            }

            Font fnt = Resources.GetFont(Resources.FontResources.tahoma11);
            bmp.DrawTextInRect("Preparing In-Field Update Region", 0, y - fnt.Height - 4, AppearanceManager.ScreenWidth, fnt.Height, Bitmap.DT_AlignmentCenter, Colors.Black, fnt);

            while (bAniBoot)
            {
                x = xx;

                bmp.DrawImage(x, y, (i == 1 || i == 3) ? bmpL1 : (i == 2) ? bmpL2 : bmpL0, 0, 0, 14, 14);
                x += 17;

                bmp.DrawImage(x, y, (i == 2 || i == 4) ? bmpL1 : (i == 3) ? bmpL2 : bmpL0, 0, 0, 14, 14);
                x += 17;

                bmp.DrawImage(x, y, (i == 3 || i == 5) ? bmpL1 : (i == 4) ? bmpL2 : bmpL0, 0, 0, 14, 14);
                x += 17;

                bmp.DrawImage(x, y, (i == 4 || i == 6) ? bmpL1 : (i == 5) ? bmpL2 : bmpL0, 0, 0, 14, 14);
                x += 17;

                bmp.DrawImage(x, y, (i == 5 || i == 7) ? bmpL1 : (i == 6) ? bmpL2 : bmpL0, 0, 0, 14, 14);
                x += 17;

                bmp.DrawImage(x, y, (i == 6 || i == 8) ? bmpL1 : (i == 7) ? bmpL2 : bmpL0, 0, 0, 14, 14);
                x += 17;

                bmp.DrawImage(x, y, (i == 7 || i == 9) ? bmpL1 : (i == 8) ? bmpL2 : bmpL0, 0, 0, 14, 14);

                i++;
                if (i > 9) i = 0;
                bmp.Flush();
                Thread.Sleep(150);
            }
        }

    }
}

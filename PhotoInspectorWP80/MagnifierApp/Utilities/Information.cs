/*
 * Copyright © 2013-2014 Microsoft Mobile. All rights reserved.
 * Nokia and Nokia Connecting People are registered trademarks of Nokia Corporation. 
 * Other product and company names mentioned herein may be trademarks
 * or trade names of their respective owners. 
 * See LICENSE.TXT for license information.
 */

using MagnifierApp.Models;
using Microsoft.Phone.Info;
using System.Linq;
using Windows.Phone.Media.Capture;

namespace MagnifierApp.Utilities
{
    public class Information
    {
        public static bool HighResolutionCaptureSupported
        {
            get
            {
                var deviceName = DeviceStatus.DeviceName;

                if (deviceName.Contains("RM-875") || deviceName.Contains("RM-876") || deviceName.Contains("RM-877"))
                {
                    return true;
                }
                else
                {
                    var captureResolutions = PhotoCaptureDevice.GetAvailableCaptureResolutions(CameraSensorLocation.Back);
                    var maxCaptureResolution = captureResolutions.First();

                    return maxCaptureResolution.Width * maxCaptureResolution.Height > PhotoModel.LibraryMaxArea;
                }
            }
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DeviceCenter
{
    /// <summary>
    /// Interaction logic for PageTestOnly.xaml
    /// </summary>
    public partial class PageTestOnly : Page
    {
        public PageTestOnly()
        {
            InitializeComponent();
        }

        private async void buttonTest_Click(object sender, RoutedEventArgs e)
        {
            // do something here
            IPAddress ip = IPAddress.Parse("10.125.140.161");
            WebBRest webbRequest = new WebBRest(ip, "Administrator", "p@ssw0rd");
            //bool x = await webbRequest.SetDeviceNameAsync("tengtestWohaha");
            //bool y = await webbRequest.SetPasswordAsync("p@ssw0rd", "password1");
            //bool z = await webbRequest.Restart();
            //string[] files = new String[4];
            //files[0] = @"InternetRadioHeaded_1.0.1.0_ARM.appx";
            //files[1] = @"InternetRadioHeaded_1.0.1.0_ARM.cer";
            //files[2] = @"Microsoft.NET.Native.Runtime.1.1.appx";
            //files[3] = @"Microsoft.VCLibs.ARM.14.00.appx";
            //bool x = await webbRequest.InstallAppxAsync(files, @"C:\Users\tenglu\Documents\DeviceCenterSamples\DeployFolder\arm\");
            bool y = await webbRequest.StopApp("InternetRadioHeaded");
        }
    }
}

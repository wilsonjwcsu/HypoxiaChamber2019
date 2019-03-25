using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Gpio;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace App1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page // from http://blog.chrisbriggsy.com/Beginners-guide-to-GPIO-Win10-IoT-Core-Insider-Preview/
    {
        private const int LED_PIN = 24;
        
        private GpioPin pin;
       
        private GpioPinValue PinValue;
       
        public MainPage()
        
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Required;
            Unloaded += MainPage_Unloaded;
           
            InitGPIO();
           


        }
        private void InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            if (gpio == null)
            {
                GpioStatus.Text = "There is no GPIO controller on this device.";
                return;
            }
            pin = gpio.OpenPin(LED_PIN);



            pin.Write(GpioPinValue.Low);
            pin.SetDriveMode(GpioPinDriveMode.Output);

            GpioStatus.Text = "GPIO pin initialized correctly.";
            
        }
        private void MainPage_Unloaded(object sender, object args)
        {
            pin.Dispose();
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            PinValue = pin.Read();
            if (PinValue == GpioPinValue.High)
            {
                pin.Write(GpioPinValue.Low);
            }
            else if (PinValue == GpioPinValue.Low)
            {
                pin.Write(GpioPinValue.High);
            }
        }
       

           
        }

    }





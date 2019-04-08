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
using Windows.System.Diagnostics;
using System.Diagnostics;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace App1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page // from http://blog.chrisbriggsy.com/Beginners-guide-to-GPIO-Win10-IoT-Core-Insider-Preview/
    {
        private const int LED_PIN = 24;
        private const int Light_PIN = 22;

        private GpioPin pin;
        private GpioPin lightpin;

        private GpioPinValue PinValue;
        private GpioPinValue LightPinValue;

        private MCP3008 mcp3008;
        const float ReferenceVoltage = 3.3F;
        public float CurrentO2;
        const byte O2ADCChannel = 0;


        public MainPage()

        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Required;
            Unloaded += MainPage_Unloaded;
            InitGPIO();
            SensorDateProvider();
            SensorInit();





        }
        public void SensorDateProvider()
        {
            mcp3008 = new MCP3008(ReferenceVoltage);
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
            lightpin = gpio.OpenPin(Light_PIN);



            pin.Write(GpioPinValue.Low);
            pin.SetDriveMode(GpioPinDriveMode.Output);
            lightpin.Write(GpioPinValue.Low);
            lightpin.SetDriveMode(GpioPinDriveMode.Output);

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

        private async void SensorInit()
        {
         await mcp3008.Initialize();
            if (mcp3008 == null)
            {
                TestBlock.Text = "this did not work";

            }
            else
            {
                TestBlock.Text = "This Did work";
            }
            
        }
        private void UpdateO2_Click(object sender, RoutedEventArgs e)
        {
            CurrentO2 = mcp3008.ReadADC(O2ADCChannel);
            //CurrentO2 = (CurrentO2 * (3300.0F / 1024.0F)) / 132F;
            TextBlock1.Text = CurrentO2.ToString();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            LightPinValue = lightpin.Read();
            if (LightPinValue == GpioPinValue.High)
            {
                lightpin.Write(GpioPinValue.Low);
            }
            else if (LightPinValue == GpioPinValue.Low)
            {
                lightpin.Write(GpioPinValue.High);
            }

        }
    }
}





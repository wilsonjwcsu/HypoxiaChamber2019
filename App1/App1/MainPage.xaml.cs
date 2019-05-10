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
using Windows.Devices.I2c;
using AdafruitClassLibrary;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace App1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page // from http://blog.chrisbriggsy.com/Beginners-guide-to-GPIO-Win10-IoT-Core-Insider-Preview/
    {
        private const int LED_PIN = 24; //this is the pin value used for the solenoid switch
        private const int Light_PIN = 22; // the is the pin value used for the light switch function. Be carefull in assigning these, all the gpio pins across all apps on a raspberry pi must be unique

        private GpioPin pin; // This creates an object that physically interacts the voltage on the solenoid relay
        private GpioPin lightpin; // This is enstanchiating an object that changes the voltage on the light relay

        private GpioPinValue PinValue; //This is a variable of type GpioPinValue, this allows us to store the state of the solenoid pin and read it off with other methods
        private GpioPinValue LightPinValue; //This variable store the state of the light relay pin. It is either going to be high or low

        private MCP3008 mcp3008; //entanchiating ADC object, you can now use all methods in MCP3008.cs file
        private MHZ16 mhz16; //enstanchiating C02 object so code can be used from MHZ16.cs file
        const float ReferenceVoltage = 3.3F; //needed to initialize MCP3008, do not change
        public float CurrentO2; // variable that stores reading of o2 for display
        public float CurrentCO2; // variable that stores the reading of CO2
        const byte O2ADCChannel = 0; // channel used on ADC, use this on whichever channel the resistors are being probed at.
        private const float factor = 0.0291F; // linear coeficient for adc to o2 conversion
        private const float Constant = -6.378F; // offset for adc to o2 conversion

        private DispatcherTimer timer; //creates a dispatch timer object to count time between readings (dispatch timers work independently from the UI)


        private const byte PCA9685_ADDRESS = 0x40; //Default I2C bus option

        private int UpperO2Lim = 580; //592 // upper limit for O2 feedback
        private int LowerO2Lim = 546; //499 // lower limit for O2 feedback
        private bool Override = false; // determines whether feedback loop is active, false means it is active
        private int shiftflag = 1; // used to test PWM fan speeds.

        private Pca9685 pca9685;//from https://learn.adafruit.com/adafruit-class-library-for-windows-iot-core?view=all#pca9685-class this is the kind of object needed to work the PWM fans from I2C
                                // public enum I2CSpeed { I2C_100kHz, I2C_400kHz };
                                //private I2CSpeed i2cSpeed;

        // public PCA9685(int addr = PCA9685_ADDRESS) : base(addr);


        public MainPage()

        {
            this.InitializeComponent(); //helps with UI setup
            NavigationCacheMode = NavigationCacheMode.Required; //UI setup
            Unloaded += MainPage_Unloaded; //UI setup
            InitGPIO(); //sets up GPIO pins
            SensorDateProvider(); //method call to construct various objects
            SensorInit(); //adc setup

            //****Figure out way to initialize fan and C02 I2C connection and put it below here*****\\\




            timer = new DispatcherTimer(); //constructs timer object
            timer.Interval = TimeSpan.FromMilliseconds(500); //sets the time on the timer in miliseconds
            timer.Tick += Timer_Tick; // calls method every time timer finishes counting
            timer.Start(); //starts/ restarts timer after timer_tick method is called

            O2Upperlimitvalue.Text= UpperO2Lim.ToString();
            O2Lowerlimitvalue.Text = LowerO2Lim.ToString();



        }

        private void Timer_Tick(object sender, object e) //method that is called when time reaches end
        {
            //****put code here to read and display CO2 levels********\\\\\





            //***************Put code to check if door is open here***************\\\\\





            //************different fan speed paradigms can be included in the various cases below***********\\\\\\\\

            if (Override == false) // checks if manual mode has been engaged 
            {
                CurrentO2 = mcp3008.ReadADC(O2ADCChannel); // reads index from adc, this loop uses adc as primary logic comparitor
                if (CurrentO2 >= UpperO2Lim) //if statement that checks if  O2 is too high
                {
                    PinValue = pin.Read(); // reads level of solenoid pin
                    if (PinValue == GpioPinValue.High) // statment that keeps the valve closed
                    {
                        pin.Write(GpioPinValue.Low);
                        return;
                    }


                }
                if (CurrentO2 <= LowerO2Lim) // if statement that checks if O2 is too low
                {
                    PinValue = pin.Read(); // reads level of solenoid pin
                    if (PinValue == GpioPinValue.Low) // statement that keeps the valve open
                    {
                        pin.Write(GpioPinValue.High);
                        return;
                    }
                }
                CurrentO2 = ((CurrentO2 * factor) + Constant); //changes ADC index into linearized O2 value
                TextBlock1.Text = CurrentO2.ToString(); //changes/updates O2 textbox with O2 value 
            }
            else //if override is engaged, the following simply reads current O2, converts, and displays it to the UI
            {
                CurrentO2 = mcp3008.ReadADC(O2ADCChannel);
                CurrentO2 = ((CurrentO2 * factor) + Constant);
                TextBlock1.Text = CurrentO2.ToString();
                return;
            }
        }

        public void SensorDateProvider() //contructor method for sensors, current and future go here
        {
            mcp3008 = new MCP3008(ReferenceVoltage); //ADC construction for O2
            mhz16 = new MHZ16(); // contructor for CO2
            Pca9685 pca9685 = new Pca9685(); //contructor for PWM 


        }
        private void InitGPIO() //Turns on GPIO pins, add all additional pins to this method
        {
            var gpio = GpioController.GetDefault();

            if (gpio == null)
            {
                GpioStatus.Text = "There is no GPIO controller on this device."; //checks if GPIO was initialized correctly
                return;
            }
            pin = gpio.OpenPin(LED_PIN); //engergizes and connects to solenoid pin
            lightpin = gpio.OpenPin(Light_PIN); // energizes and connects to light pin



            pin.Write(GpioPinValue.Low); //defaults solenoid pin to being active
            pin.SetDriveMode(GpioPinDriveMode.Output);
            lightpin.Write(GpioPinValue.Low); // defaults light pin to being active
            lightpin.SetDriveMode(GpioPinDriveMode.Output);

            GpioStatus.Text = "GPIO pin initialized correctly."; //acts as rudementary UI notification

        }
        private void MainPage_Unloaded(object sender, object args)
        {
            pin.Dispose(); // resets pins between reboots

        }

        private void Button_Click(object sender, RoutedEventArgs e) // this method runs when "manuel N2 control" button is initialized
        {
            Override = true; // sets override to suspend feedback loop
            PinValue = pin.Read(); // the following just switches the solenoid pin between off and on
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
        //****make a more robust error/ UI notification for this ADC intialization*****\\\\



        {
            await mcp3008.Initialize(); //method that calls internal initialization method  from MCP3008 class
            if (mcp3008 == null)
            {
                TestBlock.Text = "this did not work";

            }
            else
            {
                TestBlock.Text = "This Did work";
            }

            ////**************write code that initializes MHZ16 from MHZ16 class*******************\\\\\\\\\\




        }
        private void EndOverride_Click(object sender, RoutedEventArgs e) //method that is called when "end override" button is pressed and resets flag
        {

            Override = false;

        }

        private void Button_Click_2(object sender, RoutedEventArgs e) // button that controls light when "Light" button is pressed
        {
            LightPinValue = lightpin.Read(); // switches relay from being active to non-active
            if (LightPinValue == GpioPinValue.High)
            {
                lightpin.Write(GpioPinValue.Low);
            }
            else if (LightPinValue == GpioPinValue.Low)
            {
                lightpin.Write(GpioPinValue.High);
            }

        }

        //////************ the following gives rudementary control for the fans when the "fan" button is pressed in the UI*****\\\\
        ///// do not press this until PWM has been initialized properly/\\\\\\\\\\\\\\\\

        private void Fan_Click(object sender, RoutedEventArgs e)
        {

            if (shiftflag == 1)
            {
                pca9685.SetPWMFrequency(0);
                shiftflag = 2;
            }
            if (shiftflag == 2)
            {
                pca9685.SetPWMFrequency(2000);
                shiftflag = 3;
            }
            if (shiftflag == 3)
            {
                pca9685.SetPWMFrequency(4000);
                shiftflag = 0;
            }
        }

       

        private void O2Upperraise_Click(object sender, RoutedEventArgs e)
        {
            UpperO2Lim = UpperO2Lim + 50;
            O2Upperlimitvalue.Text = (factor * UpperO2Lim + Constant).ToString();
        }
        private void O2Upperlower_Click(object sender, RoutedEventArgs e)
        {
            UpperO2Lim = UpperO2Lim - 50;
            O2Upperlimitvalue.Text = (factor * UpperO2Lim + Constant).ToString();
        }
        private void O2Lowerraise_Click(object sender, RoutedEventArgs e)
        {
            LowerO2Lim = LowerO2Lim + 50;
            O2Lowerlimitvalue.Text = (factor * LowerO2Lim + Constant).ToString();
        }
        private void O2Lowerlower_Click(object sender, RoutedEventArgs e)
        {
            LowerO2Lim = LowerO2Lim - 50;
            O2Lowerlimitvalue.Text = (factor *LowerO2Lim + Constant).ToString();
        }
    }

}





﻿#region Copyright (c) 2016, Roland Harrison
/* 
 * Copyright (c) 2016, Roland Harrison
 * roland.c.harrison@gmail.com
 * 
 * All rights reserved.
 * Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
 * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
 * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
 * Neither the name of the organization nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 * A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 * 
 */
#endregion
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using System.Threading;
using J2534DotNet;
using OBD;
using J2534DotNet.Logger;
using Plugins;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Threading;

namespace Sample
{


    public partial class SimpleTest : Form
    {
        FileFormatPluginLoader _pluginLoader;
        J2534Extended _passThru;
        UDSFord _comm;
        byte[] _rawBinaryFile;
        public SimpleTest()
        {
            InitializeComponent();
            _passThru = new J2534Extended();
            _pluginLoader = new FileFormatPluginLoader(Directory.GetCurrentDirectory());
        }


        private void CmdDetectDevicesClick(object sender, EventArgs e)
        {
            // Calling J2534.GetAvailableDevices() will return a list of installed J2534 devices
            List<J2534Device> availableJ2534Devices = J2534Detect.ListDevices();
            if (availableJ2534Devices.Count == 0)
            {
                MessageBox.Show("Could not find any installed J2534 devices.");
                return;
            }


            foreach (J2534Device device in availableJ2534Devices)
            {
                log.Text += device.Name + ", " + device.Vendor + "\r\n\r\n";
                log.Text += "\tConfig Application:\t" + device.ConfigApplication + "\r\n";
                log.Text += "\tFunction Library:\t" + device.FunctionLibrary + "\r\n\r\n";
                log.Text += "\tProtocol\t\tChannels\r\n";
                log.Text += "\tCAN\t\t" + device.CANChannels + "\r\n";
                log.Text += "\tISO15765\t" + device.ISO15765Channels + "\r\n";
                log.Text += "\tISO14230\t" + device.ISO14230Channels + "\r\n";
                log.Text += "\tISO9141\t\t" + device.ISO9141Channels + "\r\n";
                log.Text += "\tJ1850PWM\t" + device.J1850PWMChannels + "\r\n";
                log.Text += "\tJ1850PWM\t" + device.J1850VPWChannels + "\r\n";
                log.Text += "\tSCI_A_ENGINE\t" + device.SCI_A_ENGINEChannels + "\r\n";
                log.Text += "\tSCI_A_TRANS\t" + device.SCI_A_TRANSChannels + "\r\n";
                log.Text += "\tSCI_B_ENGINE\t" + device.SCI_B_ENGINEChannels + "\r\n";
                log.Text += "\tSCI_B_TRANS\t" + device.SCI_B_TRANSChannels + "\r\n\r\n";
            }
        }


        private void CmdReadVoltageClick(object sender, EventArgs e)
        {
            double voltage = 0;
            try
            {
                if (!Connect())
                {
                    return;
                }

                if (!_comm.GetBatteryVoltage(ref voltage))
                {
                    MessageBox.Show(String.Format("Error reading voltage.  Error: {0}", _comm.GetLastError()), "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
            }
            catch (OBDException obdEx)
            {
                MessageBox.Show("Error reading voltage due to OBD error: " + obdEx.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            catch (UDSException udsEx)
            {
                MessageBox.Show("Error reading voltage due to UDS error: " + udsEx.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            catch (J2534Exception j2534Ex)
            {
                MessageBox.Show("Error reading voltage due to J2534 error: " + j2534Ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unknown error occured whilst reading voltage: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            finally
            {
                Disconnect();
            }

            txtVoltage.Text = voltage + @" V";
        }

        private void ReadVinClick(object sender, EventArgs e)
        {
            string vin = "";

            try
            {
                if (!Connect()) return;

                vin = _comm.GetVin();

            }
            catch (OBDException obdEx)
            {
                MessageBox.Show("Error retrieving VIN due to OBD error: " + obdEx.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            catch (UDSException udsEx)
            {
                MessageBox.Show("Error retrieving VIN due to UDS error: " + udsEx.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            catch (J2534Exception j2534Ex)
            {
                MessageBox.Show("Error retrieving VIN due to J2534 error: " + j2534Ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            } catch (Exception ex)
            {
                MessageBox.Show("Unknown error occured whilst retrieving VIN: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            } finally
            {
                Disconnect();
            }

            txtReadVin.Text = vin;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show(Assembly.GetExecutingAssembly().Location);
        }


        bool Connect()
        {
            try {
                if (!LoadJ2534())
                {
                    MessageBox.Show("No J2534 USB cables appear to be connected to the PC.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    
                    return false;
                }
                _comm = new UDSFord(_passThru);
                _comm.ConnectISO15765();
                return true;
            }
            catch (J2534Exception ex)
            {
                if (ex.Error == J2534Err.ERR_DEVICE_NOT_CONNECTED || ex.Error == J2534Err.ERR_FAILED)
                {
                    MessageBox.Show("J2534 USB cable is not attached.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
                else if (ex.Error == J2534Err.ERR_TIMEOUT || ex.Error == J2534Err.ERR_BUFFER_EMPTY)
                {
                    MessageBox.Show("A J2534 USB cable was detected however there was no CANBUS response. Check the device is connected to a vehicle with the ignition on.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
            }
            catch (OBDException ex)
            {
                MessageBox.Show("A J2534 USB cable was detected however there was no response from the CANBUS. Check the device is connected to a vehicle with the ignition on.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            catch (Exception e)
            {
                MessageBox.Show("An unknown error occured whilst attempting to make an ISO15765 connection: " + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            return false;

        }

        bool LoadJ2534()
        {
            if(_passThru == null) _passThru = new J2534Extended();
            if (_passThru.IsLoaded) return true;
            J2534Device j2534Device;

            // Find all of the installed J2534 passthru devices
            List<J2534Device> availableJ2534Devices = J2534Detect.ListDevices();
            if (availableJ2534Devices.Count == 0)
            {
                MessageBox.Show("Could not find any installed J2534 devices in the Windows registry, have you installed the device drivers for your cable?");
                _passThru.FreeLibrary();
                return false;
            }

            if (autoDetectCheckBox.Checked)
            {
                foreach(var lib in availableJ2534Devices)
                {
                    if (Path.GetFileName(lib.FunctionLibrary).Contains("J2534DotNet.Logger.dll")) continue;
                    try {
                        j2534Device = new J2534Device();
                        if (!_passThru.LoadLibrary(lib))
                        {
                            j2534Device = null;
                            continue;
                        }

                        _comm = new UDSFord(_passThru);
                        _comm.Connect();

                        //if we get this far then we have successfully connected
                        _comm.Disconnect();
                        return true;
                        
                    } catch {
                        j2534Device = null;
                        continue;
                    }

                }
                _passThru.FreeLibrary();
                return false;

            }

            if (checkBoxLogJ2534.Checked)
            {
                j2534Device = new J2534Device();
                j2534Device.FunctionLibrary = System.IO.Directory.GetCurrentDirectory() + "\\" + "J2534DotNet.Logger.dll";
                Thread.Sleep(10);
                var loaded = _passThru.LoadLibrary(j2534Device);
                if(!loaded) _passThru.FreeLibrary();
                return loaded;
            }

            //If there is only one DLL to choose from then load it
            if (availableJ2534Devices.Count == 1)
            {
                return _passThru.LoadLibrary(availableJ2534Devices[0]);
            }
            else
            {
                var sd = new SelectDevice();
                if (sd.ShowDialog() == DialogResult.OK)
                {
                    j2534Device = sd.Device;
                    var loaded = _passThru.LoadLibrary(j2534Device);
                    if (!loaded) _passThru.FreeLibrary();
                    return loaded;
                }
            }

            return false;
        }

        void Disconnect()
        {
            if(_comm != null) _comm.Disconnect();
        }

        void UpdateLog(string text)
        {
            log.Text += text + Environment.NewLine;
        }



        BackgroundWorker backgroundWorker;
        ProgressForm progressForm;
        private void ReadFlash_Click(object sender, EventArgs e)
        {
            this.Enabled = false;

            progressForm = new ProgressForm("Reading PCM flash...");

            backgroundWorker = new BackgroundWorker();

            backgroundWorker.WorkerReportsProgress = true;

            backgroundWorker.DoWork += new DoWorkEventHandler(ReadFlash);
            backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(UpdateFlashReadProgress);
            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(FlashReadFinished);

            progressForm.Owner = (Form)this.Parent;
            progressForm.StartPosition = FormStartPosition.CenterScreen;

            backgroundWorker.RunWorkerAsync();
        }

        private void FlashWriteFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            progressForm.Close();
            this.Enabled = true;
        }

        private void FlashReadFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            progressForm.Close();
            this.Enabled = true;
            if(flashMemory != null) SaveFile(flashMemory);
        }

        private void UpdateFlashReadProgress(object sender, ProgressChangedEventArgs e)
        {
            string logData = e.UserState as String;
            if (!string.IsNullOrEmpty(logData)) UpdateLog(logData);

            if (progressForm != null) progressForm.UpdatePercentage(e.ProgressPercentage);
        }


        byte[] flashMemory;
        void ReadFlash(object sender, DoWorkEventArgs e)
        {

            try
            {
                if (!Connect())
                {
                    MessageBox.Show("Failed to connect to a J2534 device", "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    return;
                }
                backgroundWorker.ReportProgress(0, "Connected");

                float voltHigh = 15000;
                float voltLow = 1000;

                if(ignoreProrgammingVoltageCheckBox.Checked)
                {
                    voltHigh = -1;
                    voltLow = 100000;
                }
                //Ensure the programming voltage is 0v as the PCM needs to see a transition from 0 -> 18v
                float voltage = _comm.ReadProgrammingVoltage();
                backgroundWorker.ReportProgress(0, "ReadProgrammingVoltage = " + voltage + " mV");
                if (voltage > voltLow)
                {
                    voltage = _comm.PassThruSetProgrammingVoltage(PinNumber.PIN_13, PinVoltage.VOLTAGE_OFF);

                    backgroundWorker.ReportProgress(0, "SetProgrammingVoltage VOLTAGE_OFF");
                    backgroundWorker.ReportProgress(0, "ReadProgrammingVoltage = " + voltage + " mV");
                    if (voltage > voltLow)
                    {
                        MessageBox.Show("Failed to set programming voltage (pin 13) to 0 volts, measured: " + voltage + " mV");
                        return;
                    }
                }

                voltage = _comm.PassThruSetProgrammingVoltage(PinNumber.PIN_13, PinVoltage.FEPS_VOLTAGE);
                backgroundWorker.ReportProgress(0, "SetProgrammingVoltage 18000 mV");
                backgroundWorker.ReportProgress(0, "ReadProgrammingVoltage = " + voltage + " mV");
                if (voltage < voltHigh)
                {
                    MessageBox.Show("Failed to set programming voltage (pin 13) to 18 volts, measured: " + voltage + " mV");
                    return;
                }
                MessageBox.Show("Please turn the ignition off, wait 3 seconds, then turn it back on before pressing OK.");

                //Ensure the programming voltage is still high after an ignition cycle
                voltage = _comm.ReadProgrammingVoltage();
                backgroundWorker.ReportProgress(0, "ReadProgrammingVoltage = " + voltage + " mV");
                if (voltage < voltHigh)
                {
                    MessageBox.Show("Programming voltage did not persist after ignition power cycle), measured: " + voltage + " mV");
                    return;
                }


                //Enter level 1 seecurity mode
                _comm.SecurityAccess(0x01);
                backgroundWorker.ReportProgress(0, "Unlocked Controller");

                this.Invoke((MethodInvoker)delegate {
                    progressForm.Show();
                });
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                flashMemory = _comm.ReadFlashMemory(backgroundWorker);
                stopwatch.Stop();
                var timeTaken = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);

                backgroundWorker.ReportProgress(0, $"Successfully read {flashMemory.Length} bytes of flash memory in {timeTaken.Minutes}:{timeTaken.Seconds}");

                voltage = _comm.PassThruSetProgrammingVoltage(PinNumber.PIN_13, PinVoltage.VOLTAGE_OFF);
                backgroundWorker.ReportProgress(0, "SetProgrammingVoltage DISCONNECT");
                backgroundWorker.ReportProgress(0, "ReadProgrammingVoltage = " + voltage + " mV");
            }
            catch (OBDException obdEx)
            {
                MessageBox.Show("Error entering reading flash due to OBD error: " + obdEx.Message, "Error", MessageBoxButtons.OK,MessageBoxIcon.Asterisk);
            }
            catch (UDSException udsEx)
            {
                MessageBox.Show("Error entering reading flash due to UDS error: " + udsEx.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            catch (J2534Exception j2534Ex)
            {
                MessageBox.Show("Error entering security level due to J2534 error: " + j2534Ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unknown error occured whilst entering security level: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            finally
            {
                Disconnect();
                backgroundWorker.ReportProgress(0, "Disconnected");
            }
        }

        public byte[] ReadFlashMemoryTest(BackgroundWorker progressReporter = null)
        {
            byte[] flashMemory = new byte[0x100000];
            for (uint i = 0x0; i <= 0xFF800; i += 0x800)
            {
                Thread.Sleep(10);

                //Report progress back to the GUI if there is one
                if (progressReporter != null) progressReporter.ReportProgress((int)((float)i / (float)0xFF800 * 100.0f));

            }
            return flashMemory;
        }

        void SaveFile(byte [] rawBinaryFile)
        {
            SaveFileDialog savefile = new SaveFileDialog();

            savefile.Filter = "Binary File|*.bin";

            if (savefile.ShowDialog() != DialogResult.OK) return;
            
            try
            {
                File.WriteAllBytes(savefile.FileName, rawBinaryFile);

                MessageBox.Show("Successfully Saved File!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Saving File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
             
        }


        bool OpenFile()
        {
            _rawBinaryFile = new byte[0];
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = _pluginLoader.GetAllFormatsFilter();
            DialogResult result = openFileDialog.ShowDialog();
            if (result != DialogResult.OK) return false;
            var fileName = openFileDialog.FileName;

            string extension = Path.GetExtension(fileName).ToLowerInvariant().Replace(".", "");
            if (!File.Exists(fileName))
            {
                MessageBox.Show("File does not exist: " + fileName, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            Plugins.FileFormat fileFormatPlugin;

            if (_pluginLoader.TryGetFileFormat(extension, out fileFormatPlugin))
            {
                if (fileFormatPlugin.Open(fileName))
                {
                    if (fileFormatPlugin.TryReadBytes(out _rawBinaryFile))
                    {
                        if (_rawBinaryFile.Length == 1048576)
                        {
                            return true;
                        } else
                        {
                            MessageBox.Show($"Invalid file size, expected {1048576} bytes but the file was {_rawBinaryFile.Length } bytes", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                MessageBox.Show("Invalid file!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            } else
            {
                MessageBox.Show("Could not find a plugin to load this type of file!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Disconnect();
        }

        private void checkBoxLogJ2534_CheckStateChanged(object sender, EventArgs e)
        {

        }

        bool _toggle = false;
        private void setProgrammingVoltage(object sender, EventArgs e)
        {
            SetVoltage();
        }

        void SetVoltage(bool off = false)
        {
            try
            {
                if (!Connect()) return;
                UpdateLog("Connected");

                if (_comm == null) _comm = new UDSFord(_passThru);

                if (off)
                {
                    UpdateLog("setProgrammingVoltage(PinNumber.PIN_13, OFF)");
                    float programmingVoltage = _comm.PassThruSetProgrammingVoltage(PinNumber.PIN_13, PinVoltage.VOLTAGE_OFF);
                    UpdateLog("Voltage = : " + programmingVoltage);
                    _toggle = false;
                }
                else
                {
                    uint mvolts = 0;
                    if (!UInt32.TryParse(textBoxVolts.Text, out mvolts)) return;

                    UpdateLog("setProgrammingVoltage(PinNumber.PIN_13 " + mvolts + " mV");
                    float programmingVoltage = _comm.PassThruSetProgrammingVoltage(PinNumber.PIN_13, (PinVoltage)mvolts);
                    UpdateLog("Voltage = " + programmingVoltage + "V");
                    _toggle = true;
                }

            }
            catch (OBDException obdEx)
            {
                MessageBox.Show("Error setting voltage due to OBD error: " + obdEx.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            catch (UDSException udsEx)
            {
                MessageBox.Show("Error setting voltage due to UDS error: " + udsEx.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            catch (J2534Exception j2534Ex)
            {
                MessageBox.Show("Error setting voltage due to J2534 error: " + j2534Ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unknown error setting voltage: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            finally
            {
                Disconnect();
                UpdateLog("Disconnect");
            }
        }

        private void SetVoltage_Click(object sender, EventArgs e)
        {
            SetVoltage(true);
        }
        private void WriteFlash_Click(object sender, EventArgs e)
        {

            if (!OpenFile()) return;

            var result = MessageBox.Show("This will write a BF/FG Spanish Oak Binary to your PCM." + Environment.NewLine + Environment.NewLine +
            "This application is a private ALPHA and is not to be used commercially!" + Environment.NewLine + Environment.NewLine +
            "The binary is not parsed or verified and hence there is a real possibility of the PCM not working afterwards, if you require verification please wait until the public BETA release." + Environment.NewLine + Environment.NewLine +
            "Ensure you have a verified backup of your flash and another method to reflash the PCM if required!" + Environment.NewLine + Environment.NewLine +
            "It is highly recommended to NOT flash a vehicle that you require to drive afterwards!", "Are you sure you want to continue?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (!Connect())
            {
                MessageBox.Show("Failed to connect to a J2534 device", "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }

            if (result != DialogResult.Yes) return;

            this.Enabled = false;

            progressForm = new ProgressForm("");

            backgroundWorker = new BackgroundWorker();

            backgroundWorker.WorkerReportsProgress = true;

            backgroundWorker.DoWork += new DoWorkEventHandler(WriteFlash);
            backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(UpdateFlashReadProgress);


            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(FlashWriteFinished);

            progressForm.Owner = (Form)this.Parent;
            progressForm.StartPosition = FormStartPosition.CenterScreen;

            backgroundWorker.RunWorkerAsync();
        }



        private void WriteFlash(object sender, DoWorkEventArgs e)
        {
            try
            {


                backgroundWorker.ReportProgress(0, "Connected");

                float voltHigh = 15000;
                float voltLow = 1000;

                if (ignoreProrgammingVoltageCheckBox.Checked)
                {
                    voltHigh = -1;
                    voltLow = 100000;
                }
                //Ensure the programming voltage is 0v as the PCM needs to see a transition from 0 -> 18v
                float voltage = _comm.ReadProgrammingVoltage();
                backgroundWorker.ReportProgress(0, "ReadProgrammingVoltage = " + voltage + " mV");
                if (voltage > voltLow)
                {
                    voltage = _comm.PassThruSetProgrammingVoltage(PinNumber.PIN_13, PinVoltage.VOLTAGE_OFF);

                    backgroundWorker.ReportProgress(0, "SetProgrammingVoltage VOLTAGE_OFF");
                    backgroundWorker.ReportProgress(0, "ReadProgrammingVoltage = " + voltage + " mV");
                    if (voltage > voltLow)
                    {
                        MessageBox.Show("Failed to set programming voltage (pin 13) to 0 volts, measured: " + voltage + " mV");
                        return;
                    }
                }

                voltage = _comm.PassThruSetProgrammingVoltage(PinNumber.PIN_13, PinVoltage.FEPS_VOLTAGE);
                backgroundWorker.ReportProgress(0, "SetProgrammingVoltage 18000 mV");
                backgroundWorker.ReportProgress(0, "ReadProgrammingVoltage = " + voltage + " mV");
                if (voltage < voltHigh)
                {
                    MessageBox.Show("Failed to set programming voltage (pin 13) to 18 volts, measured: " + voltage + " mV");
                    return;
                }
                MessageBox.Show("Please turn the ignition off, wait 3 seconds, then turn it back on before pressing OK.");

                //Ensure the programming voltage is still high after an ignition cycle
                voltage = _comm.ReadProgrammingVoltage();
                backgroundWorker.ReportProgress(0, "ReadProgrammingVoltage = " + voltage + " mV");
                if (voltage < voltHigh)
                {
                    MessageBox.Show("Programming voltage did not persist after ignition power cycle), measured: " + voltage + " mV");
                    return;
                }

                //Enter level 1 seecurity mode
                _comm.SecurityAccess(0x01);
                backgroundWorker.ReportProgress(0, "Unlocked Controller");

                this.Invoke((MethodInvoker)delegate {
                    progressForm.Show();
                    progressForm.UpdateText("Erasing flash...");
                    progressForm.StartScroll();
                });
                _comm.EraseFlash();

                this.Invoke((MethodInvoker)delegate {
                    progressForm.UpdateText("Writing flash...");
                    progressForm.StopScroll();
                });
                _comm.RequestDownload();

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                _comm.WriteFlash(_rawBinaryFile, backgroundWorker);

                stopwatch.Stop();
                var timeTaken = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);

                backgroundWorker.ReportProgress(0, $"Successfully wrote {_rawBinaryFile.Length-0x10000} bytes of flash memory in {timeTaken.Minutes}:{timeTaken.Seconds}");
                _comm.RequestTransferExit();

                voltage = _comm.PassThruSetProgrammingVoltage(PinNumber.PIN_13, PinVoltage.VOLTAGE_OFF);
                backgroundWorker.ReportProgress(0, "SetProgrammingVoltage DISCONNECT");
                backgroundWorker.ReportProgress(0, "ReadProgrammingVoltage = " + voltage + " mV");
            }
            catch (OBDException obdEx)
            {
                MessageBox.Show("Error reading flash due to OBD error: " + obdEx.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            catch (UDSException udsEx)
            {
                MessageBox.Show("Error reading flash due to UDS error: " + udsEx.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            catch (J2534Exception j2534Ex)
            {
                MessageBox.Show("Error security level due to J2534 error: " + j2534Ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unknown error occured whilst entering security level: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            finally
            {
                Disconnect();
            }
        }

        private void ChangeDevice_Click(object sender, EventArgs e)
        {
            Disconnect();
            if (_passThru != null)
            {
                _passThru.FreeLibrary();
            }
            _passThru = null;
            _comm = null;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (!Connect())
            {
                MessageBox.Show("Failed to connect to a J2534 device", "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }

            _comm.Listen();
        }
    }
}

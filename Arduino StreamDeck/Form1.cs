#define WM_APPCOMMAND
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO.Ports;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.Json.Nodes;
using System;
using System.Diagnostics;
using System.Security.Policy;
using Microsoft.Win32;
using AudioSwitcher.AudioApi.CoreAudio;

namespace Arduino_StreamDeck
{
    public partial class Form1 : Form
    {
        private SerialPort serialPort;

        string json;
        JArray parsedJson;
        int scroll = -1;
        int clicked = 0;
        bool auto = false;

        public Form1()
        {
            InitializeComponent();
            FormClosed += Form_Closed;


            //SETUP PORTS
            string[] portNames = SerialPort.GetPortNames();
            foreach (string port in portNames)
            {
                if(port == "COM4")
                {
                    auto = true;
                }
                portbox.Items.Add(port);
            }
            serialPort = new SerialPort();
            serialPort.BaudRate = 9600;

            //SETUP APP ARRAY
            json = File.ReadAllText("json1.json");
            parsedJson = JArray.Parse(json);
            Console.WriteLine(parsedJson);


            try
            {
                foreach(var app in parsedJson)
                {
                    listBox1.Items.Add(app["name"]);
                }

                if (auto == true)
                {
                    
                    portbox.Text = "COM4";
                    serialPort.PortName = "COM4";
                    clicked++;
                    serialPort.Open(); //Connects to Arduino
                    serialPort.DataReceived += portDataReceived;
                    connectionLabel.ForeColor = Color.Green;
                    connectionLabel.Text = "Connected";
                    serialPort.WriteLine("$");
                    button1.Text = "Disconnect";
                    scroll = -1;
                }
            }
            catch(Exception ex) { }

        }


        private void Form_Closed(object sender, FormClosedEventArgs e)
        {
            try
            {
                serialPort.WriteLine("!");
                serialPort.Close();
            }
            catch (Exception ex){

            }
        }


        //Recieved Data
        private void portDataReceived(object sender, EventArgs args)
        {
            SerialPort port = sender as SerialPort;
            if (port == null)
            {
                Console.WriteLine("EMPTY DATA");
                return;
            }
            try
            {
                string data = port.ReadLine();
            
                Console.WriteLine(data);
                if (data.Contains(">"))
                {
                    Scroll(1);
                }
                else if (data.Contains("<"))
                {
                    Scroll(0);
                }else if (data.Contains("*"))
                {
                    Select(data);
                }
            }
            catch (Exception ex)
            {
            };

        }
        private void Select(String data)
        {
            try
            {
                {
                    //Starting Process Selected
                    var path = parsedJson[scroll]["path"].ToString();
                    Console.WriteLine(path);
                    Process process = new Process();
                    process.StartInfo.Verb = "runas";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.FileName = path;
                    process.Start();

                    //If Spotify Init Controls
                    //if (parsedJson[scroll]["name"].ToString() == "Spotify.exe")
                    //{
                    //    serialPort.WriteLine("#");//spotify controls command = #


                    //}
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void Scroll(int direction)
        {
            int length = parsedJson.Count - 1;
            Console.WriteLine(length);
            
            if (direction == 1)
            {
                if (scroll >= -1 && scroll < length)
                {
                        scroll++;
                        Console.WriteLine(scroll);
                        var appName = parsedJson[scroll]["name"];
                        serialPort.WriteLine(">" + appName);
                        Console.WriteLine(appName);
                    
                }
                
            }
            else
            {
                if (scroll != 0 && scroll != -1)
                {
                        scroll--;
                        Console.WriteLine(scroll);
                        var appName = parsedJson[scroll]["name"];
                        serialPort.WriteLine(">" + appName);
                        Console.WriteLine(appName);
                }
            }
        }

        //Connect to arduino
        private async void button1_Click(object sender, EventArgs e)
        {
            if (clicked == 0)
            {
                try
                {
                    serialPort.PortName = portbox.Text;
                    clicked++;
                    serialPort.Open(); //Connects to Arduino
                    serialPort.DataReceived += portDataReceived;
                    connectionLabel.ForeColor = Color.Green;
                    connectionLabel.Text = "Connected";
                    serialPort.WriteLine("$");
                    button1.Text = "Disconnect";
                    scroll = -1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            else
            {
                try
                {
                    clicked--;
                    serialPort.WriteLine("!");
                    connectionLabel.ForeColor = Color.Red;
                    connectionLabel.Text = "Disconnected";
                    serialPort.Close();
                    button1.Text = "Connect";
                    scroll = -1;
                }
                catch (Exception ex) 
                {
                    Console.WriteLine(ex);
                }
            }
        }
        //ADD AN APP
        private void button2_Click(object sender, EventArgs e)
        {
            var browser = new CommonOpenFileDialog();
            browser.IsFolderPicker = false;
            browser.Filters.Add(new CommonFileDialogFilter("Executable Files", "*.exe"));
            CommonFileDialogResult res = browser.ShowDialog();
            String name;
            try
            {
                if (res == CommonFileDialogResult.Ok)
                {

                    name = Path.GetFileName(browser.FileName);
                    MessageBox.Show("Added " + name + " to Apps");
                    var addItem = new JObject();
                    addItem["name"] = name;
                    addItem["path"] = browser.FileName;
                    parsedJson.Add(addItem);
                    listBox1.Items.Add(name);
                    var jsonToOutput = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
                    File.WriteAllText("json1.json", jsonToOutput);
                    Console.WriteLine(jsonToOutput);

                }
                else
                {
                    MessageBox.Show("No File Selected");
                }
            }
            catch(Exception ex) { Console.WriteLine(ex); }
}

        //REMOVE AN APP
        private void button3_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("Are you Sure?",
                      "Remove App", MessageBoxButtons.YesNo);
            switch (dr)
            {
                case DialogResult.Yes:
                    int count = 0;
                    foreach (var app in parsedJson.ToList())
                    {
                        Console.WriteLine(app);
                        if (app["name"].ToString() == listBox1.SelectedItem.ToString())
                        {
                            parsedJson[count].Remove();
                            listBox1.Items.RemoveAt(listBox1.SelectedIndex);
                            string updatedJson = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
                            File.WriteAllText("json1.json", updatedJson);
                            Console.WriteLine(updatedJson);

                            //Reload Arduino
                            scroll = -1;
                            Scroll(1);
                        }
                        count++;
                    }
                    break;
                case DialogResult.No:
                    break;
            }
            
        }
    }
}


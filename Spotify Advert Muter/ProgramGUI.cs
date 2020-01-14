using CSCore.CoreAudioAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Spotify_Advert_Muter
{
    public partial class ProgramGUI : Form
    {
        public StreamWriter writer = new StreamWriter(Application.UserAppDataPath + "\\ErrorLog.txt", true);
        public Stannieman.AudioPlayer.AudioPlayer audioplayer;
        public int songchangesafteradvert = -1;
        public AudioSessionControl Spotify = null;
        public AudioSessionControl SpotifyAdvertMuter = null;
        public bool displayed = false;
        public String songTitle = "";
        public String display = null;
        public bool toPause = false;
        public MethodInvoker invoker;     
        public Functions functions;
        public int cycles = 0;
        private bool allowshowdisplay = false;

        public ProgramGUI()
        {
            InitializeComponent();
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            functions = new Functions(this);
            timer1.Start();
            audioplayer = new Stannieman.AudioPlayer.AudioPlayer();
            string path = Application.UserAppDataPath + "\\censor_beep.mp3";
            File.WriteAllBytes(path, Properties.Resources.censor_beep);
            audioplayer.SetFileAsync(path, "1");
            this.Show();
            notifyIcon1.Visible = true;
        }

        public async void Execute()
        {
            Task<int> cycles = ToComplete();
            int result = await cycles;
        }

        public async Task<int> ToComplete()
        {
            timer1.Stop(); await Task.Delay(50); timer1.Start();

            // Find Spotify Advert Muter
            if (SpotifyAdvertMuter == null)
            {
                ThreadPool.QueueUserWorkItem(functions.FindSpotifyAdvertMuter);
                timer1.Stop(); await Task.Delay(1000); timer1.Start();
            }

            // Find Spotify- if we need to
            if (Spotify == null)
            {
                if (!displayed)
                { 
                    this.apprehendText("Searching for Spotify audio sessions..." + System.Environment.NewLine); this.refresh(); displayed = true; 
                }
                ThreadPool.QueueUserWorkItem(functions.FindSpotify);      
                timer1.Stop(); await Task.Delay(1000); timer1.Start();
            }
            // If we have it- Update GUI and initiate tests on the audio session
            else
            {
                // Reset displayed
                displayed = false;

                // Test to see if an advert is playing, and mute and alert/censor beep if so
                ThreadPool.QueueUserWorkItem(functions.TestforAdvert);
                if (display != null)
                {
                    setDisplay();
                }

                if (toPause)
                {
                    timer1.Stop(); await Task.Delay(1000); timer1.Start();
                    toPause = false;
                }

                // Un-mute Spotify after adverts
                ThreadPool.QueueUserWorkItem(functions.TestToUnmuteAsync);

                if (display != null)
                {
                    setDisplay();
                }

                // If it is the beginning of the program and an advert hasn't been detected, update the user with the song which is playing
                if (songchangesafteradvert < 0)
                {
                    ThreadPool.QueueUserWorkItem(functions.BeginningofProgram);

                    // Wait a 1/4 of a second
                    timer1.Stop(); await Task.Delay(250); timer1.Start();

                    // Display if neccessary
                    if (display != null)
                    {
                        setDisplay();
                    }
                }

                // If an advert was just played, update with the songs now playing
                if (songchangesafteradvert > 0)
                {
                    ThreadPool.QueueUserWorkItem(functions.TestForSongChange);

                    if (display != null)
                    {
                        setDisplay();
                    }
                    
                    // Wait 1/4 of a second before re-executing this method
                    timer1.Stop(); await Task.Delay(250); timer1.Start();
                }

                // Test to see if Spotify is paused at any point
                ThreadPool.QueueUserWorkItem(functions.TestforPaused);

                if (display != null)
                {
                    setDisplay();
                }
                if (toPause)
                {
                    timer1.Stop(); await Task.Delay(1000); timer1.Start();
                    toPause = false;
                }
            }
            return cycles++;
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(allowshowdisplay ? value : allowshowdisplay);
        }

        public async Task pauseAsync()
        {
            timer1.Stop(); await Task.Delay(500); timer1.Start();
        }

        public void setDisplay()
        {
            this.apprehendText(display + System.Environment.NewLine);
            this.refresh(); 
            display = null;
        }

        public void setDisplayVariable(String input)
        {
            this.display = input;
        }

        public void setDisplayBypass(String input2)
        {
            this.apprehendText(input2 + System.Environment.NewLine);
            this.refresh();
        }

        public void playAudio()
        {
            audioplayer.PlayAsync();
        }

        public void stopTimer()
        {
            timer1.Stop();
        }

        public void startTimer()
        {
            timer1.Start();
        }

        public void incrementSongsAfterAdvertChange()
        {
            songchangesafteradvert++;
        }

        public void setSongsAfterAdvertChange(int songchangesafteradvert)
        {
            this.songchangesafteradvert = songchangesafteradvert;
        }

        public int getSongChangesAfterAdvert()
        {
            return songchangesafteradvert;
        }

        public void setSongTitle(String songTitle)
        {
            this.songTitle = songTitle;
        }

        public string getSongTitle()
        {
            return songTitle;
        }

        public void setSpotify (AudioSessionControl Spotify)
        {
            this.Spotify = Spotify;
        }

        public void setSpotifyAdvertMuter(AudioSessionControl input)
        {
            SpotifyAdvertMuter = input;
        }

        public AudioSessionControl getSpotifyAdvertMuter()
        {
            return SpotifyAdvertMuter;
        }

        public void setToPause(bool toPause)
        {
            this.toPause = toPause;
        }

        public AudioSessionControl getSpotify()
        {
            return Spotify;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Execute();
        }

        public void volumeSlider2_Load(object sender, EventArgs e)
        {

        }

        public void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                notifyIcon1.Visible = true;
                this.Hide();
            }
        }

        public void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        public void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            allowshowdisplay = true;
            this.Visible = !this.Visible;
            this.Show();
        }

        public void toolStripComboBox1_Click(object sender, EventArgs e)
        {
            allowshowdisplay = true;
            this.Visible = !this.Visible;
            this.Show();
        }

        public void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            notifyIcon1.Visible = false;
            ThreadPool.QueueUserWorkItem(functions.unMute);
            Application.Exit();
            System.Environment.Exit(1);
        }

        public void Form1_Load(object sender, EventArgs e)
        {
        }

        public void richTextBox1_TextChanged(object sender, EventArgs e)
        {
        }

        public void apprehendText(String text)
        {
            if (ControlInvokeRequired(this.richTextBox1, () => apprehendText(text)))
            {
                return;
            }
            this.richTextBox1.AppendText(text);
        }

        public bool ControlInvokeRequired(Control c, Action a)
        {
            if (c.InvokeRequired)
            {
                c.Invoke(invoker = delegate { a(); });
            }
            else
            {
                return false;
            }

            return true;
        }

        public void refresh()
        {
            if (ControlInvokeRequired(this, () => refresh()))
            {
                return;
            }
            this.Refresh();
        }

        public void RaiseException(object sender, EventArgs e)
        {
            try
            {
                int i = int.Parse("Chris");
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        public void LogError(Exception ex)
        {
            string message = string.Format("Time: {0}", DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
            message += Environment.NewLine;
            message += "-----------------------------------------------------------";
            message += Environment.NewLine;
            message += string.Format("Message: {0}", ex.Message);
            message += Environment.NewLine;
            message += string.Format("StackTrace: {0}", ex.StackTrace);
            message += Environment.NewLine;
            message += string.Format("Source: {0}", ex.Source);
            message += Environment.NewLine;
            message += string.Format("TargetSite: {0}", ex.TargetSite.ToString());
            message += Environment.NewLine;
            message += "-----------------------------------------------------------";
            message += Environment.NewLine;
            writer.WriteLine(message);
            writer.Close();
        }
    }
}

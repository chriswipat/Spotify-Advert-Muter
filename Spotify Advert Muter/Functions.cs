using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CSCore.CoreAudioAPI;
using NUnit.Framework;
using Stannieman;
using NAudio;
using Nito.AsyncEx;
using System.Windows.Forms;
using System.Threading;

namespace Spotify_Advert_Muter
{
    public class Functions
    {
        public MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
        public Stannieman.AudioPlayer.AudioPlayer audioplayer;
        public AudioSessionEnumerator sessionEnumerator;
        public AudioSessionManager2 sessionManager;
        public AudioSessionControl2 sessionControl;
        public AudioSessionControl2 spotifyControl;
        public AudioSessionControl2 spotifyAdvertMuterControl;
        public AudioSessionControl Spotify;
        public SimpleAudioVolume spotifyVolume;
        public String songTitle;
        public string display = null;
        public ProgramGUI programGUI;
        public WaitCallback wait;
        public MMDevice MMDevice;
        public float meterinfo;


        public Functions(ProgramGUI programGUI)
        {
            this.programGUI = programGUI;
        }

        public void FindSpotify(Object stateInfo)
        {
            Boolean found = false;

            // If we don't have Spotify
            if (programGUI.getSpotify() == null)
            {                
                sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render);
                sessionEnumerator = sessionManager.GetSessionEnumerator();
                Assert.IsNotNull(sessionEnumerator);

                foreach (AudioSessionControl session in sessionEnumerator)
                {
                    if (session == null) { break; }
                    sessionControl = session.QueryInterface<AudioSessionControl2>();
                    if (sessionControl == null) { break; }
                    if (sessionControl.SessionIdentifier.Contains("Spotify.exe"))
                    {
                        spotifyControl = sessionControl;
                        programGUI.setSpotify(session);
                        found = true;
                        break;
                    }
                }
            }

            if (found)
            {
                programGUI.setDisplayBypass("Running...");
            }
            else
            {
                programGUI.setSpotify(null); 
                sessionControl = null; 
                programGUI.setDisplayVariable(null);
            }
        }

        public void FindSpotifyAdvertMuter(Object stateInfo)
        {
            Boolean found = false;

            // If we don't have Spotify
            if (programGUI.getSpotifyAdvertMuter() == null)
            {
                sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render);
                sessionEnumerator = sessionManager.GetSessionEnumerator();
                Assert.IsNotNull(sessionEnumerator);

                foreach (AudioSessionControl session in sessionEnumerator)
                {
                    if (session == null) { break; }
                    sessionControl = session.QueryInterface<AudioSessionControl2>();
                    if (sessionControl == null) { break; }
                    if (sessionControl.SessionIdentifier.Contains(System.IO.Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)))
                    {
                        spotifyAdvertMuterControl = sessionControl;
                        programGUI.setSpotifyAdvertMuter(session);
                        found = true;
                        break;
                    }
                }
            }

            if (found)
            {
                using var spotifyAdvertMuterVolume = spotifyAdvertMuterControl.QueryInterface<SimpleAudioVolume>();
                spotifyAdvertMuterVolume.MasterVolume = 0.25f;
            }

            else
            {
                programGUI.setSpotifyAdvertMuter(null);
                spotifyAdvertMuterControl = null;
            }
        }

        public void BeginningofProgram(Object stateInfo)
        {
            // If Spotify was closed, an advert is detected, or Spotify is paused, return
            if (ToBreak3()) 
            {
                return;
            }

            using var spotifyVolume = spotifyControl.QueryInterface<SimpleAudioVolume>();
            String songTitle = programGUI.getSongTitle();

            // Safety Check- make sure not to execute this after an advert
            if (!spotifyVolume.IsMuted)
            {
                // If Spotify was closed, an advert is detected, or Spotify is paused, return
                if (ToBreak3()) 
                {
                    return;
                }

                // Display song that is playing once
                if (!songTitle.Equals(spotifyControl.Process.MainWindowTitle))
                {
                    if (ToBreak3()) 
                    {
                        return;
                    }
                    else
                    {
                        programGUI.setSongTitle(spotifyControl.Process.MainWindowTitle);
                        display = "Now playing... - " + programGUI.getSongTitle() + System.Environment.NewLine;
                        programGUI.setDisplayVariable(display += "No adverts detected...");
                    }
                }

                // If Spotify was closed, an advert is detected, or Spotify is paused, return
                if (ToBreak3()) 
                {
                    return;
                }
            }
            else //If muted
            {
                return;
            }
        }

        public void TestforAdvert(Object stateInfo)
        {
            // Stop executing if Spotify was closed
            if (ToBreak()) 
            {
                return;
            }

            spotifyControl = programGUI.getSpotify().QueryInterface<AudioSessionControl2>();
            if (spotifyControl == null) { return; }

            // Make sure display is null
            programGUI.setDisplayVariable(null);            

            // If an advert is playing
            if (spotifyControl.Process.MainWindowTitle.Equals("Advertisement") || spotifyControl.Process.MainWindowTitle.Equals("Spotify"))
            {
                spotifyVolume = spotifyControl.QueryInterface<SimpleAudioVolume>();
                if (!spotifyVolume.IsMuted)
                {
                    spotifyVolume.IsMuted = true;
                }

                if (!programGUI.getSongTitle().Equals(spotifyControl.Process.MainWindowTitle))
                {
                    programGUI.setSongTitle(spotifyControl.Process.MainWindowTitle);
                    programGUI.setDisplayVariable("Advert detected...");
                    programGUI.playAudio();
                }
                programGUI.setToPause(true);
            }
        }

        public void TestforPaused(Object stateInfo)
        {
            spotifyControl = programGUI.getSpotify().QueryInterface<AudioSessionControl2>();

            if (spotifyControl.Process.MainWindowTitle.Equals("Spotify Free"))
            {
                if (!programGUI.getSongTitle().Equals(spotifyControl.Process.MainWindowTitle) && !ToBreak())
                {
                    programGUI.setSongTitle(spotifyControl.Process.MainWindowTitle);
                    programGUI.setDisplayVariable("Spotify is currently paused...");
                }
                programGUI.setToPause(true);
            }
        }

        public async void TestToUnmuteAsync(Object stateInfo)
        {
            // If Spotify was closed, return
            if (ToBreak()) 
            { 
                return; 
            }

            spotifyControl = programGUI.getSpotify().QueryInterface<AudioSessionControl2>();
            spotifyVolume = spotifyControl.QueryInterface<SimpleAudioVolume>();

            if (!spotifyControl.Process.MainWindowTitle.Equals("Advertisement") && !spotifyControl.Process.MainWindowTitle.Equals("Spotify"))
            {
                // If Spotify is muted
                if (spotifyVolume.IsMuted)
                {
                    // Save song name
                    programGUI.setSongTitle(spotifyControl.Process.MainWindowTitle);

                    // Wait for the presumed advert to completely stop playing
                    await programGUI.pauseAsync();

                    // Unmute the audio session
                    spotifyVolume.IsMuted = false;

                    // Display advert prevented & the song now playing
                    programGUI.setDisplayVariable("Advert prevented..." + System.Environment.NewLine + "Now playing... - " + programGUI.getSongTitle() + System.Environment.NewLine + "One song since an advert...");
                    programGUI.setSongsAfterAdvertChange(1);
                }

            }
        }

        public async void TestForSongChange(Object stateInfo)
        {
            String songTitle = programGUI.getSongTitle();

            // If an advert is playing or Spotify was closed, return
            if (ToBreak3()) { return; }

            // Write each time the song changes
            if (!songTitle.Equals(spotifyControl.Process.MainWindowTitle) && !ToBreak3())
            {
                programGUI.setSongTitle(spotifyControl.Process.MainWindowTitle);
                programGUI.incrementSongsAfterAdvertChange();
                programGUI.setDisplayVariable("Now playing... - " + programGUI.getSongTitle() + System.Environment.NewLine + isLessThan10(programGUI.getSongChangesAfterAdvert()) + " songs since an advert...");                
                programGUI.stopTimer(); await Task.Delay(250); programGUI.startTimer();
            }
        }

        public String isLessThan10(int input)
        {
            if (input == 2) { return "Two"; }
            if (input == 3) { return "Three"; }
            if (input == 4) { return "Four"; }
            if (input == 5) { return "Five"; }
            if (input == 6) { return "Six"; }
            if (input == 7) { return "Seven"; }
            if (input == 8) { return "Eight"; }
            if (input == 9) { return "Nine"; }
            return input.ToString();
        }


        public MMDevice GetDefaultRenderDevice()
        {
            MMDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, CSCore.CoreAudioAPI.Role.Console);
            return MMDevice;
        }

        public float Audio(MMDevice device)
        {
            using AudioMeterInformation meter = AudioMeterInformation.FromDevice(device);
            meterinfo = meter.PeakValue;
            return meterinfo;
        }

        public AudioSessionManager2 GetDefaultAudioSessionManager2(DataFlow dataFlow)
        {
            MMDevice = enumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia);
            Debug.WriteLine("DefaultDevice: " + MMDevice.FriendlyName);
            sessionManager = AudioSessionManager2.FromMMDevice(MMDevice);
            return sessionManager;
        }

        public void unMute(Object stateInfo)
        {
            // If Spotify was closed, return
            if (ToBreak()) { return; }

            using var spotifyVolume = spotifyControl.QueryInterface<SimpleAudioVolume>();

            // If Spotify is muted
            if (spotifyVolume.IsMuted)
            {
                spotifyVolume.IsMuted = false;
            }
        }

        // Break if one critera is true- if a Spotify audio session can no longer be found
        public bool ToBreak()
        {
            // If we can no longer find Spotify as an audio session, return true
            Boolean found = false;
            sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render);
            sessionEnumerator = sessionManager.GetSessionEnumerator();
            Assert.IsNotNull(sessionEnumerator);

            foreach (AudioSessionControl session in sessionEnumerator)
            {
                if (session == null) { break; }
                sessionControl = session.QueryInterface<AudioSessionControl2>();
                if (sessionControl == null) { break; }
                if (sessionControl.SessionIdentifier.Contains("Spotify.exe"))
                {
                    spotifyControl = sessionControl;
                    programGUI.setSpotify(session);
                    found = true;
                    break;
                }
            }

            if (!found) { programGUI.setSpotify(null); sessionControl = null; return true; }
            return false;
        }

        // Break if one of two criteria are true - if a Spotify audio session can no longer be found or an advert is not playing
        public bool ToBreak2()
        {
            // If we can no longer find Spotify as an audio session, return true
            if (ToBreak())
            {
                return true;
            }

            if (spotifyControl != null)
            {
                if (spotifyControl.Process.MainWindowTitle.Equals("Advertisement") || spotifyControl.Process.MainWindowTitle.Equals("Spotify"))
                {
                    return false;
                }
            }
            return true;
        }

        // Break if one of three criteria are true - if a Spotify audio session can no longer be found, an advert is playng, or Spotify has been paused
        public bool ToBreak3()
        {
            // If we can no longer find Spotify as an audio session, return true
            if (ToBreak())
            {
                return true;
            }

            if (spotifyControl.Process.MainWindowTitle.Equals("Advertisement") || spotifyControl.Process.MainWindowTitle.Equals("Spotify") || spotifyControl.Process.MainWindowTitle.Equals("Spotify Free"))
            {
                return true;
            }

            return false;
        }
    }
}

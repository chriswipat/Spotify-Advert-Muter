using System;
using System.Windows.Forms;

namespace Spotify_Advert_Muter
{
    static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ProgramGUI());
        }

    }
}

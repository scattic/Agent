using System;
using System.Windows.Forms;
using System.Threading;

namespace Agent
{
  public class UserLand : ApplicationContext
  {

    private static string old_title = "";
    private static string old_ip = "";
    private NotifyIcon trayIcon;
    private static Utils u = new Utils();

    /* ==================================================================================================
     * 
     */
    private void MonitorActiveWindow(Object state)
    {
      string tmp = u.GetActiveWindowTitleAndProcess();
      if (!tmp.Equals(old_title))
      {
        u.WriteEventLog(1, tmp);
        old_title = tmp;
      }
    }

    /* ==================================================================================================
     * 
     */
    private void MonitorPublicIP(Object state)
    {
      string tmp = u.GetPublicIP();
      if (!tmp.Equals(old_ip))
      {
        u.WriteEventLog(2, tmp);
        old_ip = tmp;
      }
    }

    /* ==================================================================================================
     * 
     */
    public UserLand()
    {
      trayIcon = new NotifyIcon()
      {
        Icon = (System.Drawing.Icon)Properties.Resources.ResourceManager.GetObject("icon"),
        ContextMenu = new ContextMenu(new MenuItem[] {
          new MenuItem("About...", About)
        }),
        Text = "Ready",
        Visible = true
      };

      // start thread to collect data
      System.Threading.Timer timer_maw = new System.Threading.Timer(MonitorActiveWindow, "check", 0, 500);            // every 500ms
      System.Threading.Timer timer_mpi = new System.Threading.Timer(MonitorPublicIP, "check", 750, 10 * 60 * 1000);   // every 10m
    }

    void About(object sender, EventArgs e)
    {
      MessageBox.Show("Agent\nVersion 1.0", "About...", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
  }
}

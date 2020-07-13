using System.Linq;
using System.ServiceProcess;
using System.Threading;

namespace Agent
{
  public partial class AgentSvc: ServiceBase
  {

    private Timer timer;
    private Utils utils;
    public AgentSvc()
    {
      base.EventLog.Source = Properties.Resources.ResourceManager.GetString("AgentName"); 
      base.ServiceName = Properties.Resources.ResourceManager.GetString("AgentName");
      InitializeComponent();
      utils = new Utils();
    }

    private void CheckScheduledTask(object state)
    {
      utils.CreateScheduledTask();
    }
    public void DoWork(string[] args)
    {
      timer = new Timer(new TimerCallback(CheckScheduledTask));
      timer.Change(1000 * 60 * 1, 0); // service will check once / minute
      
      utils.LogMsg("watchdog started");
      
      // we need this to prevent from exiting when running in user land
      if (args.Contains("-u")) { while (true) { Thread.Sleep(1000 * 30); } }
    }
    protected override void OnStart(string[] args)
    {
      DoWork(args);
      base.OnStart(args);
      
      utils.LogMsg("windows service started");
    }

    protected override void OnStop()
    {
      utils.LogMsg("windows service stopping");
      timer.Dispose();
    }
  }
}

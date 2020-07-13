Agent
=====

What is it?
-----------

A small agent which logs activity from User Space into Windows EventLog. Log entries can then be shipped to a SIEM by a collector such as WinLogBeat.
The agent has a basic persistence mechanism, as it can run both as a service and as a User Space application. When running as a service it will make sure 
that a Scheduled Task exists to start the Agent as a normal application every X minutes under the/any logged on user account. 

Syntax
------

**agent.exe [-i] [-s] [-u] [-q] [-p]**

-i : installs the application. Requires elevated access rights.

-s : runs as a service. Don't use this argument when starting normally (from User Space). 

-u : runs service code, but from the User Space. Use this for debugging purposes.

-p : spawns itself then quit, used when started by Task Scheduler

Source notes
------------

* Change Resources\AgentName will define: 
  * name of service (\<AgentName\>Svc)
  * EventLog source
  * name of ScheduledTask ("Launch \<AgentName\>\-\<SID\>")
  * name of executable file installed under \\Program Files

* Other configurable strings in Resources:
  * InstallFolder for the folder name under \\Program Files
  * ServiceDisplay: display name for service
  * ServiceDescription: description for service

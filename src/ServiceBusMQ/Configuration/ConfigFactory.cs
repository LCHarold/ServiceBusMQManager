#region File Information
/********************************************************************
  Project: ServiceBusMQ
  File:    ConfigFactory.cs
  Created: 2013-02-10

  Author(s):
    Daniel Halan

 (C) Copyright 2013 Ingenious Technology with Quality Sweden AB
     all rights reserved

********************************************************************/
#endregion

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace ServiceBusMQ.Configuration {
  internal class ConfigFactory {
    private string _configFile;

    private string _configFileV1;
    private string _configFileV2;
    private string _configFileV3;


    public ConfigFactory() {
      _configFileV1 = SbmqSystem.AppDataPath + @"\config1.dat";
      _configFileV2 = SbmqSystem.AppDataPath + @"\config2.dat";
      _configFile = _configFileV3 = SbmqSystem.AppDataPath + @"\config3.dat";
    }

    internal string ConfigFile { get { return _configFile; } }

    public SystemConfig3 Create() {
      SystemConfig3 cfg = null;

      if( File.Exists(_configFileV3) ) {
        try {
          cfg = JsonFile.Read<SystemConfig3>(_configFileV3);
        } catch { }
            
      } else if( File.Exists(_configFileV2) ) {
        SystemConfig2 cfg2 = null;
        try {
          cfg2 = JsonFile.Read<SystemConfig2>(_configFileV2);
        } catch { }
        
        cfg = MapConfig2ToConfig3(cfg2);

      } else if( File.Exists(_configFileV1) ) {
        bool loaded = false;
        SystemConfig1 cfg1 = null;
        try {
          cfg1 = JsonFile.Read<SystemConfig1>(_configFileV1);
          loaded = cfg1.Servers != null;
        } catch { }

        if( !loaded ) { // Old ConfigFile + Defaults fallback
          cfg1 = LoadConfig0As1();
        }

        var cfg2 = MapConfig1ToConfig2(cfg1);
        cfg = MapConfig2ToConfig3(cfg2);

        Store(cfg);
      } else  {

        var cfg2 = MapConfig1ToConfig2(LoadConfig0As1());
        cfg = MapConfig2ToConfig3(cfg2);
      }

      return cfg;
    }

    public void Store(SystemConfig config) {
      JsonFile.Write(_configFile, config);
    }

    private string[] ParseStringList(string name) {
      var c = ConfigurationManager.AppSettings[name];
      return c.IsValid() ? c.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries) : new string[0];
    }

    private SystemConfig2 MapConfig1ToConfig2(SystemConfig1 cfg1) {
      var cfg2 = new SystemConfig2();

      if( cfg1 != null ) {

        cfg2.Servers = cfg1.Servers.Select(s => new ServerConfig2() {
          MessageBus = s.MessageBus,
          MessageBusQueueType = s.MessageBusQueueType,
          MonitorInterval = s.MonitorInterval,
          Name = s.Name,
          MonitorQueues = ConvertMultipleQueueConfigToOne(s.WatchCommandQueues, s.WatchEventQueues, s.WatchMessageQueues, s.WatchErrorQueues)
        }).ToList();

        cfg2.ShowOnNewMessages = cfg1.ShowOnNewMessages;
        cfg2.CommandsAssemblyPaths = cfg1.CommandsAssemblyPaths;
        cfg2.CommandDefinition = cfg1.CommandDefinition;

        cfg2.VersionCheck = cfg1.VersionCheck;
        cfg2.StartCount = cfg1.StartCount;

        cfg2.MonitorServer = cfg1.MonitorServer;
      }

      return cfg2;
    }

    private SystemConfig3 MapConfig2ToConfig3(SystemConfig2 cfg2) {
      var cfg3 = new SystemConfig3();

      if( cfg2 != null ) {

        cfg3.Id = cfg2.Id;

        cfg3.Servers = cfg2.Servers.Select(s => new ServerConfig3() {
          Name = s.Name,
          MessageBus = s.MessageBus + " v3", // V2 only NServiceBus3 was available
          MessageBusQueueType = s.MessageBusQueueType,
          MonitorInterval = s.MonitorInterval,
          ConnectionSettings = new Dictionary<string, string> { { "server", s.Name } },
          MonitorQueues = s.MonitorQueues
        }).ToList();

        cfg3.ShowOnNewMessages = cfg2.ShowOnNewMessages;
        cfg3.CommandsAssemblyPaths = cfg2.CommandsAssemblyPaths;
        cfg3.CommandDefinition = cfg2.CommandDefinition;
        cfg3.CommandContentType = cfg2.CommandContentType;

        cfg3.VersionCheck = cfg2.VersionCheck;
        cfg3.StartCount = cfg2.StartCount;

        cfg3.MonitorServerName = cfg2.MonitorServer;
      }

      return cfg3;
    }


    private QueueConfig[] ConvertMultipleQueueConfigToOne(string[] commandQueues, string[] eventQueues, string[] msgQueues, string[] errorQueues) {

      List<QueueConfig> r = new List<QueueConfig>();
      foreach( string q in commandQueues )
        r.Add(new QueueConfig() { Name = q, Type = Model.QueueType.Command, Color = QueueColorManager.GetRandomAvailableColorAsInt() });

      foreach( string q in eventQueues )
        r.Add(new QueueConfig() { Name = q, Type = Model.QueueType.Event, Color = QueueColorManager.GetRandomAvailableColorAsInt() });

      foreach( string q in msgQueues )
        r.Add(new QueueConfig() { Name = q, Type = Model.QueueType.Message, Color = QueueColorManager.GetRandomAvailableColorAsInt() });

      foreach( string q in errorQueues )
        r.Add(new QueueConfig() { Name = q, Type = Model.QueueType.Error, Color = QueueColorManager.RED });

      return r.ToArray();
    }

    private SystemConfig1 LoadConfig0As1() {
      var appSett = ConfigurationManager.AppSettings;

      SystemConfig1 c = new SystemConfig1();
      c.MonitorServer = !string.IsNullOrEmpty(appSett["server"]) ? appSett["server"] : Environment.MachineName;

      c.Servers = new List<ServerConfig>();
      c.Servers.Add(new ServerConfig() { Name = c.MonitorServer });

      c.CurrentServer.MessageBus = appSett["messageBus"] ?? "NServiceBus";
      c.CurrentServer.MessageBusQueueType = appSett["messageBusQueueType"] ?? "MSMQ";

      c.CurrentServer.WatchEventQueues = ParseStringList("event.queues");
      c.CurrentServer.WatchCommandQueues = ParseStringList("command.queues");
      c.CurrentServer.WatchMessageQueues = ParseStringList("message.queues");
      c.CurrentServer.WatchErrorQueues = ParseStringList("error.queues");

      c.CurrentServer.MonitorInterval = Convert.ToInt32(appSett["interval"] ?? "750");

      c.ShowOnNewMessages = Convert.ToBoolean(appSett["showOnNewMessages"] ?? "true");

      c.CommandsAssemblyPaths = ParseStringList("commandsAssemblyPath");

      return c;
    }

  }
}

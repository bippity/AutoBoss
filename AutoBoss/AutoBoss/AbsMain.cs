﻿using System;
using System.IO;
using System.Collections.Generic;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using System.Reflection;

namespace AutoBoss
{
    [ApiVersion(2, 1)]
    public class AutoBoss : TerrariaPlugin
    {
        public static AbsTools Tools;
        public static BossTimer Timers;
        public static Config config = new Config();

        public static Dictionary<int, int> bossList = new Dictionary<int, int>();
        public static Dictionary<string, int> bossCounts = new Dictionary<string, int>();

        public static readonly List<Region> ActiveArenas = new List<Region>();

        private bool dayTime;

        #region TerrariaPlugin

        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public override string Author
        {
            get { return "WhiteX"; }
        }

        public override string Description
        {
            get { return "Automatic boss spawner"; }
        }

        public override string Name
        {
            get { return "AutoBoss+"; }
        }

        public override void Initialize()
        {
            Tools = new AbsTools();

            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreet);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
            }
            base.Dispose(disposing);
        }

        public AutoBoss(Main game)
            : base(game)
        {
        }

        #endregion

        #region OnInitialize

        private static void OnInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command("boss.root", BossCommands.BossCommand, "boss")
            {
                HelpText =
                    "Toggles automatic boss spawns; Reloads the configuration; Lists bosses and minions spawned by the plugin"
            });

            var configPath = Path.Combine(TShock.SavePath, "BossConfig.json");
            (config = Config.Read(configPath)).Write(configPath);

            Timers = new BossTimer();
        }

        #endregion

        #region NetGreetPlayer

        private static void OnGreet(GreetPlayerEventArgs args)
        {
            if (TShock.Players[args.Who] != null)
            {
                if (config.AutoStartEnabled && TShock.Utils.ActivePlayers() == 1)
                {
                    Tools.ReloadConfig(true);
                    var day = config.BossToggles[BattleType.Day];
                    var night = config.BossToggles[BattleType.Night];
                    var special = config.BossToggles[BattleType.Special];
                    Tools.bossesToggled = true;
                    Timers.StartBosses(day, night, special, true);

                    TShock.Log.ConsoleInfo("[AutoBoss+] Timer started: Autostart");
                }
            }
        }

        #endregion

        private void OnGetData(GetDataEventArgs args)
        {
            //Restart boss timers if time is set
            if (args.MsgID != PacketTypes.TimeSet)
            {
                return;
            }

            bool day = config.BossToggles[BattleType.Day];
            bool night = config.BossToggles[BattleType.Night];
            bool special = config.BossToggles[BattleType.Special];

            Timers.StartBosses(day, night, special);
        }

        private void OnUpdate(EventArgs args)
        {
            //Run one wave if time has changed from day -> night
            if (dayTime != Main.dayTime)
            {
                dayTime = Main.dayTime;

                if (config.OneWave && !Tools.bossesToggled)
                {
                    bool day = config.BossToggles[BattleType.Day];
                    bool night = config.BossToggles[BattleType.Night];
                    bool special = config.BossToggles[BattleType.Special];

                    Timers.StartBosses(day, night, special);
                }
            }
        }

        #region OnLeave

        private void OnLeave(LeaveEventArgs args)
        {
            if (TShock.Utils.ActivePlayers() == 1)
            {
                foreach (var pair in bossList)
                {
                    if (pair.Value > 0 && pair.Value < Main.maxNPCs)
                    {
                        TSPlayer.Server.StrikeNPC(pair.Value, 9999, 1f, 1);
                    }
                }
                bossList.Clear();
                bossCounts.Clear();
                Timers.Stop();

                TShock.Log.ConsoleInfo("[AutoBoss+] Timer Disabled: No players are online");
            }
        }

        #endregion
    }
}
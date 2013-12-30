﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Terraria;
using TerrariaApi;
using TerrariaApi.Server;

using TShockAPI;
using TShockAPI.DB;

namespace Auto_Boss
{
    public class Boss_Tools
    {
        public static Boss_Config boss_Config { get; set; }
        public static string config_Path { get { return Path.Combine(TShock.SavePath, "Boss_Config.json"); } }

        public static string invalid_Regions = "";

        public static List<NPC> boss_List = new List<NPC>();
        public static List<NPC> minion_List = new List<NPC>();

        public static List<Region> Active_Arenas = new List<Region>();
        public static int arena_Count = 0;

        public static bool Bosses_Toggled = false;

        #region Setup_Config
        public static bool SetupConfig(bool commandRun = false, TSPlayer receiver = null)
        {
            bool success = false;
            List<string> commandExceptions = new List<string>();
            List<string> logExceptions = new List<string>();
            List<string> inactiveArenas = new List<string>();

            try
            {
                if (File.Exists(config_Path))
                {
                    boss_Config = Boss_Config.Read(config_Path);

                    foreach (Toggle_Obj t in boss_Config.Boss_Toggles)
                    {
                        if (t.type == "day" && t.enabled)
                            Boss_Timer.dayBossEnabled = true;
                        if (t.type == "night" && t.enabled)
                            Boss_Timer.nightBossEnabled = true;
                        if (t.type == "special" && t.enabled)
                            Boss_Timer.specialBossEnabled = true;
                    }

                    if (commandRun)
                    {
                        foreach (KeyValuePair<string, bool> pair in boss_Config.Boss_Arenas)
                        {
                            if (pair.Value == true)
                            {
                                if (TShock.Regions.GetRegionByName(pair.Key) != null)
                                {
                                    if (!Active_Arenas.Contains(TShock.Regions.GetRegionByName(pair.Key)))
                                        Active_Arenas.Add(TShock.Regions.GetRegionByName(pair.Key));

                                    success = true;
                                }

                                else
                                {
                                    invalid_Regions += (invalid_Regions.Length > 0 ? ", " : "") + pair.Key;

                                    commandExceptions.Add("Invalid Regions: " + invalid_Regions);

                                    invalid_Regions = string.Empty;

                                    success = false;
                                }
                            }
                            else
                            {
                                if (!inactiveArenas.Contains(pair.Key))
                                    inactiveArenas.Add(pair.Key);
                            }
                        }
                    }
                }
                else { boss_Config.Write(config_Path); }
            }
            catch (Exception ex)
            {
                if (!commandRun)
                    logExceptions.Add("Configuration error");
                else
                    commandExceptions.Add("Configuration error; Check logs for details");

                logExceptions.Add(ex.ToString());

                success = false;
            }

            if (success == false)
            {
                if (commandRun)
                {
                    SendMultipleErrors(false, receiver, commandExceptions);
                    receiver.SendErrorMessage("Reload failed: No active arenas");
                    receiver.SendErrorMessage("You have " + inactiveArenas.Count + " inactive arenas: {0}",
                        string.Join(", ", inactiveArenas));
                }

                if (!commandRun)
                    SendMultipleErrors(true, null, logExceptions);
            }

            return success;
        }
        #endregion

        #region PostInitialize
        public static void PostInitialize(EventArgs args)
        {
            bool failed = false;

            List<string> logExceptions = new List<string>();

            foreach (KeyValuePair<string, bool> pair in boss_Config.Boss_Arenas)
            {
                if (pair.Value == true)
                {
                    if (TShock.Regions.GetRegionByName(pair.Key) != null)
                    {
                        if (!Active_Arenas.Contains(TShock.Regions.GetRegionByName(pair.Key)))
                            Active_Arenas.Add(TShock.Regions.GetRegionByName(pair.Key));

                        failed = false;
                    }

                    else
                    {
                        invalid_Regions += (invalid_Regions.Length > 0 ? ", " : "") + pair.Key;

                        logExceptions.Add("Invalid Regions: " + invalid_Regions);

                        invalid_Regions = string.Empty;

                        failed = true;
                    }
                }
            }

            arena_Count = Active_Arenas.Count;

            if (arena_Count > 0)
                Bosses_Toggled = boss_Config.AutoStart_Enabled;

            if (arena_Count == 0)
                logExceptions.Add("No arenas defined");

            if (!failed)
                Log.ConsoleInfo("[AutoBoss+] Initialized successfully");
            else
                SendMultipleErrors(true, null, logExceptions);
        }
        #endregion

        #region Boss&Minion slayers
        public static void Kill_Bosses()
        {
            foreach (NPC boss in boss_List)
            {
                if (boss.active)
                    TSPlayer.Server.StrikeNPC(boss.type, boss.lifeMax * boss.defense, 1f, 1);
                else
                    TSPlayer.Server.StrikeNPC(boss.type, boss.lifeMax * boss.defense, 1f, 1);
                boss_List.Remove(boss);
            }
            boss_List.Clear();
        }

        public static void Kill_Minions()
        {
            foreach (NPC minion in minion_List)
            {
                if (minion.active)
                    TSPlayer.Server.StrikeNPC(minion.type, minion.lifeMax * minion.defense, 1f, 1);
                else
                    TSPlayer.Server.StrikeNPC(minion.type, minion.lifeMax * minion.defense, 1f, 1);
                minion_List.Remove(minion);
            }

            minion_List.Clear();
        }
        #endregion
        
        #region MultipleError
        public static void SendMultipleErrors(bool console = false, TSPlayer receiver = null, List<string> errors = null)
        {
            if (errors.Count > 1)
            {
                if (console)
                    TSServerPlayer.Server.SendErrorMessage("Multiple errors encountered: '{0}'", string.Join("', '", errors));
                else
                {
                    receiver.SendErrorMessage("Multiple errors found on reloading:");
                    receiver.SendErrorMessage("{0}", string.Join(", ", errors));
                }
            }
            else if (errors.Count == 1)
            {
                if (console)
                    TSServerPlayer.Server.SendErrorMessage("Error encountered: '{0}'", string.Join("", errors));
                else
                    receiver.SendErrorMessage("Error encountered on reloading: '{0}'", string.Join("", errors));
            }
              
        }
        #endregion
    }
}
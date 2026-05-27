using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using System.Diagnostics;

namespace Nereid
{
   namespace FinalFrontier
   {
      static class Persistence
      {
         private static readonly String ROOT_PATH = Utils.GetRootPath();
         private static readonly String SAVE_BASE_FOLDER = ROOT_PATH + "/saves/"; // suggestion/hint from Cydonian Monk
         private const String FILE_NAME = "halloffame.ksp";

         private const String PERSISTENCE_NODE_ENTRY_NAME = "ENTRY";

         /***************************************************************************************************************
          * new persistence model
          ***************************************************************************************************************/

         public static void SaveHallOfFame(List<LogbookEntry> logbook, ConfigNode node)
         {
            Log.Info("saving hall of fame (" + logbook.Count + " logbook entries)");

            List<LogbookEntry> logbookCopy = new List<LogbookEntry>(logbook);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            try
            {
               foreach (LogbookEntry entry in logbookCopy)
               {
                  ConfigNode entryNode = new ConfigNode(PERSISTENCE_NODE_ENTRY_NAME);
                  if (Log.IsLogable(Log.LEVEL.DETAIL)) Log.Detail("saving logbook entry " + entry);
                  entryNode.AddValue(Constants.CONFIGNODE_KEY_TIME, entry.UniversalTime.ToString());
                  entryNode.AddValue(Constants.CONFIGNODE_KEY_NAME, entry.Name);
                  entryNode.AddValue(Constants.CONFIGNODE_KEY_CODE, entry.Code);
                  entryNode.AddValue(Constants.CONFIGNODE_KEY_DATA, entry.Data);
                  entryNode.AddValue(Constants.CONFIGNODE_KEY_PLAYER, entry.Player);
                  entryNode.AddValue(Constants.CONFIGNODE_KEY_WALLTIME, entry.WallTime.ToString());
                  entryNode.AddValue(Constants.CONFIGNODE_KEY_TYPE, entry.EntryType);
                  node.AddNode(entryNode);
               }
            }
            catch
            {
               Log.Error("exception while saving hall of fame detected; hall of fame may be corrupt");
            }
            finally
            {
               sw.Stop();
               Log.Info("hall of fame saved in " + sw.ElapsedMilliseconds + "ms");
            }
         }


         /// <summary>
         /// Check if an entry should be filtered out (junk from test missions, etc)
         /// </summary>
         private static bool IsJunkEntry(double time, String code, String name, String player)
         {
            // Filter out known junk entries from test missions
            // These were accumulated from non-server test/debug saves
            
            // List of known junk player/time combinations to reject
            // Format: player name suffixes that indicate test missions
            String[] testPlayers = new[] { "Evan" };
            
            // If player matches a test player name AND time is in test range, it's junk
            if (!string.IsNullOrEmpty(player))
            {
               foreach (String testPlayer in testPlayers)
               {
                  if (player.Contains(testPlayer))
                  {
                     // Check if time looks like a test mission (e.g., very large met values)
                     // Test missions often have huge accumulated time values
                     // Server missions are more recent and have reasonable met values
                     if (time > 2378500000)  // Arbitrary threshold for test mission timestamps
                     {
                        Log.Detail("Filtering junk entry: time=" + time + " code=" + code + " player=" + player + " name=" + name);
                        return true;
                     }
                  }
               }
            }

            return false;
         }

         public static List<LogbookEntry> LoadHallOfFame(ConfigNode node)
         {
            Log.Info("loading hall of fame");

            // create a temporary logbook
            // unnecessary in the current concept, but kept to keep changes at a minimum
            List<LogbookEntry> logbook = new List<LogbookEntry>();

            if(node==null)
            {
               Log.Warning("no config node found. hall of fame will not load");
               return logbook;
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();

            int junkCount = 0;

            try
            {
               foreach (ConfigNode childNode in node.GetNodes())
               {
                  if (Log.IsLogable(Log.LEVEL.TRACE)) Log.Trace("child node found: " + childNode.name);
                  String sTime = childNode.GetValue(Constants.CONFIGNODE_KEY_TIME);
                  String code = childNode.GetValue(Constants.CONFIGNODE_KEY_CODE);
                  String name = childNode.GetValue(Constants.CONFIGNODE_KEY_NAME);
                  String data = childNode.GetValue(Constants.CONFIGNODE_KEY_DATA);
                  String player = childNode.GetValue(Constants.CONFIGNODE_KEY_PLAYER);
                  String sWall = childNode.GetValue(Constants.CONFIGNODE_KEY_WALLTIME);
                  String type = childNode.GetValue(Constants.CONFIGNODE_KEY_TYPE);

                  try
                  {
                     double time = Double.Parse(sTime);
                     
                     // Filter out junk entries from test missions
                     if (IsJunkEntry(time, code, name, player))
                     {
                        junkCount++;
                        continue;
                     }
                     
                     long wallTime = 0;
                     if (!string.IsNullOrEmpty(sWall)) long.TryParse(sWall, out wallTime);
                     logbook.Add(new LogbookEntry(time, code, name, data ?? "",
                        player ?? "", wallTime, type ?? ""));
                  }
                  catch
                  {
                     Log.Error("corrupt data in child node");
                  }
               }
               
               if (junkCount > 0)
               {
                  Log.Info("Filtered out " + junkCount + " junk entries during hall of fame load");
               }
               
               return logbook;
            }
            catch
            {
               Log.Error("exception while loading hall of fame detected; hall of fame may be corrupt");
               return logbook;
            }
            finally
            {
               sw.Stop();
               Log.Info("hall of fame loaded in " + sw.ElapsedMilliseconds + "ms");
            }

         }

         public static String[] GetSaveGameFolders()
         {
            return Directory.GetDirectories(SAVE_BASE_FOLDER);
         }

         /***************************************************************************************************************
          * Shadow logbook — local persistent backup that survives LMP scenario overwrites
          ***************************************************************************************************************/

         private static readonly String SHADOW_DIR = ROOT_PATH + "/PluginData/FinalFrontier";
         private static readonly String SHADOW_PATH = SHADOW_DIR + "/shadow.cfg";
         private const String SHADOW_ROOT_NODE = "FF_SHADOW_LOG";

         public static void SaveShadowLog(List<LogbookEntry> logbook)
         {
            try
            {
               Directory.CreateDirectory(SHADOW_DIR);
               ConfigNode root = new ConfigNode(SHADOW_ROOT_NODE);
               SaveHallOfFame(logbook, root);
               ConfigNode wrapper = new ConfigNode();
               wrapper.AddNode(root);
               wrapper.Save(SHADOW_PATH);
               Log.Info("shadow log saved (" + logbook.Count + " entries)");
            }
            catch (Exception e)
            {
               Log.Error("failed to save shadow log: " + e.Message);
            }
         }

         public static List<LogbookEntry> LoadShadowLog()
         {
            try
            {
               if (!File.Exists(SHADOW_PATH))
               {
                  Log.Info("no shadow log found at " + SHADOW_PATH);
                  return new List<LogbookEntry>();
               }
               ConfigNode wrapper = ConfigNode.Load(SHADOW_PATH);
               if (wrapper == null) return new List<LogbookEntry>();
               ConfigNode root = wrapper.GetNode(SHADOW_ROOT_NODE);
               if (root == null) return new List<LogbookEntry>();
               List<LogbookEntry> result = LoadHallOfFame(root);
               Log.Info("shadow log loaded (" + result.Count + " entries)");
               return result;
            }
            catch (Exception e)
            {
               Log.Error("failed to load shadow log: " + e.Message);
               return new List<LogbookEntry>();
            }
         }

         /***************************************************************************************************************
          * debugging
          ***************************************************************************************************************/

         /*
          * This method is called for testign purposes only. It should never be called in a public release
          */
         public static void WriteSupersedeChain(Pool<Ribbon> ribbons)
         {
            StreamWriter file = File.CreateText(ROOT_PATH+"/GameData/Nereid/FinalFrontier/supersede.txt");
            List<Ribbon> sorted = new List<Ribbon>(ribbons);
            sorted.Sort(delegate(Ribbon left, Ribbon right)
            {
               return left.GetCode().CompareTo(right.GetCode());
            });
            try
            {
               foreach (Ribbon ribbon in sorted)
               {
                  String code = ribbon.GetCode().PadRight(20);
                  Ribbon supersede = ribbon.SupersedeRibbon();
                  file.WriteLine(code+(supersede!=null?supersede.GetCode():""));
               }
            }
            finally
            {
               file.Close();
            }
         }
      }
   }
}
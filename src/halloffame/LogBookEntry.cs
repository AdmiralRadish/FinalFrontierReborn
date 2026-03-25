using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nereid
{
   namespace FinalFrontier
   {
      public class LogbookEntry
      {
         // serializion
         public static readonly Char TEXT_DELIM = '~';
         private static readonly Char[] TEXT_SEPARATORS = new Char[] { TEXT_DELIM };
         private static readonly Char[] FIELD_SEPARATORS = new Char[] { ' ' };

         // Entry types: normal award/action, or explicit revocation
         public const String TYPE_AWARD = "AWARD";
         public const String TYPE_REVOKE = "REVOKE";

         public double UniversalTime { get; set; }
         public String Code { get; set; }
         public String Name { get; set; }
         public String Data { get; set; }
         // Who created this entry (system username — unique per player machine)
         public String Player { get; set; }
         // Real-world timestamp (Unix ms) for tie-breaking across machines
         public long WallTime { get; set; }
         // AWARD (default) or REVOKE
         public String EntryType { get; set; }

         private static readonly long EPOCH_TICKS = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc).Ticks;
         private static long NowUnixMs()
         {
            return (System.DateTime.UtcNow.Ticks - EPOCH_TICKS) / System.TimeSpan.TicksPerMillisecond;
         }

         public static String LocalPlayer()
         {
            return System.Environment.UserName ?? "Unknown";
         }

         public bool IsRevoke { get { return TYPE_REVOKE.Equals(EntryType, System.StringComparison.OrdinalIgnoreCase); } }

         public LogbookEntry(double time, String code, String name, String text = "")
            : this(time, code, name, text, LocalPlayer(), NowUnixMs(), TYPE_AWARD) { }

         public LogbookEntry(double time, String code, String name, String text,
            String player, long wallTime, String entryType)
         {
            this.UniversalTime = time;
            this.Code = code;
            this.Name = name!=null?name:"";
            this.Data = text;
            this.Player = player ?? LocalPlayer();
            this.WallTime = wallTime > 0 ? wallTime : NowUnixMs();
            this.EntryType = string.IsNullOrEmpty(entryType) ? TYPE_AWARD : entryType;
            //
            if (Name.Contains(TEXT_DELIM))
            {
               Log.Error("name field contains invalid character '" + TEXT_DELIM + "': " + Name);
               Name.Replace(TEXT_DELIM, '_');
            }
         }

         public override String ToString()
         {
            String timestamp = Utils.TimeAsString(UniversalTime) + ": ";
            Action action = ActionPool.Instance().GetActionForCode(Code);
            if(action!=null)
            {
               return timestamp + action.CreateLogBookEntry(this);
            }

            Ribbon ribbon = RibbonPool.Instance().GetRibbonForCode(Code);
            if (ribbon != null)
            {
               Achievement achievement = ribbon.GetAchievement();
               return timestamp + achievement.CreateLogBookEntry(this);
            }

            return "unknown logbook entry (code " + Code + ")";
         }

         public String AsString()
         {
            return Utils.ConvertToKerbinTime(UniversalTime) + ": " + Name + " " + Code;
         }

         public String Serialize()
         {
            String line = UniversalTime.ToString() + " " + Code + " " + Name;
            if (Data != null && Data.Length > 0) line = line + TEXT_DELIM + Data;
            return line;
         }

         public static LogbookEntry Deserialize(String line)
         {
            String[] field = line.Split(FIELD_SEPARATORS, 3);
            if (field.Length == 3)
            {
               double time = double.Parse(field[0]);
               String code = field[1];
               String name = field[2];
               String text = "";
               if (name.Contains(TEXT_DELIM))
               {
                  String[] subfields = field[2].Split(TEXT_SEPARATORS, 2);
                  name = subfields[0];
                  text = (subfields.Length == 2) ? subfields[1] : "";
               }
               return new LogbookEntry(time, code, name, text);
            }
            else
            {
               Log.Warning("invalid logbook entry: " + line );
            }
            return null;
         }
      }
   }
}
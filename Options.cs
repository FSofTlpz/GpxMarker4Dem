using System;
using System.Collections.Generic;

namespace GpxMarker4Dem {

   /// <summary>
   /// Optionen und Argumente werden zweckmäßigerweise in eine (programmabhängige) Klasse gekapselt.
   /// Erzeugen des Objektes und Evaluate() sollten in einem try-catch-Block erfolgen.
   /// </summary>
   public class Options {

      // alle Optionen sind i.A. 'read-only'

      /// <summary>
      /// Name der Input-Datei
      /// </summary>
      public string Input { get; private set; }

      /// <summary>
      /// Pfad für die Ausgabedateien
      /// </summary>
      public string OutPath { get; private set; }

      /// <summary>
      /// Symbolname für die GPX-Datei
      /// </summary>
      public string Symbolname { get; private set; }

      /// <summary>
      /// Nummern der gewünschten Zoomlevel
      /// </summary>
      public List<int> Zoomlevel { get; private set; }

      /// <summary>
      /// Ausgabeziel ev. überschreiben
      /// </summary>
      public bool OutputOverwrite { get; private set; }


      FSoftUtils.CmdlineOptions cmd;


      enum MyOptions {
         Input,
         OutPath,
         Symbolname,
         Zoomlevel,
         OutputOverwrite,

         Help,
      }

      public Options() {
         Init();
         cmd = new FSoftUtils.CmdlineOptions();
         // Definition der Optionen
         cmd.DefineOption((int)MyOptions.Input, "input", "i", "DEM- or IMG-File", FSoftUtils.CmdlineOptions.OptionArgumentType.String);
         cmd.DefineOption((int)MyOptions.OutPath, "outpath", "o", "Path for GPX-Files", FSoftUtils.CmdlineOptions.OptionArgumentType.String);
         cmd.DefineOption((int)MyOptions.Symbolname, "symbol", "s", "Garmin-Symbolname for GPX-File", FSoftUtils.CmdlineOptions.OptionArgumentType.String);
         cmd.DefineOption((int)MyOptions.Zoomlevel, "zoomlevel", "z", "zoomlevels", FSoftUtils.CmdlineOptions.OptionArgumentType.UnsignedInteger, int.MaxValue);
         cmd.DefineOption((int)MyOptions.OutputOverwrite, "overwrite", "O", "overwrite existing files (without argument 'true', default 'false')", FSoftUtils.CmdlineOptions.OptionArgumentType.BooleanOrNothing);

         cmd.DefineOption((int)MyOptions.Help, "help", "?", "diese Hilfe", FSoftUtils.CmdlineOptions.OptionArgumentType.Nothing);
      }

      /// <summary>
      /// Standardwerte setzen
      /// </summary>
      void Init() {
         Input = "";
         OutPath = "";
         Symbolname = "";
         Zoomlevel = new List<int>();
         OutputOverwrite = false;
      }

      /// <summary>
      /// Auswertung der Optionen
      /// </summary>
      /// <param name="args"></param>
      public void Evaluate(string[] args) {
         if (args == null) return;
         List<string> InputArray_Tmp = new List<string>();

         try {
            cmd.Parse(args);

            foreach (MyOptions opt in Enum.GetValues(typeof(MyOptions))) {    // jede denkbare Option testen
               int optcount = cmd.OptionAssignment((int)opt);                 // Wie oft wurde diese Option verwendet?
               if (optcount > 0)
                  switch (opt) {
                     case MyOptions.Input:
                        Input = cmd.StringValue((int)opt).Trim();
                        break;

                     case MyOptions.OutPath:
                        OutPath = cmd.StringValue((int)opt).Trim();
                        break;

                     case MyOptions.Symbolname:
                        Symbolname = cmd.StringValue((int)opt).Trim();
                        break;

                     case MyOptions.Zoomlevel:
                        for (int i = 0; i < cmd.OptionAssignment((int)opt); i++)
                           Zoomlevel.Add((int)cmd.UnsignedIntegerValue((int)opt, i));
                        break;

                     case MyOptions.OutputOverwrite:
                        if (cmd.ArgIsUsed((int)opt))
                           OutputOverwrite = cmd.BooleanValue((int)opt);
                        else
                           OutputOverwrite = true;
                        break;

                     case MyOptions.Help:
                        ShowHelp();
                        break;

                  }
            }

            //TestParameter = new string[cmd.Parameters.Count];
            //cmd.Parameters.CopyTo(TestParameter);

            if (string.IsNullOrEmpty(Symbolname))
               Symbolname = "Waypoint";

            if (string.IsNullOrEmpty(OutPath))
               OutPath = ".\\gpx";

            if (Zoomlevel.Count == 0)
               Zoomlevel.Add(0);

            if (cmd.Parameters.Count > 0)
               throw new Exception("Es sind keine Argumente sondern nur Optionen erlaubt.");

         } catch (Exception ex) {
            Console.Error.WriteLine(ex.Message);
            ShowHelp();
            throw new Exception("Fehler beim Ermitteln oder Anwenden der Programmoptionen.");
         }
      }

      /// <summary>
      /// Hilfetext für Optionen ausgeben
      /// </summary>
      /// <param name="cmd"></param>
      public void ShowHelp() {
         List<string> help = cmd.GetHelpText();
         for (int i = 0; i < help.Count; i++) Console.Error.WriteLine(help[i]);
         Console.Error.WriteLine();
         Console.Error.WriteLine("Zusatzinfos:");


         Console.Error.WriteLine("Für '--' darf auch '/' stehen und für '=' auch ':' oder Leerzeichen.");
         Console.Error.WriteLine("Argumente mit ';' werden an diesen Stellen in Einzelargumente aufgetrennt.");

         // ...

      }


   }
}

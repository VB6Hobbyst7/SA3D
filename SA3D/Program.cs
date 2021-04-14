using SATools.SAModel.Graphics;
using SATools.SAModel.Graphics.OpenGL;
using SATools.SAModel.ObjData.Animation;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SATools.SA3D
{
    public class Program
    {
        [DllImport("Kernel32.dll")]
        public static extern bool AttachConsole(int processId);

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        [STAThread]
        public static void Main(string[] args)
        {
            AttachConsole(-1);
            Run(args);
            FreeConsole();
        }

        private static void Run(string[] args)
        {
            // when running from cmd, attach to the cmd console

            string path = "";

            if(args.Length > 0)
            {
                if(args[0].StartsWith("-"))
                    args[0] = "?";
                switch(args[0])
                {
                    case "?":
                        string output = "";

                        output += "\nSA3D Standalone @X-Hax\n";
                        output += "  Usage: [filepath] [options]\n\n";

                        output += "   filepath\n";
                        output += "       Path to a sonic adventure level or model file that should be opened\n\n";

                        output += "  Options:\n";
                        output += "   -h --help           Help \n\n";

                        output += "   -mtn --motion\n";
                        output += "       Loads a motion file and attaches it to the loaded model.\n\n\n";

                        output += "   -st  --standlone\n";
                        output += "       Starts SA3D as a standalone window (only used for model inspection).\n\n";

                        output += "   -res --resolution   [Width]x[Height]\n";
                        output += "       Used to start the standalone with specific dimensions.\n\n";

                        Console.WriteLine(output);
                        return;
                    default:
                        path = Path.Combine(Environment.CurrentDirectory, args[0]);
                        if(!File.Exists(path))
                        {
                            Console.WriteLine("Path does not lead to a file! enter --help for more info");
                            return;
                        }
                        break;
                }
            }

            DebugContext context = new(default, new GLAPIAccessObject());
            string motionPath = null;
            bool standalone = false;
            int width = 1280;
            int height = 720;

            for(int i = 1; i < args.Length; i++)
            {
                switch(args[i].ToLower())
                {
                    case "-res":
                    case "--resolution":
                        i++;
                        string[] res = args[i].Split('x');
                        if(!int.TryParse(res[0], out width) || !int.TryParse(res[1], out height))
                        {
                            Console.WriteLine("Resolution not valid:\n -res [WIDTH]x[HEIGHT]\n  example: 1280x720");
                            return;
                        }
                        break;
                    case "-st":
                    case "--standalone":
                        standalone = true;
                        break;
                    case "-mtn":
                    case "--motion":
                        i++;
                        motionPath = args[i];

                        motionPath = Path.Combine(Environment.CurrentDirectory, motionPath);
                        if(!File.Exists(path))
                        {
                            Console.WriteLine("Motion filepath does not lead to a file!");
                            return;
                        }
                        break;
                }
            }

            if(path != null)
            {
                string ext = Path.GetExtension(path);
                if(ext.EndsWith("lvl"))
                {
                    var ltbl = SAModel.ObjData.LandTable.ReadFile(path);
                    context.Scene.LoadLandtable(ltbl);
                }
                else if(ext.EndsWith("mdl") || ext.EndsWith("nj"))
                {
                    var file = SAModel.ObjData.ModelFile.Read(path);
                    if(motionPath == null)
                        context.Scene.LoadModelFile(file);
                    else
                        context.Scene.LoadModelFile(file, Motion.ReadFile(motionPath, file.Model.CountAnimated()), 30);
                }
                else
                {
                    Console.WriteLine($"Not a valid file format: {ext}");
                }
            }

            if(standalone)
            {
                if(width > 0 && height > 0)
                    context.Resolution = new System.Drawing.Size(width, height);

                AppDomain.CurrentDomain.UnhandledException +=
                    new((o, e) => SAWPF.ErrorDialog.UnhandledException((Exception)e.ExceptionObject));

                context.AsWindow();
            }
            else
            {
                XAML.App app = new(context);
                app.DispatcherUnhandledException += (o, e) =>
                {
                    SAWPF.ErrorDialog.UnhandledException(e.Exception);
                    e.Handled = true;
                };
                app.InitializeComponent();
                app.Run();

            }

        }
    }
}

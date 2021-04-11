using SATools.SAModel.Graphics;
using SATools.SAModel.Graphics.OpenGL;
using SATools.SAModel.ObjData.Animation;
using System;
using System.IO;

namespace SATools.SA3D
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if(args.Length == 0)
            {
                var app = new XAML.App();
                app.InitializeComponent();
                app.Run();
                return;
            }

            string path = "";

            switch(args[0])
            {
                case "--help":
                case "-h":
                case "-help":
                case "help":
                case "-?":
                case "?":
                    string output = "";

                    output += "SA3D Standalone @Justin113D";
                    output += "  Usage: SA3D [path] [options]\n\n";
                    output += "  Options:\n";
                    output += "   -h --help           Help \n\n";
                    output += "   -res --Resolution   [Width]x[Height]";

                    Console.WriteLine(output);
                    return;
                case "-test":
                    break;
                default:
                    path = Path.Combine(Environment.CurrentDirectory, args[0]);
                    if(!File.Exists(path))
                        goto case "?";
                    break;
            }

            int width = 1280;
            int height = 720;
            Motion motion = null;

            for(int i = 1; i < args.Length; i += 2)
            {
                switch(args[i])
                {
                    case "-res":
                    case "--Resolution":
                        string[] res = args[i + 1].Split('x');
                        if(!int.TryParse(res[0], out width) || !int.TryParse(res[1], out height))
                        {
                            Console.WriteLine("Resolution not valid: [WIDTH]x[HEIGHT]  example: 1280x720");
                            return;
                        }
                        break;
                    case "-mtn":
                    case "--Motion":
                        motion = Motion.ReadFile(args[i + 1]);
                        break;
                }
            }

            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, width, height);
            DebugContext context = new DebugContext(rect, new GLAPIAccessObject());
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
                    //file.Animations[102].WriteFile("C:\\Users\\Justin113D\\Downloads\\test.saanim");
                    if(motion == null)
                        context.Scene.LoadModelFile(file);
                    else
                        context.Scene.LoadModelFile(file, motion, 60);

                }
                else
                {
                    Console.WriteLine($"Not a valid file format: {ext}");
                }
            }

            context.AsWindow();
        }
    }
}

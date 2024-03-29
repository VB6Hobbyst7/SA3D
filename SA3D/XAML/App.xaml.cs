﻿using SATools.SAModel.Graphics;
using System.Windows;

namespace SATools.SA3D.XAML
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static DebugContext CurrentContext { get; private set; }

        public App(DebugContext context)
        {
            CurrentContext = context;
            InitializeComponent();
            MainWindow = new WndMain();
            MainWindow.Show();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Example.Consumer.Wpf.ViewModels;

namespace Example.Consumer.Wpf {
	public partial class App : Application {
		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);

			MainWindow = new MainWindow {
				DataContext = new CatViewModel()
			};

			MainWindow.Show();
		}
	}
}

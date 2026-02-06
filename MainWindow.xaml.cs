using RisingCitiesOffline.Resources.Local.Scripts;
using System;
using System.Windows;

namespace RisingCitiesOffline
{
    public partial class MainWindow : Window
    {
        private readonly HttpServer _httpServer = new HttpServer();
        private readonly SocketServer _socketServer = new SocketServer();

        public MainWindow()
        {
            InitializeComponent();

            // Suscribimos los logs del servidor TCP a la UI
            _socketServer.Log += msg =>
            {
                Dispatcher.Invoke(() =>
                {
                    LogTextBox.AppendText(msg + "\n");
                    LogTextBox.ScrollToEnd();
                });
            };
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _httpServer.Start();
                _socketServer.Start(8080);

                AppendLog("Servidores iniciados correctamente");
            }
            catch (Exception ex)
            {
                AppendLog("Error al iniciar servidores: " + ex.Message);
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _socketServer.Stop();
                _httpServer.Stop();

                AppendLog("Servidores detenidos");
            }
            catch (Exception ex)
            {
                AppendLog("Error al detener servidores: " + ex.Message);
            }
        }

        private void AppendLog(string msg)
        {
            LogTextBox.AppendText(msg + "\n");
            LogTextBox.ScrollToEnd();
        }
    }
}

using FlixTubes.Models;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FlixTubes.UI
{
    public partial class DetalhesPage : System.Windows.Controls.UserControl
    {
        public event EventHandler? FecharHandler;

        public BoxArquivoView? _boxFilmeSelecionado;

        public DetalhesPage()
        {
            InitializeComponent();

            // Injeta um script JavaScript para detectar quando a tela final é exibida, removida e enviar uma mensagem
            webView2.WebMessageReceived += (sender, args) =>
            {
                // Verifica se a mensagem recebida é sobre a tela final sendo removida
                if (args.TryGetWebMessageAsString() == "videoEnded")
                {
                    grdTrailer.Visibility = Visibility.Collapsed;
                }
            };
        }

        public void CarregarDados()
        {
            if (_boxFilmeSelecionado == null || _boxFilmeSelecionado.FileInfo == null)
            {
                btnFechar_MouseLeftButtonDown(null, null);// fecha tudo
                return;
            }

            string dirArqFilme = _boxFilmeSelecionado.FileInfo.DirectoryName ?? "";
            string NomeArqFilme = System.IO.Path.GetFileNameWithoutExtension(_boxFilmeSelecionado.FileInfo.Name);

            InfosFilme? infosFilme = InfosServicos.ReadFromJsonFile(dirArqFilme, NomeArqFilme) ?? new InfosFilme();

            txtTitulo.Text = infosFilme.Nome ?? NomeArqFilme;
            txtSinopse.Text = infosFilme.Sinopse;

            //Carrega a imagem
            FileInfo fileInfo = new FileInfo(System.IO.Path.Combine(dirArqFilme, NomeArqFilme + ".jpg")); //Mudar!
            if (fileInfo.Exists)
            {
                // Criar a URI da imagem com o identificador único
                string uriImagem = "file:///" + fileInfo.FullName.Replace("\\", "/") + "?" + DateTime.Now.Ticks;
                CarregarImagemNoGrid(uriImagem);
            }
            else
            {
                CarregarImagemNoGrid("pack://application:,,,/Resources/box/capa.jpg");
            }

            CarregarPlayerDoYouTubeAsync(infosFilme.IDVideoYoutube);
        }

        private async void CarregarPlayerDoYouTubeAsync(string? videoId)
        {
            if (string.IsNullOrEmpty(videoId)) return;

            // Inicializa o WebView2
            await webView2.EnsureCoreWebView2Async();

            // Limpa os cookies
            webView2.CoreWebView2.CookieManager.DeleteAllCookies();

            // URL da página de incorporação do YouTube com o ID do vídeo
            string embedUrl = $"https://www.youtube.com/embed/{videoId}?autoplay=1&loop=1&rel=0&showinfo=0";

            // Carrega a página de incorporação do YouTube no WebView2
            webView2.CoreWebView2.Navigate(embedUrl);

            await Task.Run(() =>
            {
                Thread.Sleep(1000);

                Dispatcher.Invoke(async () =>
                 {

                     // Injeta um script JavaScript para acionar a reprodução automática
                     await webView2.CoreWebView2.ExecuteScriptAsync("document.querySelector('video').play();");

                     // Injeta um script JavaScript para detectar quando o vídeo termina e enviar uma mensagem
                     await webView2.CoreWebView2.ExecuteScriptAsync(@"
                        document.querySelector('video').addEventListener('ended', function() {
                        window.chrome.webview.postMessage('videoEnded');
                        });
                    ");


                     // Injeta um script JavaScript para ocultar os controles do player
                     await webView2.CoreWebView2.ExecuteScriptAsync(@"
        var style = document.createElement('style');
        style.textContent = '.ytp-chrome-top, .ytp-chrome-bottom { display: none !important; }';
        document.head.appendChild(style);
    ");

                     // Injeta um script JavaScript para ocultar o elemento ytp-pause-overlay-container
                     await webView2.CoreWebView2.ExecuteScriptAsync(@"
            var pauseOverlayContainer = document.querySelector('.ytp-pause-overlay-container');
            if (pauseOverlayContainer) {
                pauseOverlayContainer.style.display = 'none';
            }
        ");


                     grdTrailer.Visibility = Visibility.Visible;
                 });


            });
        }

        private void CarregarImagemNoGrid(string dirImagem)
        {
            // Carregar imagem e atribuir ao controle Image
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(dirImagem);
            bitmap.EndInit();
            grdImagem.Background = new ImageBrush(bitmap) { Stretch = Stretch.UniformToFill };
        }

        private async void btnFechar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (webView2?.CoreWebView2 != null)
            {
                // Injeta um script JavaScript para pausar o vídeo
                await webView2.CoreWebView2.ExecuteScriptAsync(@"
            var videoElement = document.querySelector('video');
            if (videoElement) {
                videoElement.pause();
            }
        ");
            }

            grdTrailer.Visibility = Visibility.Collapsed;
            _boxFilmeSelecionado = null;
            FecharHandler?.Invoke(this, e);
        }

        private async void btnPlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_boxFilmeSelecionado == null) return;

            if (webView2?.CoreWebView2 != null)
            {
                // Injeta um script JavaScript para pausar o vídeo
                await webView2.CoreWebView2.ExecuteScriptAsync(@"
            var videoElement = document.querySelector('video');
            if (videoElement) {
                videoElement.pause();
            }
        ");
            }

            _boxFilmeSelecionado.PlayFilme();
        }
    }
}

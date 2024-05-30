using FlixTubes.Helpers;
using FlixTubes.Models;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace FlixTubes.UI
{
    public partial class DetalhesPage : System.Windows.Controls.UserControl
    {
        public event EventHandler? FecharHandler;

        private BoxArquivoView? _boxFilmeSelecionado;
        private InfosFilme? _infosFilme;

        public DetalhesPage()
        {
            InitializeComponent();
        }

        public void CarregarDados(BoxArquivoView boxArquivoView)
        {
            _boxFilmeSelecionado = boxArquivoView;

            if (_boxFilmeSelecionado == null)
            {
                System.Windows.MessageBox.Show("Erro ao carregar filme!");
                btnFechar_MouseLeftButtonDown(null, null);
                return;
            }

            string dirArqFilme = _boxFilmeSelecionado.ArquivoFilme.DirectoryName ?? "";
            string NomeArqFilme = System.IO.Path.GetFileNameWithoutExtension(_boxFilmeSelecionado.ArquivoFilme.Name);
            string uriImagem = "pack://application:,,,/Resources/box/capa.jpg";

            _infosFilme = InfosServicos.ReadFromJsonFile<InfosFilme>(dirArqFilme, NomeArqFilme) ?? new InfosFilme();

            txtTitulo.Text = _infosFilme.Nome ?? NomeArqFilme;
            txtTitulo.Visibility = Visibility.Visible;
            txtSinopse.Text = _infosFilme.Sinopse;

            //Carrega a imagem
            FileInfo fileInfo = new FileInfo(System.IO.Path.Combine(dirArqFilme, NomeArqFilme + ".jpg"));
            if (fileInfo.Exists)
            {
                //Criar a URI da imagem com o identificador único
                uriImagem = "file:///" + fileInfo.FullName.Replace("\\", "/") + "?" + DateTime.Now.Ticks;
            }

            FuncoesUteis.CarregarImagemNoGrid(grdImagem, uriImagem, Stretch.UniformToFill);
            grdTrailer.Visibility = Visibility.Collapsed;
            CarregarPlayerDoYouTubeAsync();
        }

        private async void CarregarPlayerDoYouTubeAsync()
        {
            if (_infosFilme == null || string.IsNullOrEmpty(_infosFilme.IDVideoYoutube)) return;

            txtTitulo.Visibility = Visibility.Hidden; //pq tem titulo

            // Inicializa o WebView2

            await webView2.EnsureCoreWebView2Async();


            if (webView2.CoreWebView2 == null) return;

            // URL da página de incorporação do YouTube com o ID do vídeo
            string embedUrl = $"https://www.youtube.com/embed/{_infosFilme.IDVideoYoutube}?autoplay=1&loop=1&rel=0&showinfo=0";

            // Carrega a página de incorporação do YouTube no WebView2
            webView2.CoreWebView2.Navigate(embedUrl);
            webView2.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
        }

        private async void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            // Injeta um script JavaScript para detectar quando o vídeo termina e enviar uma mensagem
            await webView2.CoreWebView2.ExecuteScriptAsync(@"
              document.querySelector('video').addEventListener('ended', function() {
                    var thumbnailContainer = document.getElementById('thumbnailContainer');
                    if (thumbnailContainer) {
                           thumbnailContainer.style.display = 'block'; // Exibe a miniatura e o ícone quando o vídeo termina
                    }
                    //window.chrome.webview.postMessage('videoEnded'); //envia mensagem
              });");

            // Injeta um script JavaScript para ocultar os controles do player
            await webView2.CoreWebView2.ExecuteScriptAsync(@"
                var style = document.createElement('style');
                style.textContent = '.ytp-chrome-top, .ytp-chrome-bottom { display: none !important; }';
                document.head.appendChild(style);");

            // Injeta um script JavaScript para ocultar o elemento ytp-pause-overlay-container
            await webView2.CoreWebView2.ExecuteScriptAsync(@"
                var pauseOverlayContainer = document.querySelector('.ytp-pause-overlay-container');
                if (pauseOverlayContainer) {
                    pauseOverlayContainer.style.display = 'none';
                }");

            // Injeta um script JavaScript para ocultar o elem,ento html5-endscreen
            await webView2.CoreWebView2.ExecuteScriptAsync(@"
                var style = document.createElement('style');
                style.textContent = '.html5-endscreen { display: none !important; }';
                document.head.appendChild(style);");

            // Injeta um script JavaScript para ocultar a miniatura do vídeo
            await webView2.CoreWebView2.ExecuteScriptAsync(@"
                 var thumbnailContainer = document.createElement('div');
                 thumbnailContainer.id = 'thumbnailContainer'; // Define um ID para o contêiner da miniatura para referência posterior
                 thumbnailContainer.style.position = 'absolute'; // Define a posição relativa para o contêiner
                 thumbnailContainer.style.width = '100%'; /* Redimensiona a largura para 100% do WebView2 */
                 thumbnailContainer.style.height = '100%'; /* Mantém a proporção da altura */
                 thumbnailContainer.style.top = '0';
                 thumbnailContainer.style.left = '0';
                 document.body.appendChild(thumbnailContainer);

                 var thumbnailImage = document.createElement('img');
                 thumbnailImage.id = 'thumbnailImage'; // Define um ID para a miniatura para referência posterior
                 thumbnailImage.src = 'https://i.ytimg.com/vi/" + _infosFilme?.IDVideoYoutube + @"/maxresdefault.jpg';
                 
                 thumbnailImage.style.position = 'absolute';
                 thumbnailImage.style.top = '0';
                 thumbnailImage.style.left = '0';
                 thumbnailImage.style.width = '100%'; /* Redimensiona a largura para 100% do contêiner */
                 thumbnailImage.style.height = 'auto'; /* Mantém a proporção da altura */
                 thumbnailContainer.style.display = 'none';
                 thumbnailContainer.appendChild(thumbnailImage);

                 var playIcon = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
                 playIcon.setAttribute('xmlns', 'http://www.w3.org/2000/svg');
                 playIcon.setAttribute('xmlns:xlink', 'http://www.w3.org/1999/xlink');
                 playIcon.setAttribute('width', '64');
                 playIcon.setAttribute('height', '64');
                 playIcon.setAttribute('viewBox', '0 0 64 64');
                 playIcon.style.position = 'absolute';
                 playIcon.style.top = '50%'; // Posiciona verticalmente no meio
                 playIcon.style.left = '50%'; // Posiciona horizontalmente no meio
                 playIcon.style.transform = 'translate(-50%, -50%)'; // Centraliza o ícone

                 var playPath = document.createElementNS('http://www.w3.org/2000/svg', 'path');
                 playPath.setAttribute('d', 'M14 7v50l36-25z');
                 playPath.setAttribute('fill', 'white');
                 playIcon.appendChild(playPath);

                 playIcon.style.cursor = 'pointer'; // Altera o cursor para indicar que é clicável
                 playIcon.onclick = function() {
                     document.querySelector('video').play(); // Inicia o vídeo quando o ícone é clicado
                     thumbnailContainer.style.display = 'none'; // Oculta a miniatura e o ícone após clicar
                 };
                thumbnailContainer.appendChild(playIcon);");

            // Injeta o titulo
            await webView2.CoreWebView2.ExecuteScriptAsync(@"
                 var overlayText = document.createElement('div');
                 overlayText.innerText = '" + txtTitulo.Text + @"';
                 overlayText.style.position = 'absolute';
                 overlayText.style.top = '20px'; /* Distância do topo */
                 overlayText.style.left = '20px'; /* Distância da esquerda */
                 overlayText.style.color = 'white'; /* Cor do texto */
                 overlayText.style.textShadow = '2px 2px 2px rgba(0, 0, 0, 0.5)'; /* Sombra do texto */
                 overlayText.style.fontSize = '40px'; /* Tamanho da fonte */
                 overlayText.style.fontWeight = 'bold'; /* Peso da fonte */
                 document.body.appendChild(overlayText);");

            // Injeta um script JavaScript para acionar a reprodução automática
            await webView2.CoreWebView2.ExecuteScriptAsync(@"
                 document.querySelector('video').play(); ");

            grdTrailer.Visibility = Visibility.Visible;
        }

        private void btnFechar_MouseLeftButtonDown(object? sender, MouseButtonEventArgs? e)
        {
            if (webView2?.CoreWebView2 != null)
            {
                PararVideoYoutube();
                webView2.Dispose();
            }

            _boxFilmeSelecionado = null;
            FecharHandler?.Invoke(this, EventArgs.Empty);
        }

        private void btnPlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_boxFilmeSelecionado == null) return;

            PararVideoYoutube();

            _boxFilmeSelecionado.PlayFilme();
        }

        async void PararVideoYoutube()
        {
            if (webView2?.CoreWebView2 != null)
            {
                // Injeta um script JavaScript para pausar o vídeo
                await webView2.CoreWebView2.ExecuteScriptAsync(@"
                    var videoElement = document.querySelector('video');
                    if (videoElement) {
                        videoElement.pause();
                    }");
            }
        }
    }
}

using FlixTubes.Helpers;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace FlixTubes.UI
{
    public partial class BoxArquivoView : System.Windows.Controls.UserControl
    {
        public FileInfo ArquivoFilme = null!;
        public DirectoryInfo DiretorioSerie = null!;

        public event EventHandler? EditarHandler;
        public event EventHandler? DetalhesHandler;

        private System.Timers.Timer ClickTimer  = null!;
        private int ClickCounter;

        public BoxArquivoView()
        {
            InitializeComponent();
        }

        public BoxArquivoView(FileInfo file)
        {
            InitializeComponent();

            if (file != null)
            {
                CarregarDados(file);
            }
        }

        public BoxArquivoView(DirectoryInfo diretorioSerie)
        {
            InitializeComponent();

            if (diretorioSerie != null)
            {
                CarregarDados(diretorioSerie);

                imgBtnExcluir.Visibility = Visibility.Collapsed; //esconde o excluir pq é serie
            }
        }

        #region Filme

        public void CarregarDados(FileInfo fileInfo)
        {
            ClickTimer = new System.Timers.Timer(200);
            ClickTimer.Elapsed += new System.Timers.ElapsedEventHandler(EvaluateClicks);

            ArquivoFilme = fileInfo;
            txtNome.Text = ArquivoFilme.Name;
            ProcurarCapa(ArquivoFilme.DirectoryName, Path.GetFileNameWithoutExtension(ArquivoFilme.FullName));
        }

        private void imgBtnExcluir_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ArquivoFilme == null) return;

            var resposta = System.Windows.MessageBox.Show($"Deseja realmente excluir é irreversivel!! \r\n {ArquivoFilme.Name}", "Atenção", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (resposta == MessageBoxResult.Yes)
            {
                ArquivoFilme.Delete();
                System.Windows.MessageBox.Show($"Deletado com Sucesso!", "Sucesso", MessageBoxButton.OK);

                // Obtenha o pai deste UserControl
                var parent = this.Parent as System.Windows.Controls.Panel;

                // Verifique se o pai é um Panel válido
                if (parent != null)
                {
                    // Remova este UserControl do pai
                    parent.Children.Remove(this);
                }
            }
        }

        public void PlayFilme()
        {
            if (ArquivoFilme == null) return;

            // Abrir o arquivo com o programa associado
            Process.Start(new ProcessStartInfo
            {
                FileName = ArquivoFilme.FullName,
                UseShellExecute = true,
            });
        }

        private void EvaluateClicks(object? sender, System.Timers.ElapsedEventArgs e)
        {
            ClickTimer?.Stop();
            // Evaluate ClickCounter here
            if (ClickCounter >= 2)
            {
                //Abre o filme
                PlayFilme();
            }
            else
            {
                //Abre o detalhes
                DetalhesHandler?.Invoke(this, e);
            }
            ClickCounter = 0;
        }

        #endregion

        #region Serie

        public void CarregarDados(DirectoryInfo fileInfo)
        {
            DiretorioSerie = fileInfo;
            txtNome.Text = DiretorioSerie.Name;

            ProcurarCapa(DiretorioSerie.FullName, DiretorioSerie.Name);
        }

        #endregion

        public void ProcurarCapa(string? diretorio, string? nome)
        {
            if (string.IsNullOrEmpty(diretorio) || string.IsNullOrEmpty(nome)) return;
            // Suponha que fileInfo seja seu objeto FileInfo

            string caminhoImagem = System.IO.Path.Combine(diretorio, nome + ".jpg");

            // Verifique se o arquivo existe
            if (File.Exists(caminhoImagem))
            {
                // Criar a URI da imagem com o identificador único
                string uriImagem = "file:///" + caminhoImagem.Replace("\\", "/") + "?" + DateTime.Now.Ticks;
                FuncoesUteis.CarregarImagemNoGrid(grid, uriImagem, Stretch.UniformToFill);
            }
        }

        private void grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Se nao tiver clickTime é pq é serie e tem q mostrar o detalhes
            if (ClickTimer == null)
            {
                DetalhesHandler?.Invoke(this, e);
                return;
            }
            //Se tiver é pq é filme e ai conta os clicks
            else
            {
                ClickTimer.Stop();
                ClickCounter++;
                ClickTimer.Start();
            }
        }

        private void imgBtnEditar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            EditarHandler?.Invoke(this, e);
        }
    }
}
